"""
배 (2026-10-20, 사진 대조로 Kenney 장난감 배를 바꾼다). build_props.py가 불러 PROPS에 더한다. 길이는 실제 미터, 위 = +Z, 뱃머리 = -Y (Unity +Z).
기준점 = 용골 바닥 가운데 (Kenney 배와 같다 — 맵 코드의 물 높이 오프셋을 그대로 쓴다).

  boat_kr      한국 연안 어선 (사진: 통영·서귀포 항구) — 흰 선체, 흘수선 아래 붉은 방오 도료 + 검은 띠, 뱃전 파란 띠, 높이 들린 뱃머리,
               조타실(창 띠·문·지붕 난간), 흰 돛대·레이더 막대·안테나, 집어등 전구 줄, 뱃전 타이어, 오색기, 고물 보조 돛, 그물·바구니·부표
  boat_speed   모터보트 (사진: 콘솔 보트) — 깊은 V 선체, 파란 띠, 가운데 콘솔·휜 앞유리·키, 스테인리스 난간, 선외기
  boat_rib     고무보트 (사진: 회색 튜브 보트) — U자 튜브 + 고물 원뿔, 손잡이 줄, 바닥판, 나무 가로대, 작은 선외기 (게임 고무보트 주황)
  boat_row     나무 노 젓는 배 — 겹붙인 판자(클링커) 단, 가로 좌석 셋, 노걸이에 걸친 노 둘, 칠한 겉 + 나무 속
  boat_caique  그리스 카이크 (크레타 항구) — 둥근 나무 선체, 높은 이물 기둥, 흰 바탕에 파랑·빨강 띠, 짧은 돛대·붐, 그물 더미
  yacht        세일 요트 (센토사 마리나) — 흰 선체·낮은 선실 창, 높은 돛대, 접은 돛 덮개(파랑), 앞 지삭, 난간
  boat_dinghy  딩기 선체 (게임 돛단배: 돛·붐은 코드가 바람 따라 돌린다) — 흰 선체 + 남색 띠, 나무 갑판, 센터보드·키
"""
import math

import bmesh
import build_icons as B
from mathutils import Vector as V

mat, cyl, box, sphere, tube = B.mat, B.cyl, B.box, B.sphere, B.tube


def smooth01(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def hull(L, Bm, D, mats, wl=0.5, bow_rise=0.8, stern_rise=0.2, transom=0.75, round_=2.2, flare=0.12, forefoot=0.18,
         clinker=0, nsec=30, nside=18, band=0.3, boot=0.1, deck_mat=None, deck_drop=0.15):
    """
    선체 로프트. 단면 nsec+1개 (t 0 = 뱃머리 -Y, 1 = 고물). 단면은 용골(q 0) → 뱃전(q 1).
    mats = (방오 바닥, 흘수 검은 띠, 선체 옆, 뱃전 띠). 흘수선 높이 wl, 뱃전 띠 band m.
    round_: 가운데 단면 둥글기 (1 = V, 클수록 U). 뱃머리로 갈수록 V. clinker > 0이면 판자 단 수 (겹붙인 나무배).
    반환: (선체 물체, 뱃전 점 목록 [(왼쪽, 오른쪽) per t], 갑판 높이 함수)
    """
    bm = bmesh.new()
    rings = []
    sheer_pts = []
    for i in range(nsec + 1):
        t = i / nsec
        y = -L / 2 + L * t
        # 반폭: 뱃머리는 뾰족, 가운데 최대, 고물은 트랜섬 폭으로
        if t < 0.45:
            b = (t / 0.45) ** 0.62
        elif t < 0.8:
            b = 1.0
        else:
            b = 1.0 - (1.0 - transom) * ((t - 0.8) / 0.2) ** 1.6
        b = max(0.02, b) * Bm / 2
        sheer = D + bow_rise * (1 - t) ** 2.4 + stern_rise * t ** 3
        keel = forefoot * D * max(0.0, 1 - t / 0.14) ** 1.5 * 3.0 if t < 0.14 else 0.0
        keel = min(keel, sheer * 0.7)
        p = 1.0 + (round_ - 1.0) * smooth01(0.0, 0.4, t)       # 뱃머리 V → 가운데 U
        ring = []
        for k in range(nside + 1):
            a = k / nside
            side = -1 if a < 0.5 else 1
            q = abs(a - 0.5) * 2
            if clinker:
                # 판자 단: 위 판이 아래 판을 덮어 바깥으로 살짝 튀어나온 계단
                step = (q * clinker) % 1.0
                q2 = q + (0.04 / clinker) * smooth01(0.0, 0.15, step)
            else:
                q2 = q
            x = b * (1 - (1 - min(q2, 1.0)) ** p) * (1 + flare * q * (1 - t))
            z = keel + (sheer - keel) * q
            ring.append(bm.verts.new((side * x, y, z)))
        rings.append(ring)
        sheer_pts.append((V(ring[0].co), V(ring[-1].co)))
    for i in range(nsec):
        for k in range(nside):
            f = bm.faces.new((rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]))
            cz = f.calc_center_median().z
            topz = (rings[i][0].co.z + rings[i + 1][0].co.z) / 2
            f.material_index = 0 if cz < wl - 0.02 else 1 if cz < wl + boot else 3 if cz > topz - band else 2
    tf = bm.faces.new(rings[-1][::-1])     # 트랜섬
    tf.material_index = 2
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    o = B.obj_from_bm(bm, 'Hull', mats[0])
    for m in mats[1:]:
        o.data.materials.append(m)
    parts = [o]
    # 갑판: 뱃전보다 deck_drop 낮은 판 (뱃전 안쪽)
    if deck_mat is not None:
        bm2 = bmesh.new()
        left, right = [], []
        for i, (l, r) in enumerate(sheer_pts):
            t = i / nsec
            dz = l.z - deck_drop
            left.append(bm2.verts.new((l.x * 0.96, l.y, dz)))
            right.append(bm2.verts.new((r.x * 0.96, r.y, dz)))
        for i in range(nsec):
            bm2.faces.new((left[i], left[i + 1], right[i + 1], right[i]))
        bmesh.ops.recalc_face_normals(bm2, faces=list(bm2.faces))
        for f in bm2.faces:
            if f.normal.z < 0:
                f.normal_flip()
        parts.append(B.obj_from_bm(bm2, 'Deck', deck_mat))
    return parts, sheer_pts


