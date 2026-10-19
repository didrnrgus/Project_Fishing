"""
새 해외 낚시터 소품 (2026-10-19, World/Maps/*.cs). build_props.py가 불러 PROPS에 더한다.
길이는 실제 미터, 위 = +Z, 앞 = -Y (Unity +Z). 조각상은 앞(-Y)을 본다.

  buddha_gold, buddha_white  결가부좌한 대불 (코사무이 황금 대불 · 푸켓 하얀 대불): 연꽃 받침, 무릎 위 손, 길게 늘어진 귓불, 육계와 불꽃
  merlion                    센토사 머라이언: 갈기 두른 사자 머리 + 비늘 띠 물고기 몸, 꼬리가 등 뒤로 말려 오르고 물결 받침
  eagle                      랑카위 독수리 광장: 날개를 반쯤 펴고 바위에 앉은 적갈색 독수리, 흰 머리, 노란 갈고리 부리
  longtail_a, longtail_b     롱테일 보트: 좁고 긴 나무배, 하늘로 휜 뱃머리에 꽃줄·리본, 고물의 긴 축 엔진
  balloon_a, balloon_b       열기구: 세로 색 띠 풍선, 버너, 고리 밧줄, 바구니
  parasol_thatch             초가 파라솔 (빈펄·텔록 바텍): 나무 기둥에 짚 원뿔, 끝이 들쭉날쭉한 짚 술
  parasol_purple             보라 천 파라솔 (티오만)
  sunbed                     나무 선베드 + 흰 쿠션
  lifeguard                  인명구조 망루 (텔록 바텍): 네 다리 나무 탑, 비스듬한 사다리, 초가 지붕 망대
  singha                     앙코르 사자 석상 (계단 양옆): 앞발을 세우고 앉은 사자, 갈기 말림, 사각 받침
  shipwreck                  자킨토스 나바지오 난파선: 녹슨 소형 화물선이 모래에 기울어 박힘, 선교·돛대·구멍
"""
import math

import bmesh
import build_icons as B
from mathutils import Vector as V

mat, cyl, box, sphere, tube = B.mat, B.cyl, B.box, B.sphere, B.tube


def lathe(prof, m, seg=32, sy=1.0, name='Lathe', cap_bot=True, cap_top=True, cols=None):
    """(반지름, 높이) 단면을 z축으로 돌린 회전체. sy: y 방향 납작함. cols: 세로 띠 재질 목록 (열기구 색 띠)."""
    bm = bmesh.new()
    rings = []
    for r, z in prof:
        rings.append([bm.verts.new((r * math.cos(2 * math.pi * k / seg), r * math.sin(2 * math.pi * k / seg) * sy, z)) for k in range(seg)])
    for i in range(len(rings) - 1):
        for k in range(seg):
            f = bm.faces.new((rings[i][k], rings[i][(k + 1) % seg], rings[i + 1][(k + 1) % seg], rings[i + 1][k]))
            if cols:
                f.material_index = (k * len(cols)) // seg % len(cols)
    if cap_bot and prof[0][0] > 1e-4:
        c = bm.verts.new((0, 0, prof[0][1]))
        for k in range(seg):
            bm.faces.new((rings[0][(k + 1) % seg], rings[0][k], c))
    if cap_top and prof[-1][0] > 1e-4:
        c = bm.verts.new((0, 0, prof[-1][1]))
        for k in range(seg):
            bm.faces.new((rings[-1][k], rings[-1][(k + 1) % seg], c))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    ob = B.obj_from_bm(bm, name, cols[0] if cols else m)
    if cols:
        for cm in cols[1:]:
            ob.data.materials.append(cm)
    return ob


def at(ob, loc=(0, 0, 0), rot=(0, 0, 0), scale=None):
    ob.location, ob.rotation_euler = loc, rot
    if scale:
        ob.scale = scale
    return ob


def sheet(rows, m, thick=0.03, name='Sheet'):
    """점 격자(rows[i][j])로 만든 얇은 판 (날개·돛·지느러미)."""
    bm = bmesh.new()
    vs = [[bm.verts.new(p) for p in row] for row in rows]
    for i in range(len(vs) - 1):
        for j in range(len(vs[i]) - 1):
            bm.faces.new((vs[i][j], vs[i][j + 1], vs[i + 1][j + 1], vs[i + 1][j]))
    if thick:
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=thick)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    return B.obj_from_bm(bm, name, m)


# ---------------------------------------------------------------- 대불

