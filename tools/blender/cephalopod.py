"""
두족류(주꾸미·대문어·갑오징어·무늬오징어·오징어) 모델. build_fish.py가 type 'O'(문어형)·'Q'(갑오징어형)·'S'(오징어형)일 때 부른다.

좌표는 물고기와 같다: 길이 1m, 다리 끝(앞) y = -0.5, 몸통(외투막) 끝 y = +0.5, 위 +Z.
  - Body: 머리 ~ 외투막을 한 덩어리로 이은 몸(단면 고리를 이어 붙인다 — 예전에는 공 두 개를 겹쳐 이음매가 보였다)
          + 눈(눈두덩이 솟은 자리에 박힌다) + 깔때기 + 다리 8개(+ 오징어 촉완 2개) + 빨판 + 문어 다리 사이 막
  - Fins: 갑오징어·무늬오징어 외투막 둘레의 지느러미 치마, 오징어 외투막 끝의 화살촉 지느러미 (주꾸미는 없음 → 아주 작은 판 하나)
UV는 물고기 몸과 같은 원통 투영(u = 길이, v = 배 0 ~ 등 1)이라 fish_tex의 몸 무늬가 그대로 입혀진다. 빨판은 아틀라스의 빨판 색 칸.
다리마다 뼈 4개(Arm{i}_{0..3})를 따로 두고, 몸통 사슬(Head·Spine·Tail)과 함께 애니메이션한다.

다리 모양 (실물 사진 기준, 예전에는 굵기가 거의 같은 막대가 한 점에서 뿔처럼 뻗고 끝이 갈고리로 꺾였다):
  - 문어: 뿌리가 굵고 끝은 실처럼 가늘다. 뿌리 쪽 1/4은 다리 사이 막(산개막)으로 이어지고, 끝은 안쪽(빨판 쪽)으로 말린다.
    안쪽 면에 빨판 두 줄 (끝으로 갈수록 작고 촘촘하다)
  - 오징어·갑오징어: 다리 8개가 앞으로 모여 원뿔을 이루고 끝이 뾰족하다. 오징어는 끝이 넓적한(곤봉) 긴 촉완 2개가 따로 뻗는다.
    갑오징어는 촉완을 주머니에 넣고 다녀서 보이지 않는다
"""
import math

import bmesh
import bpy
import mathutils

V = mathutils.Vector
SUCKER_UV = (0.94, 0.06)   # fish_tex: 두족류 빨판 색 칸 (아틀라스 오른쪽 아래)
ARM_N = 40                 # 다리 경로 점 수
ARM_BONES = 4


def body_uv(p):
    u = min(0.749, max(0.0, (p.y + 0.5) * 0.75))
    v = abs(math.atan2(p.x, -p.z)) / math.pi
    return (u, v)


def catmull(pts, y):
    """제어점 [(y, a, b, ...)]를 y로 잇는 Catmull-Rom (값마다)."""
    if y <= pts[0][0]:
        return pts[0][1:]
    if y >= pts[-1][0]:
        return pts[-1][1:]
    for i in range(len(pts) - 1):
        if pts[i][0] <= y <= pts[i + 1][0]:
            break
    p0, p1, p2, p3 = pts[max(0, i - 1)], pts[i], pts[i + 1], pts[min(len(pts) - 1, i + 2)]
    t = (y - p1[0]) / (p2[0] - p1[0])
    out = []
    for k in range(1, len(p1)):
        a, b, c, e = p0[k], p1[k], p2[k], p3[k]
        out.append(0.5 * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - e) * t * t + (-a + 3 * b - 3 * c + e) * t ** 3))
    return out