def rail_along(sheer_pts, side, r, m, dz=0.0, inset=1.0, t0=0.0, t1=1.0):
    n = len(sheer_pts)
    pts = []
    for i in range(int(t0 * (n - 1)), int(t1 * (n - 1)) + 1):
        p = sheer_pts[i][0 if side < 0 else 1]
        pts.append(V((p.x * inset, p.y, p.z + dz)))
    return tube(pts, r, m, sides=8)


def sheer_at(sheer_pts, t, side):
    n = len(sheer_pts) - 1
    f = t * n
    i = min(int(f), n - 1)
    a, b = sheer_pts[i][0 if side < 0 else 1], sheer_pts[i + 1][0 if side < 0 else 1]
    return a.lerp(b, f - i)


def tri_flag(p, m, size=0.45, yaw=0.0):
    bm = bmesh.new()
    d = V((math.sin(yaw), math.cos(yaw), 0))
    a = bm.verts.new(p)
    b = bm.verts.new(p + V((0, 0, -size * 0.6)))
    c = bm.verts.new(p + d * size + V((0, 0, -size * 0.3)))
    bm.faces.new((a, b, c))
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.01)   # 양면 (같은 꼭짓점에 뒤집은 면은 못 만든다)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    return flat(B.obj_from_bm(bm, 'Flag', m))


def flat(ob):
    """얇은 판(깃발·돛)은 평면 음영으로: 부드러운 음영이면 앞뒤 법선이 평균돼 검게 보였다."""
    for p in ob.data.polygons:
        p.use_smooth = False
    return ob


# ---------------------------------------------------------------- 한국 연안 어선