def buddha(pid):
    """결가부좌 대불, 키 약 1 단위 = 받침 포함 높이 10 (Unity에서 높이로 맞춘다)."""
    gold = pid == 'buddha_gold'
    skin = mat('#d8a73e', 0.28, 0.85) if gold else mat('#f0ece4', 0.55)
    robe = mat('#c8952c', 0.32, 0.85) if gold else mat('#e6e0d6', 0.6)
    dark = mat('#9a6e1e', 0.4, 0.8) if gold else mat('#c8c2b8', 0.7)
    hair = mat('#7a5418', 0.35, 0.8) if gold else mat('#b8b2a8', 0.7)
    petal = mat('#e0b450', 0.3, 0.85) if gold else mat('#f4f0ea', 0.5)
    base = mat('#b08a3a', 0.4, 0.6) if gold else mat('#d8d2c6', 0.75)
    o = []
    # 팔각 단 두 층 + 연꽃 받침 (꽃잎 두 줄이 바깥으로 벌어진다)
    o.append(cyl(5.2, 5.0, 0.9, base, loc=(0, 0, 0.45), seg=8))
    o.append(cyl(4.6, 4.4, 0.7, base, loc=(0, 0, 1.25), seg=8))
    o.append(lathe([(3.4, 1.6), (4.3, 2.0), (4.4, 2.5), (4.0, 2.9)], dark, seg=40))
    for row, (n, rr, z, tilt, s) in enumerate(((24, 4.1, 2.3, 0.75, 0.75), (24, 3.8, 2.75, 0.5, 0.7))):
        for k in range(n):
            a = 2 * math.pi * (k + 0.5 * row) / n
            o.append(at(sphere(1.0, petal, seg=12, rings=8), (math.cos(a) * rr, math.sin(a) * rr, z), (tilt, 0, a + math.pi / 2), (s * 0.55, 0.16, s)))
    z0 = 2.9
    # 결가부좌 다리: 넓은 타원 + 앞으로 나온 두 무릎, 발바닥이 위를 본다
    o.append(at(sphere(1.0, robe, seg=32, rings=16), (0, -0.2, z0 + 0.55), scale=(3.2, 1.9, 0.75)))
    for sx in (-1, 1):
        o.append(at(sphere(1.0, robe, seg=20, rings=12), (sx * 2.3, -0.9, z0 + 0.6), scale=(1.1, 1.0, 0.7)))
        o.append(at(sphere(1.0, skin, seg=14, rings=10), (sx * 0.9, -1.25, z0 + 1.05), (0.4, 0, sx * 0.5), (0.65, 0.3, 0.28)))
    # 몸통: 아래가 넓고 가슴이 둥근 회전체 (앞뒤로 납작)
    o.append(lathe([(2.0, z0 + 0.6), (2.15, z0 + 1.4), (1.95, z0 + 2.6), (2.0, z0 + 3.5), (1.75, z0 + 4.2), (0.9, z0 + 4.65), (0.55, z0 + 4.8)],
                   robe, seg=36, sy=0.62))
    # 맨 오른쪽 어깨와 가슴 (가사를 왼 어깨에만 걸친다)
    o.append(at(sphere(1.0, skin, seg=20, rings=12), (1.35, -0.25, z0 + 3.9), scale=(0.8, 0.9, 0.75)))
    o.append(at(sphere(1.0, robe, seg=20, rings=12), (-1.35, -0.1, z0 + 3.95), scale=(0.85, 0.95, 0.8)))
    o.append(tube([(-1.6, -0.9, z0 + 4.2), (-0.5, -1.25, z0 + 3.2), (0.7, -1.2, z0 + 1.9), (1.4, -0.9, z0 + 1.2)], 0.26, dark, sides=10))
    # 팔: 왼손은 무릎 위(손바닥 위), 오른손은 오른 무릎을 짚는다 (항마촉지인)
    o.append(tube([(-1.9, -0.1, z0 + 3.9), (-2.25, -0.4, z0 + 2.7), (-1.6, -1.0, z0 + 1.7), (-0.5, -1.35, z0 + 1.35)], 0.5, robe, r_end=0.38, sides=14))
    o.append(at(sphere(1.0, skin, seg=14, rings=10), (-0.2, -1.45, z0 + 1.35), scale=(0.6, 0.45, 0.2)))
    o.append(tube([(1.9, -0.15, z0 + 3.9), (2.3, -0.5, z0 + 2.7), (2.4, -1.1, z0 + 1.8), (2.35, -1.55, z0 + 1.1)], 0.46, skin, r_end=0.33, sides=14))
    o.append(at(sphere(1.0, skin, seg=14, rings=10), (2.3, -1.75, z0 + 0.95), (0.3, 0, 0), (0.35, 0.5, 0.25)))
    # 목 · 머리 (살짝 갸름) · 귓불 · 머리카락 소라 · 육계 · 불꽃
    zh = z0 + 5.75
    o.append(cyl(0.62, 0.58, 0.7, skin, loc=(0, -0.05, z0 + 4.95), seg=20))
    o.append(at(sphere(1.0, skin, seg=28, rings=18), (0, -0.1, zh), scale=(0.95, 1.0, 1.15)))
    o.append(at(sphere(1.0, skin, seg=12, rings=8), (0, -1.05, zh - 0.05), scale=(0.16, 0.2, 0.3)))   # 코
    for sx in (-1, 1):
        o.append(at(sphere(1.0, skin, seg=12, rings=10), (sx * 0.98, 0.0, zh - 0.35), scale=(0.16, 0.32, 0.72)))
        o.append(at(sphere(1.0, dark, seg=10, rings=6), (sx * 0.36, -0.93, zh + 0.12), (0, 0, sx * 0.15), (0.28, 0.06, 0.05)))   # 감은 눈
    o.append(B.lumpy(1.0, hair, loc=(0, 0.02, zh + 0.32), scale=(0.98, 1.03, 0.92), amp=0.06, freq=7.0, seed=3, seg=36, rings=20))
    o.append(B.lumpy(0.55, hair, loc=(0, 0.05, zh + 1.2), amp=0.08, freq=6.0, seed=5, seg=24, rings=14))
    o.append(lathe([(0.3, zh + 1.55), (0.22, zh + 1.9), (0.12, zh + 2.4), (0.02, zh + 2.9)], robe, seg=16))
    return o


