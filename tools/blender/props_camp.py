"""
한강 둔치 캠핑 소품 (2026-10-24, WorldBuilder.BuildTrees). build_props.py가 불러 PROPS에 더한다.
길이는 실제 미터, 위 = +Z, 앞 = -Y (Unity +Z). 입구가 앞(-Y)을 본다.
겉감 재질 이름은 'TentFabric' — Unity가 이름을 보고 텐트마다 색을 바꿔 칠한다 (FBX 색은 기본 파랑).

  tent_dome   2~3인용 원터치 돔 텐트: 바닥 2.1×1.5m, 높이 1.25m. 대각선 두 폴(슬리브 띠)이 모서리에서 땅에 닿고
              폴 사이 천이 안으로 조금 처진다. 회색 욕조형 바닥 띠, 앞 D자 입구는 열려 안이 어둡고 위에 말아 올린 문,
              뒤에 메시 창, 폴 중턱에서 네 모서리 쪽 땅으로 가는 스트링과 팩
  tent_shade  원터치 그늘막 (해변 텐트): 앞이 아치로 트인 1/4 타원체 껍데기, 안쪽은 은색 UV 코팅, 바닥 깔개가 앞으로 나오고
              앞 모서리에 모래주머니
  (CU 편의점 앞마당 — HanRiverLandmarks.ConvenienceStore)
  parasol_cu  보라 팔각 파라솔: 살 8개 사이가 살짝 처진 천, 가장자리 늘어진 띠(한 칸 걸러 흰 "CU"), 꼭대기 바람구멍 덮개, 흰 기둥
  chair_cu    보라 플라스틱 팔걸이 의자 (모노블록): 살짝 오목한 좌판, 세로 홈 난 휜 등판, 팔걸이, 벌어진 다리
  table_cu    흰 정사각 탁자 0.8m: 가운데 파라솔 구멍, 은색 다리 넷
"""
import math

import bmesh
import build_icons as B
from mathutils import Vector as V

mat, cyl, box, tube = B.mat, B.cyl, B.box, B.tube


def _mats():
    return dict(
        fabric=mat('#3a78c0', 0.75, name='TentFabric'),
        trim=mat('#3a3c40', 0.8, name='TentTrim'),
        dark=mat('#15171c', 0.95, name='TentInside'),
        mesh=mat('#2c3036', 0.9, name='TentMesh'),
        floor=mat('#585c62', 0.8, name='TentFloor'),
        pole=mat('#26272a', 0.5, name='TentPole'),
        rope=mat('#d6d2c4', 0.9, name='TentRope'),
        silver=mat('#b4b8bc', 0.45, 0.3, name='TentSilver'),
        sand=mat('#8a7a5c', 0.95, name='TentSandbag'),
    )


def _grid(n, m, fn, mats, pick, flip=False):
    """(i/n, j/m) → 점 fn, 면마다 pick(가운데 점, i, j) → 재질 번호. 재질 목록 mats."""
    bm = bmesh.new()
    vs = [[bm.verts.new(fn(i / n, j / m)) for j in range(m + 1)] for i in range(n + 1)]
    for i in range(n):
        for j in range(m):
            q = (vs[i][j], vs[i + 1][j], vs[i + 1][j + 1], vs[i][j + 1])
            f = bm.faces.new(q[::-1] if flip else q)
            f.material_index = pick(fn((i + 0.5) / n, (j + 0.5) / m), (i + 0.5) / n, (j + 0.5) / m)
    ob = B.obj_from_bm(bm, 'Tent', mats[0])
    for mm in mats[1:]:
        ob.data.materials.append(mm)
    return ob


# ---------------------------------------------------------------- 돔 텐트

W, D, H, A = 2.1, 1.5, 1.25, 0.55


def _dome(u, v):
    """u, v ∈ [-1, 1] (바닥 사각형). 가장자리는 땅, 모서리로 대각선 폴이 내려간다."""
    z = H * ((1 - u * u) * (1 - v * v)) ** A
    phi = math.atan2(v, u)
    c = math.cos(2 * phi) ** 2                  # 옆면 가운데 1, 대각선(폴) 0
    pinch = 1 - 0.08 * c * math.sin(math.pi * min(1.0, z / H))
    return V((u * W / 2 * pinch, v * D / 2 * pinch, z))


