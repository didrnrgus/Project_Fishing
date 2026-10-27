"""
바닷가 낚시점 가게 앞 소품 (2026-10-27, CoastProps.TackleShop). build_props.py가 불러 PROPS에 더한다.
길이는 실제 미터, 위 = +Z, 앞 = -Y (Unity +Z). 원점은 바닥 가운데.

  shop_rodrack   낚싯대 거치대: 쇠 받침틀(아래 구멍판·위 고리판)에 색색 낚싯대 아홉 대가 비스듬히, 몇 대는 릴이 달렸다
  shop_fridge    미끼·음료 냉장고: 흰 몸통, 유리문 안에 칸칸이 병·통, 위 빨간 띠
  shop_stool     빨간 플라스틱 의자 (등받이 없는 사각 의자, 다리 사이가 뚫림)
  shop_vending   음료 자판기: 빨간 몸통, 앞 유리 진열칸에 캔 줄, 버튼 줄, 아래 꺼내는 구멍
  shop_freezer   크릴·밑밥 냉동 쇼케이스 (가로 누운 냉동고): 흰 몸통 속이 파여 미닫이 유리 뚜껑 테 사이로 크릴 블록·밑밥 덩이가 보인다,
                 앞 회색 범퍼 띠, 아래 기계실 통풍 살, 바퀴 받침. 앞 파란 라벨 판(C#이 '냉동 크릴' 글자를 붙인다)
  shop_baitbags  집어제 포대 더미: 나무 팔레트(널 다섯·받침목 셋) 위에 색 띠 두른 종이 포대 세 켜(켜마다 방향 엇갈림), 맨 위 한 포대는 비스듬히
  shop_lurewall  가게 안 루어 벽: 흰 타공판 + 걸이 고리마다 색색 루어 블리스터 포장(군데군데 빈 고리), 아래 나무 진열장에 낚싯줄 실패·찌 상자
                 (원점 = 벽 면 바닥 가운데, 앞 = -Y)
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


def freezer(_):
    white = mat('#eeede8', 0.35)
    liner = mat('#9fb0bc', 0.35)
    alu = mat('#c9ccd0', 0.3, 0.7)
    grey = mat('#6d7277', 0.5)
    dark = mat('#26292c', 0.6)
    krill = [mat('#e0794a', 0.35), mat('#d65a3a', 0.35), mat('#ecc9a8', 0.3)]
    chum = mat('#6b5236', 0.8)
    label = mat('#2a5ea8', 0.4)
    w, d, h, t = 1.32, 0.72, 0.86, 0.07
    floor = 0.36
    objs = [box((w, d, floor), white, loc=(0, 0, floor / 2), bevel=0.02)]                    # 기계실 (아래)
    # 속이 빈 몸통: 네 벽 (위 테두리는 둥글게)
    objs.append(box((w, t, h - floor), white, loc=(0, -d / 2 + t / 2, floor + (h - floor) / 2), bevel=0.012))
    objs.append(box((w, t, h - floor), white, loc=(0, d / 2 - t / 2, floor + (h - floor) / 2), bevel=0.012))
    for x in (-w / 2 + t / 2, w / 2 - t / 2):
        objs.append(box((t, d - t * 2, h - floor), white, loc=(x, 0, floor + (h - floor) / 2), bevel=0.012))
    objs.append(box((w - t * 2, d - t * 2, 0.02), liner, loc=(0, 0, floor + 0.01)))          # 안쪽 바닥
    # 크릴 블록(비닐 포장 납작한 판)·밑밥 덩이: 두세 켜로 쌓였다
    k = 0
    for row in range(3):
        for col in range(4):
            x = -w / 2 + t + 0.16 + col * 0.29 + 0.02 * math.sin(k * 2.1)
            y = -d / 2 + t + 0.1 + row * 0.18
            for z in range(2 + (k * 5) % 3):
                m = chum if (row == 2 and col % 2 == 0) else krill[(k + z) % 3]
                objs.append(box((0.26, 0.16, 0.055), m, loc=(x, y, floor + 0.05 + z * 0.06), rot=(0, 0, 0.12 * math.sin(k * 3.7 + z)), bevel=0.012))
            k += 1
    # 미닫이 유리 뚜껑 두 짝 (유리는 비우고 테만 — 불투명 유리는 안이 안 보였다), 앞뒤로 어긋나 겹친다
    for i, sx in enumerate((-1, 1)):
        zz = h + 0.012 + i * 0.022
        cx = sx * (w / 4 - 0.02) + (0.08 if i == 0 else -0.08)
        lw, ld = w / 2 + 0.04, d - 0.04
        for (sz, x, y) in (((lw, 0.035, 0.022), cx, -ld / 2 + 0.018), ((lw, 0.035, 0.022), cx, ld / 2 - 0.018),
                           ((0.035, ld, 0.022), cx - lw / 2 + 0.018, 0), ((0.035, ld, 0.022), cx + lw / 2 - 0.018, 0)):
            objs.append(box(sz, alu, loc=(x, y, zz)))
        objs.append(box((0.12, 0.03, 0.03), grey, loc=(cx + sx * 0.18, -ld / 2 + 0.05, zz + 0.02)))    # 손잡이
    # 앞 범퍼 띠, 라벨 판, 아래 통풍 살, 바퀴 받침
    objs.append(box((w + 0.01, 0.03, 0.06), grey, loc=(0, -d / 2 - 0.005, 0.42), bevel=0.008))
    objs.append(box((0.5, 0.012, 0.16), label, loc=(0, -d / 2 - 0.004, 0.63)))
    for j in range(6):
        objs.append(box((0.34, 0.012, 0.012), dark, loc=(w / 2 - 0.26, -d / 2 - 0.004, 0.08 + j * 0.035)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            objs.append(cyl(0.035, 0.035, 0.03, dark, loc=(sx * (w / 2 - 0.08), sy * (d / 2 - 0.08), 0.015), seg=10))
    return objs


def baitbags(_):
    wood = mat('#9a7a52', 0.8)
    paper = mat('#e9e2cf', 0.75)
    bands = [mat('#d8a326', 0.6), mat('#c0392b', 0.6), mat('#2f7d46', 0.6), mat('#2a5ea8', 0.6)]
    objs = []
    # 팔레트: 위 널 다섯 + 받침목 셋 + 아래 널 셋
    for i in range(5):
        objs.append(box((1.1, 0.16, 0.022), wood, loc=(0, -0.44 + i * 0.22, 0.129)))
    for x in (-0.5, 0, 0.5):
        objs.append(box((0.1, 1.0, 0.09), wood, loc=(x, 0, 0.072)))
    for i in range(3):
        objs.append(box((1.1, 0.14, 0.022), wood, loc=(0, -0.43 + i * 0.43, 0.011)))

    def bag(x, y, z, rz, along_x, band, tilt=(0, 0)):
        sz = (0.52, 0.34, 0.12) if along_x else (0.34, 0.5, 0.12)
        bw = (0.16, sz[1] + 0.004, 0.1) if along_x else (sz[0] + 0.004, 0.15, 0.1)
        objs.append(box(sz, paper, loc=(x, y, z), rot=(tilt[0], tilt[1], rz), bevel=0.045))
        objs.append(box(bw, band, loc=(x, y, z), rot=(tilt[0], tilt[1], rz), bevel=0.02))

    # 포대 세 켜: 켜마다 방향을 엇갈린다
    k = 0
    for layer in range(3):
        z = 0.2 + layer * 0.115
        if layer % 2 == 0:
            cells = [(-0.27 + i * 0.54, -0.36 + j * 0.36, True) for i in range(2) for j in range(3)]
        else:
            cells = [(-0.36 + i * 0.36, -0.26 + j * 0.52, False) for i in range(3) for j in range(2)]
        for x, y, ax in cells:
            jit = 0.015 * math.sin(k * 4.3)
            bag(x + jit, y - jit, z, 0.04 * math.sin(k * 2.9), ax, bands[k % 4])
            k += 1
    bag(0.1, 0.05, 0.57, 0.6, True, bands[1], (0.08, -0.05))   # 맨 위 비스듬히 얹은 한 포대
    return objs


def lurewall(_):
    board = mat('#e8e4da', 0.6)
    holes = mat('#8a857a', 0.8)
    frame = mat('#5a4a3a', 0.6)
    steel = mat('#b8bcc0', 0.3, 0.8)
    card = [mat(c, 0.45) for c in ('#c0392b', '#2a5ea8', '#e0b93a', '#1f1f22', '#2f7d46', '#e8e3d6', '#d0662a', '#7a3a8a')]
    bubble = mat('#dfe6ea', 0.15)
    lure = [mat(c, 0.25, 0.3) for c in ('#f05a28', '#2ec4b6', '#f4d35e', '#e84a8a', '#9bc53d', '#c0c4c8')]
    cab = mat('#6b5440', 0.55)
    spool = mat('#f2f2ee', 0.3)
    W, H0, H1 = 2.4, 0.75, 2.35
    objs = [box((W, 0.025, H1 - H0), board, loc=(0, 0.0, (H0 + H1) / 2))]
    for x in (-W / 2, W / 2):
        objs.append(box((0.05, 0.05, H1 - H0 + 0.05), frame, loc=(x, 0, (H0 + H1) / 2)))
    objs.append(box((W + 0.05, 0.05, 0.05), frame, loc=(0, 0, H1)))
    # 타공판 구멍 줄 (가는 짙은 가로 띠로)
    for j in range(16):
        objs.append(box((W - 0.1, 0.004, 0.006), holes, loc=(0, -0.014, H0 + 0.05 + j * 0.1)))
    # 걸이 고리 + 블리스터 포장 (판지 + 볼록한 투명 덮개 + 속 루어)
    k = 0
    for row in range(5):
        z = H1 - 0.12 - row * 0.3
        for col in range(13):
            x = -W / 2 + 0.14 + col * 0.177
            objs.append(cyl(0.004, 0.004, 0.12, steel, loc=(x, -0.07, z + 0.03), rot=(math.pi / 2, 0, 0), seg=4))
            if (k * 7) % 11 == 3:      # 군데군데 팔려서 빈 고리
                k += 1
                continue
            for s in range(2):         # 한 고리에 두 개 겹쳐 건다
                y = -0.04 - s * 0.025
                objs.append(box((0.11, 0.006, 0.2), card[(k + s * 3) % len(card)], loc=(x, y, z - 0.09)))
                objs.append(box((0.07, 0.016, 0.12), bubble, loc=(x, y - 0.01, z - 0.11), bevel=0.006))
                if s == 1:
                    objs.append(box((0.022, 0.02, 0.08), lure[k % len(lure)], loc=(x, y - 0.015, z - 0.11), bevel=0.008))
            k += 1
    # 아래 진열장: 나무 상자 + 위에 낚싯줄 실패·찌 상자
    objs.append(box((W, 0.45, 0.72), cab, loc=(0, -0.24, 0.36), bevel=0.01))
    for i in range(14):
        x = -W / 2 + 0.12 + i * 0.165
        if i % 4 == 3:
            objs.append(box((0.13, 0.2, 0.08), card[i % len(card)], loc=(x, -0.3, 0.76), bevel=0.006))
        else:
            objs.append(cyl(0.045, 0.045, 0.05, card[(i * 3) % len(card)], loc=(x, -0.3, 0.745), seg=12))
            objs.append(cyl(0.03, 0.03, 0.052, spool, loc=(x, -0.3, 0.745), seg=10))
    return objs


PROPS = {
    'shop_freezer': freezer,
    'shop_baitbags': baitbags,
    'shop_lurewall': lurewall,
    'shop_rodrack': rodrack,
    'shop_fridge': fridge,
    'shop_stool': stool,
    'shop_vending': vending,
}