def boat_kr(_):
    L, Bm, D = 10.0, 3.1, 1.55
    red = mat('#9e2f26', 0.7)
    blk = mat('#1e1e20', 0.6)
    white = mat('#eeeeea', 0.45)
    blue = mat('#2457a8', 0.4)
    deckm = mat('#8e9290', 0.8)
    glass = mat('#1d2a33', 0.15)
    tire = mat('#161616', 0.85)
    steel = mat('#c8ccd0', 0.35, 0.6)
    o, sp = hull(L, Bm, D, (red, blk, white, blue), wl=0.55, bow_rise=0.95, stern_rise=0.25, transom=0.8, round_=2.4, flare=0.18,
                 deck_mat=deckm, deck_drop=0.32, band=0.32)
    deck_z = D - 0.32
    # 뱃전 윗테 (파란 굵은 테) + 이물 기둥
    for s in (-1, 1):
        o.append(rail_along(sp, s, 0.07, blue, dz=0.02))
    o.append(tube([V((0, -L / 2 + 0.05, D + 0.95)), V((0, -L / 2 + 0.25, D + 1.25))], 0.07, blue, sides=8))
    # 조타실: 가운데보다 조금 뒤 (사진: 앞에 넓은 작업 갑판), 아래 흰 벽 + 위 창 띠 + 지붕 차양
    wy0, wy1 = 0.4, 3.0
    wy = (wy0 + wy1) / 2
    ww, wh = 2.15, 1.95
    o.append(box((ww, wy1 - wy0, wh), white, loc=(0, wy, deck_z + wh / 2), bevel=0.05))
    o.append(box((ww + 0.02, wy1 - wy0 + 0.02, 0.62), glass, loc=(0, wy, deck_z + wh - 0.48)))
    for k in range(4):     # 앞 창 칸살
        x = -ww / 2 + ww * (k + 1) / 5
        o.append(box((0.07, 0.06, 0.64), white, loc=(x, wy0 - 0.02, deck_z + wh - 0.48)))
    for s in (-1, 1):      # 옆 창 칸살 · 옆문
        for k in range(3):
            o.append(box((0.06, 0.07, 0.64), white, loc=(s * (ww / 2 + 0.01), wy0 + (wy1 - wy0) * (k + 1) / 4, deck_z + wh - 0.48)))
        o.append(box((0.04, 0.7, 1.35), blue, loc=(s * (ww / 2 + 0.02), wy1 - 0.55, deck_z + 0.72)))
    o.append(box((ww + 0.5, wy1 - wy0 + 0.6, 0.1), white, loc=(0, wy - 0.15, deck_z + wh + 0.05), bevel=0.02))
    o.append(box((ww + 0.52, 0.08, 0.16), blue, loc=(0, wy0 - 0.46, deck_z + wh)))
    # 지붕 난간 · 레이더 막대 · 돛대 · 안테나 · 서치라이트
    rz = deck_z + wh + 0.1
    for s in (-1, 1):
        o.append(tube([V((s * (ww / 2 + 0.15), wy0, rz + 0.45)), V((s * (ww / 2 + 0.15), wy1 + 0.1, rz + 0.45))], 0.025, steel, sides=6))
        for y in (wy0, wy, wy1):
            o.append(cyl(0.02, 0.02, 0.45, steel, loc=(s * (ww / 2 + 0.15), y, rz + 0.22), seg=6))
    mast_top = V((0, wy0 + 0.4, rz + 3.6))
    o.append(cyl(0.07, 0.05, 3.6, white, loc=(0, wy0 + 0.4, rz + 1.8), seg=10))
    o.append(box((1.6, 0.08, 0.08), white, loc=(0, wy0 + 0.4, rz + 2.6)))
    o.append(box((0.9, 0.22, 0.12), white, loc=(0, wy0 + 0.4, rz + 3.1)))          # 레이더
    for x in (-0.7, 0.7):
        o.append(cyl(0.015, 0.01, 1.4, white, loc=(x, wy0 + 0.4, rz + 3.3), seg=5))   # 안테나
    o.append(cyl(0.12, 0.12, 0.22, steel, loc=(0.6, wy0 - 0.1, rz + 0.25), rot=(math.pi / 2, 0, 0), seg=10))
    # 오색기 (돛대 꼭대기 → 이물로 줄에 매단 깃발)
    flag_cols = [mat(c, 0.6) for c in ('#d83a30', '#f2c230', '#2a6ad0', '#f2f0ea', '#2a9a50')]
    bow_top = V((0, -L / 2 + 0.25, D + 1.25))
    o.append(tube([mast_top, bow_top], 0.012, blk, sides=4))
    for k in range(7):
        p = mast_top.lerp(bow_top, (k + 1) / 8)
        o.append(tri_flag(p, flag_cols[k % 5], 0.38, 0.0))
    # 집어등 전구 줄: 돛대 가로대 → 이물·고물로 늘어진 전구 (사진: 흰 전구가 줄줄이)
    bulb = mat('#f6f2dc', 0.2)
    for end in (bow_top + V((0, 0.3, -0.3)), V((0, L / 2 - 0.2, D + 0.6))):
        a = V((0, wy0 + 0.4, rz + 2.6))
        for k in range(1, 9):
            p = a.lerp(end, k / 9) + V((0, 0, -0.35 * math.sin(math.pi * k / 9)))
            o.append(sphere(0.11, bulb, loc=tuple(p), seg=10, rings=6))
            o.append(cyl(0.05, 0.05, 0.08, blk, loc=tuple(p + V((0, 0, 0.1))), seg=8))
        o.append(tube([a, a.lerp(end, 0.5) + V((0, 0, -0.35)), end], 0.01, blk, sides=4))
    # 뱃전 타이어 방현재 (사진: 검은 타이어가 줄지어 걸림)
    for s in (-1, 1):
        for t in (0.3, 0.45, 0.6, 0.75):
            p = sheer_at(sp, t, s)
            o.append(B.torus(0.3, 0.11, tire, loc=(p.x + s * 0.12, p.y, p.z - 0.45), rot=(0, math.pi / 2, 0), seg=16, mseg=8))
            o.append(tube([V((p.x + s * 0.02, p.y, p.z + 0.02)), V((p.x + s * 0.12, p.y, p.z - 0.17))], 0.015, blk, sides=4))
    # 앞 작업 갑판: 그물 더미, 플라스틱 바구니, 양망기 드럼, 부표
    o.append(B.lumpy(0.55, mat('#2f6a3a', 0.9), loc=(0.3, -1.9, deck_z + 0.3), scale=(1.3, 1.0, 0.5), amp=0.2, freq=6.0, seed=4))
    for k, c in enumerate(('#e8722a', '#2a6ad0', '#e8722a')):
        o.append(box((0.6, 0.45, 0.35), mat(c, 0.5), loc=(-0.75, -1.0 - k * 0.55, deck_z + 0.18 + (k == 1) * 0.35), bevel=0.02))
    o.append(cyl(0.28, 0.28, 0.6, steel, loc=(0.75, -0.3, deck_z + 0.5), rot=(0, math.pi / 2, 0), seg=14))
    o.append(box((0.3, 0.3, 0.5), blue, loc=(0.75, -0.3, deck_z + 0.25)))
    for k in range(3):
        o.append(sphere(0.2, mat('#f07a20', 0.5), loc=(-0.2 + k * 0.3, -3.0, deck_z + 0.2), seg=12, rings=8))
    # 고물: 보조 돛 (사진: 고물에 작은 세모 돛) + 고물 난간
    o.append(cyl(0.05, 0.04, 2.6, white, loc=(0, L / 2 - 0.5, deck_z + 1.3), seg=8))
    bm = bmesh.new()
    a, b2, c = bm.verts.new((0, L / 2 - 0.45, deck_z + 2.5)), bm.verts.new((0, L / 2 - 0.45, deck_z + 0.6)), bm.verts.new((0, L / 2 + 0.6, deck_z + 0.75))
    bm.faces.new((a, b2, c))
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.02)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    o.append(flat(B.obj_from_bm(bm, 'MizzenSail', mat('#c83a2c', 0.7))))
    o.append(tube([sheer_at(sp, 0.86, -1) + V((0, 0, 0.6)), V((0, L / 2 - 0.05, D + 0.85)), sheer_at(sp, 0.86, 1) + V((0, 0, 0.6))], 0.025, steel, sides=6))
    return o