# ---------------------------------------------------------------- 머라이언

def merlion(_):
    """머라이언, 높이 약 9 (받침 포함 11)."""
    white = mat('#ebe8e0', 0.55)
    shade = mat('#d2cec4', 0.6)
    dark = mat('#4a4844', 0.6)
    wave = mat('#c8c4ba', 0.7)
    o = []
    # 물결 받침: 넘실대는 고리 세 겹
    o.append(B.lumpy(1.0, wave, loc=(0, 0.6, 0.9), scale=(3.6, 3.2, 1.2), amp=0.22, freq=3.2, seed=2, seg=40, rings=16))
    for k in range(7):
        a = 2 * math.pi * k / 7
        o.append(at(B.lumpy(1.0, wave, amp=0.25, freq=4.0, seed=k, seg=16, rings=10), (math.cos(a) * 3.0, 0.6 + math.sin(a) * 2.7, 1.6), scale=(1.2, 1.1, 0.8)))
    # 물고기 몸: 받침에서 솟아 앞으로 굽어 오르고, 꼬리는 뒤로 말려 올라간다
    spine = [V((0, 1.6 - 1.2 * math.sin(t * 1.4), 1.6 + 4.6 * t)) for t in [i / 12 for i in range(13)]]
    o.append(tube(spine, lambda u: 1.55 - 0.35 * u, white, sides=24, name='Body'))
    # 비늘: 몸 겉에 얕게 솟은 비늘 줄 (엇갈린 작은 판)
    for i in range(2, 12):
        p = spine[i]
        r = 1.55 - 0.35 * i / 12
        for k in range(10):
            a = 2 * math.pi * (k + 0.5 * (i % 2)) / 10
            if math.sin(a) < -0.2:
                continue
            o.append(at(sphere(1.0, shade, seg=10, rings=6), tuple(p + V((math.cos(a) * r * 0.97, math.sin(a) * r * 0.97 * -1, 0))), (0.25, 0, a), (0.34, 0.1, 0.24)))
    tail = [V((0, 1.9 + 2.4 * math.sin(t * 1.6), 1.4 + 4.0 * (1 - math.cos(t * 1.6)))) for t in [i / 12 for i in range(13)]]
    o.append(tube(tail, lambda u: 1.0 - 0.75 * u, white, sides=18, name='Tail'))
    tip = tail[-1]
    for sx in (-1, 1):
        o.append(sheet([[tip + V((0, 0, 0)), tip + V((sx * 0.2, 0.3, 0.2))], [tip + V((sx * 1.6, 0.8, 1.4)), tip + V((sx * 1.2, 1.6, 0.6))]], shade, 0.12, 'Fluke'))
    # 사자 머리: 둥근 머리 + 앞으로 나온 주둥이 + 벌린 입(물 뿜는 곳) + 갈기
    hp = spine[-1] + V((0, -0.55, 1.4))
    o.append(at(sphere(1.0, white, seg=32, rings=20), tuple(hp), scale=(1.45, 1.5, 1.5)))
    o.append(at(sphere(1.0, white, seg=24, rings=14), tuple(hp + V((0, -1.25, -0.35))), scale=(0.95, 0.8, 0.7)))
    o.append(at(sphere(1.0, dark, seg=16, rings=10), tuple(hp + V((0, -1.75, -0.55))), scale=(0.5, 0.35, 0.22)))      # 입 안
    o.append(at(sphere(1.0, white, seg=16, rings=10), tuple(hp + V((0, -1.55, -0.95))), scale=(0.7, 0.55, 0.28)))     # 아래턱
    o.append(at(sphere(1.0, dark, seg=12, rings=8), tuple(hp + V((0, -2.0, -0.08))), scale=(0.3, 0.18, 0.2)))        # 코
    for sx in (-1, 1):
        o.append(at(sphere(1.0, dark, seg=12, rings=8), tuple(hp + V((sx * 0.55, -1.28, 0.3))), scale=(0.2, 0.1, 0.13)))   # 눈
        o.append(at(sphere(1.0, white, seg=12, rings=8), tuple(hp + V((sx * 0.6, -1.15, 0.55))), scale=(0.36, 0.2, 0.12)))  # 눈썹뼈
        o.append(at(sphere(1.0, white, seg=12, rings=8), tuple(hp + V((sx * 1.1, 0.1, 1.15))), scale=(0.3, 0.25, 0.35)))   # 귀
    # 갈기: 머리 둘레에서 뒤·아래로 흘러내린 굵은 물결 타래 (목 뒤를 덮는다)
    for k in range(18):
        a = math.pi * (-0.15 + 1.3 * k / 17)
        root = hp + V((math.cos(a) * 1.35, 0.25, math.sin(a) * 1.35))
        out = V((math.cos(a), 0, math.sin(a)))
        pts = [root + out * 0.25 * u + V((0, 1.6 * u, -2.2 * u * u)) + out * 0.18 * math.sin(u * 6 + k) for u in [j / 6 for j in range(7)]]
        o.append(tube(pts, lambda u: 0.42 * (1 - 0.7 * u), shade if k % 2 else white, sides=10))
    for k in range(8):
        a = math.pi * (0.1 + 0.8 * k / 7)
        root = hp + V((math.cos(a) * 0.9, -0.5, math.sin(a) * 0.95 + 0.3))
        pts = [root + V((math.cos(a) * 0.3 * u, 0.9 * u, 0.25 * u + 0.12 * math.sin(u * 5 + k))) for u in [j / 5 for j in range(6)]]
        o.append(tube(pts, lambda u: 0.32 * (1 - 0.6 * u), white, sides=10))
    return o