def _lam(ub, vb, z):
    """바닥 테두리 점 (ub, vb)에서 가운데로 λ만큼 들어간 곳이 높이 z가 되는 λ."""
    lo, hi = 0.0, 1.0
    for _ in range(40):
        mid = (lo + hi) / 2
        lo, hi = (mid, hi) if _dome(mid * ub, mid * vb).z > z else (lo, mid)
    return (lo + hi) / 2


def _front(x, z, back=False):
    """앞(뒤)면에서 가로 x, 높이 z인 겉면 점과 바깥 방향 (문·창 조각용)."""
    u = 2 * x / W
    r = (z / H) ** (1 / A) / (1 - u * u)
    v = math.sqrt(max(0.0, 1 - r)) * (1 if back else -1)
    p = _dome(u, v)
    q = _dome(u, v * 0.98 if abs(v) > 0.05 else v)
    n = (p - q)
    n.z = max(n.z, 0.0)
    return p, n.normalized() if n.length > 1e-6 else V((0, 1 if back else -1, 0))


def _patch(edge, m, n=12, k=10, lift=0.012, back=False):
    """앞(뒤)면에 붙이는 조각: a ∈ [-1,1] 가로, b ∈ [0,1] 아래 → edge(a) 높이."""
    bm = bmesh.new()
    rows = []
    for i in range(n + 1):
        a = -1 + 2 * i / n
        z0, z1, x = edge(a)
        row = []
        for j in range(k + 1):
            p, nn = _front(x, z0 + (z1 - z0) * j / k, back)
            row.append(bm.verts.new(p + nn * lift))
        rows.append(row)
    for i in range(n):
        for j in range(k):
            q = (rows[i][j], rows[i + 1][j], rows[i + 1][j + 1], rows[i][j + 1])
            bm.faces.new(q[::-1] if back else q)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    return B.obj_from_bm(bm, 'Patch', m)


def tent_dome(_):
    m = _mats()
    # 겉감: 바닥 테두리를 돌아가는 열 × 높이 고른 줄 (욕조형 바닥 띠가 줄에 딱 맞는다), 꼭대기는 한 점
    side = 16
    rim = []
    for c in range(4 * side):
        e, f = divmod(c, side)
        t = -1 + 2 * f / side
        rim.append([(t, -1), (1, t), (-t, 1), (-1, -t)][e])
    levels = [0.0, 0.065, 0.13] + [0.13 + (H - 0.13) * math.sin(math.pi / 2 * k / 20) for k in range(1, 20)]
    bm = bmesh.new()
    rings = [[bm.verts.new(_dome(_lam(ub, vb, z) * ub, _lam(ub, vb, z) * vb)) for ub, vb in rim] for z in levels]
    top = bm.verts.new(_dome(0, 0))
    nc = len(rim)
    for i in range(len(levels) - 1):
        for c in range(nc):
            f = bm.faces.new((rings[i][c], rings[i][(c + 1) % nc], rings[i + 1][(c + 1) % nc], rings[i + 1][c]))
            f.material_index = 1 if levels[i] < 0.12 else 0
    for c in range(nc):
        bm.faces.new((rings[-1][c], rings[-1][(c + 1) % nc], top))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    shell = B.obj_from_bm(bm, 'Tent', m['fabric'])
    shell.data.materials.append(m['trim'])
    o = [shell]

    # 열린 D자 입구 (안이 그늘져 어둡다) + 지퍼 테두리
    door = lambda a: (0.14, 0.14 + 0.74 * math.sqrt(max(0.0, 1 - a * a)) ** 0.8, 0.46 * a)
    o.append(_patch(door, m['dark']))
    edge = [_front(door(a)[2], door(a)[1]) for a in (-1 + 2 * k / 24 for k in range(25))]
    o.append(tube([p + nn * 0.02 for p, nn in edge], 0.012, m['trim'], sides=5, name='Zip'))
    # 뒤 메시 창
    o.append(_patch(lambda a: (0.5, 0.8 - 0.06 * a * a, 0.36 * a), m['mesh'], 8, 4, back=True))

    # 대각선 폴 슬리브 (겉감 위로 도톰하게)
    for sg in (1, -1):
        pts = []
        for k in range(25):
            t = -0.995 + 1.99 * k / 24
            p = _dome(t, sg * t)
            pts.append(p + V((p.x * 0.01, p.y * 0.01, 0.012)))
        o.append(tube(pts, 0.022, m['trim'], sides=6, name='Sleeve'))

    # 입구 위에 말아 올린 문짝: 앞면에서 높이 0.9m 되는 곳
    lo, hi = -1.0, 0.0
    for _ in range(30):
        mid = (lo + hi) / 2
        lo, hi = (mid, hi) if _dome(0, mid).z < 0.92 else (lo, mid)
    fy = _dome(0, lo).y
    o.append(cyl(0.055, 0.055, 0.86, m['fabric'], loc=(0, fy - 0.05, 0.93), rot=(0, math.pi / 2, 0), seg=10, name='Roll'))
    for sx in (-1, 1):
        o.append(box((0.025, 0.012, 0.16), m['trim'], loc=(sx * 0.3, fy - 0.1, 0.9)))   # 말린 문 묶는 끈

    # 폴 중턱에서 비스듬히 땅으로 가는 스트링 + 팩
    for sx in (-1, 1):
        for sy in (-1, 1):
            p0 = _dome(sx * 0.6, sy * 0.6)
            g = p0 + V((sx, sy * 0.8, 0)).normalized() * 0.8
            g.z = 0.02
            o.append(tube([p0, g], 0.005, m['rope'], sides=4, name='Guy'))
            o.append(cyl(0.012, 0.008, 0.08, m['pole'], loc=(g.x, g.y, 0.03), seg=5, name='Peg'))
    # 모서리 팩 고리
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(box((0.06, 0.06, 0.02), m['trim'], loc=(sx * W / 2 * 1.01, sy * D / 2 * 1.01, 0.01)))
    return o


