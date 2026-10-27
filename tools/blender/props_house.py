"""
한국 바닷가 집 살림 장비 (2026-10-27, World/HouseKit.cs · CoastProps.KoreanHouse). build_props.py가 불러 PROPS에 더한다.
길이는 실제 미터, 위 = +Z, 앞 = -Y (Unity +Z). 원점은 바닥 가운데 (벽에 거는 것은 벽 면 가운데).

  house_ac     에어컨 실외기: 흰 철판 상자, 앞면 둥근 팬 그릴(동심원 + 십자 살), 옆 방열판 살, 받침 다리
  house_tank   옥상 물탱크: 파란 플라스틱 원통에 가로 골, 둥근 어깨, 위 뚜껑과 배관
  house_lpg    LPG 가스통 두 개: 회색 원통 + 위 보호 고리(손잡이) + 빨간 밸브 뚜껑, 벽으로 가는 노란 가스관
  house_dish   위성 안테나: 흰 접시(얕은 회전체) + 비스듬한 팔 끝 LNB, 벽 받침
  house_onggi  옹기 항아리: 배가 부른 갈색 유약 회전체, 어깨 줄무늬, 뚜껑
  house_pole   콘크리트 전봇대(10m): 위로 가늘어지는 회색 기둥, 완금(가로대) 둘, 애자, 변압기 통, 발판 볼트 (전선은 C#이 늘어지게 잇는다)
  house_pots   현관 앞 화분 무리: 갈색·빨간 화분 셋에 둥근 잎 덩이
  house_garden 스티로폼 상자 텃밭: 흰 상자 셋에 흙과 상추·파 잎
  house_basin  고무 대야 둘 (빨강·파랑, 겹쳐 엎어 둔 것과 바로 둔 것)
"""
import math

import build_icons as B
from props_harbor import lathe

mat, cyl, box, tube, torus = B.mat, B.cyl, B.box, B.tube, B.torus


def ac(_):
    body = mat('#e6e5df', 0.45)
    grille = mat('#3a3c40', 0.5, 0.4)
    dark = mat('#1d1e20', 0.7)
    w, d, h = 0.82, 0.3, 0.6
    objs = [box((w, d, h), body, loc=(0, 0, 0.08 + h / 2), bevel=0.015)]
    # 팬 자리: 어두운 원판 + 동심원 그릴 + 십자 살
    cx, cz = -0.08, 0.08 + h / 2
    objs.append(cyl(0.22, 0.22, 0.01, dark, loc=(cx, -d / 2 - 0.002, cz), rot=(math.pi / 2, 0, 0), seg=28))
    for r in (0.07, 0.13, 0.19, 0.23):
        objs.append(torus(r, 0.005, grille, loc=(cx, -d / 2 - 0.01, cz), rot=(math.pi / 2, 0, 0), seg=28, mseg=4))
    for a in (0, math.pi / 2):
        objs.append(box((0.46, 0.01, 0.012), grille, loc=(cx, -d / 2 - 0.01, cz), rot=(0, a, 0)))
    # 오른쪽 옆 방열판 살 (가로 줄)
    for k in range(9):
        objs.append(box((0.008, d - 0.06, 0.012), grille, loc=(w / 2 + 0.002, 0, 0.14 + k * 0.055)))
    # 받침 다리 두 개
    for x in (-w * 0.35, w * 0.35):
        objs.append(box((0.06, d + 0.06, 0.08), grille, loc=(x, 0, 0.04)))
    return objs


def tank(_):
    blue = mat('#2a63b3', 0.45)
    lid = mat('#1f4f94', 0.5)
    pipe = mat('#d8d8d2', 0.5)
    r, h = 0.62, 1.2
    prof = [(0.0, 0.0), (r - 0.05, 0.0), (r, 0.05)]
    # 가로 골 (위로 갈수록 4줄)
    for k in range(4):
        z = 0.15 + k * 0.24
        prof += [(r, z), (r - 0.025, z + 0.03), (r - 0.025, z + 0.07), (r, z + 0.1)]
    prof += [(r, h - 0.2), (r * 0.82, h - 0.05), (r * 0.42, h), (r * 0.42, h + 0.02)]
    objs = [lathe(prof, blue, 32, 'Tank')]
    objs.append(cyl(r * 0.42, r * 0.42, 0.06, lid, loc=(0, 0, h + 0.05), seg=24))
    objs.append(tube([(r - 0.02, 0, 0.12), (r + 0.12, 0, 0.12), (r + 0.12, 0, -0.1)], 0.025, pipe, sides=8, name='Pipe'))
    return objs