def profile(d):
    """몸 단면: [(y, 폭 반지름 rx, 높이 반지름 rz, 중심 z)] 앞(다리 뿌리) → 뒤(외투막 끝)."""
    t = d['type']
    if t == 'O':
        # 다리 뿌리(입 둘레) → 눈이 붙은 머리 → 살짝 잘록한 목 → 뒤·위로 처진 외투막 주머니
        return [(-0.07, 0.07, 0.06, -0.025), (-0.03, 0.088, 0.08, -0.008), (0.01, 0.095, 0.09, 0.0), (0.06, 0.088, 0.088, 0.008),
                (0.14, 0.112, 0.112, 0.03), (0.26, 0.128, 0.13, 0.045), (0.38, 0.118, 0.12, 0.045), (0.46, 0.08, 0.082, 0.04),
                (0.495, 0.03, 0.03, 0.035), (0.5, 0.0, 0.0, 0.034)]
    if t == 'S':
        # 오징어: 작은 머리(큰 눈), 목, 깃 → 길쭉한 원뿔 외투막이 뾰족하게 끝난다
        return [(-0.17, 0.03, 0.028, -0.004), (-0.14, 0.05, 0.046, 0.0), (-0.11, 0.056, 0.05, 0.0), (-0.075, 0.044, 0.04, 0.0),
                (-0.05, 0.06, 0.054, 0.0), (0.05, 0.068, 0.06, 0.0), (0.2, 0.058, 0.05, 0.0), (0.36, 0.036, 0.032, 0.0),
                (0.47, 0.012, 0.011, 0.0), (0.5, 0.0, 0.0, 0.0)]
    # 'Q' 갑오징어(납작하고 넓은 타원 외투막) · 무늬오징어(조금 더 길고 좁다)
    w = d.get('mantle_w', 1.0)
    return [(-0.2, 0.038, 0.032, -0.004), (-0.17, 0.07, 0.054, 0.0), (-0.135, 0.08, 0.06, 0.0), (-0.1, 0.066, 0.052, 0.0),
            (-0.075, 0.1 * w, 0.064, 0.004), (0.05, 0.13 * w, 0.078, 0.008), (0.22, 0.125 * w, 0.074, 0.008), (0.38, 0.098 * w, 0.06, 0.006),
            (0.46, 0.06 * w, 0.04, 0.004), (0.495, 0.018, 0.014, 0.002), (0.5, 0.0, 0.0, 0.0)]


def eye_spot(d):
    """눈 자리 (y, 각도[0 = 배, π = 등], 반지름)."""
    t = d['type']
    if t == 'O':
        return 0.0, 2.05, 0.026
    if t == 'S':
        return -0.11, 1.75, 0.03
    return -0.135, 1.85, 0.03


def loft_body(bm, uvl, d, n=72, sides=44):
    prof = profile(d)
    y0, y1 = prof[0][0], prof[-1][0]
    ey, ea, er = eye_spot(d)
    rings = []
    for i in range(n + 1):
        y = y0 + (y1 - y0) * (1 - math.cos(math.pi * i / n)) / 2   # 양 끝을 촘촘히
        rx, rz, zc = catmull(prof, y)
        rx, rz = max(rx, 0.0), max(rz, 0.0)
        ring = []
        for k in range(sides):
            a = 2 * math.pi * k / sides           # 0 = 배(-Z), π = 등
            ax = abs(math.atan2(math.sin(a), -math.cos(a)))
            # 눈두덩: 눈 자리가 솟는다 (양쪽 대칭)
            bump = 1 + 0.32 * math.exp(-((y - ey) / 0.035) ** 2 - ((ax - ea) / 0.45) ** 2) * (1 if d['type'] != 'S' else 0.6)
            x = math.sin(a) * rx * bump
            z = -math.cos(a) * rz * bump + zc
            ring.append(bm.verts.new((x, y, z)))
        rings.append(ring)
    for i in range(n):
        for k in range(sides):
            f = bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k]))
            for l in f.loops:
                l[uvl].uv = body_uv(l.vert.co)
    for ring, sgn in ((rings[0], -1), (rings[-1], 1)):
        c = sum((v.co for v in ring), V()) / len(ring) + V((0, sgn * 0.002, 0))
        pole = bm.verts.new(c)
        for k in range(sides):
            vs = (ring[k], ring[(k + 1) % sides], pole) if sgn > 0 else (ring[(k + 1) % sides], ring[k], pole)
            f = bm.faces.new(vs)
            for l in f.loops:
                l[uvl].uv = body_uv(l.vert.co)
    return prof