# ---------------------------------------------------------------- 그늘막

SW, SB, SH = 2.0, 1.45, 1.2


def _shell(s, t, shrink=1.0):
    """s ∈ [0,1] 아치 둘레(오른쪽 땅 → 꼭대기 → 왼쪽 땅), t ∈ [0,1] 앞 아치 → 뒤 꼭지. 앞이 트인 1/4 타원체."""
    th = math.pi * s
    ps = math.pi / 2 * t
    x = SW / 2 * math.cos(th) * math.cos(ps) * shrink
    z = SH * math.sin(th) * math.cos(ps) * shrink
    y = SB * math.sin(ps) * shrink
    # 앞 가장자리가 차양처럼 살짝 앞으로 나온다
    y -= 0.12 * (1 - t) ** 3 * math.sin(th)
    return V((x, y, z))


def tent_shade(_):
    m = _mats()
    FAB, TRIM = 0, 1

    def pick(p, s, t):
        if t < 0.04 or p.z < 0.06:
            return TRIM                         # 앞 아치 테두리·밑단
        if abs(t - 0.55) < 0.025:
            return TRIM                         # 가운데 아치 폴 슬리브
        return FAB

    # (s, t) 순서로 이은 면은 안을 본다 → 겉감은 뒤집고, 안쪽 은색 코팅은 그대로 (Unity는 뒷면을 안 그린다)
    o = [_grid(36, 18, lambda s, t: _shell(s, t), [m['fabric'], m['trim']], pick, flip=True)]
    o.append(_grid(36, 18, lambda s, t: _shell(s, t, 0.985), [m['silver']], lambda p, s, t: 0))
    # 바닥 깔개: 껍데기 안 + 앞으로 0.5m
    o.append(box((SW * 0.98, SB + 0.5, 0.012), m['floor'], loc=(0, (SB - 0.5) / 2, 0.006)))
    # 앞 아치 폴
    o.append(tube([_shell(k / 30, 0.0) + V((0, -0.01, 0)) for k in range(31)], 0.015, m['pole'], sides=6, name='Pole'))
    # 앞 모서리 모래주머니
    for sx in (-1, 1):
        o.append(box((0.3, 0.2, 0.09), m['sand'], loc=(sx * (SW / 2 - 0.12), -0.3, 0.045), bevel=0.03))
        o.append(tube([V((sx * (SW / 2 - 0.02), 0.0, 0.03)), V((sx * (SW / 2 - 0.12), -0.24, 0.07))], 0.012, m['trim'], sides=4))
    return o