# ---------------------------------------------------------------- 모터보트

def boat_speed(pid):
    L, Bm, D = 5.2, 2.05, 0.95
    red = mat('#2b2b30', 0.5)
    blk = mat('#16161a', 0.5)
    white = mat('#f2f2f0', 0.25)
    navy = mat('#1e3a78', 0.35)
    deckm = mat('#d2cdbf', 0.75)
    glass = mat('#2a3a44', 0.05)
    steel = mat('#d4d8dc', 0.2, 0.8)
    o, sp = hull(L, Bm, D, (red, navy, white, navy), wl=0.3, bow_rise=0.35, stern_rise=0.0, transom=0.9, round_=1.6, flare=0.15,
                 deck_mat=deckm, deck_drop=0.28, band=0.12, boot=0.18)
    dz = D - 0.28
    # 뱃전 테 (흰 갑판 테두리) + 앞 갑판 덮개
    for s in (-1, 1):
        o.append(rail_along(sp, s, 0.05, white))
    bm = bmesh.new()
    pts = [sheer_at(sp, t, s) for s in (-1,) for t in (0.0, 0.12, 0.25, 0.32)] + [sheer_at(sp, t, 1) for t in (0.32, 0.25, 0.12)]
    vs = [bm.verts.new(p + V((0, 0, 0.01))) for p in pts]
    bm.faces.new(vs)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    o.append(B.obj_from_bm(bm, 'BowDeck', white))
    # 가운데 콘솔 (2026-10-27 다시 — 예전엔 흰 상자·파란 상자 좌석이라 가까이서 보면 도형 같았다, 사용자 지적):
    # 앞이 비스듬히 깎인 FRP 콘솔(둥근 모서리, 짙은 계기판 위 계기 둘·스로틀 레버), 휜 앞유리 + 스테인리스 테, 키
    cb = bmesh.new()
    cw, cd, ch = 0.82, 0.72, 0.88
    prof = [(-cd / 2, 0.0), (cd / 2, 0.0), (cd / 2, ch), (-cd / 2 + 0.22, ch), (-cd / 2, ch - 0.3)]   # (y, z) 앞(-y) 위가 깎였다
    for x in (-cw / 2, cw / 2):
        pass
    ring = [[cb.verts.new((x, y, z)) for (y, z) in prof] for x in (-cw / 2, cw / 2)]
    n = len(prof)
    for i in range(n):
        cb.faces.new((ring[0][i], ring[0][(i + 1) % n], ring[1][(i + 1) % n], ring[1][i]))
    cb.faces.new(ring[0][::-1])
    cb.faces.new(ring[1])
    bmesh.ops.recalc_face_normals(cb, faces=list(cb.faces))
    bmesh.ops.bevel(cb, geom=list(cb.edges), offset=0.05, segments=3, affect='EDGES', profile=0.5)
    con = B.obj_from_bm(cb, 'Console', white)
    con.location = (0, 0.1, dz)
    o.append(con)
    o.append(box((cw - 0.12, 0.3, 0.02), blk, loc=(0, 0.1 - cd / 2 + 0.17, dz + ch - 0.13), rot=(-0.94, 0, 0)))          # 계기판
    for x in (-0.16, 0.16):
        o.append(cyl(0.065, 0.065, 0.02, steel, loc=(x, 0.1 - cd / 2 + 0.15, dz + ch - 0.11), rot=(-0.94 + math.pi / 2, 0, 0), seg=14))
        o.append(cyl(0.05, 0.05, 0.022, glass, loc=(x, 0.1 - cd / 2 + 0.145, dz + ch - 0.105), rot=(-0.94 + math.pi / 2, 0, 0), seg=14))
    o.append(box((0.05, 0.05, 0.22), blk, loc=(cw / 2 + 0.03, 0.15, dz + 0.7), rot=(0.3, 0, 0)))                            # 스로틀
    o.append(B.sphere(0.04, blk, loc=(cw / 2 + 0.03, 0.12, dz + 0.81)))
    bm = bmesh.new()
    rows = []
    for k in range(7):
        a = (k / 6 - 0.5) * 2.2
        x, y = math.sin(a) * 0.5, -math.cos(a) * 0.5 + 0.2
        rows.append((bm.verts.new((x, y - 0.2, dz + 0.85)), bm.verts.new((x * 0.9, y - 0.05, dz + 1.25))))
    for k in range(6):
        bm.faces.new((rows[k][0], rows[k + 1][0], rows[k + 1][1], rows[k][1]))
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.015)
    o.append(B.obj_from_bm(bm, 'Windshield', glass))
    o.append(tube([((math.sin((k / 6 - 0.5) * 2.2) * 0.45), -math.cos((k / 6 - 0.5) * 2.2) * 0.45 + 0.15, dz + 1.25) for k in range(7)], 0.012, steel, sides=6))
    o.append(B.torus(0.17, 0.02, blk, loc=(0.0, 0.0, dz + ch + 0.02), rot=(math.radians(55), 0, 0), seg=16, mseg=6))
    # 좌석: 흰 FRP 받침 + 짙은 남색 쿠션(둥근 모서리, 가운데 이음 홈) + 등받이 쿠션
    def seat(cx, cy, w, d, back):
        o.append(box((w, d, 0.36), white, loc=(cx, cy, dz + 0.18), bevel=0.05))
        for k in (-1, 1):
            o.append(box((w / 2 - 0.03, d - 0.06, 0.1), navy, loc=(cx + k * w / 4, cy, dz + 0.41), bevel=0.04))
        if back:
            o.append(box((w - 0.06, 0.1, 0.4), navy, loc=(cx, cy + d / 2 - 0.05, dz + 0.68), rot=(-0.18, 0, 0), bevel=0.04))
            o.append(box((w, 0.08, 0.5), white, loc=(cx, cy + d / 2 + 0.03, dz + 0.6), bevel=0.03))
    seat(0, 1.05, 1.1, 0.55, True)                                      # 콘솔 뒤 키잡이 의자
    seat(0, 2.0, Bm * 0.82, 0.5, False)                                  # 고물 벤치
    o.append(box((Bm * 0.82 - 0.06, 0.1, 0.36), navy, loc=(0, 2.24, dz + 0.62), rot=(-0.15, 0, 0), bevel=0.04))
    # 클리트 (뱃머리·고물 양쪽)
    for t in (0.06, 0.9):
        for side in (-1, 1):
            p = sheer_at(sp, t, side) + V((-side * 0.1, 0, 0.03))
            o.append(box((0.04, 0.16, 0.03), steel, loc=tuple(p), bevel=0.01))
    # 스테인리스 앞 난간 (사진: 뱃머리를 감싼 관 난간)
    for s in (-1, 1):
        pts = [sheer_at(sp, t, s) + V((-s * 0.06, 0, 0.32)) for t in (0.02, 0.1, 0.2, 0.3)]
        o.append(tube(pts, 0.018, steel, sides=6))
        for t in (0.1, 0.2, 0.3):
            p = sheer_at(sp, t, s) + V((-s * 0.06, 0, 0))
            o.append(cyl(0.015, 0.015, 0.32, steel, loc=tuple(p + V((0, 0, 0.16))), seg=6))
    # 선외기: 둥근 흰 덮개(앞뒤로 긴 달걀꼴, 옆 띠·통풍구) + 검은 허리 + 다리 + 프로펠러
    sy = L / 2
    o.append(B.sphere(0.3, white, loc=(0, sy + 0.28, D + 0.2), scale=(0.75, 1.05, 1.0), seg=20, rings=12))
    o.append(box((0.44, 0.6, 0.06), navy, loc=(0, sy + 0.28, D + 0.12), bevel=0.02))
    for k in range(3):
        o.append(box((0.02, 0.18, 0.03), blk, loc=(0.22, sy + 0.22 + k * 0.08, D + 0.32)))
    o.append(box((0.4, 0.5, 0.18), blk, loc=(0, sy + 0.28, D - 0.1), bevel=0.04))
    o.append(box((0.14, 0.2, 1.0), blk, loc=(0, sy + 0.3, D - 0.65), bevel=0.03))
    o.append(box((0.06, 0.34, 0.03), blk, loc=(0, sy + 0.32, 0.3)))                          # 물 가르는 판
    o.append(cyl(0.05, 0.05, 0.2, blk, loc=(0, sy + 0.3, 0.08), rot=(math.pi / 2, 0, 0), seg=10))
    for k in range(3):
        a = k * 2 * math.pi / 3
        o.append(box((0.03, 0.02, 0.13), steel, loc=(math.cos(a) * 0.08, sy + 0.2, 0.08 + math.sin(a) * 0.08), rot=(0, a, 0.4)))
    return o