def lpg(_):
    grey = mat('#b7bcc2', 0.4, 0.3)
    red = mat('#c42a1e', 0.45)
    yellow = mat('#d9b52a', 0.45)
    objs = []
    for k, x in enumerate((-0.2, 0.22)):
        prof = [(0.0, 0.0), (0.17, 0.0), (0.18, 0.04), (0.18, 0.95), (0.13, 1.06), (0.05, 1.1)]
        c = lathe(prof, grey, 20, 'Cylinder')
        c.location = (x, 0, 0)
        objs.append(c)
        objs.append(torus(0.1, 0.012, grey, loc=(x, 0, 1.2), seg=16, mseg=5))       # 보호 고리 (손잡이)
        for a in (0.4, 2.7):
            objs.append(box((0.02, 0.02, 0.12), grey, loc=(x + 0.1 * math.cos(a), 0.1 * math.sin(a), 1.14)))
        objs.append(cyl(0.035, 0.035, 0.07, red, loc=(x, 0, 1.15), seg=10))          # 밸브 뚜껑
    # 벽(+Y = 뒤)으로 가는 노란 가스관
    objs.append(tube([(-0.2, 0, 1.18), (-0.2, 0.22, 1.25), (0.0, 0.25, 1.3), (0.0, 0.25, 2.0)], 0.014, yellow, sides=8, name='GasPipe'))
    return objs


def dish(_):
    white = mat('#ecebe6', 0.35)
    grey = mat('#7c8084', 0.4, 0.6)
    objs = []
    # 얕은 접시 (앞 = -Y를 본다, 위로 30° 들림)
    prof = [(0.0, 0.0), (0.15, 0.006), (0.3, 0.03), (0.4, 0.06), (0.4, 0.07), (0.0, 0.015)]
    d = lathe(prof, white, 32, 'Dish')
    d.rotation_euler = (math.radians(60), 0, 0)
    d.location = (0, -0.12, 0.45)
    objs.append(d)
    # LNB 팔과 머리, 벽 받침
    objs.append(tube([(0, -0.1, 0.32), (0, -0.42, 0.55)], 0.012, grey, sides=6, name='Arm'))
    objs.append(box((0.07, 0.1, 0.06), grey, loc=(0, -0.45, 0.57), rot=(math.radians(-30), 0, 0)))
    objs.append(tube([(0, -0.08, 0.45), (0, 0.1, 0.45)], 0.025, grey, sides=8, name='Mast'))
    objs.append(box((0.16, 0.02, 0.22), grey, loc=(0, 0.11, 0.45)))
    return objs


def onggi(_):
    glaze = mat('#4a2c1a', 0.3)
    band = mat('#3a2214', 0.35)
    prof = [(0.0, 0.0), (0.18, 0.0), (0.22, 0.04), (0.33, 0.25), (0.38, 0.45), (0.36, 0.6), (0.27, 0.74), (0.2, 0.78), (0.2, 0.82), (0.0, 0.82)]
    objs = [lathe(prof, glaze, 28, 'Jar')]
    objs.append(torus(0.355, 0.008, band, loc=(0, 0, 0.6), seg=28, mseg=4))
    objs.append(torus(0.375, 0.008, band, loc=(0, 0, 0.5), seg=28, mseg=4))
    lid = lathe([(0.0, 0.8), (0.24, 0.8), (0.23, 0.84), (0.12, 0.9), (0.05, 0.92), (0.05, 0.95), (0.0, 0.95)], glaze, 24, 'Lid')
    objs.append(lid)
    return objs


