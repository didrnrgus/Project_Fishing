"""
한국 포구 방파제·호안 소품 (2026-10-27, World/HarborKit.cs). build_props.py가 불러 PROPS에 더한다.
길이는 실제 미터, 위 = +Z, 앞 = -Y (Unity +Z = 바다 쪽). 원점은 놓을 자리(바닥 가운데 또는 매다는 고리).

  harbor_bollard   곡주형 계선주: 볼트 박힌 밑판, 허리가 잘록한 주철 기둥, 버섯 머리 (몸통 검정, 머리 노랑 도장)
  harbor_bitt      쌍계주: 긴 밑판에 굵은 기둥 두 개, 위에 테 두른 모자
  harbor_tire      폐타이어 방충재: 홈 파인 트레드 타이어를 벽에 납작하게 대고 밧줄 두 가닥으로 매단다 (원점 = 밧줄 고리)
  harbor_fender    V형 고무 방충재: 벽에 세로로 붙는 사다리꼴 고무 블록, 볼트 구멍 (원점 = 윗면 벽 쪽 가운데)
  harbor_lifering  인명구조함: 쇠 기둥 위 주황 상자, 앞에 흰 띠 구명환
  잡동사니 (방파제 위·호안 앞마당): harbor_bucket 밑밥통 · harbor_cooler 아이스박스 · harbor_coil 사려 둔 밧줄 ·
                                  harbor_netpile 그물 더미 · harbor_crates 생선 상자 더미 · harbor_buoys 부표 더미
"""
import math

import bmesh
import build_icons as B
from mathutils import Vector as V

mat, cyl, box, tube, torus = B.mat, B.cyl, B.box, B.tube, B.torus


def lathe(prof, m, seg=24, name='Lathe'):
    """(반지름, 높이) 단면을 z축으로 돌린 회전체 (바닥·꼭대기 마개)."""
    bm = bmesh.new()
    rings = [[bm.verts.new((r * math.cos(2 * math.pi * k / seg), r * math.sin(2 * math.pi * k / seg), z)) for k in range(seg)] for r, z in prof]
    for i in range(len(rings) - 1):
        for k in range(seg):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % seg], rings[i + 1][(k + 1) % seg], rings[i + 1][k]))
    for ring, (r, z), top in ((rings[0], prof[0], False), (rings[-1], prof[-1], True)):
        if r > 1e-4:
            c = bm.verts.new((0, 0, z))
            for k in range(seg):
                bm.faces.new((ring[k], ring[(k + 1) % seg], c) if top else (ring[(k + 1) % seg], ring[k], c))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    return B.obj_from_bm(bm, name, m)


def base_plate(w, d, t, m, bolts, bolt_m):
    """모서리 둥근 밑판 + 육각 볼트 머리."""
    objs = [box((w, d, t), m, loc=(0, 0, t / 2), bevel=0.012)]
    for x, y in bolts:
        objs.append(cyl(0.028, 0.028, 0.03, bolt_m, loc=(x, y, t + 0.015), seg=6))
    return objs


def bollard(_):
    iron = mat('#2a2b2e', 0.55, 0.4)
    head = mat('#d6a91e', 0.45, 0.2)
    bolt = mat('#3a3a3c', 0.5, 0.6)
    s = 0.19
    objs = base_plate(0.5, 0.5, 0.05, iron, [(sx * s, sy * s) for sx in (-1, 1) for sy in (-1, 1)], bolt)
    # 아래가 넓게 퍼지고 허리가 잘록, 위에서 다시 벌어지는 주철 기둥
    objs.append(lathe([(0.2, 0.05), (0.17, 0.08), (0.14, 0.13), (0.125, 0.22), (0.12, 0.32), (0.13, 0.37)], iron, 28, 'Post'))
    # 버섯 머리 (배 밧줄이 빠지지 않게 턱이 진다)
    objs.append(lathe([(0.13, 0.37), (0.2, 0.4), (0.225, 0.425), (0.22, 0.45), (0.19, 0.47), (0.12, 0.485), (0.0, 0.49)], head, 28, 'Head'))
    return objs