# ---------------------------------------------------------------- 고무보트

def boat_rib(_):
    """길이 3.4. 튜브는 뱃머리에서 U자로 돌아 양옆 고물까지, 끝은 원뿔 (사진). 바닥은 회색, 튜브는 게임의 주황."""
    tube_m = mat('#e0602a', 0.45)
    rub = mat('#4a4c50', 0.6)
    floor_m = mat('#7a7e84', 0.7)
    wood = mat('#8a6a44', 0.6)
    blk = mat('#1a1a1c', 0.5)
    white = mat('#e8e8e4', 0.3)
    rope = mat('#d8d4c8', 0.8)
    o = []
    R = 0.24
    # 튜브 중심선: 고물 왼쪽 → 뱃머리 둥글게 → 고물 오른쪽
    pts = []
    for k in range(25):
        u = k / 24
        a = math.pi * u
        if u < 0.5:
            pass
        x = -math.cos(a) * 0.62
        y = -math.sin(a) * 0.9 - 0.65 + (1 - math.sin(a)) * 0.0
        pts.append(V((x, y, R + 0.02 + 0.08 * math.sin(a))))
    left = [V((-0.62, 1.6 - i * 2.25 / 6, R + 0.02)) for i in range(7)]
    right = [V((0.62, -0.65 + i * 2.25 / 6, R + 0.02)) for i in range(7)]
    path = left[:-1] + pts + right[1:]
    o.append(tube(path, R, tube_m, sides=18, cap=False))
    # 고물 원뿔 (튜브 끝)
    for s in (-1, 1):
        c = V((s * 0.62, 1.6, R + 0.02))
        o.append(tube([c, c + V((0, 0.35, 0)), c + V((0, 0.48, 0))], lambda u: R * (1 - u) ** 0.7 + 0.02, tube_m, sides=18))
    # 튜브 아래 짙은 문지름 띠 · 손잡이 줄 (튜브 위를 따라 처진 줄)
    o.append(tube([p + V((math.copysign(0.0, p.x), 0, -R * 0.55)) for p in path], R * 0.62, rub, sides=12, cap=False))
    for s in (-1, 1):
        rp = [V((s * 0.62, y, 2 * R + 0.06 - 0.03 * abs(math.sin(y * 4)))) for y in [1.5 - i * 0.18 for i in range(13)]]
        o.append(tube(rp, 0.012, rope, sides=4))
    # 바닥판 · 가로대 · 고물판 · 선외기
    o.append(box((1.0, 3.0, 0.06), floor_m, loc=(0, 0.25, 0.06)))
    o.append(box((1.05, 0.28, 0.05), wood, loc=(0, 0.45, 0.42)))
    o.append(box((1.05, 0.08, 0.42), floor_m, loc=(0, 1.62, 0.25)))
    o.append(box((0.3, 0.38, 0.4), white, loc=(0, 1.82, 0.72), bevel=0.06))
    o.append(box((0.32, 0.4, 0.08), blk, loc=(0, 1.82, 0.5)))
    o.append(box((0.1, 0.12, 0.65), blk, loc=(0, 1.86, 0.15)))
    o.append(tube([V((0, 1.62, 0.72)), V((0, 1.2, 0.8))], 0.025, blk, sides=6))
    return o


