"""
바닷가 낚시점 가게 앞 소품 (2026-10-27, CoastProps.TackleShop). build_props.py가 불러 PROPS에 더한다.
길이는 실제 미터, 위 = +Z, 앞 = -Y (Unity +Z). 원점은 바닥 가운데.

  shop_rodrack   낚싯대 거치대: 쇠 받침틀(아래 구멍판·위 고리판)에 색색 낚싯대 아홉 대가 비스듬히, 몇 대는 릴이 달렸다
  shop_fridge    미끼·음료 냉장고: 흰 몸통, 유리문 안에 칸칸이 병·통, 위 빨간 띠
  shop_stool     빨간 플라스틱 의자 (등받이 없는 사각 의자, 다리 사이가 뚫림)
  shop_vending   음료 자판기: 빨간 몸통, 앞 유리 진열칸에 캔 줄, 버튼 줄, 아래 꺼내는 구멍
"""
import math

import build_icons as B

mat, cyl, box, tube = B.mat, B.cyl, B.box, B.tube


def rodrack(_):
    steel = mat('#3c3f43', 0.45, 0.6)
    reel = mat('#b9bcc0', 0.35, 0.8)
    cols = ['#c0232e', '#1b2747', '#d6b04a', '#1c1c1e', '#2d6a42', '#e8e3d6', '#2a5ea8', '#7a2a2a', '#c86a1e']
    objs = [box((1.1, 0.32, 0.05), steel, loc=(0, 0, 0.18), bevel=0.01), box((1.1, 0.22, 0.04), steel, loc=(0, 0.04, 1.2), bevel=0.01)]
    for x in (-0.53, 0.53):
        objs.append(box((0.04, 0.04, 1.25), steel, loc=(x, 0.05, 0.62)))
        objs.append(box((0.04, 0.4, 0.04), steel, loc=(x, 0.0, 0.02)))
    for i, c in enumerate(cols):
        x = -0.48 + i * 0.12
        lean = 0.12 + 0.04 * ((i * 7) % 3)
        top = (x + 0.06 * math.sin(i), 0.04 + lean * 2.6, 3.2 - 0.15 * (i % 3))
        objs.append(tube([(x, -0.02, 0.2), top], 0.012, mat(c, 0.35), r_end=0.004, sides=6, name='Rod'))
        objs.append(cyl(0.022, 0.022, 0.32, mat('#2a2a2a', 0.8), loc=(x, -0.0, 0.42), rot=(lean * 0.4, 0, 0), seg=8))  # 손잡이
        if i % 3 == 1:
            objs.append(cyl(0.045, 0.045, 0.05, reel, loc=(x, -0.06, 0.62), rot=(0, math.pi / 2, 0), seg=12))
    return objs


def fridge(_):
    white = mat('#efeeea', 0.4)
    red = mat('#c8281e', 0.45)
    dark = mat('#1e2226', 0.6)
    bottle_cols = ['#3a8a3a', '#d8d4c8', '#c4502a', '#2a6ab0', '#e0c040']
    w, d, h = 0.75, 0.68, 1.85
    # 몸통은 앞을 10cm 비워 둔 껍데기: 뒤 상자 + 옆·아래 테 → 칸칸이 병이 앞에서 보인다
    objs = [box((w, d - 0.1, h), white, loc=(0, 0.05, h / 2), bevel=0.02), box((w + 0.01, d + 0.01, 0.22), red, loc=(0, 0, h - 0.12))]
    for x in (-w / 2 + 0.03, w / 2 - 0.03):
        objs.append(box((0.06, 0.12, h - 0.25), white, loc=(x, -d / 2 + 0.06, (h - 0.25) / 2)))
    objs.append(box((w, 0.12, 0.14), white, loc=(0, -d / 2 + 0.06, 0.07)))
    objs.append(box((w - 0.1, 0.02, h - 0.5), dark, loc=(0, -d / 2 + 0.095, h / 2 - 0.08)))         # 안쪽 어둠
    for r in range(4):                                                                          # 칸칸이 병·통
        z = 0.32 + r * 0.33
        objs.append(box((w - 0.14, 0.09, 0.012), mat('#c8cacc', 0.3, 0.6), loc=(0, -d / 2 + 0.05, z - 0.02)))
        for k in range(6):
            x = -w / 2 + 0.12 + k * 0.1
            objs.append(cyl(0.032, 0.026, 0.22, mat(bottle_cols[(k + r) % 5], 0.3), loc=(x, -d / 2 + 0.05, z + 0.1), seg=10))
    # 유리문 테 (유리는 비워 두고 테만 — 불투명 유리는 안이 안 보였다)
    fz = -d / 2 - 0.015
    for (sx, sz, x, z) in ((w - 0.06, 0.04, 0, h - 0.33), (w - 0.06, 0.04, 0, 0.1), (0.04, h - 0.42, -w / 2 + 0.05, h / 2 - 0.12), (0.04, h - 0.42, w / 2 - 0.05, h / 2 - 0.12)):
        objs.append(box((sx, 0.03, sz), mat('#d8d8d4', 0.35, 0.4), loc=(x, fz, z)))
    objs.append(box((0.03, 0.04, 0.5), mat('#8a8e92', 0.35, 0.8), loc=(w / 2 - 0.08, -d / 2 - 0.03, h / 2)))  # 손잡이
    return objs