def surface_point(d, prof, y, ang, out=0.0):
    """몸 표면 점 (ang 0 = 배, π/2 = +X 옆, π = 등)."""
    rx, rz, zc = catmull(prof, y)
    return V((math.sin(ang) * (rx + out), y, -math.cos(ang) * (rz + out) + zc))


def eye(bm, uvl, center, r, side):
    new = bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=r)
    verts = new['verts']
    faces = {f for vv in verts for f in vv.link_faces}
    for f in faces:
        for l in f.loops:
            p = l.vert.co
            # 눈 텍스처의 가운데가 바깥(옆)을 보게: 옆에서 본 y·z로 투영
            l[uvl].uv = (0.75 + 0.125 * (0.5 + side * p.y / (2 * r) * 0.98), 0.25 + 0.25 * (0.5 + p.z / (2 * r) * 0.98))
    for vv in verts:
        vv.co += V(center)
    return verts


def frame_along(path):
    """경로를 따라 비틀림 없는 틀 (접선, a, b)."""
    out = []
    prev = None
    n = len(path)
    for i in range(n):
        t = (path[min(i + 1, n - 1)] - path[max(i - 1, 0)]).normalized()
        a = (prev - t * prev.dot(t)).normalized() if prev is not None else t.orthogonal().normalized()
        prev = a
        out.append((t, a, t.cross(a)))
    return out


def tube(bm, uvl, path, radii, oral=None, flat=0.8, sides=12, uv=None):
    """관 (다리·촉완·깔때기). radii[i] = 굵기, oral[i] = 빨판 쪽 방향(그쪽을 납작하게). 끝은 점으로 닫는다."""
    fr = frame_along(path)
    rings = []
    for i, p in enumerate(path):
        t, a, b = fr[i]
        o = None
        if oral is not None:
            o = (oral[i] - t * oral[i].dot(t)).normalized()
        ring = []
        for k in range(sides):
            ang = 2 * math.pi * k / sides
            off = a * math.cos(ang) + b * math.sin(ang)
            if o is not None:
                dn = off.dot(o)
                if dn > 0:
                    off = off - o * dn * (1 - flat)   # 빨판 면은 납작하다
            ring.append(bm.verts.new(p + off * radii[i]))
        rings.append(ring)
    faces = []
    for i in range(len(path) - 1):
        for k in range(sides):
            faces.append(bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k])))
    tip = bm.verts.new(path[-1] + (path[-1] - path[-2]).normalized() * radii[-1])
    for k in range(sides):
        faces.append(bm.faces.new((rings[-1][k], rings[-1][(k + 1) % sides], tip)))
    for f in faces:
        for l in f.loops:
            l[uvl].uv = uv if uv is not None else body_uv(l.vert.co)
    return [v for ring in rings for v in ring] + [tip]


def sucker(bm, uvl, center, normal, r):
    """빨판 하나: 다리 면에서 살짝 솟은 테 + 가운데 오목한 구멍. 닫힌 조각이라 면 방향이 바르게 맞는다."""
    n = normal.normalized()
    a = n.orthogonal().normalized()
    b = n.cross(a)
    seg = 8
    base, rim, inner = [], [], []
    for k in range(seg):
        ang = 2 * math.pi * k / seg
        off = a * math.cos(ang) + b * math.sin(ang)
        base.append(bm.verts.new(center - n * r * 0.5 + off * r * 1.05))
        rim.append(bm.verts.new(center + n * r * 0.42 + off * r * 0.92))
        inner.append(bm.verts.new(center + n * r * 0.3 + off * r * 0.5))
    bot = bm.verts.new(center - n * r * 0.55)
    pit = bm.verts.new(center + n * r * 0.05)
    faces = []
    for k in range(seg):
        k1 = (k + 1) % seg
        faces.append(bm.faces.new((base[k], base[k1], rim[k1], rim[k])))
        faces.append(bm.faces.new((rim[k], rim[k1], inner[k1], inner[k])))
        faces.append(bm.faces.new((inner[k], inner[k1], pit)))
        faces.append(bm.faces.new((base[k1], base[k], bot)))
    for f in faces:
        for l in f.loops:
            l[uvl].uv = SUCKER_UV
    return base + rim + inner + [bot, pit]


