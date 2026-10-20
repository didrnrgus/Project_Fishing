"""
석상·조각 소품을 "한 덩어리로 깎은" 모양으로 만드는 도구 + 다시 만든 석상 (2026-10-20, 사진 대조).
build_props.py가 불러 PROPS를 덮어쓴다 (같은 id면 이쪽이 이긴다).

방법: 부위(공·관·판)를 겹쳐 놓고 → 합쳐서 복셀 리메시(이음매 없이 녹아 붙는다) → 부드럽게 → 결 새기기(비늘·깃털·갈기 홈을
꼭짓점마다 함수로 밀고 당긴다) → 구름 노이즈로 닳은 겉면 → 줄여서(데시메이트) 내보낸다.
도형을 이어 붙인 티(구슬 갈기, 공 무릎)가 사라지고 조각한 돌처럼 보인다.

  merlion    센토사 머라이언 (사진: 갈기가 불꽃처럼 겹겹이 뒤로 흘러내린 사자 머리, 긴 주둥이, 벌린 입, 비늘 덮인 몸, 등 뒤로 오른 꼬리)
  eagle      랑카위 다타란 랑 독수리 (사진: 날개를 V자로 높이 들어 활짝 편 적갈색 독수리, 끝깃이 손가락처럼 갈라짐, 노란 부리, 바위 받침)
  singha     앙코르와트 사자상 (사진: 뒷다리를 접고 앞다리를 곧게 세워 가슴을 편 채 고개를 든 날씬한 사자, 얕게 새긴 갈기 말림, 사암 받침)
  shipwreck  자킨토스 파나기오티스호 (사진: 뱃머리가 들린 소형 화물선 선체, 고물 선교, 앞 돛대·하역 붐, 철판 이음매, 전체가 녹)

미리보기:  bpyenv/Scripts/python -I tools/blender/build_props.py --only merlion --preview3 Screenshots/props
"""
import math
import os

import bmesh
import bpy
import build_icons as B
from mathutils import Vector as V, noise

mat, cyl, box, sphere, tube = B.mat, B.cyl, B.box, B.sphere, B.tube


# ---------------------------------------------------------------- 도구

def ell(loc, scale, rot=(0, 0, 0), seg=32, rings=20):
    return B.sphere(1.0, None, loc=loc, scale=scale, rot=rot, seg=seg, rings=rings)


def strand(pts, r0, r1, sides=10):
    """끝으로 갈수록 가늘어지는 관 (갈기 타래·깃·발톱)."""
    return B.tube(pts, lambda u: r0 + (r1 - r0) * u, None, sides=sides)


def bezier(p0, p1, p2, p3, n=12):
    out = []
    for i in range(n + 1):
        t = i / n
        a = (1 - t) ** 3
        b = 3 * (1 - t) ** 2 * t
        c = 3 * (1 - t) * t * t
        d = t ** 3
        out.append(V(p0) * a + V(p1) * b + V(p2) * c + V(p3) * d)
    return out


def fuse(objs, name, voxel, smooth=4, carve=None, erode=(0.0, 1.0), target=60000, paint=None, mats=()):
    """부위를 합쳐 복셀 리메시 → 부드럽게 → carve(co, normal) 결 새기기 → 닳은 겉면 → 데시메이트 → 부위별 칠(paint)."""
    o = B.join(objs, name) if len(objs) > 1 else objs[0]
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    m = o.modifiers.new('Remesh', 'REMESH')
    m.mode = 'VOXEL'
    m.voxel_size = voxel
    m.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=m.name)
    if smooth:
        s = o.modifiers.new('Smooth', 'SMOOTH')
        s.factor = 0.6
        s.iterations = smooth
        bpy.ops.object.modifier_apply(modifier=s.name)
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.normal_update()
    amp, freq = erode
    for v in bm.verts:
        d = 0.0
        if carve:
            d += carve(v.co, v.normal)
        if amp:
            d += amp * (noise.noise(v.co * freq) + 0.5 * noise.noise(v.co * freq * 2.7 + V((3.1, 1.7, 0.4))))
        v.co += v.normal * d
    bm.to_mesh(o.data)
    bm.free()
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    if tris > target:
        dm = o.modifiers.new('Dec', 'DECIMATE')
        dm.ratio = target / tris
        bpy.ops.object.modifier_apply(modifier=dm.name)
    o.data.materials.clear()
    for mm in mats:
        o.data.materials.append(mm)
    for p in o.data.polygons:
        p.use_smooth = True
        if paint:
            p.material_index = paint(p.center, p.normal)
    return o


