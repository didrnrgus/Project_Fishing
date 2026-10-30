"""캐릭터 쇼케이스 스크린샷(sc{번호}_{파일}_{동작}.png)을 캐릭터마다 한 장(sheet_*.png)과 전체 정면 한 장(sheet_all.png)으로 모은다.
python tools/showcase_sheet.py Screenshots/showcase   (tools/showcase.sh가 끝에 부른다)
"""
import glob
import os
import re
import sys

from PIL import Image, ImageDraw, ImageFont

ORDER = ["1_front", "2_walk", "3_run", "4_windup", "5_swing", "6_cast", "7_reel", "8_fight", "9_catch", "r_cheer", "r_excited", "r_scared"]
LABEL = {"1_front": "서 있기", "2_walk": "걷기", "3_run": "달리기", "4_windup": "캐스팅 젖힘", "5_swing": "휘두르기", "6_cast": "던짐",
         "7_reel": "감기", "8_fight": "파이팅", "9_catch": "고기 들기", "r_cheer": "환호", "r_excited": "신남", "r_scared": "놀람"}
W, H, COLS = 480, 270, 4


def font(size, bold=False):
    try:
        return ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf" if bold else "C:/Windows/Fonts/malgun.ttf", size)
    except OSError:
        return ImageFont.load_default()


def tile(path, label):
    im = Image.open(path).convert("RGB").resize((W, H), Image.LANCZOS)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W, 26], fill=(0, 0, 0))
    d.text((8, 2), label, font=font(16, True), fill=(255, 230, 90))
    return im


def main(folder):
    shots = {}
    for p in glob.glob(os.path.join(folder, "sc*.png")):
        m = re.match(r"(sc\d+_.+?)_(\d_\w+|r_\w+)\.png$", os.path.basename(p))
        if m:
            shots.setdefault(m.group(1), {})[m.group(2)] = p
    fronts = []
    for who in sorted(shots):
        have = [k for k in ORDER if k in shots[who]]
        rows = (len(have) + COLS - 1) // COLS
        sheet = Image.new("RGB", (W * COLS, H * rows + 40), (20, 22, 28))
        ImageDraw.Draw(sheet).text((10, 6), who, font=font(22, True), fill=(255, 255, 255))
        for i, k in enumerate(have):
            sheet.paste(tile(shots[who][k], LABEL[k]), ((i % COLS) * W, 40 + (i // COLS) * H))
        sheet.save(os.path.join(folder, f"sheet_{who}.png"))
        if "1_front" in shots[who]:
            fronts.append(tile(shots[who]["1_front"], who))
    if fronts:
        cols = 5
        rows = (len(fronts) + cols - 1) // cols
        sheet = Image.new("RGB", (W * cols, H * rows), (20, 22, 28))
        for i, t in enumerate(fronts):
            sheet.paste(t, ((i % cols) * W, (i // cols) * H))
        sheet.save(os.path.join(folder, "sheet_all.png"))
    print(f"시트 {len(shots)}명 → {folder}")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Screenshots/showcase")