def arm_paths(d):
    """다리 경로: [(점 리스트, 굵기 리스트, 빨판 쪽 방향 리스트, 빨판 줄 수)].

    거북이처럼 한 걸음씩 나아가며 방향을 튼다: phi = 앞(-Y)과 바깥(방사) 사이 각, psi = 옆으로 꿈틀.
    문어는 바깥으로 벌어졌다가 앞으로 뻗고, 끝 1/4이 안쪽으로 말린다. 오징어는 앞으로 모여 원뿔이 된다."""
    out = []
    t_ = d['type']
    octo, squid = t_ == 'O', t_ == 'S'
    prof = profile(d)
    y_base = prof[0][0] + 0.012
    zc0 = prof[0][3]
    ring_r = 0.05 if octo else 0.022 if squid else 0.026
    fwd = V((0, -1, 0))
    for i in range(8):
        a = (i + 0.5) * 2 * math.pi / 8
        radial = V((math.cos(a), 0, math.sin(a)))
        lat = fwd.cross(radial)
        base = V((0, y_base, zc0)) + radial * ring_r
        lower = -math.sin(a)     # 아래쪽 다리일수록 1 (오징어는 배 쪽 다리가 길다)
        if octo:
            L = 0.5 + 0.05 * math.sin(i * 2.3)
            phi0, phi1 = 1.05, 0.12
            curl = (2.6 + 0.6 * math.sin(i * 1.7)) * (1 if i % 3 else -0.6)
            r0, r1 = (0.03 if d['id'] != 'giantoctopus' else 0.034), 0.0016
        elif squid:
            L = 0.15 + 0.035 * (lower + 1) / 2
            phi0, phi1 = 0.35, -0.12
            curl = 0.25
            r0, r1 = 0.0145, 0.0012
        else:
            L = 0.13 + 0.03 * (lower + 1) / 2
            phi0, phi1 = 0.4, -0.1
            curl = 0.35
            r0, r1 = 0.018, 0.0014
        pts, rad, oral = [base], [], []
        p = V(base)
        ds = L / (ARM_N - 1)
        for k in range(ARM_N):
            t = k / (ARM_N - 1)
            # 뿌리에서 바깥으로 벌어졌다가 앞으로 (phi0 → phi1), 끝은 안쪽으로 말린다
            ph = phi1 + (phi0 - phi1) * math.exp(-t / 0.18)
            ph -= curl * max(0.0, (t - 0.68) / 0.32) ** 1.6
            psi = (0.22 if octo else 0.05) * math.sin(2 * math.pi * t * 1.2 + i * 1.3) * t
            h = fwd * (math.cos(ph) * math.cos(psi)) + radial * (math.sin(ph) * math.cos(psi)) + lat * math.sin(psi)
            if not octo:
                h += V((0, 0, -0.12 * t))   # 오징어 다리 묶음은 끝이 살짝 아래로 처진다
            o = fwd * math.sin(ph) - radial * math.cos(ph)  # 빨판 쪽 = 안쪽
            if k > 0:
                p = p + h.normalized() * ds
                pts.append(V(p))
            rad.append(r1 + (r0 - r1) * (1 - t) ** (1.1 if octo else 0.9))
            oral.append(o)
        out.append((pts, rad, oral, 2))
    if t_ == 'S' or d.get('tentacles'):
        # 촉완 2개: 배 쪽 다리 사이에서 나와 길게 뻗고, 끝 1/4이 넓적한 곤봉 (빨판이 몰려 있다)
        reach = 0.33 if squid else 0.27
        for s in (-1, 1):
            base = V((s * 0.012, y_base, zc0 - 0.012))
            pts, rad, oral = [], [], []
            for k in range(ARM_N):
                t = k / (ARM_N - 1)
                pts.append(base + V((s * 0.025 * math.sin(t * math.pi * 0.8), -reach * t, -0.035 * math.sin(t * math.pi * 0.9) + 0.01 * math.sin(t * 7 + s))))
                club = math.exp(-((t - 0.86) / 0.09) ** 2)
                rad.append(0.0055 * (1 - 0.35 * t) + 0.0075 * club + 0.0008)
                oral.append(V((-s * 0.3, 0, 1)).normalized())
            out.append((pts, rad, oral, 1))
    return out