def eagle(_):
    """랑카위 독수리, 높이 약 12 (받침 바위 포함)."""
    brown = mat('#7a3e20', 0.6)
    brown2 = mat('#5a2c16', 0.65)
    white = mat('#efe6d6', 0.55)
    beak = mat('#e0a830', 0.4)
    rock = mat('#6a6258', 0.85)
    eye = mat('#1a1410', 0.3)
    o = []
    o.append(B.lumpy(1.0, rock, loc=(0, 0, 1.4), scale=(3.6, 3.0, 2.0), amp=0.2, freq=3.0, seed=9, seg=32, rings=16))
    # 몸 (앞으로 숙인 타원) · 가슴 깃털 · 다리
    body = (0, 0.1, 5.4)
    o.append(at(sphere(1.0, brown, seg=32, rings=20), body, (0.35, 0, 0), (1.35, 1.25, 2.2)))
    o.append(at(B.lumpy(1.0, brown2, amp=0.08, freq=9.0, seed=4, seg=28, rings=16), (0, -0.55, 4.9), (0.35, 0, 0), (1.05, 0.7, 1.6)))
    for sx in (-1, 1):
        o.append(tube([(sx * 0.6, -0.1, 3.6), (sx * 0.65, -0.4, 2.9), (sx * 0.6, -0.7, 2.5)], 0.35, brown2, r_end=0.22, sides=12))
        for k in (-1, 0, 1):
            o.append(tube([(sx * 0.6, -0.7, 2.55), (sx * 0.6 + k * 0.35, -1.4, 2.45), (sx * 0.6 + k * 0.45, -1.65, 2.25)], 0.12, beak, r_end=0.04, sides=8))
    # 꼬리 깃 부채
    rows = []
    for i in range(6):
        t = i / 5
        rows.append([V((0, 1.3, 3.8)), V(((t - 0.5) * 2.2, 3.1, 1.9 - 0.15 * abs(t - 0.5)))])
    o.append(sheet(list(map(list, zip(*rows))), brown2, 0.12, 'Tail'))
    # 날개: 어깨에서 위·뒤로 들어 올린 반쯤 편 날개, 뒷전에 첫째날개깃이 손가락처럼 갈라진다
    for sx in (-1, 1):
        rows = []
        n = 9
        for i in range(n + 1):
            t = i / n
            lead = V((sx * (1.0 + 6.2 * t), 0.2 + 1.2 * t, 6.6 + 3.0 * t - 1.6 * t * t))
            chord = 2.8 * (1 - 0.35 * t)
            jag = 0.6 if (t > 0.55 and i % 2) else 0.0
            trail = lead + V((sx * -0.3 * t, 0.9 + 0.3 * t, -(chord + jag)))
            mid = lead.lerp(trail, 0.5) + V((0, -0.3, 0))
            rows.append([lead, mid, trail])
        o.append(sheet(rows, brown, 0.22, 'Wing'))
        # 덮깃 줄 (날개 위 짙은 띠)
        o.append(tube([V((sx * 1.2, 0.6, 6.9)), V((sx * 3.6, 1.3, 8.2)), V((sx * 6.4, 1.6, 8.9))], 0.18, brown2, r_end=0.06, sides=8))
    # 머리 (흰색) · 갈고리 부리 · 눈
    hp = V((0, -1.0, 8.0))
    o.append(tube([V((0, -0.3, 7.0)), hp + V((0, 0.3, -0.3))], 0.75, white, r_end=0.62, sides=18))
    o.append(at(sphere(1.0, white, seg=24, rings=16), tuple(hp), scale=(0.75, 0.95, 0.8)))
    o.append(tube([hp + V((0, -0.8, 0.0)), hp + V((0, -1.5, -0.05)), hp + V((0, -1.75, -0.35)), hp + V((0, -1.65, -0.6))], 0.32, beak, r_end=0.05, sides=12))
    for sx in (-1, 1):
        o.append(at(sphere(0.13, eye, seg=10, rings=8), tuple(hp + V((sx * 0.55, -0.55, 0.2)))))
        o.append(at(sphere(1.0, white, seg=10, rings=6), tuple(hp + V((sx * 0.55, -0.6, 0.38))), scale=(0.25, 0.3, 0.08)))
    return o