def preview3(objs, out, size=420):
    """앞 · 3/4 · 옆 세 장을 이어 붙인 미리보기 (원근 카메라, 앞 = -Y)."""
    from PIL import Image
    sc = bpy.context.scene
    sc.render.resolution_x = sc.render.resolution_y = size
    sc.cycles.samples = 32
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ V(c) for o in objs for c in o.bound_box]
    lo = V((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = V((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    ctr, rad = (lo + hi) / 2, (hi - lo).length / 2
    cam = sc.camera
    cam.data.type = 'PERSP'
    cam.data.lens = 50
    cam.data.shift_x = cam.data.shift_y = 0
    tiles = []
    for k, d in enumerate((V((0, -1, 0.15)), V((0.75, -0.75, 0.3)), V((1, 0, 0.1)))):
        d = d.normalized()
        cam.location = ctr + d * rad * 2.9
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        f = out[:-4] + f'_{k}.png'
        B.render(f)
        tiles.append(Image.open(f).convert('RGBA'))
    sheet = Image.new('RGBA', (size * 3, size), (110, 125, 140, 255))
    for k, t in enumerate(tiles):
        sheet.alpha_composite(t, (k * size, 0))
        os.remove(out[:-4] + f'_{k}.png')
    sheet.convert('RGB').save(out)


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def scale_pattern(u, w, rowh):
    """물고기 비늘: 엇갈린 줄마다 아래가 둥근 비늘. 0(홈) ~ 1(비늘 가운데 볼록). u = 둘레 길이, w = 높이."""
    row = math.floor(w / rowh)
    uu = u / (rowh * 1.15) + 0.5 * (row % 2)
    fu = uu - math.floor(uu) - 0.5
    fw = (w / rowh) - row
    # 비늘 하나: 위쪽(이전 줄에 덮인 쪽)은 낮고 아래 둥근 끝으로 갈수록 두툼, 테두리는 홈
    r = math.sqrt(fu * fu * 4 + (1 - fw) ** 2)
    edge = smoothstep(0.92, 1.0, r)
    return (1 - edge) * (0.35 + 0.65 * fw) - edge * 0.3


# ---------------------------------------------------------------- 머라이언

def merlion(_):
    """센토사 머라이언: 높이 약 12. 앞 = -Y."""
    stone = mat('#cfc6b6', 0.75)      # 사진의 베이지 회색 GRC
    shade = mat('#a89e8e', 0.8)       # 입 속·눈 그늘
    rockm = mat('#7d776c', 0.9)
    parts = []
    # 몸: 아래가 넓은 물고기 몸통이 앞으로 살짝 기울며 목까지 오른다 (앞뒤로 납작)
    spine = bezier((0, 0.4, 0.0), (0, 0.5, 2.5), (0, 0.0, 5.0), (0, -0.4, 7.2), 14)
    parts.append(B.tube(spine, lambda u: 2.4 - 0.8 * u, None, sides=28))
    for o in parts[-1:]:
        o.scale = (1.0, 0.82, 1.0)
    # 가슴 (머리 아래 두툼하게)
    parts.append(ell((0, -0.9, 6.4), (1.75, 1.35, 1.6)))
    # 머리: 두개골 + 긴 주둥이 + 넓은 코 + 볼 + 위턱 / 벌린 입 아래 아래턱 (틈은 리메시 뒤에도 남게 0.35)
    hp = V((0, -1.2, 8.6))
    parts.append(ell(tuple(hp), (1.45, 1.55, 1.45)))
    parts.append(ell(tuple(hp + V((0, -1.25, -0.25))), (1.0, 1.1, 0.75)))          # 주둥이
    parts.append(ell(tuple(hp + V((0, -2.15, 0.05))), (0.62, 0.42, 0.42)))         # 코끝
    parts.append(ell(tuple(hp + V((0, -1.6, 0.55))), (0.45, 0.8, 0.35), rot=(-0.35, 0, 0)))  # 콧등
    for sx in (-1, 1):
        parts.append(ell(tuple(hp + V((sx * 0.75, -1.55, -0.45))), (0.55, 0.65, 0.45)))  # 윗입술 (볼)
        parts.append(ell(tuple(hp + V((sx * 0.62, -1.05, 0.62))), (0.55, 0.45, 0.25), rot=(0.2, 0, sx * 0.3)))  # 눈두덩
    parts.append(ell(tuple(hp + V((0, -1.45, -1.35))), (0.75, 0.75, 0.32), rot=(0.25, 0, 0)))  # 아래턱
    for sx in (-1, 1):  # 송곳니
        parts.append(strand([hp + V((sx * 0.45, -1.95, -0.6)), hp + V((sx * 0.45, -2.0, -0.95))], 0.11, 0.03, 8))
    # 갈기: 얼굴 둘레(이마 → 볼 → 턱 밑)에서 뒤로 흐르는 불꽃 타래 세 겹. 끝은 바깥으로 말린다
    for layer, (rr, back, n, thick, drop) in enumerate(((1.5, 1.3, 13, 0.72, 1.5), (1.8, 2.1, 12, 0.8, 2.6), (1.95, 2.9, 10, 0.85, 4.0))):
        for k in range(n):
            a = math.pi * (-0.32 + 1.64 * k / (n - 1))       # 턱 밑 → 옆 → 정수리 → 옆 → 턱 밑
            c, s = math.cos(a), math.sin(a)
            root = hp + V((c * rr * 0.95, -0.45 + layer * 0.35, s * rr * 0.9 + 0.1))
            out = V((c, 0, s))
            w = 0.25 * math.sin(k * 1.7 + layer)
            # 위쪽 타래는 뒤·위로 솟았다가 뒤로 흘러내리고, 옆·아래 타래는 뒤·아래로
            fall = drop * (0.6 + 0.4 * (1 - abs(s)))
            p1 = root + out * 0.6 + V((0, back * 0.35, 0.3 * s))
            p2 = root + out * (0.9 + 0.25 * layer) + V((w, back * 0.8, -fall * 0.35))
            p3 = root + out * (1.1 + 0.3 * layer) + V((w * 2, back, -fall)) + V((0, 0, 0.35))   # 끝이 위로 말림
            parts.append(strand(bezier(root, p1, p2, p3, 10), thick, 0.16, 12))
    # 등 갈기: 목덜미에서 몸 뒤로 길게 흘러내린 타래
    for k in range(9):
        x = (k - 4) * 0.42
        root = hp + V((x, 0.9, 0.4 - abs(x) * 0.2))
        parts.append(strand(bezier(root, root + V((x * 0.3, 1.2, -0.8)), root + V((x * 0.5, 1.8, -3.0)), root + V((x * 0.6, 2.2, -5.2)), 12), 0.5, 0.12, 10))
    # 꼬리: 등 뒤에서 위로 휘어 올라 끝이 갈라진 지느러미
    tail = bezier((0, 1.8, 1.4), (0, 3.8, 2.0), (0, 4.2, 5.5), (0, 3.3, 7.6), 16)
    parts.append(B.tube(tail, lambda u: 1.1 - 0.85 * u, None, sides=18))
    tip = tail[-1]
    for sx in (-1, 1):
        parts.append(B.tube(bezier(tip, tip + V((sx * 0.5, -0.2, 0.6)), tip + V((sx * 1.3, -0.4, 1.3)), tip + V((sx * 1.9, -0.1, 1.5)), 8),
                            lambda u: 0.32 * (1 - 0.8 * u), None, sides=10))
    # 가슴지느러미 (몸 옆에 붙은 물결 판)
    for sx in (-1, 1):
        parts.append(ell((sx * 1.9, -0.3, 3.6), (0.35, 1.1, 1.4), rot=(0.4, sx * 0.3, 0)))

    def carve(co, n):
        # 몸통 비늘 (z 0.6~6.3, 머리·갈기 아래): 둘레 각 × 반지름 → 둘레 길이
        if co.z < 6.3 and co.z > 0.6 and co.y < 1.8:
            ang = math.atan2(co.x, -co.y)
            u = ang * 2.2
            return 0.035 * scale_pattern(u * 1.4, co.z, 0.3) * smoothstep(0.6, 1.2, co.z) * smoothstep(6.3, 5.6, co.z)
        # 갈기 결: 타래를 따라 가는 홈 (위치에 따라 줄무늬)
        if co.z > 6.5 and (co.y > hp.y - 0.2 or abs(co.x) > 1.3):
            return 0.05 * math.sin(co.x * 5 + co.z * 4 + co.y * 2)   # 타래를 따라 굵은 홈
        return 0.0

    def paint(c, n):
        if (c - (hp + V((0, -1.95, -0.75)))).length < 0.55:   # 벌린 입 속
            return 1
        return 0

    body = fuse(parts, 'Merlion', voxel=0.07, smooth=3, carve=carve, erode=(0.025, 1.4), target=70000, paint=paint, mats=(stone, shade))
    # 받침: 물결 바위 (사진: 바위·물결 위에 선다)
    base = B.lumpy(1.0, rockm, loc=(0, 0.8, 0.2), scale=(4.2, 4.0, 1.2), amp=0.22, freq=3.2, seed=11, seg=40, rings=18)
    return [body, base]


# ---------------------------------------------------------------- 독수리

def eagle(_):
    """다타란 랑 독수리: 높이 약 12 (바위 포함). 날개를 V자로 높이 들었다. 앞 = -Y."""
    brown = mat('#8a4524', 0.6)
    dark = mat('#5e2c16', 0.7)
    pale = mat('#b07a4e', 0.6)
    beak = mat('#d8a02c', 0.45)
    eye = mat('#2a1a10', 0.3)
    rockm = mat('#5a544c', 0.9)
    parts = []
    # 몸: 곧게 선 가슴 (위로 넓다가 꼬리 쪽으로 좁아진다)
    parts.append(B.tube(bezier((0, 0.5, 3.3), (0, 0.3, 4.6), (0, -0.2, 6.2), (0, -0.35, 7.4), 12),
                        lambda u: 1.0 + 0.45 * math.sin(math.pi * min(1, u * 1.1)), None, sides=24))
    parts.append(ell((0, -0.55, 6.1), (1.2, 0.95, 1.35)))                  # 가슴
    # 넓적다리 깃 + 정강이 + 발가락 셋·뒷발가락 하나 (바위를 움켜쥔다)
    for sx in (-1, 1):
        parts.append(ell((sx * 0.55, -0.15, 3.7), (0.55, 0.6, 0.8)))
        parts.append(strand([V((sx * 0.55, -0.25, 3.2)), V((sx * 0.6, -0.45, 2.55))], 0.28, 0.22, 10))
        for k in (-1, 0, 1):
            parts.append(strand(bezier((sx * 0.6, -0.5, 2.55), (sx * 0.6 + k * 0.2, -0.9, 2.55), (sx * 0.6 + k * 0.35, -1.25, 2.45), (sx * 0.6 + k * 0.4, -1.35, 2.2), 6), 0.13, 0.04, 8))
        parts.append(strand([V((sx * 0.6, -0.4, 2.55)), V((sx * 0.6, 0.05, 2.35))], 0.11, 0.04, 8))
    # 꼬리깃: 아래·뒤로 펼친 부채
    for k in range(7):
        a = (k - 3) * 0.13
        parts.append(strand([V((0, 0.9, 3.6)), V((math.sin(a) * 0.8, 2.0, 2.6)), V((math.sin(a) * 1.3, 2.7, 1.9))], 0.26, 0.14, 8))
    # 날개: 어깨에서 바깥·위로 V자, 앞전은 굵은 뼈대, 뒷전으로 깃 줄. 끝에 첫째날개깃 6장이 손가락처럼 갈라진다
    for sx in (-1, 1):
        sh = V((sx * 0.9, 0.0, 7.0))
        elbow = V((sx * 3.4, 0.5, 9.0))
        wrist = V((sx * 5.6, 0.7, 11.2))
        parts.append(strand(bezier(sh, sh + V((sx * 1.0, 0.1, 1.0)), elbow, wrist, 12), 0.55, 0.28, 12))   # 앞전
        # 깃 판: 앞전에서 뒤(+Y)·아래로 내려오는 깃 줄을 촘촘히 (줄마다 끝이 톱니)
        for i in range(16):
            t = i / 15
            root = sh.lerp(elbow, t * 2) if t < 0.5 else elbow.lerp(wrist, (t - 0.5) * 2)
            length = 2.6 - 0.9 * t
            tipp = root + V((sx * 0.15 * t, 1.0 + 0.4 * t, -length))
            parts.append(strand([root, root.lerp(tipp, 0.5) + V((0, 0.1, 0)), tipp], 0.36, 0.14, 8))
        for f in range(6):
            a = 0.32 * (f - 2.5)
            d = V((sx * math.cos(a) * 0.55, 0.25 + 0.15 * f, math.sin(a) * 0.55 + 0.55)).normalized()
            parts.append(strand([wrist + V((0, 0.2 * f, -0.2 * f)), wrist + d * 1.4 + V((0, 0.2 * f, -0.2 * f)), wrist + d * 2.6 + V((0, 0.2 * f, -0.2 * f)) + V((0, 0, 0.25))],
                                0.3, 0.06, 8))
    # 목·머리: 앞으로 내민 머리, 두툼한 눈두덩, 갈고리 부리
    hp = V((0, -1.0, 8.55))
    parts.append(strand([V((0, -0.3, 7.3)), V((0, -0.6, 8.0)), hp], 0.8, 0.62, 18))
    parts.append(ell(tuple(hp), (0.7, 0.85, 0.72)))
    for sx in (-1, 1):
        parts.append(ell(tuple(hp + V((sx * 0.42, -0.45, 0.25))), (0.32, 0.4, 0.14), rot=(0.1, 0, sx * 0.25)))   # 눈두덩
    beak_pts = bezier(hp + V((0, -0.55, 0.05)), hp + V((0, -1.25, 0.1)), hp + V((0, -1.75, -0.15)), hp + V((0, -1.6, -0.6)), 10)
    parts.append(B.tube(beak_pts, lambda u: 0.36 * (1 - 0.82 * u), None, sides=12))
    parts.append(ell(tuple(hp + V((0, -1.05, -0.3))), (0.28, 0.4, 0.15)))   # 아래 부리
    for sx in (-1, 1):
        parts.append(ell(tuple(hp + V((sx * 0.5, -0.55, 0.05))), (0.13, 0.13, 0.13)))

    def carve(co, n):
        # 깃털 비늘결: 가슴·목·넓적다리는 작은 비늘 무늬, 날개는 깃 줄을 따라 홈
        if abs(co.x) < 1.5 and co.z < 8.0 and co.z > 3.0:
            return 0.022 * scale_pattern(math.atan2(co.x, -co.y) * 2.6, co.z, 0.24)
        if abs(co.x) > 1.4:
            return 0.03 * math.sin(abs(co.x) * 7.0)
        return 0.0

    def paint(c, n):
        bt = beak_pts
        if min((c - p).length for p in bt) < 0.38 or (c - (hp + V((0, -1.05, -0.3)))).length < 0.35:
            return 3
        for sx in (-1, 1):
            if (c - (hp + V((sx * 0.5, -0.55, 0.05)))).length < 0.16:
                return 4
        if c.z > 7.6 and abs(c.x) < 1.1:
            return 2                                  # 머리·목: 연한 갈색 (사진: 머리가 몸보다 밝다)
        if abs(c.x) > 2.2 and n.y > 0.3:
            return 1                                  # 날개 뒷면 짙게
        return 0

    body = fuse(parts, 'Eagle', voxel=0.06, smooth=2, carve=carve, erode=(0.015, 2.0), target=80000, paint=paint, mats=(brown, dark, pale, beak, eye))
    base = B.lumpy(1.0, rockm, loc=(0, 0, 1.3), scale=(3.4, 2.9, 1.5), amp=0.24, freq=3.0, seed=9, seg=40, rings=18)
    return [body, base]


# ---------------------------------------------------------------- 앙코르 사자

def singha(_):
    """앙코르와트 사자상: 높이 약 3.2 (받침 포함). 앞다리를 곧게 세우고 가슴을 편 채 앉아 고개를 든다. 앞 = -Y."""
    stone = mat('#8f8a7c', 0.92)
    moss = mat('#5f6352', 0.95)
    dark = mat('#4e4a42', 0.95)
    parts = []
    z0 = 0.95  # 받침 윗면
    # 엉덩이·접은 뒷다리 (뒤 아래), 곧게 선 가슴통 (사진: 몸이 거의 세로)
    parts.append(ell((0, 0.38, z0 + 0.32), (0.4, 0.42, 0.34)))
    for sx in (-1, 1):
        parts.append(ell((sx * 0.27, 0.3, z0 + 0.28), (0.17, 0.36, 0.26)))
        parts.append(ell((sx * 0.25, 0.02, z0 + 0.08), (0.12, 0.26, 0.08)))        # 뒷발
    # 몸통: 엉덩이에서 가슴까지 겹친 타원체 (관을 쓰면 윗면이 잘려 보였다) + 넓은 어깨 + 굵은 목
    for (y, z, sx_, sy_, sz_) in ((0.25, 0.55, 0.36, 0.36, 0.34), (0.08, 0.9, 0.33, 0.32, 0.36), (-0.1, 1.2, 0.34, 0.3, 0.36)):
        parts.append(ell((0, y, z0 + z), (sx_, sy_, sz_)))
    for sx in (-1, 1):
        parts.append(ell((sx * 0.2, -0.2, z0 + 1.3), (0.17, 0.2, 0.26)))
    parts.append(ell((0, -0.24, z0 + 1.62), (0.24, 0.23, 0.3)))
    # 앞다리: 어깨에서 받침까지 곧게, 발목에서 살짝 굽고 발
    for sx in (-1, 1):
        parts.append(strand([V((sx * 0.19, -0.24, z0 + 1.2)), V((sx * 0.18, -0.31, z0 + 0.6)), V((sx * 0.18, -0.34, z0 + 0.12))], 0.13, 0.095, 12))
        parts.append(ell((sx * 0.18, -0.44, z0 + 0.06), (0.12, 0.16, 0.07)))
    # 머리: 고개를 들고 입을 크게 벌림. 두꺼운 눈두덩, 부릅뜬 눈, 넓적한 코, 말린 윗입술, 송곳니
    hp = V((0, -0.32, z0 + 1.95))
    parts.append(ell(tuple(hp), (0.26, 0.26, 0.26)))
    parts.append(ell(tuple(hp + V((0, -0.22, -0.02))), (0.21, 0.19, 0.13)))       # 윗주둥이
    parts.append(ell(tuple(hp + V((0, -0.36, 0.02))), (0.13, 0.07, 0.08)))        # 코
    for sx in (-1, 1):
        parts.append(ell(tuple(hp + V((sx * 0.12, -0.2, 0.13))), (0.1, 0.07, 0.055)))   # 눈두덩
        parts.append(ell(tuple(hp + V((sx * 0.13, -0.25, 0.08))), (0.05, 0.045, 0.05)))  # 눈알
        parts.append(ell(tuple(hp + V((sx * 0.22, 0.0, 0.2))), (0.06, 0.045, 0.09)))   # 귀
        parts.append(strand([hp + V((sx * 0.1, -0.33, -0.1)), hp + V((sx * 0.1, -0.34, -0.19))], 0.025, 0.008, 6))  # 송곳니
    parts.append(ell(tuple(hp + V((0, -0.25, -0.24))), (0.16, 0.14, 0.05), rot=(0.45, 0, 0)))   # 아래턱 (입 벌림)
    # 갈기: 얼굴 뒤 하나로 이어진 두툼한 깃 (방사 홈은 carve가 새긴다) + 가슴으로 흘러내린 갈기 판
    parts.append(ell(tuple(hp + V((0, 0.06, -0.05))), (0.42, 0.14, 0.42)))
    parts.append(ell((0, -0.33, z0 + 1.5), (0.26, 0.12, 0.3), rot=(-0.2, 0, 0)))
    # 꼬리: 엉덩이에서 등에 붙어 올라가 끝이 뭉친다 (막대처럼 서지 않게 몸 가까이)
    parts.append(strand(bezier((0, 0.6, z0 + 0.3), (0, 0.78, z0 + 0.65), (0, 0.6, z0 + 1.05), (0, 0.38, z0 + 1.3), 12), 0.075, 0.06, 10))
    parts.append(ell((0, 0.36, z0 + 1.34), (0.1, 0.1, 0.12)))

    def carve(co, n):
        # 갈기 깃: 얼굴 둘레 방사 홈 + 동심 고리 두 줄 (크메르 사자 갈기의 얕은 부조)
        d = co - (hp + V((0, 0.06, -0.05)))
        if abs(d.y) < 0.2 and d.x * d.x + d.z * d.z > 0.06 and co.z > z0 + 1.45:
            ang = math.atan2(d.z, d.x)
            r = math.sqrt(d.x * d.x + d.z * d.z)
            return 0.012 * math.cos(ang * 22) - 0.01 * (abs(math.sin(r * 38)) < 0.25)
        # 가슴 갈기: 아래로 흐르는 물결 홈
        if co.y < -0.3 and co.z > z0 + 1.2 and co.z < z0 + 1.8:
            return 0.008 * math.sin(co.x * 45 + math.sin(co.z * 30) * 2)
        return 0.0

    def paint(c, n):
        k = noise.noise(c * 3.0)
        if n.z > 0.62 and k > 0.0:
            return 1                                   # 위를 보는 면의 이끼·지의류 (빗물 줄은 셰이더가)
        return 0

    body = fuse(parts, 'Singha', voxel=0.016, smooth=2, carve=carve, erode=(0.01, 7.0), target=40000, paint=paint, mats=(stone, moss))
    # 받침 (사진: 장식 띠를 두른 사암 단, 모서리가 닳았다)
    base = box((0.95, 1.35, 0.7), stone, loc=(0, 0.05, 0.35), bevel=0.04)
    band = box((1.02, 1.42, 0.12), dark, loc=(0, 0.05, 0.62), bevel=0.02)
    top = box((0.9, 1.3, 0.18), stone, loc=(0, 0.05, z0 - 0.09), bevel=0.03)
    plinth = box((1.1, 1.5, 0.15), dark, loc=(0, 0.05, 0.075), bevel=0.02)
    return [body, base, band, top, plinth]


# ---------------------------------------------------------------- 난파선

def shipwreck(_):
    """파나기오티스호: 길이 약 32, 녹슨 소형 화물선. 뱃머리(-Y)가 들리고, 고물(+Y)에 선교. 철판 이음매 + 구멍."""
    rust = mat('#7c3d1e', 0.88)
    rust2 = mat('#5a2a14', 0.9)
    paint = mat('#9a8a78', 0.85)   # 벗겨진 흰 칠 (선교)
    dark = mat('#1c1612', 0.95)
    L, D = 32.0, 4.6
    # 선체: 길이 방향 단면 24개 × 반폭 단면 14점. 뱃머리는 날카로운 V, 가운데는 둥근 U, 고물은 둥근 선미
    bm = bmesh.new()
    nsec, nside = 26, 14
    rings = []
    for i in range(nsec + 1):
        t = i / nsec                  # 0 = 뱃머리(-Y), 1 = 고물
        y = -L / 2 + L * t
        beam = 3.6 * (math.sin(math.pi * min(1.0, t * 1.25 + 0.02)) ** 0.55 if t < 0.8 else 0.96 - 0.6 * ((t - 0.8) / 0.2) ** 2)
        beam = max(beam, 0.05)
        sheer = D + 1.6 * (1 - t) ** 3 + 0.4 * t ** 4        # 뱃머리가 들린 갑판선
        keel = 0.0 if t > 0.06 else 1.6 * (1 - t / 0.06)     # 뱃머리 밑이 위로
        vee = 1.0 - 0.75 * smoothstep(0.35, 0.0, t)          # 뱃머리일수록 V
        ring = []
        for k in range(nside + 1):
            a = k / nside                  # 0 = 왼쪽 갑판 가장자리 → 0.5 용골 → 1 오른쪽
            side = -1 if a < 0.5 else 1
            q = abs(a - 0.5) * 2           # 0 용골 ~ 1 갑판
            # 단면: 바닥은 U(vee=1)~V(vee 작음), 위는 거의 수직 뱃전
            hw = beam * (q ** (0.35 * vee + 0.6 * (1 - vee)))
            z = keel + (sheer - keel) * (q ** 2.2 if vee > 0.5 else q ** 1.2)
            ring.append(bm.verts.new((side * hw, y, z)))
        rings.append(ring)
    for i in range(nsec):
        for k in range(nside):
            bm.faces.new((rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]))
    bm.faces.new(rings[-1][::-1])                       # 고물 판
    bm.faces.new(rings[0])                              # 뱃머리 끝 (열려 있으면 앞에서 속이 보였다)
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.22)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    # 철판 이음매: 세로(늑골 간격 1.6m)·가로(1.2m) 줄을 홈으로, 아래는 거뭇한 띠
    bm.normal_update()
    for v in bm.verts:
        gy = abs(((v.co.y / 1.6) % 1.0) - 0.5)
        gz = abs(((v.co.z / 1.2) % 1.0) - 0.5)
        if gy > 0.47 or gz > 0.47:
            v.co -= v.normal * 0.035
    hull = B.obj_from_bm(bm, 'Hull', rust)
    hull.data.materials.append(rust2)
    for p in hull.data.polygons:
        # 흘수선 아래 짙은 녹 띠 (경계는 물결) — 얼룩은 셰이더 빗물 줄이 그린다
        p.material_index = 1 if p.center.z < 1.5 + 0.4 * math.sin(p.center.y * 0.7) else 0
    o = [hull]
    # 갑판 (뱃전 안쪽) · 구멍 몇 개 (검은 판)
    o.append(box((6.4, L * 0.78, 0.18), rust2, loc=(0, 0.6, D - 0.25)))
    for (x, y, z, w, h) in ((3.55, 4.0, 2.4, 2.2, 1.6), (-3.55, -5.0, 2.8, 1.5, 1.1), (3.5, -9.0, 3.2, 1.0, 0.8)):
        o.append(box((0.3, w, h), dark, loc=(x, y, z)))
    # 고물 선교: 두 층 + 창 줄 + 위 난간 · 굴뚝
    o.append(box((5.6, 5.4, 3.0), paint, loc=(0, 11.6, D + 1.5), bevel=0.05))
    o.append(box((5.7, 0.12, 0.8), dark, loc=(0, 8.9, D + 2.0)))
    o.append(box((4.6, 4.2, 2.2), paint, loc=(0, 11.9, D + 4.1), bevel=0.05))
    o.append(box((4.7, 0.12, 0.7), dark, loc=(0, 9.75, D + 4.4)))
    o.append(box((6.4, 5.0, 0.15), rust2, loc=(0, 11.8, D + 3.0)))           # 선교 날개 갑판
    o.append(cyl(0.85, 0.75, 3.0, rust2, loc=(0, 13.4, D + 6.6), seg=16))
    o.append(cyl(0.95, 0.95, 0.25, dark, loc=(0, 13.4, D + 8.1), seg=16))
    # 앞 돛대 + 하역 붐 (비스듬히) + 돛대 받침 기둥 둘 + 화물창 덮개
    o.append(cyl(0.22, 0.12, 9.5, rust2, loc=(0, -6.0, D + 4.75), seg=10))
    o.append(tube([V((0, -6.0, D + 1.2)), V((0, 3.0, D + 4.6))], 0.12, rust2, sides=8))
    o.append(tube([V((0, -6.0, D + 9.3)), V((0, -14.8, D + 1.6))], 0.04, rust2, sides=5))
    o.append(tube([V((0, -6.0, D + 9.3)), V((0, 10.0, D + 5.2))], 0.04, rust2, sides=5))
    o.append(box((3.8, 0.18, 0.18), rust2, loc=(0, -6.0, D + 7.8)))
    o.append(box((4.2, 7.0, 0.9), rust, loc=(0, 0.8, D + 0.2), bevel=0.04))
    o.append(box((4.2, 4.6, 0.9), rust, loc=(0, -10.5, D + 0.6), bevel=0.04))
    # 뱃전 난간 (기둥 + 가로대)
    for sx in (-1, 1):
        for y in [(-12 + 2.0 * i) for i in range(12)]:
            o.append(cyl(0.04, 0.04, 0.9, rust2, loc=(sx * 3.4, y, D + 0.45), seg=5))
        o.append(tube([V((sx * 3.4, -12.5, D + 0.9)), V((sx * 3.4, 10.0, D + 0.9))], 0.035, rust2, sides=5))
    return o


# ---------------------------------------------------------------- 대불

def buddha(pid):
    """대불 (푸켓 하얀 대불 · 코사무이 황금 대불): props_sea.buddha의 부위를 그대로 쓰되, 받침 위 몸(다리·몸통·팔·머리)을
    한 덩어리로 녹여 붙인다 (구슬 무릎·관 팔 이음매가 사라진 매끈한 조각). 소라 머리 결은 carve로 새긴다."""
    import props_sea
    gold = pid == 'buddha_gold'
    objs = props_sea.buddha(pid)
    bpy.context.view_layer.update()
    base, figure = [], []
    for o in objs:
        c = sum((o.matrix_world @ V(b) for b in o.bound_box), V()) / 8
        (base if c.z < 2.95 else figure).append(o)
    skin = mat('#d8a73e', 0.28, 0.85) if gold else mat('#f0ece4', 0.55)
    hair = mat('#7a5418', 0.35, 0.8) if gold else mat('#b8b2a8', 0.7)
    zh = 2.9 + 5.75

    def carve(co, n):
        # 나발 (소라 머리): 머리 윗부분에 작은 동그란 돌기 격자
        if co.z > zh + 0.05 and co.y > -0.9:
            return 0.03 * (math.sin(co.x * 28) * math.sin(co.y * 28) * math.sin(co.z * 28) > 0.2)
        # 가사 주름: 몸통에 비스듬한 홈
        if 3.3 < co.z < 7.6:
            return 0.02 * math.sin((co.x * 0.7 + co.z) * 6.0)
        return 0.0

    def paint(c, n):
        return 1 if (c.z > zh + 0.1 and c.y > -0.95 and (c - V((0, -0.1, zh))).length > 0.6) else 0

    body = fuse(figure, 'Buddha', voxel=0.045, smooth=2, carve=carve, erode=(0.008, 2.0), target=70000, paint=paint, mats=(skin, hair))
    # 연꽃 받침: 작은 구슬 꽃잎(이빨처럼 보였다) 대신 넓적하고 끝이 뾰족한 꽃잎 두 겹을 받침과 한 덩어리로
    plinth = [o for o in base if o.name.startswith('Cyl')]
    lotus = [o for o in base if not o.name.startswith('Cyl') and not o.name.startswith('Sph')]
    for o in base:
        if o.name.startswith('Sph'):
            bpy.data.objects.remove(o, do_unlink=True)
    petal_m = lotus[0].data.materials[0] if lotus and lotus[0].data.materials else skin
    for row, (n, rr, z, tilt) in enumerate(((16, 4.0, 2.25, 0.55), (16, 3.6, 2.7, 0.3))):
        for k in range(n):
            a = 2 * math.pi * (k + 0.5 * row) / n
            c, sn = math.cos(a), math.sin(a)
            # 꽃잎: 넓적한 조개 모양 판이 바깥으로 젖혀지고, 위 끝에 짧은 뾰족 끝 (사진: 대불 연화좌)
            lotus.append(ell((c * rr, sn * rr, z), (0.95, 0.24, 0.72), rot=(tilt + 0.25, 0, a + math.pi / 2)))
            tip0 = V((c * (rr + 0.12 + tilt * 0.25), sn * (rr + 0.12 + tilt * 0.25), z + 0.62))
            lotus.append(B.tube([tip0 - V((c * 0.12, sn * 0.12, 0.3)), tip0, tip0 + V((c * 0.12, sn * 0.12, 0.22))], lambda u: 0.16 * (1 - u) + 0.02, None, sides=8))
    lot = fuse(lotus, 'Lotus', voxel=0.05, smooth=2, erode=(0.006, 2.0), target=30000, mats=(petal_m,))
    return plinth + [lot, body]


# ---------------------------------------------------------------- 돌하르방

def hareubang(_):
    """제주 돌하르방: 높이 약 2.55 (받침 포함). 사진: 벙거지 모자, 툭 튀어나온 왕방울 눈, 길고 넓은 코, 꾹 다문 입,
    배 위에 한 손은 위·한 손은 아래로 얹은 손, 위아래 굵기가 거의 같은 몸. 구멍 숭숭한 현무암 (결은 Unity 셰이더). 앞 = -Y."""
    stone = mat('#5c5a56', 0.95)
    dark = mat('#3a3936', 0.95)
    parts = []
    z0 = 0.3
    # 몸: 위아래 굵기가 거의 같은 기둥 + 둥근 어깨 (관 윗면이 잘려 보이지 않게 타원체를 겹친다)
    parts.append(ell((0, 0, z0 + 0.5), (0.34, 0.29, 0.55)))
    parts.append(B.cyl(0.34, 0.33, 0.5, None, loc=(0, 0, z0 + 0.25), seg=28))   # 아래는 곧은 기둥 (받침에 평평하게 선다)
    parts[-1].scale = (1.0, 0.86, 1.0)
    parts.append(ell((0, 0, z0 + 0.95), (0.35, 0.3, 0.3)))
    parts.append(ell((0, 0, z0 + 1.12), (0.31, 0.26, 0.16)))
    # 머리: 몸만큼 크고 길쭉, 어깨에 묻힌다 (목이 없다)
    hz = z0 + 1.45
    parts.append(ell((0, -0.02, hz), (0.33, 0.3, 0.4)))
    # 모자: 넓적한 챙 + 둥근 꼭대기 (벙거지)
    parts.append(ell((0, 0.0, hz + 0.36), (0.36, 0.34, 0.06)))
    parts.append(ell((0, 0.0, hz + 0.47), (0.25, 0.24, 0.14)))
    # 왕방울 눈: 크게 튀어나온 둥근 눈알
    for sx in (-1, 1):
        parts.append(ell((sx * 0.14, -0.26, hz + 0.08), (0.12, 0.08, 0.1)))
    # 코: 미간에서 시작해 아래로 넓어지는 큰 주먹코 + 콧방울
    parts.append(B.tube([V((0, -0.27, hz + 0.03)), V((0, -0.33, hz - 0.09)), V((0, -0.35, hz - 0.16))], lambda u: 0.05 + 0.045 * u, None, sides=12))
    for sx in (-1, 1):
        parts.append(ell((sx * 0.07, -0.31, hz - 0.16), (0.05, 0.045, 0.04)))
    # 입: 꾹 다문 두툼한 입술
    parts.append(ell((0, -0.28, hz - 0.27), (0.12, 0.045, 0.04)))
    # 귀: 길게 늘어진 귀
    for sx in (-1, 1):
        parts.append(ell((sx * 0.32, 0.0, hz - 0.02), (0.045, 0.08, 0.13)))
    # 팔·손: 몸 겉에 얕게 도드라진 부조 (오른손은 가슴 아래·왼손은 배꼽 아래, 한 손이 위)
    for sx, hzz in ((1, z0 + 0.86), (-1, z0 + 0.66)):
        parts.append(B.tube(bezier((sx * 0.3, -0.12, z0 + 1.08), (sx * 0.34, -0.22, hzz + 0.2), (sx * 0.24, -0.27, hzz + 0.02), (sx * 0.02, -0.28, hzz), 8),
                            lambda u: 0.085 - 0.01 * u, None, sides=10))
        parts.append(ell((sx * 0.03, -0.28, hzz), (0.15, 0.06, 0.06)))

    eyes = [V((sx * 0.14, -0.34, hz + 0.08)) for sx in (-1, 1)]

    def carve(co, n):
        d = 0.0
        for e in eyes:   # 눈알 가운데 동그란 홈 (사진의 패인 눈동자)
            r = (co - e).length
            if r < 0.055:
                d -= 0.025 * (1 - r / 0.055)
        if abs(co.z - (hz - 0.27)) < 0.008 and co.y < -0.3 and abs(co.x) < 0.11:
            d -= 0.012     # 입술 사이 홈
        for sx, hzz in ((1, z0 + 0.88), (-1, z0 + 0.66)):   # 손가락 홈 넷
            if abs(co.z - hzz) < 0.055 and co.y < -0.31 and 0 < co.x * sx < 0.16:
                d -= 0.008 * (abs(math.sin(co.x * 90)) > 0.8)
        return d

    body = fuse(parts, 'Hareubang', voxel=0.012, smooth=2, carve=carve, erode=(0.006, 14.0), target=30000, mats=(stone,))
    base = box((0.82, 0.82, 0.3), stone, loc=(0, 0, 0.15), bevel=0.03)
    return [body, base]


PROPS = {'buddha_white': buddha, 'buddha_gold': buddha, 'merlion': merlion, 'eagle': eagle, 'singha': singha, 'shipwreck': shipwreck, 'hareubang': hareubang}
