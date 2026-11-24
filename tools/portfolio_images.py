"""포트폴리오 그림 만들기: 고른 스크린샷 → docs/portfolio/images/*.jpg (노션 올리기 좋게 JPG 품질 88, 긴 변 1600px).

찾는 순서: --from <폴더>(여러 번 가능) → Screenshots/ → docs/images/.
    python tools/portfolio_images.py                      # 그림 만들기
    python tools/portfolio_images.py --from <폴더> --zip  # + docs/portfolio/notion_import.zip (PORTFOLIO.md + images/)
드론쇼 장면(72l·72m·72n)은 docs/images에 없다 → sh tools/autoshot.sh --maps tancheon 으로 찍고 그 폴더를 --from으로.
72m_drone_1 이후 장면은 내 문서 폴더의 개인 그림일 수 있어 쓰지 않는다.
"""
import os
import sys
import zipfile

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "portfolio", "images")

# 포트폴리오 그림 이름 → 원본 스크린샷 이름
PICKS = {
    "01_title": "72m_drone_0",  # 드론쇼 「청풍명월」 장면 (타이틀 화면은 "짭풍명월" 로고라 쓰지 않는다)
    "02_lobby": "00a_lobby",
    "03_area_list": "53_area_list_all",
    "04_casting": "02_casting",
    "05_bite": "04_bite_gauge",
    "06_fight": "06_fight",
    "07_counter": "07_counter_chance",
    "08_result": "10_result",
    "09_rod_slack": "03d_rod_low_slack",
    "11_tension_danger": "06b_tension_danger",
    "12_landing": "09_landing",
    "13_fish_school": "01b_fish_school",
    "14_fish_shadow": "01c_fish_shadow",
    "15_rod_arms": "02v_vrm_155",
    # 낚시터: 맵마다 가장 멋진 한 장만 (경치·모델링 자랑 사진은 넣지 않는다 — 2026-11-25 사용자)
    "17_seocheon": "36b_seocheon_sunset",
    "18_halong": "55f_halong_quay",
    "19_santorini": "56_santorini_cliff",
    "20_jeju": "54f_jeju_yongduam",
    "23_samcheok": "45d_samcheok_corner_night",
    "27_fish_models": "32_fish_models",
    "29_cephalopods": "32b_cephalopods",
    "31_item_icons": "33_item_icons",
    "34_character_model": "21e_character_model",
    "35_outfit": "00a2_outfit_random",
    "37_buddy_answer": "30e_buddy_answer",
    "38_buddy_lead": "30g_buddy_lead",
    "39_buddy_point": "30i_buddy_point",
    "41_drone_show": "72l_tancheon_drones",
    "42_drone_transition": "72n_drone_transition",
    "43_esc_menu": "22a_esc_menu",
    "44_result_rewards": "10f_result_rewards",
    "47_warning_retry": "10g_warning_retry",
    "48_boat_summon": "18a_boat_summon",
    "49_winter_ice": "41_winter_ice",
    "50_tournament": "30_tournament_hud",
}


EXTRA = [sys.argv[i + 1] for i, a in enumerate(sys.argv[:-1]) if a == "--from"]


def find(name):
    for d in EXTRA + [os.path.join(ROOT, "Screenshots"), os.path.join(ROOT, "docs", "images")]:
        for ext in (".png", ".jpg"):  # 새로 찍은 건 .png, docs/images는 .jpg (2026-11-25 용량 줄이려고 바꿈)
            p = os.path.join(d, name + ext)
            if os.path.exists(p):
                return p
    return None


def main():
    os.makedirs(OUT, exist_ok=True)
    for n in os.listdir(OUT):
        if n.endswith(".jpg") and n[:-4] not in PICKS:
            os.remove(os.path.join(OUT, n))
    missing = []
    for out, src in PICKS.items():
        p = find(src)
        if not p:
            missing.append(src)
            continue
        im = Image.open(p).convert("RGB")
        if max(im.size) > 1600:
            im.thumbnail((1600, 1600), Image.LANCZOS)
        im.save(os.path.join(OUT, out + ".jpg"), quality=88, optimize=True, progressive=True)
        print(f"{out}.jpg  <-  {p}")
    if missing:
        print("없음:", ", ".join(missing))
    if "--zip" in sys.argv:
        z = os.path.join(ROOT, "docs", "portfolio", "notion_import.zip")
        with zipfile.ZipFile(z, "w", zipfile.ZIP_DEFLATED) as f:
            f.write(os.path.join(ROOT, "docs", "portfolio", "PORTFOLIO.md"), "PORTFOLIO.md")
            for n in sorted(os.listdir(OUT)):
                f.write(os.path.join(OUT, n), "images/" + n)
        print("zip:", os.path.relpath(z, ROOT))


if __name__ == "__main__":
    main()