def longtail(pid):
    red = pid == 'longtail_b'
    hull = mat('#2c5aa0' if not red else '#a8322a', 0.5)
    band = mat('#f2ead8', 0.5)
    band2 = mat('#e8b830' if not red else '#2a8a54', 0.5)
    wood = mat('#7a5232', 0.7)
    eng = mat('#5a5e62', 0.4, 0.6)
    ribbon = [mat(c, 0.6) for c in ('#e83a6a', '#f2c230', '#3a9ae0', '#f07a28', '#9a4ac8')]
    o = []
    L, W = 9.5, 1.5
    bm = bmesh.new()
    prof = [(-0.5, 0.55), (-0.38, 0.95), (0.0, 1.0), (0.3, 0.8), (0.45, 0.35), (0.5, 0.06)]
    rings = []
    for y, w in prof:
        hw = W * 0.5 * w
        rise = 1.6 * max(0.0, y - 0.3) ** 1.6 / 0.2 ** 1.6 * (1 if y > 0.3 else 0) + 0.15 * max(0.0, -y - 0.3) / 0.2
        rings.append([bm.verts.new((-hw, -y * L, 0.7 + rise)), bm.verts.new((-hw * 0.8, -y * L, 0.12)), bm.verts.new((0, -y * L, -0.1)),
                      bm.verts.new((hw * 0.8, -y * L, 0.12)), bm.verts.new((hw, -y * L, 0.7 + rise))])
    for i in range(len(rings) - 1):
        for k in range(4):
            bm.faces.new((rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]))
    bm.faces.new(rings[0][::-1])
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.05)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    o.append(B.obj_from_bm(bm, 'Hull', hull))
    # 뱃전 색 띠 두 줄
    for z, m in ((0.62, band), (0.5, band2)):
        for sx in (-1, 1):
            o.append(box((0.04, L * 0.68, 0.07), m, loc=(sx * W * 0.5 * 0.97, 0.3, z)))
    o.append(box((W * 0.8, L * 0.7, 0.04), wood, loc=(0, 0.3, 0.3)))
    for y in (-2.0, 0.0, 2.0):
        o.append(box((W * 0.9, 0.25, 0.05), wood, loc=(0, y, 0.55)))
    # 뱃머리 꽃줄과 리본 (뱃머리 끝 = -Y)
    bow = V((0, -L * 0.5, 2.35))
    o.append(B.torus(0.18, 0.05, ribbon[1], loc=tuple(bow + V((0, 0.25, -0.2))), rot=(math.pi / 2, 0, 0), seg=14, mseg=6))
    for k in range(5):
        a = (k - 2) * 0.35
        o.append(tube([bow + V((0, 0.2, -0.1)), bow + V((math.sin(a) * 0.35, 0.5, -0.8)), bow + V((math.sin(a) * 0.45, 0.65, -1.4))], 0.05, ribbon[k], r_end=0.02, sides=6))
    # 고물: 엔진(상자) + 비스듬히 물로 뻗는 긴 축 + 프로펠러, 키잡이 손잡이
    stern = V((0, L * 0.5 - 0.3, 0.9))
    o.append(box((0.5, 0.7, 0.45), eng, loc=tuple(stern + V((0, 0, 0.35))), bevel=0.04))
    shaft = [stern + V((0, 0.2, 0.2)), stern + V((0, 2.0, -0.55)), stern + V((0, 3.6, -1.15))]
    o.append(tube(shaft, 0.05, eng, sides=8))
    o.append(tube([stern + V((0, -0.2, 0.4)), stern + V((0, -1.4, 0.75))], 0.035, eng, sides=6))
    p = shaft[-1]
    for k in range(3):
        a = 2 * math.pi * k / 3
        o.append(box((0.04, 0.03, 0.28), eng, loc=tuple(p + V((math.cos(a) * 0.12, 0.02, math.sin(a) * 0.12))), rot=(0, a, 0)))
    return o


# ---------------------------------------------------------------- 열기구

def balloon(pid):
    if pid == 'balloon_a':
        cols = [mat(c, 0.55) for c in ('#e83a3a', '#f2c230', '#2a7ad0', '#f2f0e8')]
    else:
        cols = [mat(c, 0.55) for c in ('#2a9a6a', '#f07a28', '#f2f0e8', '#7a3ac0')]
    rope = mat('#4a3e30', 0.8)
    basket = mat('#8a6438', 0.85)
    burner = mat('#7a7e82', 0.4, 0.7)
    o = []
    # 풍선: 아래가 좁은 물방울 (16개 세로 띠)
    prof = [(1.3, 4.0), (2.4, 5.0), (4.2, 7.0), (5.6, 9.5), (6.2, 12.0), (5.9, 14.2), (4.8, 16.0), (2.8, 17.3), (0.2, 17.8)]
    o.append(lathe(prof, None, seg=32, cols=cols, cap_bot=False))
    # 아래 고리 · 버너 · 밧줄 여덟 가닥 · 바구니
    o.append(B.torus(1.3, 0.06, burner, loc=(0, 0, 4.0), seg=24, mseg=6))
    o.append(cyl(0.45, 0.45, 0.6, burner, loc=(0, 0, 2.6), seg=16))
    for k in range(8):
        a = 2 * math.pi * k / 8
        o.append(tube([(math.cos(a) * 1.3, math.sin(a) * 1.3, 4.0), (math.cos(a) * 0.7, math.sin(a) * 0.7, 1.4)], 0.025, rope, sides=5))
    o.append(box((1.5, 1.5, 1.1), basket, loc=(0, 0, 0.55), bevel=0.06))
    o.append(box((1.6, 1.6, 0.12), rope, loc=(0, 0, 1.12)))
    return o


# ---------------------------------------------------------------- 해변 소품