def build(d):
    """Body, Fins 오브젝트와 다리 경로(뼈용), 다리별 버텍스, 막 버텍스를 만든다."""
    octo = d['type'] == 'O'
    squid = d['type'] == 'S'
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UV')
    prof = loft_body(bm, uvl, d)

    # 눈: 눈두덩에 반쯤 묻힌다
    ey, ea, er = eye_spot(d)
    for s in (1, -1):
        c = surface_point(d, prof, ey, s * ea)
        rx, rz, zc = catmull(prof, ey)
        nrm = V((math.sin(s * ea) / max(rx, 1e-3), 0, -math.cos(s * ea) / max(rz, 1e-3))).normalized()
        eye(bm, uvl, c + nrm * (0.05 * er) + V((0, 0, 0)), er * (1.0 if not squid else 1.1), s)

    # 깔때기(물을 뿜는 관): 문어는 머리 옆(왼쪽), 오징어·갑오징어는 머리 밑에서 앞을 향한다
    if octo:
        fy, fa = 0.07, -1.6
    else:
        fy, fa = (-0.07 if squid else -0.095), 0.0
    c0 = surface_point(d, prof, fy, fa, -0.012)
    dirn = (V((0, -1, 0)) + (V((math.sin(fa), 0, -math.cos(fa))) * 0.35)).normalized()
    fpath = [c0 + dirn * (0.05 * k / 7) for k in range(8)]
    tube(bm, uvl, fpath, [0.016 - 0.006 * k / 7 for k in range(8)], sides=10)

    paths = arm_paths(d)
    arm_verts = []
    for pts, rad, oral, rows in paths:
        vs = tube(bm, uvl, pts, rad, oral, flat=0.72 if octo else 0.85)
        # 빨판: 안쪽 면에 두 줄(촉완은 곤봉에만). 끝으로 갈수록 작고 촘촘
        fr = frame_along(pts)
        t = 0.04 if rows == 2 else 0.72
        row = 0
        while t < 0.95:
            k = int(t * (len(pts) - 1))
            tan = fr[k][0]
            o = (oral[k] - tan * oral[k].dot(tan)).normalized()
            side = tan.cross(o).normalized()
            r = rad[k]
            sr = r * (0.42 if octo else 0.36)
            off = side * r * 0.42 * ((row % 2) * 2 - 1) if rows == 2 else side * r * 0.3 * ((row % 2) * 2 - 1)
            vs += sucker(bm, uvl, pts[k] + o * r * (0.72 if octo else 0.85) + off, o, sr)
            t += max(0.012, 2.1 * sr / sum(((pts[q + 1] - pts[q]).length for q in range(len(pts) - 1))))
            row += 1
        arm_verts.append(vs)

    # 문어 다리 사이 막: 뿌리 쪽 1/4을 잇는 얇은 막, 가장자리가 다리 사이로 오목하게 파인다 (닫힌 얇은 판)
    web = []
    if octo:
        K, C, th = 9, 8, 0.0025
        for i in range(8):
            j = (i + 1) % 8
            Pi, Pj = paths[i][0], paths[j][0]
            tw = 0.26
            grid_top, grid_bot = [], []
            for kk in range(K + 1):
                rt, rb = [], []
                for cc in range(C + 1):
                    c = cc / C
                    scal = 1 - 0.45 * math.sin(math.pi * c)
                    t = tw * kk / K * scal
                    q = t * (ARM_N - 1)
                    q0 = min(int(q), ARM_N - 2)
                    f = q - q0
                    a = Pi[q0].lerp(Pi[q0 + 1], f)
                    b = Pj[q0].lerp(Pj[q0 + 1], f)
                    p = a.lerp(b, c)
                    # 막은 다리 사이에서 안쪽(축)으로 살짝 처진다
                    axis_pt = V((0, p.y, prof[0][3]))
                    p = p.lerp(axis_pt, 0.22 * math.sin(math.pi * c) * (kk / K))
                    nrm = (b - a).cross(V((0, -1, 0))).normalized()
                    vt = bm.verts.new(p + nrm * th)
                    vb = bm.verts.new(p - nrm * th)
                    rt.append(vt)
                    rb.append(vb)
                    web.append((vt, i, j, c, kk / K))
                    web.append((vb, i, j, c, kk / K))
                grid_top.append(rt)
                grid_bot.append(rb)
            faces = []
            for kk in range(K):
                for cc in range(C):
                    faces.append(bm.faces.new((grid_top[kk][cc], grid_top[kk][cc + 1], grid_top[kk + 1][cc + 1], grid_top[kk + 1][cc])))
                    faces.append(bm.faces.new((grid_bot[kk][cc], grid_bot[kk + 1][cc], grid_bot[kk + 1][cc + 1], grid_bot[kk][cc + 1])))
            # 테두리를 닫는다 (뒷면을 버리는 Unity에서도 양쪽이 보이고, 면 방향 맞추기가 바르게 된다)
            edges = [(grid_top[kk][0], grid_top[kk + 1][0], grid_bot[kk + 1][0], grid_bot[kk][0]) for kk in range(K)]
            edges += [(grid_top[kk + 1][C], grid_top[kk][C], grid_bot[kk][C], grid_bot[kk + 1][C]) for kk in range(K)]
            edges += [(grid_top[0][cc + 1], grid_top[0][cc], grid_bot[0][cc], grid_bot[0][cc + 1]) for cc in range(C)]
            edges += [(grid_top[K][cc], grid_top[K][cc + 1], grid_bot[K][cc + 1], grid_bot[K][cc]) for cc in range(C)]
            for e in edges:
                faces.append(bm.faces.new(e))
            for f in faces:
                for l in f.loops:
                    l[uvl].uv = body_uv(l.vert.co)

    me = bpy.data.meshes.new('Body')
    bm.verts.index_update()
    arm_idx = [[v.index for v in vs] for vs in arm_verts]
    web_idx = [(v.index, i, j, c, t) for v, i, j, c, t in web]
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    body = bpy.data.objects.new('Body', me)
    bpy.context.scene.collection.objects.link(body)

    fins = build_fins(d, prof)
    return body, fins, paths, (arm_idx, web_idx)