def pole(_):
    conc = mat('#9a9893', 0.8)
    steel = mat('#5a5d60', 0.5, 0.6)
    insul = mat('#e6e3da', 0.3)
    can = mat('#7d8288', 0.45, 0.5)
    h = 10.0
    objs = [lathe([(0.0, 0.0), (0.19, 0.0), (0.18, 2.0), (0.14, h - 0.2), (0.13, h), (0.0, h)], conc, 16, 'Pole')]
    # 완금 둘 (길 방향 = x), 애자 셋씩
    for z, w in ((h - 0.5, 1.8), (h - 1.4, 1.4)):
        objs.append(box((w, 0.09, 0.09), steel, loc=(0, 0, z)))
        for x in (-w * 0.45, 0.0, w * 0.45):
            objs.append(cyl(0.05, 0.035, 0.14, insul, loc=(x, 0, z + 0.12), seg=8))
    # 변압기 통 + 받침
    objs.append(cyl(0.3, 0.3, 0.9, can, loc=(0, -0.42, h - 3.0), seg=16))
    objs.append(cyl(0.32, 0.32, 0.05, can, loc=(0, -0.42, h - 2.53), seg=16))
    objs.append(box((0.5, 0.35, 0.06), steel, loc=(0, -0.3, h - 3.48)))
    # 발판 볼트 (2m 위부터 엇갈려)
    for k in range(10):
        z = 2.3 + k * 0.45
        objs.append(cyl(0.015, 0.015, 0.4, steel, loc=(0.18 if k % 2 else -0.18, 0, z), rot=(0, math.pi / 2, 0), seg=6))
    return objs


def pots(_):
    clay = mat('#8a4b2e', 0.7)
    red = mat('#a8352a', 0.6)
    soil = mat('#3a2a1e', 0.9)
    leaf = mat('#3f6e2c', 0.7)
    leaf2 = mat('#5a8a34', 0.7)
    objs = []
    for k, (x, y, r, hh, m) in enumerate(((0, 0, 0.18, 0.3, clay), (0.42, 0.08, 0.14, 0.24, red), (0.2, 0.38, 0.22, 0.34, clay))):
        p = lathe([(0.0, 0.0), (r * 0.7, 0.0), (r, hh), (r + 0.02, hh), (r + 0.02, hh + 0.03), (0.0, hh + 0.03)], m, 16, 'Pot')
        p.location = (x, y, 0)
        objs.append(p)
        objs.append(cyl(r * 0.95, r * 0.95, 0.02, soil, loc=(x, y, hh - 0.02), seg=14))
        objs.append(B.lumpy(r * 1.3, leaf if k % 2 == 0 else leaf2, loc=(x, y, hh + r * 0.9), scale=(1, 1, 0.8), amp=0.3, freq=3.5, seed=k * 3 + 1, seg=14, rings=10))
    return objs


def garden(_):
    foam = mat('#ecebe6', 0.75)
    soil = mat('#3a2a1e', 0.95)
    lettuce = mat('#6aa83a', 0.6)
    onion = mat('#4f8a36', 0.6)
    objs = []
    for k, x in enumerate((0.0, 0.62, 1.24)):
        objs.append(box((0.58, 0.4, 0.3), foam, loc=(x, 0, 0.15), bevel=0.01))
        objs.append(box((0.52, 0.34, 0.02), soil, loc=(x, 0, 0.29)))
        if k == 1:  # 파: 가는 세로 잎
            for i in range(14):
                a = i * 2.4
                objs.append(cyl(0.012, 0.004, 0.32, onion, loc=(x + 0.18 * math.cos(a) * (i % 3) / 2, 0.12 * math.sin(a) * (i % 3) / 2, 0.45), rot=(0.1 * math.sin(a), 0.1 * math.cos(a), 0), seg=5))
        else:       # 상추: 둥근 잎 덩이
            for i in range(4):
                objs.append(B.lumpy(0.1, lettuce, loc=(x - 0.15 + (i % 2) * 0.3, -0.08 + (i // 2) * 0.16, 0.35), scale=(1, 1, 0.6), amp=0.35, freq=4, seed=i + k * 5, seg=12, rings=8))
    return objs


def basin(_):
    red = mat('#c8352a', 0.5)
    blue = mat('#2c63b0', 0.5)
    prof = [(0.0, 0.0), (0.24, 0.0), (0.27, 0.02), (0.34, 0.2), (0.37, 0.21), (0.37, 0.23), (0.35, 0.23), (0.32, 0.21), (0.25, 0.04), (0.0, 0.04)]
    a = lathe(prof, red, 24, 'Basin')
    b = lathe(prof, blue, 24, 'Basin2')
    b.location = (0.75, 0.1, 0.0)
    c = lathe(prof, blue, 24, 'Basin3')       # 엎어 둔 것
    c.rotation_euler = (math.pi, 0, 0)
    c.location = (0.75, 0.1, 0.46)
    return [a, b, c]


PROPS = {
    'house_pole': pole,
    'house_pots': pots,
    'house_garden': garden,
    'house_basin': basin,
    'house_ac': ac,
    'house_tank': tank,
    'house_lpg': lpg,
    'house_dish': dish,
    'house_onggi': onggi,
}