# ---------------------------------------------------------------- CU 편의점 앞마당

CU_PURPLE = '#5e2a8c'


def _letters_cu(m, h):
    """흰 "CU" 글자 (x-z 평면, 앞 = -Y, 가운데 원점, 글자 높이 h)."""
    o = []
    r, w = h * 0.42, h * 0.11
    # C: 300° 호
    pts = [V((-h * 0.45 + r * math.cos(a), 0, r * math.sin(a))) for a in (math.radians(40 + 280 * k / 14) for k in range(15))]
    o.append(tube(pts, w, m, sides=4, name='C'))
    # U: 아래 반원 + 세로 두 줄
    cx = h * 0.45
    pts = [V((cx - r, 0, r * 0.9))] + [V((cx + r * math.cos(a), 0, r * math.sin(a) * 0.9 - 0.0)) for a in (math.pi + math.pi * k / 10 for k in range(11))] + [V((cx + r, 0, r * 0.9))]
    o.append(tube(pts, w, m, sides=4, name='U'))
    for ob in o:
        ob.scale = (1, 0.25, 1)
    return o


def parasol_cu(_):
    cloth = mat(CU_PURPLE, 0.7, name='CuCloth')
    cloth2 = mat('#4c2274', 0.75, name='CuCloth2')
    white = mat('#f4f4f6', 0.5, name='CuWhite')
    pole = mat('#e8e8ea', 0.35, 0.4, name='CuPole')
    rib = mat('#d0d0d4', 0.4, 0.6, name='CuRib')
    R, top, drop = 1.35, 2.55, 0.42
    o = []
    # 천: 팔각, 살 사이는 살짝 처지고 바깥으로 갈수록 내려간다
    bm = bmesh.new()
    nseg, nr = 8 * 6, 10
    rings = []
    for i in range(nr + 1):
        t = 0.06 + 0.94 * i / nr
        ring = []
        for k in range(nseg):
            a = 2 * math.pi * k / nseg
            f = (k % 6) / 6.0                  # 살과 살 사이 0→1
            sag = math.sin(math.pi * f) * 0.05 * t
            rr = R * t * (math.cos(math.pi / 8) / math.cos((f - 0.5) * math.pi / 4)) * (1 - 0.03 * math.sin(math.pi * f))
            ring.append(bm.verts.new((rr * math.cos(a), rr * math.sin(a), top - drop * t ** 1.4 - sag)))
        rings.append(ring)
    for i in range(nr):
        for k in range(nseg):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % nseg], rings[i + 1][(k + 1) % nseg], rings[i + 1][k]))
    c = bm.verts.new((0, 0, top + 0.01))
    for k in range(nseg):
        bm.faces.new((rings[0][k], rings[0][(k + 1) % nseg], c))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.01)
    o.append(B.obj_from_bm(bm, 'Canopy', cloth))
    # 가장자리 늘어진 띠 8장 (한 칸 걸러 흰 CU)
    rim = R * (1.0)
    zr = top - drop - 0.005
    for k in range(8):
        a0, a1 = 2 * math.pi * k / 8, 2 * math.pi * (k + 1) / 8
        p0 = V((rim * math.cos(a0), rim * math.sin(a0), zr))
        p1 = V((rim * math.cos(a1), rim * math.sin(a1), zr))
        mid = (p0 + p1) / 2
        ln = (p1 - p0).length
        ang = math.atan2(mid.y, mid.x)
        o.append(box((ln * 0.98, 0.012, 0.2), cloth2 if k % 2 else cloth, loc=(mid.x, mid.y, zr - 0.1), rot=(0, 0, ang + math.pi / 2)))
        if k % 2 == 0:
            o.extend(_letters_cu_place(white, mid, ang, zr - 0.1))
    # 살 8개 (천 밑) + 받침살
    for k in range(8):
        a = 2 * math.pi * k / 8
        d = V((math.cos(a), math.sin(a), 0))
        pts = [V((0, 0, top - 0.02)) + d * R * t + V((0, 0, -drop * t ** 1.4 - 0.015)) for t in (0.0, 0.25, 0.5, 0.75, 1.0)]
        o.append(tube(pts, 0.009, rib, sides=4, name='Rib'))
        o.append(tube([V((0, 0, 1.95)), pts[2]], 0.007, rib, sides=4, name='Stretcher'))
    # 바람구멍 덮개
    o.append(B.cyl(0.32, 0.05, 0.12, cloth, loc=(0, 0, top + 0.1), seg=8))
    o.append(B.cyl(0.03, 0.03, 0.1, pole, loc=(0, 0, top + 0.02), seg=8))
    o.append(B.sphere(0.035, pole, loc=(0, 0, top + 0.19), seg=10, rings=6))
    # 기둥·도르래·러너
    o.append(B.cyl(0.022, 0.022, top, pole, loc=(0, 0, top / 2), seg=10))
    o.append(B.cyl(0.04, 0.04, 0.1, pole, loc=(0, 0, 1.95), seg=10))
    o.append(B.cyl(0.035, 0.035, 0.08, rib, loc=(0, 0, 1.25), seg=10))
    o.append(box((0.12, 0.015, 0.015), rib, loc=(0.06, 0, 1.25)))
    return o