def build_fins(d, prof):
    """갑오징어·무늬오징어 치마 / 오징어 화살촉 / 주꾸미는 외투막 끝의 아주 작은 판(지느러미가 없지만 머티리얼 슬롯을 맞추려고)."""
    octo = d['type'] == 'O'
    squid = d['type'] == 'S'
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UV')
    nu, nv = 32, 4
    fw = d.get('fin_w', 1.0)
    for side in ((1, -1) if not octo else (1,)):
        grid = []
        for j in range(nv + 1):
            row = []
            for i in range(nu + 1):
                t = i / nu
                if octo:
                    p = V((side * 0.005 * j, 0.47 + 0.03 * t, 0.03))
                elif squid:
                    # 외투막 끝 40%의 화살촉 지느러미: 앞 가장자리는 볼록, 가장 넓은 곳이 앞쪽 60%, 뒤로 뾰족하게 모인다
                    y = 0.25 + 0.25 * t
                    rx, rz, zc = catmull(prof, y)
                    w = 0.105 * (math.sin(math.pi * min(1.0, t / 0.62) / 2) ** 0.7 if t < 0.62 else ((1 - t) / 0.38) ** 0.9) + 0.002
                    p = V((side * (rx * 0.92 + w * j / nv), y + 0.03 * (j / nv) * (1 - t), zc + 0.004 * j / nv))
                else:
                    # 외투막 거의 전체를 두르는 치마. 가장자리가 잔물결로 일렁인다
                    y = -0.07 + 0.56 * t
                    rx, rz, zc = catmull(prof, y)
                    w = (0.036 * fw) * math.sin(math.pi * min(1.0, t * 1.04)) ** 0.45 + 0.003
                    p = V((side * (rx * 0.96 + w * j / nv), y, zc + 0.006 * math.sin(t * 26) * (j / nv) ** 1.5))
                row.append(bm.verts.new(p))
            grid.append(row)
        for j in range(nv):
            for i in range(nu):
                f = bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]) if side > 0 else
                                 (grid[j][i], grid[j + 1][i], grid[j + 1][i + 1], grid[j][i + 1]))
                for l, (ii, jj) in zip(f.loops, ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)) if side > 0 else ((i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j))):
                    l[uvl].uv = (0.875 + 0.125 * ii / nu, 0.5 + 0.5 * jj / nv)
    me = bpy.data.meshes.new('Fins')
    bm.to_mesh(me)
    bm.free()
    fins = bpy.data.objects.new('Fins', me)
    bpy.context.scene.collection.objects.link(fins)
    return fins


