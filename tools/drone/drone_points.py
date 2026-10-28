"""이미지 -> 드론 N대 좌표 변환 (범용, 배경 자동 인식)

사용법:
    pip install pillow numpy scikit-learn scipy
    python drone_points.py input.png --n 3000 --out points.json
    python drone_points.py input.png --preview preview.png   # 배경 제거 결과 확인

출력: [[x, y, r, g, b], ...]  (x, y는 0~1로 정규화, y는 아래쪽이 +)
배경 픽셀은 드론 대상에서 제외되며(= 검정/소등), 피사체에만 드론을 배치합니다.
"""
import argparse
import json

import numpy as np
from PIL import Image
from scipy import ndimage
from sklearn.cluster import KMeans


def detect_background(arr, tol=45, border=3, palette_k=4, seed=0):
    """이미지 가장자리 색을 기준으로 배경 마스크(True=배경)를 자동 생성."""
    h, w, _ = arr.shape
    b = min(border, h // 2, w // 2)

    # 1) 가장자리 픽셀로 배경 팔레트 추정 (단색/그라데이션 배경 대응)
    edge = np.concatenate([
        arr[:b].reshape(-1, 3),
        arr[-b:].reshape(-1, 3),
        arr[:, :b].reshape(-1, 3),
        arr[:, -b:].reshape(-1, 3),
    ]).astype(float)
    k = min(palette_k, len(np.unique(edge, axis=0)))
    palette = KMeans(n_clusters=k, n_init=3, random_state=seed).fit(edge).cluster_centers_

    # 2) 팔레트 색과 가까운 픽셀 = 배경 후보
    flat = arr.reshape(-1, 3).astype(float)
    dist = np.min(np.linalg.norm(flat[:, None, :] - palette[None, :, :], axis=2), axis=1)
    candidate = (dist < tol).reshape(h, w)

    # 3) 가장자리와 연결된 후보만 진짜 배경으로 인정
    #    (피사체 내부에 배경과 비슷한 색이 있어도 구멍으로 오인하지 않음)
    labels, _ = ndimage.label(candidate)
    edge_labels = np.unique(np.concatenate([
        labels[0], labels[-1], labels[:, 0], labels[:, -1]
    ]))
    edge_labels = edge_labels[edge_labels != 0]
    bg = np.isin(labels, edge_labels)

    # 4) 경계의 잔여 노이즈 정리
    bg = ndimage.binary_opening(bg, iterations=1) | bg
    return bg


def extract_points(path, n=3000, tol=45, max_side=400, seed=0, preview=None,
                   dark=30, edge_boost=0.0):
    img = Image.open(path).convert("RGB")
    img.thumbnail((max_side, max_side))
    arr = np.asarray(img)
    h, w, _ = arr.shape

    bg = detect_background(arr, tol=tol, seed=seed)

    # 피사체 안쪽의 검은 픽셀도 어차피 LED를 꺼야 하므로 드론 대상에서 제외
    # -> 절약된 드론이 밝은 부분에 더 촘촘하게 배치되어 해상도가 올라감
    off = bg | (arr.max(axis=2) <= dark)
    fg = ~off

    if preview:  # 소등(드론 미배치) 영역을 검정으로 칠한 결과 저장
        out = arr.copy()
        out[off] = 0
        Image.fromarray(out).save(preview)

    # 윤곽선/디테일이 많은 곳에 가중치를 더 줘서 드론을 촘촘하게 (edge_boost=0이면 균등)
    gray = arr.mean(axis=2)
    grad = np.hypot(ndimage.sobel(gray, axis=0), ndimage.sobel(gray, axis=1))
    grad = grad / (grad.max() + 1e-9)
    weight_map = 1.0 + edge_boost * grad

    ys, xs = np.nonzero(fg)
    coords = np.column_stack([xs, ys]).astype(float)
    if len(coords) < n:
        raise ValueError(
            f"피사체 픽셀({len(coords)})이 드론 수({n})보다 적습니다. "
            "--tol을 낮추거나 --max-side를 키우세요."
        )

    # 피사체 영역을 n개 점으로 균등 분할 (중심 = 드론 위치)
    weights = weight_map[ys, xs]
    km = KMeans(n_clusters=n, n_init=1, random_state=seed).fit(coords, sample_weight=weights)
    centers = km.cluster_centers_

    # 각 드론 색 = 소속 클러스터 픽셀 평균색
    colors = np.zeros((n, 3))
    for k in range(n):
        m = coords[km.labels_ == k].astype(int)
        colors[k] = arr[m[:, 1], m[:, 0]].mean(axis=0)

    scale = max(w, h)
    return [
        [round(cx / scale, 4), round(cy / scale, 4), *map(int, c)]
        for (cx, cy), c in zip(centers, colors)
    ]


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("image")
    ap.add_argument("--n", type=int, default=3000, help="드론 수")
    ap.add_argument("--tol", type=int, default=45, help="배경 색 허용 오차 (클수록 배경을 넓게 제거)")
    ap.add_argument("--max-side", type=int, default=400, help="처리용 최대 해상도(긴 변)")
    ap.add_argument("--out", default="points.json")
    ap.add_argument("--preview", default=None, help="소등 영역 제거 결과 미리보기 이미지 경로")
    ap.add_argument("--dark", type=int, default=30,
                    help="이 밝기 이하의 어두운 픽셀은 소등으로 보고 드론 제외 (0이면 비활성)")
    ap.add_argument("--edge-boost", type=float, default=0.0,
                    help="윤곽선/디테일 부분 드론 밀도 가중치 (0=균등, 2~4 권장)")
    args = ap.parse_args()

    pts = extract_points(args.image, args.n, args.tol, args.max_side,
                         preview=args.preview, dark=args.dark, edge_boost=args.edge_boost)
    with open(args.out, "w") as f:
        json.dump(pts, f)
    print(f"{len(pts)}개 점 저장 -> {args.out}")