# ---------------------------------------------------------------- 나무 노 젓는 배

def boat_row(_):
    L, Bm, D = 4.2, 1.45, 0.6
    paint = mat('#3a7ab0', 0.6)
    white = mat('#e8e2d4', 0.6)
    wood = mat('#9a7048', 0.7)
    dark = mat('#5a3e24', 0.75)
    o, sp = hull(L, Bm, D, (dark, white, paint, white), wl=0.18, bow_rise=0.22, stern_rise=0.12, transom=0.55, round_=2.0, flare=0.2,
                 clinker=5, deck_mat=wood, deck_drop=0.45, band=0.07, boot=0.04)
    for s in (-1, 1):
        o.append(rail_along(sp, s, 0.035, wood))
    for t in (0.3, 0.52, 0.75):
        l, r = sheer_at(sp, t, -1), sheer_at(sp, t, 1)
        o.append(box((abs(r.x - l.x) * 0.95, 0.24, 0.04), wood, loc=(0, l.y, l.z - 0.15)))
    # 노걸이 + 노 둘 (뱃전에 걸쳐 물 쪽으로 비스듬히)
    for s in (-1, 1):
        p = sheer_at(sp, 0.48, s)
        o.append(cyl(0.02, 0.02, 0.12, dark, loc=(p.x, p.y, p.z + 0.06), seg=6))
        o.append(tube([V((p.x - s * 0.5, p.y + 0.4, p.z + 0.15)), V((p.x + s * 0.9, p.y - 0.3, p.z - 0.3))], 0.025, wood, sides=6))
        o.append(box((0.04, 0.16, 0.55), wood, loc=(p.x + s * 1.1, p.y - 0.4, p.z - 0.42), rot=(0.4, 0, s * 0.5)))
    return o