def parasol_thatch(_):
    wood = mat('#6a4a2e', 0.8)
    straw = mat('#c8a660', 0.9)
    straw2 = mat('#a88a4c', 0.9)
    o = [cyl(0.08, 0.07, 2.8, wood, loc=(0, 0, 1.4), seg=10)]
    o.append(lathe([(0.1, 3.25), (0.8, 2.95), (1.7, 2.55), (2.0, 2.4)], straw, seg=28, cap_bot=True))
    # 들쭉날쭉한 짚 술 (테두리에 늘어뜨린 짚 다발)
    for k in range(28):
        a = 2 * math.pi * k / 28
        ln = 0.35 + 0.15 * ((k * 7) % 3)
        o.append(box((0.32, 0.05, ln), straw2 if k % 2 else straw, loc=(math.cos(a) * 1.95, math.sin(a) * 1.95, 2.42 - ln / 2), rot=(0, 0, a + math.pi / 2)))
    o.append(cyl(0.12, 0.02, 0.4, straw2, loc=(0, 0, 3.4), seg=8))
    return o


def parasol_purple(_):
    pole = mat('#e8e6e0', 0.4, 0.5)
    cloth = mat('#7a3aa8', 0.65)
    cloth2 = mat('#9a5ac8', 0.65)
    o = [cyl(0.04, 0.04, 2.5, pole, loc=(0, 0, 1.25), seg=8)]
    o.append(lathe([(0.05, 2.75), (0.9, 2.55), (1.6, 2.25), (1.65, 2.2)], None, seg=16, cols=[cloth, cloth2]))
    o.append(cyl(0.3, 0.3, 0.08, pole, loc=(0, 0, 0.04), seg=12))
    return o


def sunbed(_):
    wood = mat('#8a6440', 0.6)
    cushion = mat('#f2f0ea', 0.8)
    o = []
    o.append(box((0.7, 1.4, 0.06), wood, loc=(0, 0.2, 0.32)))
    o.append(box((0.7, 0.75, 0.06), wood, loc=(0, -0.75, 0.55), rot=(-0.6, 0, 0)))
    o.append(box((0.62, 1.32, 0.07), cushion, loc=(0, 0.2, 0.38), bevel=0.02))
    o.append(box((0.62, 0.7, 0.07), cushion, loc=(0, -0.72, 0.6), rot=(-0.6, 0, 0), bevel=0.02))
    for sx in (-1, 1):
        for y in (-0.4, 0.8):
            o.append(box((0.05, 0.05, 0.3), wood, loc=(sx * 0.3, y, 0.15)))
    return o