def stool(_):
    red = mat('#c9261e', 0.45)
    t, s, h = 0.03, 0.34, 0.45
    objs = [box((s, s, t), red, loc=(0, 0, h - t / 2), bevel=0.008)]
    for sx in (-1, 1):
        for sy in (-1, 1):
            objs.append(box((0.045, 0.045, h - t), red, loc=(sx * (s / 2 - 0.03), sy * (s / 2 - 0.03), (h - t) / 2), rot=(sy * 0.05, -sx * 0.05, 0), bevel=0.006))
    for sx in (-1, 1):  # 다리 사이 가로대
        objs.append(box((0.02, s - 0.06, 0.03), red, loc=(sx * (s / 2 - 0.03), 0, 0.12)))
        objs.append(box((s - 0.06, 0.02, 0.03), red, loc=(0, sx * (s / 2 - 0.03), 0.12)))
    return objs


def vending(_):
    red = mat('#b51e24', 0.4)
    white = mat('#efeeea', 0.35)
    dark = mat('#1b1d20', 0.6)
    can_cols = ['#c8281e', '#2a6ab0', '#e0c040', '#3a8a3a', '#f0f0f0', '#e07a20']
    w, d, h = 1.0, 0.75, 1.83
    # 몸통 뒤 상자 + 앞 테(아래 판·오른쪽 기둥·위 띠) → 가운데 진열칸이 8cm 들어가 캔이 보인다
    objs = [box((w, d - 0.08, h), red, loc=(0, 0.04, h / 2), bevel=0.015)]
    objs.append(box((w, 0.08, 0.75), red, loc=(0, -d / 2 + 0.04, 0.375)))
    objs.append(box((0.3, 0.08, h - 0.75), red, loc=(w / 2 - 0.15, -d / 2 + 0.04, 0.75 + (h - 0.75) / 2)))
    objs.append(box((w - 0.3, 0.08, 0.13), red, loc=(-0.15, -d / 2 + 0.04, h - 0.065)))
    objs.append(box((w - 0.32, 0.02, 0.95), dark, loc=(-0.12, -d / 2 + 0.075, 1.25)))        # 진열칸 안쪽
    for r in range(3):
        for k in range(6):
            objs.append(cyl(0.03, 0.03, 0.11, mat(can_cols[(k + r * 2) % 6], 0.35, 0.3), loc=(-0.4 + k * 0.11, -d / 2 + 0.035, 0.88 + r * 0.3), seg=10))
        objs.append(box((w - 0.36, 0.03, 0.02), white, loc=(-0.12, -d / 2 + 0.0, 0.8 + r * 0.3)))  # 가격표 줄
    for k in range(6):  # 버튼 줄
        objs.append(box((0.05, 0.02, 0.025), white, loc=(-0.4 + k * 0.11, -d / 2 - 0.012, 0.74)))
    objs.append(box((0.18, 0.03, 0.4), dark, loc=(w / 2 - 0.15, -d / 2 - 0.01, 1.2)))         # 돈 넣는 판
    objs.append(box((w - 0.3, 0.04, 0.18), dark, loc=(-0.05, -d / 2 - 0.01, 0.3)))            # 꺼내는 구멍
    return objs


PROPS = {
    'shop_rodrack': rodrack,
    'shop_fridge': fridge,
    'shop_stool': stool,
    'shop_vending': vending,
}