# ---------------------------------------------------------------- 그리스 카이크

def boat_caique(_):
    L, Bm, D = 7.0, 2.5, 1.1
    red = mat('#8a2a22', 0.7)
    blk = mat('#202022', 0.7)
    white = mat('#efece4', 0.55)
    blue = mat('#1e5aa8', 0.45)
    wood = mat('#8a6440', 0.7)
    o, sp = hull(L, Bm, D, (red, blk, white, blue), wl=0.4, bow_rise=0.45, stern_rise=0.35, transom=0.15, round_=2.6, flare=0.1,
                 deck_mat=wood, deck_drop=0.2, band=0.18)
    for s in (-1, 1):
        o.append(rail_along(sp, s, 0.05, red, dz=0.0, t0=0.0, t1=1.0))
        o.append(rail_along(sp, s, 0.025, wood, dz=-0.32))
    # 높이 솟은 이물 기둥 (사진: 카이크의 휜 이물)
    o.append(tube([V((0, -L / 2 + 0.02, D + 0.3)), V((0, -L / 2 - 0.05, D + 0.75)), V((0, -L / 2 + 0.05, D + 1.0))], 0.07, blue, sides=8))
    o.append(tube([V((0, L / 2 - 0.05, D + 0.3)), V((0, L / 2 + 0.02, D + 0.7))], 0.06, blue, sides=8))
    # 작은 선실 · 짧은 돛대와 붐 · 그물 더미 · 노란 부표
    o.append(box((1.3, 1.4, 1.1), white, loc=(0, 1.3, D - 0.2 + 0.55), bevel=0.04))
    o.append(box((1.45, 1.55, 0.08), blue, loc=(0, 1.3, D + 0.5)))
    o.append(box((1.32, 0.04, 0.35), mat('#24323a', 0.1), loc=(0, 0.59, D + 0.6 - 0.1)))
    o.append(cyl(0.06, 0.05, 3.2, wood, loc=(0, -0.9, D + 1.4), seg=8))
    o.append(tube([V((0, -0.9, D + 0.7)), V((0, 0.9, D + 0.95))], 0.04, wood, sides=6))
    o.append(B.lumpy(0.5, mat('#c84a2a', 0.9), loc=(0.2, -1.8, D - 0.05), scale=(1.1, 0.9, 0.4), amp=0.2, freq=6.0, seed=7))
    for k in range(2):
        o.append(sphere(0.18, mat('#f2c230', 0.5), loc=(-0.6, -2.3 + k * 0.4, D), seg=10, rings=8))
    return o