def bitt(_):
    iron = mat('#2a2b2e', 0.55, 0.4)
    cap = mat('#d6a91e', 0.45, 0.2)
    bolt = mat('#3a3a3c', 0.5, 0.6)
    objs = base_plate(1.15, 0.46, 0.05, iron, [(x, y) for x in (-0.5, 0, 0.5) for y in (-0.17, 0.17)], bolt)
    for x in (-0.3, 0.3):
        p = lathe([(0.15, 0.05), (0.125, 0.09), (0.115, 0.3), (0.12, 0.42)], iron, 24, 'Post')
        cp = lathe([(0.12, 0.42), (0.16, 0.44), (0.165, 0.47), (0.14, 0.49), (0.0, 0.495)], cap, 24, 'Cap')
        p.location = cp.location = (x, 0, 0)
        objs += [p, cp]
    return objs


def tire_mesh(m, R_out=0.36, R_in=0.2, width=0.2, seg=56, lugs=28):
    """타이어: 둥근 사각 단면을 축(Y)으로 돌리고, 바깥 트레드에 홈(러그)을 판다. 축이 Y라 벽에 납작하게 붙는다."""
    hw = width / 2
    # (반지름, 축 방향 y) 단면: 안쪽 림 → 옆면 → 둥근 어깨 → 트레드 → 반대쪽
    sec = [(R_in, -hw * 0.7), (R_in + 0.03, -hw), (R_out - 0.05, -hw * 0.95), (R_out - 0.012, -hw * 0.8), (R_out, -hw * 0.45),
           (R_out, hw * 0.45), (R_out - 0.012, hw * 0.8), (R_out - 0.05, hw * 0.95), (R_in + 0.03, hw), (R_in, hw * 0.7)]
    bm = bmesh.new()
    rings = []
    for k in range(seg):
        a = 2 * math.pi * k / seg
        groove = 0.016 if (k * lugs // seg) % 2 else 0.0  # 트레드 블록 사이 홈
        ring = []
        for j, (r, y) in enumerate(sec):
            rr = r - (groove if 3 <= j <= 6 and r > R_out - 0.02 else 0.0)
            ring.append(bm.verts.new((rr * math.cos(a), y, rr * math.sin(a))))
        rings.append(ring)
    n = len(sec)
    for k in range(seg):
        a, b = rings[k], rings[(k + 1) % seg]
        for j in range(n):  # 단면을 닫는다 (마지막 → 처음 = 안쪽 림)
            bm.faces.new((a[j], a[(j + 1) % n], b[(j + 1) % n], b[j]))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    return B.obj_from_bm(bm, 'Tire', m)


def tire(_):
    rubber = mat('#1c1c1e', 0.85)
    rope = mat('#c9b48a', 0.9)
    cz = -0.72  # 타이어 가운데 (원점 = 상판 모서리의 밧줄 고리)
    t = tire_mesh(rubber)
    t.location = (0, 0, cz)
    objs = [t]
    # 밧줄 두 가닥: 고리에서 갈라져 타이어 구멍 위쪽 테를 감고 뒤로 넘어간다
    for sx in (-1, 1):
        pts = [(0, 0.02, 0.0), (sx * 0.04, 0.0, -0.12), (sx * 0.1, -0.04, cz + 0.36), (sx * 0.12, -0.02, cz + 0.2), (sx * 0.1, 0.06, cz + 0.17),
               (sx * 0.08, 0.1, cz + 0.32)]
        objs.append(tube(pts, 0.014, rope, sides=8, name='Rope'))
    objs.append(torus(0.035, 0.008, mat('#5a5a5c', 0.4, 0.8), loc=(0, 0.03, 0.0), rot=(math.pi / 2, 0, 0), seg=12, mseg=6))
    return objs


def fender(_):
    rubber = mat('#1e1e20', 0.8)
    bolt = mat('#4a4a4c', 0.45, 0.7)
    h, wb, wf, d = 1.1, 0.3, 0.18, 0.18  # 높이, 벽 쪽 너비, 앞면 너비, 두께
    bm = bmesh.new()
    prof = [(-wb / 2, 0), (-wf / 2, -d), (wf / 2, -d), (wb / 2, 0)]  # (x, y) — 벽(y=0)에서 바다(-y)로 튀어나온다
    bot = [bm.verts.new((x, y, -h)) for x, y in prof]
    top = [bm.verts.new((x, y, 0.0)) for x, y in prof]
    for i in range(4):
        j = (i + 1) % 4
        bm.faces.new((bot[i], bot[j], top[j], top[i]))
    bm.faces.new(list(reversed(top)))
    bm.faces.new(bot)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bmesh.ops.bevel(bm, geom=[e for e in bm.edges], offset=0.012, segments=2, affect='EDGES', profile=0.5)
    ob = B.obj_from_bm(bm, 'Fender', rubber)
    objs = [ob]
    # 앞면 볼트 구멍 (오목한 자리에 볼트 머리)
    for z in (-0.18, -h + 0.18):
        objs.append(cyl(0.045, 0.045, 0.02, mat('#0e0e10', 0.9), loc=(0, -d - 0.002, z), rot=(math.pi / 2, 0, 0), seg=12))
        objs.append(cyl(0.022, 0.022, 0.03, bolt, loc=(0, -d + 0.01, z), rot=(math.pi / 2, 0, 0), seg=6))
    return objs


def lifering(_):
    steel = mat('#8a8e92', 0.45, 0.6)
    orange = mat('#e8611c', 0.5)
    white = mat('#ece9e2', 0.5)
    dark = mat('#2a2b2e', 0.6)
    objs = [box((0.3, 0.3, 0.02), steel, loc=(0, 0, 0.01)), cyl(0.04, 0.04, 1.2, steel, loc=(0, 0, 0.6), seg=12)]
    # 주황 함 (지붕이 앞으로 살짝 기운다)
    objs.append(box((0.75, 0.3, 0.62), orange, loc=(0, 0, 1.45), bevel=0.02))
    objs.append(box((0.82, 0.38, 0.05), orange, loc=(0, -0.02, 1.79), rot=(0.12, 0, 0), bevel=0.01))
    objs.append(box((0.6, 0.01, 0.12), white, loc=(0, -0.155, 1.62)))           # 글자 띠
    objs.append(box((0.05, 0.02, 0.08), dark, loc=(0.3, -0.16, 1.4)))           # 손잡이
    # 함 앞에 걸린 구명환 (주황·흰 네 칸)
    ring = []
    for k in range(4):
        r = torus(0.26, 0.055, orange if k % 2 == 0 else white, loc=(0, -0.22, 1.3), rot=(math.pi / 2, 0, 0), seg=10, mseg=10,
                  arc=math.pi / 2)
        r.rotation_euler = (math.pi / 2, k * math.pi / 2, 0)
        ring.append(r)
    objs += ring
    objs.append(torus(0.2, 0.006, white, loc=(0, -0.29, 1.3), rot=(math.pi / 2, 0, 0), seg=24, mseg=4))  # 붙잡는 줄
    return objs


def bucket(_):
    """밑밥통: 위가 넓은 파란 플라스틱 통, 테 두른 입구, 철사 손잡이, 안에 붉은 밑밥."""
    blue = mat('#2f6fb8', 0.45)
    bait = mat('#7a3a2a', 0.8)
    wire = mat('#9a9ea2', 0.35, 0.8)
    objs = [lathe([(0.0, 0.0), (0.13, 0.0), (0.135, 0.01), (0.16, 0.3), (0.175, 0.31), (0.175, 0.33), (0.155, 0.33), (0.135, 0.03), (0.0, 0.03)], blue, 24, 'Pail')]
    objs.append(cyl(0.15, 0.15, 0.01, bait, loc=(0, 0, 0.24), seg=20))
    pts = [(0.17 * math.cos(math.pi * t / 8), 0, 0.3 + 0.16 * math.sin(math.pi * t / 8)) for t in range(9)]
    objs.append(tube(pts, 0.004, wire, sides=5, name='Handle'))
    return objs


def cooler(_):
    """아이스박스: 흰 몸통 + 파란 뚜껑, 옆 손잡이, 앞 걸쇠."""
    white = mat('#e8e6e0', 0.4)
    blue = mat('#2a5ea8', 0.4)
    grey = mat('#6a6e72', 0.5)
    objs = [box((0.6, 0.36, 0.34), white, loc=(0, 0, 0.17), bevel=0.03),
            box((0.62, 0.38, 0.07), blue, loc=(0, 0, 0.37), bevel=0.025)]
    for sx in (-1, 1):
        objs.append(box((0.03, 0.16, 0.04), grey, loc=(sx * 0.31, 0, 0.28), bevel=0.008))
    objs.append(box((0.08, 0.02, 0.05), grey, loc=(0, -0.185, 0.33), bevel=0.005))
    return objs


def coil(_):
    """밧줄 뭉치: 바닥에 둥글게 사려 놓은 굵은 밧줄 (나선 관), 끝이 한 가닥 빠져나온다."""
    rope = mat('#c9a96a', 0.9)
    pts = []
    turns, r0 = 5.5, 0.14
    n = 140
    for i in range(n + 1):
        t = i / n
        a = 2 * math.pi * turns * t
        r = r0 + 0.2 * t
        z = 0.025 + 0.05 * math.sin(math.pi * t) * (1 if (i // 25) % 2 else 0.6)
        pts.append((r * math.cos(a), r * math.sin(a), z))
    end = pts[-1]
    pts += [(end[0] + 0.15, end[1] - 0.25, 0.02), (end[0] + 0.25, end[1] - 0.6, 0.02)]
    return [tube(pts, 0.022, rope, sides=7, name='Coil')]


def netpile(_):
    """그물 더미: 초록 그물 덩어리(울퉁불퉁) 위에 주황 뜸 줄과 흰 스티로폼 부표 몇 개."""
    net = mat('#2f6a3a', 0.85)
    net2 = mat('#3c7e48', 0.85)
    orange = mat('#e8761c', 0.5)
    foam = mat('#ecebe6', 0.7)
    objs = [B.lumpy(0.6, net, loc=(0, 0, 0.22), scale=(1.5, 1.0, 0.45), amp=0.25, freq=2.5, seed=3),
            B.lumpy(0.45, net2, loc=(0.55, 0.25, 0.18), scale=(1.1, 0.9, 0.4), amp=0.3, freq=3.0, seed=7)]
    for i in range(7):
        a = i * 0.9
        objs.append(B.sphere(0.07, orange, loc=(0.75 * math.cos(a) * 1.1, 0.5 * math.sin(a), 0.32 + 0.06 * math.sin(i * 2.1)), seg=10, rings=6))
    for x, y in ((-0.7, -0.45), (0.9, -0.3)):
        objs.append(cyl(0.14, 0.14, 0.34, foam, loc=(x, y, 0.17), seg=12))
    return objs


def crates(_):
    """생선 상자 더미: 구멍 난 플라스틱 상자(노랑·파랑) 세 개를 엇갈려 쌓는다."""
    cols = [mat('#e3b81e', 0.5), mat('#2a64b0', 0.5), mat('#e3b81e', 0.5)]
    hole = mat('#202224', 0.8)
    objs = []
    for k, (x, y, z, yaw) in enumerate(((0, 0, 0, 0), (0.62, 0.05, 0, 0.06), (0.3, 0.02, 0.27, -0.08))):
        c = box((0.58, 0.4, 0.26), cols[k], loc=(x, y, z + 0.13), rot=(0, 0, yaw), bevel=0.015)
        objs.append(c)
        # 옆면 손잡이 구멍
        for sx in (-1, 1):
            objs.append(box((0.01, 0.12, 0.04), hole, loc=(x + sx * 0.292 * math.cos(yaw), y + sx * 0.292 * math.sin(yaw), z + 0.2), rot=(0, 0, yaw)))
    return objs


def buoys(_):
    """부표 더미: 주황 공 부표 다섯 개와 흰 스티로폼 원통 부표 둘을 아무렇게나 모아 둔다."""
    orange = mat('#e8611c', 0.4)
    rope = mat('#c9a96a', 0.9)
    foam = mat('#ecebe6', 0.7)
    objs = []
    for x, y, z, r in ((0, 0, 0.22, 0.22), (0.42, 0.1, 0.2, 0.2), (0.18, 0.38, 0.21, 0.21), (0.2, 0.15, 0.55, 0.2), (-0.35, 0.3, 0.19, 0.19)):
        objs.append(B.sphere(r, orange, loc=(x, y, z), seg=16, rings=10))
        objs.append(B.torus(0.035, 0.01, rope, loc=(x, y, z + r + 0.02), rot=(math.pi / 2, 0, 0), seg=10, mseg=5))
    for x, y, yaw in ((-0.45, -0.25, 0.3), (0.55, -0.3, -0.5)):
        objs.append(cyl(0.17, 0.17, 0.45, foam, loc=(x, y, 0.17), rot=(math.pi / 2, 0, yaw), seg=14))
    return objs


PROPS = {
    'harbor_bucket': bucket,
    'harbor_cooler': cooler,
    'harbor_coil': coil,
    'harbor_netpile': netpile,
    'harbor_crates': crates,
    'harbor_buoys': buoys,
    'harbor_bollard': bollard,
    'harbor_bitt': bitt,
    'harbor_tire': tire,
    'harbor_fender': fender,
    'harbor_lifering': lifering,
}