def bone_cuts():
    return [round(k * (ARM_N - 1) / ARM_BONES) for k in range(ARM_BONES + 1)]


def add_arm_bones(arm_obj, paths):
    """다리마다 뼈 4개 (머리에 붙는다)."""
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_obj.data.edit_bones
    head = eb['Head']
    names = []
    idx = bone_cuts()
    for i, path in enumerate(paths):
        pts = path[0]
        prev = head
        for j in range(ARM_BONES):
            b = eb.new(f'Arm{i}_{j}')
            b.head = pts[idx[j]]
            b.tail = pts[idx[j + 1]]
            b.parent = prev
            b.use_connect = j > 0
            prev = b
            names.append(b.name)
    bpy.ops.object.mode_set(mode='OBJECT')
    return names


def _arm_bone_weights(t):
    """다리 경로 비율 t(0~1) → [(뼈 번호, 가중치)] (이웃 뼈와 섞는다)."""
    x = t * ARM_BONES - 0.5
    j0 = max(0, min(ARM_BONES - 1, int(math.floor(x))))
    f = max(0.0, min(1.0, x - j0))
    j1 = min(ARM_BONES - 1, j0 + 1)
    return [(j0, 1.0)] if j1 == j0 else [(j0, 1 - f), (j1, f)]


def weight_arms(body, paths, idx):
    """다리·빨판 버텍스는 그 다리 뼈에만, 막은 양옆 다리의 뿌리 쪽 뼈에 나눠서."""
    arm_idx, web_idx = idx
    me = body.data
    all_arm = [vi for ids in arm_idx for vi in ids] + [w[0] for w in web_idx]
    for g in body.vertex_groups:
        if not g.name.startswith('Arm'):
            g.remove(all_arm)   # 몸통 사슬 가중치는 빼고 다리 뼈에만
    groups = {}

    def grp(i, j):
        key = f'Arm{i}_{j}'
        if key not in groups:
            groups[key] = body.vertex_groups.get(key) or body.vertex_groups.new(name=key)
        return groups[key]
    for i, ids in enumerate(arm_idx):
        pts = paths[i][0]
        for vi in ids:
            co = me.vertices[vi].co
            k = min(range(len(pts)), key=lambda q: (pts[q] - co).length_squared)
            for j, w in _arm_bone_weights(k / (len(pts) - 1)):
                grp(i, j).add([vi], w, 'REPLACE')
    for vi, i, j, c, t in web_idx:
        tt = t * 0.26
        acc = {}
        for arm_i, wa in ((i, 1 - c), (j, c)):
            for b, w in _arm_bone_weights(tt):
                acc[(arm_i, b)] = acc.get((arm_i, b), 0.0) + wa * w
        for (arm_i, b), w in acc.items():
            if w > 1e-4:
                grp(arm_i, b).add([vi], w, 'REPLACE')