# ---------------------------------------------------------------- 요트

def yacht(_):
    L, Bm, D = 10.0, 3.2, 1.35
    bot = mat('#1e2a44', 0.6)
    white = mat('#f4f4f2', 0.25)
    navy = mat('#1e3a78', 0.4)
    teak = mat('#a07850', 0.7)
    glass = mat('#1a2630', 0.05)
    steel = mat('#d4d8dc', 0.2, 0.8)
    cover = mat('#2a4a8a', 0.6)
    o, sp = hull(L, Bm, D, (bot, navy, white, white), wl=0.45, bow_rise=0.35, stern_rise=0.1, transom=0.85, round_=1.9, flare=0.08,
                 deck_mat=teak, deck_drop=0.12, band=0.08, boot=0.08)
    # 핀 용골 · 키
    o.append(box((0.2, 1.4, 1.4), bot, loc=(0, 0.2, -0.6), bevel=0.05))
    o.append(box((0.12, 0.6, 1.0), bot, loc=(0, L / 2 - 1.0, -0.3), bevel=0.03))
    # 선실 (낮은 흰 지붕 + 옆 창 띠) · 조종석
    o.append(box((2.0, 3.6, 0.55), white, loc=(0, -0.3, D + 0.15), bevel=0.15))
    for s in (-1, 1):
        o.append(box((0.04, 2.6, 0.18), glass, loc=(s * 1.0, -0.3, D + 0.2)))
    o.append(box((2.2, 2.2, 0.3), white, loc=(0, 3.2, D - 0.1), bevel=0.05))
    # 돛대 13m · 붐 · 접어 덮은 돛 · 앞뒤 지삭 · 난간
    mz = D + 0.4
    o.append(cyl(0.09, 0.06, 13.0, steel, loc=(0, -1.6, mz + 6.5), seg=10))
    o.append(box((1.6, 0.06, 0.06), steel, loc=(0, -1.6, mz + 8.5)))
    o.append(tube([V((0, -1.6, mz + 0.9)), V((0, 2.4, mz + 1.0))], 0.06, steel, sides=8))
    o.append(tube([V((0, -1.5, mz + 1.15)), V((0, 2.2, mz + 1.15))], lambda u: 0.22 * (1 - 0.5 * u), cover, sides=10))
    o.append(tube([V((0, -1.6, mz + 12.9)), V((0, -L / 2 + 0.1, D + 0.4))], 0.01, steel, sides=4))
    o.append(tube([V((0, -1.6, mz + 12.9)), V((0, L / 2 - 0.1, D + 0.3))], 0.01, steel, sides=4))
    o.append(tube([V((0, -L / 2 + 0.2, D + 0.4)), V((0, -L / 2 + 0.2, D + 2.4)), V((0, -1.75, mz + 9))], lambda u: 0.12 * (1 - u) + 0.02, cover, sides=8))
    for s in (-1, 1):
        o.append(rail_along(sp, s, 0.015, steel, dz=0.6, inset=0.95, t0=0.05, t1=0.95))
        for t in (0.15, 0.35, 0.55, 0.75, 0.92):
            p = sheer_at(sp, t, s)
            o.append(cyl(0.015, 0.015, 0.6, steel, loc=(p.x * 0.95, p.y, p.z + 0.3), seg=6))
    return o


# ---------------------------------------------------------------- 딩기 (게임 돛단배 선체)

def boat_dinghy(_):
    L, Bm, D = 4.4, 1.8, 0.62
    navy = mat('#1e3a78', 0.4)
    white = mat('#f2f2f0', 0.3)
    wood = mat('#a07a50', 0.65)
    o, sp = hull(L, Bm, D, (navy, navy, white, navy), wl=0.22, bow_rise=0.2, stern_rise=0.0, transom=0.85, round_=1.8, flare=0.1,
                 deck_mat=wood, deck_drop=0.18, band=0.1, boot=0.06)
    for s in (-1, 1):
        o.append(rail_along(sp, s, 0.035, white))
    o.append(box((0.08, 0.9, 0.7), navy, loc=(0, 0.1, -0.3)))          # 센터보드
    o.append(box((0.06, 0.35, 0.8), white, loc=(0, L / 2 + 0.05, 0.1)))  # 키
    o.append(tube([V((0, L / 2 + 0.05, 0.5)), V((0, L / 2 - 0.9, 0.7))], 0.02, wood, sides=6))
    return o


PROPS = {'boat_kr': boat_kr, 'boat_speed': boat_speed, 'boat_rib': boat_rib, 'boat_row': boat_row, 'boat_caique': boat_caique,
         'yacht': yacht, 'boat_dinghy': boat_dinghy}
