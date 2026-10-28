"""게임 타이틀 「청풍명월」 드론쇼 장면 -> Assets/StreamingAssets/DroneShow/title.json (게임 폴더의 첫 장면, 빌드한 뒤에도 파일로 바꾼다).

  python tools/drone/title_scene.py              # 인트로 타이틀 그림(tools/intro/build_intro_art.py 결과)으로 다시 뽑는다
  python tools/drone/title_scene.py --dots a.png # 점을 찍어 본 미리보기도

타이틀 화면처럼 붓글씨(제주한라산체, OFL), 그 밑에 금빛 영문 (붉은 낙관 「짭」은 사용자 요청으로 뺀다) — 검정 바탕에 놓고 drone_points.py 로 3,000점.
빛 번짐(title_glow)과 한 줄 문구는 넣지 않는다 (드론 점으로는 뭉개진다). 우리 그림이라 저장소에 넣는다.
"""
import argparse
import json
import os
import sys
import tempfile

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
INTRO = os.path.join(ROOT, "Assets", "Resources", "Intro")
OUT = os.path.join(ROOT, "Assets", "StreamingAssets", "DroneShow", "title.json")
sys.path.insert(0, HERE)
import drone_points  # noqa: E402

sys.stdout.reconfigure(encoding="utf-8")


def compose():
    """IntroSequence 타이틀 배치(build_intro_art.compose_preview)를 검정 바탕에 (낙관 빼고)."""
    t = Image.open(os.path.join(INTRO, "title.png"))
    tw = 900
    t = t.resize((tw, int(t.size[1] * tw / t.size[0])), Image.LANCZOS)
    en = Image.open(os.path.join(INTRO, "title_en.png"))
    en = en.resize((int(en.size[0] * 0.62), int(en.size[1] * 0.62)), Image.LANCZOS)
    canvas = Image.new("RGBA", (tw + 120, t.size[1] + en.size[1] + 40), (0, 0, 0, 255))
    canvas.alpha_composite(t, (60, 0))
    canvas.alpha_composite(en, ((canvas.size[0] - en.size[0]) // 2, t.size[1] - 40))
    path = os.path.join(tempfile.gettempdir(), "drone_title_src.png")
    canvas.convert("RGB").save(path)
    return path


def dots(pts, path, size=900):
    img = Image.new("RGB", (size, size), (0, 0, 0))
    d = ImageDraw.Draw(img)
    for x, y, r, g, b in pts:
        d.ellipse((x * size - 2, y * size - 2, x * size + 2, y * size + 2), fill=(r, g, b))
    img.save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dots", help="점 미리보기 그림 경로")
    args = ap.parse_args()
    src = compose()
    # 붓글씨 획이 가늘어 윤곽에 조금 더 모이게
    pts = drone_points.extract_points(src, 3000, tol=45, max_side=800, dark=40, edge_boost=1.5)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(pts, f)
    print(f"{len(pts)}점 -> {OUT}")
    if args.dots:
        dots(pts, args.dots)
        print("점 미리보기:", args.dots)


if __name__ == "__main__":
    main()
