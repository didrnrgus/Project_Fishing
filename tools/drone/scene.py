"""그림 한 장 -> 탄천 드론쇼 장면 하나 (내 문서/짭풍명월/DroneShow/*.json, 게임이 이름 순서로 읽는다).

  python tools/drone/scene.py 그림.png                # 다음 번호(points_4.json …)로 추가
  python tools/drone/scene.py --pasted                # Claude Code 대화에 붙여 넣은 마지막 이미지
  python tools/drone/scene.py --pasted 2 --name points_2   # 끝에서 둘째 이미지로 2번 장면을 바꾼다
  python tools/drone/scene.py 그림.png --tol 70 --edge-boost 3

점 뽑기는 drone_points.py (사용자 스크립트: 배경 자동 인식 + K-means 3,000점). 미리보기(소등 = 검정)를
%TEMP%/drone_preview_<이름>.png 로 같이 저장하니 배경이 제대로 빠졌는지 보고 --tol 을 고친다.
그림·점 파일은 저작권이 그림 주인에게 있어 저장소에 넣지 않는다 (내 문서에만 둔다). 게임은 맵을 열 때 다시 읽으니 빌드는 필요 없다.
"""
import argparse
import base64
import ctypes
import glob
import json
import os
import re
import sys
import tempfile

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import drone_points  # noqa: E402

sys.stdout.reconfigure(encoding="utf-8")


def documents():
    """Environment.SpecialFolder.MyDocuments 와 같은 곳 (OneDrive 로 옮겨진 내 문서도)."""
    try:
        buf = ctypes.create_unicode_buffer(260)
        if ctypes.windll.shell32.SHGetFolderPathW(None, 5, None, 0, buf) == 0:
            return buf.value
    except (AttributeError, OSError):
        pass
    return os.path.join(os.path.expanduser("~"), "Documents")


def show_folder():
    return os.path.join(documents(), "짭풍명월", "DroneShow")


def pasted_image(k=1, transcript=None):
    """Claude Code 대화 기록(~/.claude/projects/*/*.jsonl, 가장 최근 것)에서 사용자가 붙여 넣은 이미지 중 끝에서 k번째를 임시 파일로."""
    if transcript is None:
        logs = glob.glob(os.path.join(os.path.expanduser("~"), ".claude", "projects", "*", "*.jsonl"))
        if not logs:
            raise SystemExit("대화 기록을 찾지 못했습니다 (~/.claude/projects)")
        transcript = max(logs, key=os.path.getmtime)
    images = []
    for line in open(transcript, encoding="utf-8"):
        try:
            d = json.loads(line)
        except ValueError:
            continue
        if d.get("type") != "user":
            continue
        content = (d.get("message") or {}).get("content")
        if not isinstance(content, list):
            continue
        for b in content:   # 맨 위 image 블록만 = 붙여 넣은 이미지 (도구 결과 안의 스크린샷은 tool_result 안에 있다)
            if isinstance(b, dict) and b.get("type") == "image" and (b.get("source") or {}).get("type") == "base64":
                images.append(b["source"])
    if len(images) < k:
        raise SystemExit(f"붙여 넣은 이미지가 {len(images)}장뿐입니다 ({transcript})")
    src = images[-k]
    ext = {"image/png": ".png", "image/jpeg": ".jpg", "image/webp": ".webp", "image/gif": ".gif"}.get(src.get("media_type"), ".png")
    path = os.path.join(tempfile.gettempdir(), f"drone_pasted_{k}{ext}")
    with open(path, "wb") as f:
        f.write(base64.b64decode(src["data"]))
    return path


def enlarged(src, side):
    """작은 그림(스크린샷 조각 등)은 drone_points 가 줄이기만 하므로 긴 변을 side 까지 키운 사본을 쓴다."""
    img = Image.open(src).convert("RGB")
    if max(img.size) >= side:
        return src
    k = side / max(img.size)
    path = os.path.join(tempfile.gettempdir(), f"drone_enlarged_{side}.png")
    img.resize((round(img.width * k), round(img.height * k)), Image.LANCZOS).save(path)
    return path


def next_name(folder):
    nums = [int(m.group(1)) for f in os.listdir(folder) if (m := re.fullmatch(r"points_(\d+)\.json", f))]
    return f"points_{max(nums, default=0) + 1}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("image", nargs="?", help="그림 경로 (--pasted 와 둘 중 하나)")
    ap.add_argument("--pasted", type=int, nargs="?", const=1, help="붙여 넣은 이미지 중 끝에서 몇 번째 (기본 1 = 마지막)")
    ap.add_argument("--transcript", help="대화 기록 파일 (기본: 가장 최근 것)")
    ap.add_argument("--name", help="장면 파일 이름 (기본: 다음 번호 points_N). 같은 이름이면 덮어쓴다")
    ap.add_argument("--n", type=int, default=3000)
    ap.add_argument("--tol", type=int, default=45)
    ap.add_argument("--dark", type=int, default=30)
    ap.add_argument("--edge-boost", type=float, default=0.0)
    ap.add_argument("--max-side", type=int, default=400)
    args = ap.parse_args()
    if (args.image is None) == (args.pasted is None):
        ap.error("그림 경로나 --pasted 중 하나만")

    src = args.image or pasted_image(args.pasted, args.transcript)
    folder = show_folder()
    os.makedirs(folder, exist_ok=True)
    name = (args.name or next_name(folder)).removesuffix(".json")
    preview = os.path.join(tempfile.gettempdir(), f"drone_preview_{name}.png")

    # 피사체가 작아 픽셀이 드론 수보다 적으면 해상도를 올려 다시
    for side in (args.max_side, 600, 800, 1200):
        if side < args.max_side:
            continue
        try:
            pts = drone_points.extract_points(enlarged(src, side), args.n, args.tol, side, preview=preview,
                                              dark=args.dark, edge_boost=args.edge_boost)
            break
        except ValueError as e:
            print(f"max-side {side}: {e}")
    else:
        raise SystemExit("점을 뽑지 못했습니다: --tol 을 낮춰 보세요")

    out = os.path.join(folder, name + ".json")
    with open(out, "w") as f:
        json.dump(pts, f)
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    print(f"그림: {src}")
    print(f"{len(pts)}점 -> {out}")
    print(f"범위 x {min(xs):.3f}~{max(xs):.3f}, y {min(ys):.3f}~{max(ys):.3f} (가로:세로 = {(max(xs) - min(xs)) / max(1e-6, max(ys) - min(ys)):.2f})")
    print(f"미리보기(검정 = 소등): {preview}")
    order = sorted((f for f in os.listdir(folder) if f.endswith(".json")), key=lambda f: re.sub(r"\d+", lambda m: m.group().zfill(8), f))
    print("장면 순서 (게임과 같이 숫자는 수 크기로):", ", ".join(order))


if __name__ == "__main__":
    main()