def lifeguard(_):
    wood = mat('#8a6a48', 0.8)
    dark = mat('#5a4430', 0.8)
    straw = mat('#b89a58', 0.9)
    red = mat('#c83a2a', 0.6)
    o = []
    H = 3.2
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(box((0.16, 0.16, H + 0.1), dark, loc=(sx * 1.0, sy * 1.0, H / 2), rot=(sy * 0.05, -sx * 0.05, 0)))
    for z in (1.0, 2.1):
        for sy in (-1, 1):
            o.append(box((2.1, 0.08, 0.1), wood, loc=(0, sy * 1.0, z)))
            o.append(box((0.08, 2.1, 0.1), wood, loc=(sy * 1.0, 0, z)))
    o.append(box((2.4, 2.4, 0.12), wood, loc=(0, 0, H)))
    for sx in (-1, 1):
        o.append(box((2.4, 0.06, 0.9), wood, loc=(0, sx * 1.17, H + 0.5)))
        o.append(box((0.06, 2.4, 0.9), wood, loc=(sx * 1.17, 0, H + 0.5)))
    o.append(box((2.42, 0.07, 0.25), red, loc=(0, -1.2, H + 0.75)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(box((0.08, 0.08, 1.9), dark, loc=(sx * 1.1, sy * 1.1, H + 0.95)))
    o.append(lathe([(0.05, H + 2.9), (1.0, H + 2.4), (2.0, H + 1.85), (2.1, H + 1.75)], straw, seg=4))
    o[-1].rotation_euler = (0, 0, math.pi / 4)
    # 사다리 (앞으로 비스듬히)
    for sx in (-1, 1):
        o.append(box((0.08, 0.08, H * 1.2), dark, loc=(sx * 0.35, -1.85, H * 0.5), rot=(-0.38, 0, 0)))
    for k in range(7):
        z = 0.35 + k * 0.42
        o.append(box((0.7, 0.06, 0.06), wood, loc=(0, -2.45 + z * 0.4, z)))
    return o


# ---------------------------------------------------------------- 앙코르 사자상 · 난파선

def _stone_paint(poly, n):
    """돌 조각상: 위를 보는 면 일부와 아랫단에 이끼·때 (재질 1)."""
    return 1 if (poly.normal.z > 0.55 and n > 0.25) or (poly.center.z < 1.1 and n > 0.45) else 0


def _eroded(mb_obj, stone, moss, strength=0.03, seed=0, ratio=0.35, mats=None, paint=None):
    """메타볼 → 메쉬로 굳히고, 구름 노이즈로 겉을 깎아 닳은 돌처럼 만든 뒤 면마다 재질을 칠한다
    (기본: 위를 보는 면 일부에 이끼. mats·paint(poly, 노이즈)를 주면 그걸로 — 독수리의 흰 머리·노란 부리)."""
    import bpy
    bpy.ops.object.select_all(action='DESELECT')
    mb_obj.select_set(True)
    bpy.context.view_layer.objects.active = mb_obj
    bpy.ops.object.convert(target='MESH')
    o = bpy.context.view_layer.objects.active
    for scale, st in ((0.35, strength), (0.08, strength * 0.45)):
        tex = bpy.data.textures.new(f'Erode{seed}_{scale}', 'CLOUDS')
        tex.noise_scale = scale
        tex.noise_depth = 3
        m = o.modifiers.new('Erode', 'DISPLACE')
        m.texture = tex
        m.strength = st
        m.mid_level = 0.5
        bpy.ops.object.modifier_apply(modifier=m.name)
    d = o.modifiers.new('Dec', 'DECIMATE')
    d.ratio = ratio
    bpy.ops.object.modifier_apply(modifier=d.name)
    o.data.materials.clear()
    for m in (mats or [stone, moss]):
        o.data.materials.append(m)
    paint = paint or _stone_paint
    for poly in o.data.polygons:
        c = poly.center
        n = math.sin(c.x * 23.1 + seed) * math.sin(c.y * 19.3) * math.sin(c.z * 21.7 + seed * 0.5)
        poly.material_index = paint(poly, n)
        poly.use_smooth = True
    return o


def singha(_):
    """앙코르 사자상 (크메르 양식): 뒷다리를 접고 가슴을 내밀어 앉은 사자. 메타볼로 한 덩어리 돌에서 깎아 낸 듯 이어진 몸,
    크게 벌린 입·부릅뜬 눈·두툼한 눈두덩, 얼굴을 두른 갈기 말림, 등 뒤로 말린 꼬리. 비바람에 닳은 겉면(노이즈 깎기) + 윗면 이끼."""
    import bpy
    from mathutils import Quaternion
    stone = mat('#8c897a', 0.93)
    moss = mat('#6c705a', 0.96)   # 이끼·지의류로 거뭇해진 돌 (튀는 초록이면 얼룩무늬처럼 보였다)
    mb = bpy.data.metaballs.new('Singha')
    mb.resolution = 0.03
    mb.render_resolution = 0.03
    mb.threshold = 0.22   # 낮을수록 덩어리가 커지고 서로 녹아 붙는다 (한 덩어리 돌)
    ob = B.link(bpy.data.objects.new('Singha', mb))

    def el(kind, co, r, size=None, rot=None, neg=False, stiff=2.0):
        e = mb.elements.new(type=kind)
        e.co = co
        e.radius = r
        e.stiffness = stiff
        e.use_negative = neg
        if size:
            e.size_x, e.size_y, e.size_z = size
        if rot:
            e.rotation = rot
        return e

    up = Quaternion((0, 1, 0), math.pi / 2)   # 캡슐(x축)을 세운다
    # 몸: 엉덩이·허벅지·가슴통·목덜미
    el('ELLIPSOID', (0, 0.48, 1.32), 1.0, (0.5, 0.58, 0.4))
    for sx in (-1, 1):
        el('ELLIPSOID', (sx * 0.34, 0.52, 1.18), 1.0, (0.2, 0.44, 0.28))
        el('ELLIPSOID', (sx * 0.3, 0.05, 1.05), 1.0, (0.14, 0.34, 0.1))          # 접은 뒷발
    el('ELLIPSOID', (0, 0.06, 1.88), 1.0, (0.4, 0.34, 0.6))
    el('ELLIPSOID', (0, -0.08, 2.38), 1.0, (0.46, 0.38, 0.4))
    # 앞다리: 곧은 다리 + 발
    for sx in (-1, 1):
        el('CAPSULE', (sx * 0.22, -0.36, 1.42), 0.12, (0.38, 0, 0), up)
        el('ELLIPSOID', (sx * 0.22, -0.46, 1.0), 1.0, (0.14, 0.19, 0.08))
    # 머리: 두개골·눈두덩·부릅뜬 눈·주둥이·넓적한 코·아래턱, 입속은 파낸다
    el('ELLIPSOID', (0, -0.3, 2.62), 1.0, (0.34, 0.3, 0.3))
    el('ELLIPSOID', (0, -0.53, 2.73), 1.0, (0.34, 0.1, 0.07))
    for sx in (-1, 1):
        el('BALL', (sx * 0.16, -0.6, 2.66), 0.075)
        el('ELLIPSOID', (sx * 0.33, -0.22, 2.84), 1.0, (0.07, 0.05, 0.1))          # 귀
    el('ELLIPSOID', (0, -0.64, 2.52), 1.0, (0.24, 0.17, 0.12))
    el('ELLIPSOID', (0, -0.79, 2.56), 1.0, (0.13, 0.07, 0.07))
    el('ELLIPSOID', (0, -0.6, 2.3), 1.0, (0.2, 0.18, 0.06))
    el('ELLIPSOID', (0, -0.8, 2.41), 1.0, (0.17, 0.14, 0.065), neg=True)
    for sx in (-1, 1):
        el('CAPSULE', (sx * 0.12, -0.8, 2.44), 0.025, (0.05, 0, 0), up)        # 송곳니
    # 갈기: 얼굴 둘레 두 줄, 가슴으로 흘러내리는 세 줄 (작은 공이 녹아 붙어 깎은 말림이 된다)
    # 갈기 깃: 얼굴 뒤 두툼한 원반 → 그 앞면에 반쯤 묻힌 말림 (떨어진 구슬이 아니라 깎아 낸 말림이 된다)
    el('ELLIPSOID', (0, -0.14, 2.56), 1.0, (0.5, 0.2, 0.52))
    for row, (rr, n, ry) in enumerate(((0.33, 12, -0.26), (0.43, 15, -0.2))):
        for k in range(n):
            a = math.pi * (-0.2 + 1.4 * k / (n - 1))
            el('BALL', (math.cos(a) * rr, ry, 2.56 + math.sin(a) * rr), 0.075, stiff=2.0)
    for k in range(3):
        for j in range(5):
            el('BALL', ((j - 2) * 0.12, -0.32 + k * 0.02, 2.12 - k * 0.16), 0.07, stiff=1.6)
    # 꼬리: 등 뒤로 S자로 말려 오른다
    for i in range(9):
        tt = i / 8
        el('BALL', (0, 0.95 + 0.25 * math.sin(tt * 2.8), 1.12 + 1.0 * tt), 0.1 + 0.03 * (1 - tt))
    body = _eroded(ob, stone, moss, strength=0.055, seed=3)
    # 받침: 닳은 사암 블록 두 단
    base = box((1.3, 2.0, 0.75), stone, loc=(0, 0.1, 0.375), bevel=0.05)
    base.data.materials.append(moss)
    for poly in base.data.polygons:
        poly.material_index = 1 if poly.center.z < 0.4 or (poly.normal.z > 0.5 and math.sin(poly.center.x * 13) > 0.3) else 0
    top = box((1.15, 1.85, 0.2), stone, loc=(0, 0.1, 0.85), bevel=0.03)
    return [body, base, top]


def shipwreck(_):
    rust = mat('#7a3e22', 0.85)
    rust2 = mat('#5a2c18', 0.9)
    white = mat('#c8c0b0', 0.8)
    dark = mat('#1e1a16', 0.9)
    o = []
    L, W = 32.0, 7.0
    bm = bmesh.new()
    prof = [(-0.5, 0.75), (-0.3, 1.0), (0.2, 1.0), (0.4, 0.75), (0.5, 0.08)]
    rings = []
    for y, w in prof:
        hw = W * 0.5 * w
        rise = 1.2 * max(0.0, y - 0.25) / 0.25
        rings.append([bm.verts.new((-hw, -y * L, 4.2 + rise)), bm.verts.new((-hw * 0.95, -y * L, 1.0)), bm.verts.new((-hw * 0.5, -y * L, 0)),
                      bm.verts.new((hw * 0.5, -y * L, 0)), bm.verts.new((hw * 0.95, -y * L, 1.0)), bm.verts.new((hw, -y * L, 4.2 + rise))])
    for i in range(len(rings) - 1):
        for k in range(5):
            bm.faces.new((rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]))
    bm.faces.new(rings[0][::-1])
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.25)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    o.append(B.obj_from_bm(bm, 'Hull', rust))
    # 얼룩·구멍 (짙은 녹 판과 검은 구멍)
    for k in range(9):
        y = -12 + k * 3.0
        sx = 1 if k % 2 else -1
        o.append(box((0.08, 2.2 + (k % 3), 1.2 + (k % 2)), rust2, loc=(sx * W * 0.5 * 0.99, y, 2.2 + (k % 3) * 0.5)))
    o.append(box((0.1, 2.4, 1.8), dark, loc=(W * 0.5 * 0.985, 3.0, 1.8)))
    o.append(box((0.1, 1.4, 1.1), dark, loc=(-W * 0.5 * 0.985, -6.0, 2.4)))
    # 갑판 · 선교 (고물 쪽 흰 상자, 창 줄) · 굴뚝 · 돛대
    o.append(box((W * 0.9, L * 0.8, 0.2), rust2, loc=(0, 0, 3.9)))
    o.append(box((5.2, 5.0, 3.6), white, loc=(0, 11.5, 5.8)))
    o.append(box((5.4, 0.1, 0.8), dark, loc=(0, 8.95, 6.6)))
    o.append(box((4.6, 4.4, 2.0), white, loc=(0, 11.5, 8.6)))
    o.append(box((4.8, 0.1, 0.7), dark, loc=(0, 9.25, 8.9)))
    o.append(cyl(0.9, 0.8, 3.2, rust2, loc=(0, 13.0, 10.6), seg=14))
    o.append(cyl(0.15, 0.1, 9.0, rust2, loc=(0, -6.0, 8.4), seg=8))
    o.append(tube([V((0, -6.0, 11.0)), V((0, -15.5, 5.6))], 0.05, rust2, sides=5))
    o.append(box((3.2, 0.12, 0.12), rust2, loc=(0, -6.0, 10.0)))
    return o


PROPS = {
    'buddha_gold': buddha, 'buddha_white': buddha, 'merlion': merlion, 'eagle': eagle,
    'longtail_a': longtail, 'longtail_b': longtail, 'balloon_a': balloon, 'balloon_b': balloon,
    'parasol_thatch': parasol_thatch, 'parasol_purple': parasol_purple, 'sunbed': sunbed, 'lifeguard': lifeguard,
    'singha': singha, 'shipwreck': shipwreck,
}