def _letters_cu_place(m, mid, ang, z):
    out = []
    for L in _letters_cu(m, 0.11):
        L.rotation_euler = (0, 0, ang + math.pi / 2)
        L.location = mid + V((math.cos(ang), math.sin(ang), 0)) * 0.012 + V((0, 0, z - mid.z))
        out.append(L)
    return out


def chair_cu(_):
    pl = mat(CU_PURPLE, 0.45, name='CuPlastic')
    o = []
    # 좌판 (앞이 살짝 둥근 오목판)
    o.append(box((0.46, 0.44, 0.04), pl, loc=(0, -0.02, 0.44), bevel=0.015))
    # 휜 등판: 뒤쪽 호(반지름 0.3m)를 따라 세운 살 다섯 장 (사이가 세로 홈) + 위 가로대
    arc = lambda deg, z: V((0.3 * math.sin(math.radians(deg)), -0.06 + 0.3 * math.cos(math.radians(deg)), z))
    for k in range(5):
        d = -36 + 18 * k
        p = arc(d, 0.66)
        o.append(box((0.085, 0.025, 0.4), pl, loc=tuple(p), rot=(-0.12, 0, -math.radians(d)), bevel=0.008))
    o.append(tube([arc(d, 0.87) + V((0, 0.025, 0)) for d in range(-45, 46, 9)], 0.022, pl, sides=6, name='TopRail'))
    # 팔걸이
    for sx in (-1, 1):
        o.append(tube([V((sx * 0.25, 0.16, 0.67)), V((sx * 0.27, 0.0, 0.66)), V((sx * 0.26, -0.16, 0.64)), V((sx * 0.24, -0.19, 0.55)), V((sx * 0.23, -0.19, 0.46))], 0.022, pl, sides=6, name='Arm'))
    # 벌어진 다리
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(tube([V((sx * 0.2, sy * 0.18, 0.43)), V((sx * 0.25, sy * 0.24, 0.0))], 0.022, pl, r_end=0.018, sides=6, name='Leg'))
    return o


def table_cu(_):
    top = mat('#f2f2f0', 0.4, name='CuTableTop')
    alu = mat('#c4c6ca', 0.35, 0.7, name='CuAlu')
    o = [box((0.8, 0.8, 0.03), top, loc=(0, 0, 0.72), bevel=0.01)]
    o.append(box((0.82, 0.82, 0.03), alu, loc=(0, 0, 0.695)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(B.cyl(0.018, 0.018, 0.69, alu, loc=(sx * 0.35, sy * 0.35, 0.345), seg=8))
            o.append(B.cyl(0.024, 0.024, 0.02, mat('#222222', 0.8), loc=(sx * 0.35, sy * 0.35, 0.01), seg=8))
    o.append(B.cyl(0.03, 0.03, 0.035, alu, loc=(0, 0, 0.73), seg=10))
    return o


PROPS = {'tent_dome': tent_dome, 'tent_shade': tent_shade, 'parasol_cu': parasol_cu, 'chair_cu': chair_cu, 'table_cu': table_cu}
