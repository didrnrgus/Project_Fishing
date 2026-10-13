"""
아이템 아이콘 + 잡동사니 모델을 만든다. docs/09-art-plan.md 4장.

  python -I tools/blender/build_icons.py                  # 모든 아이콘 + 잡동사니 FBX
  python -I tools/blender/build_icons.py --only rod_basic,bait_worm
  python -I tools/blender/build_icons.py --sheet docs/images/33_item_icons.png

결과 (Unity):
  Assets/Resources/Icons/Items/<아이템 Id>.png   256×256, 투명 배경. 모두 같은 각도(비스듬히 3/4)·조명(왼쪽 위 주광 + 테두리 빛)
  Assets/Resources/Junk/<잡동사니 Id>.fbx         잡동사니 3D 모델 (파이팅·결과 화면). 길이 1m 기준, 머리(앞) = +Z
등급 테두리나 배경은 아이콘에 굽지 않는다 (UI에서 그린다).
"""
import math
import os
import sys

ARGS = sys.argv[1:]
sys.argv = [sys.argv[0]]
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT_ICON = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Items')
OUT_JUNK = os.path.join(ROOT, 'Assets', 'Resources', 'Junk')
OUT_JUNK_ICON = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Junk')

import bpy  # noqa: E402
import bmesh  # noqa: E402
import mathutils  # noqa: E402
from mathutils import Vector, Matrix  # noqa: E402

V = Vector


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


def hexc(h, a=1.0):
    h = h.lstrip('#')
    # sRGB → 선형 (Blender 색 입력은 선형)
    def lin(c):
        c = int(c, 16) / 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (lin(h[0:2]), lin(h[2:4]), lin(h[4:6]), a)


# =====================================================================
#  기본 도구
# =====================================================================

_mats = {}


def mat(color, rough=0.5, metal=0.0, trans=0.0, emit=0.0, film=0.0, coat=0.0, alpha=1.0, name=None):
    key = (color, rough, metal, trans, emit, film, coat, alpha)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(name or f'M{len(_mats)}')
    m.use_nodes = True
    b = m.node_tree.nodes['Principled BSDF']
    c = hexc(color) if isinstance(color, str) else color
    b.inputs['Base Color'].default_value = c
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    for nm, val in (('Transmission Weight', trans), ('Coat Weight', coat), ('Alpha', alpha), ('Thin Film Thickness', film)):
        if nm in b.inputs and val:
            b.inputs[nm].default_value = val
    if emit:
        b.inputs['Emission Color'].default_value = c
        b.inputs['Emission Strength'].default_value = emit
    m.diffuse_color = c  # FBX 내보내기 기본 색
    _mats[key] = m
    return m


def link(ob):
    bpy.context.scene.collection.objects.link(ob)
    return ob


def obj_from_bm(bm, name, m):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    ob = link(bpy.data.objects.new(name, me))
    if m:
        ob.data.materials.append(m)
    return ob


def cyl(r1, r2, depth, m, loc=(0, 0, 0), rot=(0, 0, 0), seg=28, cap=True, name='Cyl'):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap, cap_tris=False, segments=seg, radius1=r1, radius2=r2, depth=depth)
    ob = obj_from_bm(bm, name, m)
    ob.location = loc
    ob.rotation_euler = rot
    return ob


def sphere(r, m, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), seg=24, rings=14, name='Sph'):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=r)
    ob = obj_from_bm(bm, name, m)
    ob.location, ob.scale, ob.rotation_euler = loc, scale, rot
    return ob


def box(size, m, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.0, name='Box'):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = V((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    if bevel:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=3, affect='EDGES', profile=0.5)
    ob = obj_from_bm(bm, name, m)
    for p in ob.data.polygons:
        p.use_smooth = bevel > 0
    ob.location, ob.rotation_euler = loc, rot
    return ob


def torus(R, r, m, loc=(0, 0, 0), rot=(0, 0, 0), seg=32, mseg=10, arc=2 * math.pi, name='Torus'):
    bm = bmesh.new()
    rings = []
    closed = arc >= 2 * math.pi - 1e-6
    n = seg if closed else seg + 1
    for i in range(n):
        a = arc * i / seg
        c = V((math.cos(a) * R, math.sin(a) * R, 0))
        out = V((math.cos(a), math.sin(a), 0))
        ring = [bm.verts.new(c + out * math.cos(2 * math.pi * k / mseg) * r + V((0, 0, 1)) * math.sin(2 * math.pi * k / mseg) * r) for k in range(mseg)]
        rings.append(ring)
    for i in range(seg if closed else seg):
        a, b = rings[i], rings[(i + 1) % n]
        for k in range(mseg):
            bm.faces.new((a[k], a[(k + 1) % mseg], b[(k + 1) % mseg], b[k]))
    ob = obj_from_bm(bm, name, m)
    ob.location, ob.rotation_euler = loc, rot
    return ob


def tube(points, r, m, r_end=None, sides=12, name='Tube', cap=True):
    """점들을 따라 가는 관 (지렁이, 낚싯바늘, 철사)."""
    bm = bmesh.new()
    pts = [V(p) for p in points]
    n = len(pts)
    rings = []
    prev_a = None
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        a = (prev_a - t * prev_a.dot(t)).normalized() if prev_a is not None else t.orthogonal().normalized()
        prev_a = a
        b = t.cross(a)
        rr = r if r_end is None else r + (r_end - r) * i / (n - 1)
        rr = rr(i / (n - 1)) if callable(rr) else rr
        rings.append([bm.verts.new(p + (a * math.cos(2 * math.pi * k / sides) + b * math.sin(2 * math.pi * k / sides)) * rr) for k in range(sides)])
    for i in range(n - 1):
        for k in range(sides):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k]))
    if cap:
        for ring, p in ((rings[0], pts[0]), (rings[-1], pts[-1])):
            c = bm.verts.new(p)
            for k in range(sides):
                try:
                    bm.faces.new((ring[k], ring[(k + 1) % sides], c))
                except ValueError:
                    pass
    return obj_from_bm(bm, name, m)


def lumpy(r, m, loc=(0, 0, 0), scale=(1, 1, 1), amp=0.18, freq=3.0, seed=0, seg=32, rings=20, name='Lump'):
    """울퉁불퉁한 덩어리 (떡밥, 살점)."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=r)
    for v in bm.verts:
        p = v.co.normalized()
        n = (math.sin(p.x * freq * 3.1 + seed) * math.sin(p.y * freq * 2.7 + seed * 1.7) * math.sin(p.z * freq * 3.3 + seed * 0.7)
             + 0.5 * math.sin(p.x * freq * 7.1 + p.z * 5.3 + seed * 2.1))
        v.co = v.co * (1 + amp * n)
    ob = obj_from_bm(bm, name, m)
    ob.location, ob.scale = loc, scale
    return ob


def curve_pts(fn, n):
    return [fn(i / (n - 1)) for i in range(n)]


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    # 합친 물체는 첫 물체의 회전·크기를 이어받는다. 나중에 rotation_euler를 덮어써도 모양이 안 바뀌게 굳힌다
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return o


# =====================================================================
#  모델: 장비
# =====================================================================

ROD_STYLE = {  # TelescopicRod.StyleFor 와 같은 색
    'rod_basic': ('#c02234', '#ccccd6', '#222222', 4, 'ring'),
    'rod_carbon': ('#121214', '#d9ad40', '#9e7347', 5, 'carbon'),
    'rod_bamboo': ('#c7a35c', '#59381f', '#8c6133', 5, 'bamboo'),
    'rod_heavy': ('#1a265c', '#d1d6e0', '#1f1f1f', 4, 'heavy'),
    'rod_gege': ('#e6e0d1', '#ebb84d', '#52331f', 5, 'pearl'),
}


def model_rod(iid):
    blank, ferrule, grip, sections, kind = ROD_STYLE[iid]
    mb = mat(blank, 0.25, coat=0.6) if kind != 'pearl' else mat(blank, 0.2, film=350, coat=0.8)
    mf = mat(ferrule, 0.25, metal=0.9)
    mg = mat(grip, 0.75)
    L = 1.6
    rb, rt = 0.036, 0.008
    if kind == 'heavy':
        rb, rt = 0.044, 0.011
    objs = []
    # 손잡이 (코르크/고무) + 릴 시트
    objs.append(cyl(rb * 1.25, rb * 1.15, 0.36, mg, loc=(0, 0, 0.18)))
    objs.append(cyl(rb * 1.3, rb * 1.3, 0.04, mf, loc=(0, 0, 0.0)))
    objs.append(cyl(rb * 1.15, rb * 1.15, 0.1, mf, loc=(0, 0, 0.41)))
    # 마디: 끝으로 갈수록 가늘다
    z = 0.46
    seg_len = (L - z) / sections
    for i in range(sections):
        r0 = rb + (rt - rb) * i / sections
        r1 = rb + (rt - rb) * (i + 1) / sections
        objs.append(cyl(r0, r1, seg_len, mb, loc=(0, 0, z + seg_len / 2), seg=20))
        if i < sections - 1:
            objs.append(cyl(r1 * 1.25, r1 * 1.18, 0.025, mf, loc=(0, 0, z + seg_len - 0.005), seg=20))
        # 줄 고리 (가이드)
        gz = z + seg_len * 0.6
        gr = 0.022 + 0.012 * (1 - i / sections) + (0.008 if kind == 'heavy' else 0)
        objs.append(torus(gr, 0.0035, mf, loc=(0, -(r0 + gr + 0.006), gz), rot=(0, math.pi / 2, 0), seg=20, mseg=6))
        objs.append(cyl(0.002, 0.002, gr * 1.2, mf, loc=(0, -(r0 + gr * 0.5), gz), rot=(math.pi / 2, 0, 0), seg=6))
        if kind == 'bamboo':
            for k in range(2):
                bz = z + seg_len * (0.3 + 0.4 * k)
                rr = r0 + (r1 - r0) * (0.3 + 0.4 * k)
                objs.append(torus(rr, 0.004, mat('#4a2e18', 0.5), loc=(0, 0, bz), seg=20, mseg=6))
        z += seg_len
    objs.append(sphere(rt * 1.4, mf, loc=(0, 0, L)))
    o = join(objs, 'Rod')
    o.rotation_euler = (0, math.radians(-50), 0)
    return [o], 'long'


REEL_STYLE = {
    'reel_basic': ('#b3b5bd', '#333338', '#9ea1a8', 1.0, False),
    'reel_power': ('#141417', '#e6b340', '#d9a838', 1.2, False),
    'reel_hispeed': ('#264d99', '#d9dde6', '#bfc7d9', 1.1, True),
    'reel_napal': ('#edd9b3', '#b85c29', '#d98c4d', 1.15, False),
}


def model_reel(iid):
    body, accent, spool, size, double = REEL_STYLE[iid]
    mb = mat(body, 0.3, metal=0.6)
    ma = mat(accent, 0.3, metal=0.7)
    ms = mat(spool, 0.25, metal=0.8)
    ml = mat('#e8f0e8', 0.6)
    mk = mat('#1a1a1a', 0.5)
    objs = []
    # 다리 + 몸통
    objs.append(box((0.03, 0.14, 0.02), mb, loc=(0, 0, 0.2), bevel=0.006))
    objs.append(box((0.025, 0.03, 0.12), mb, loc=(0, 0, 0.13), bevel=0.008))
    objs.append(sphere(0.07, mb, loc=(0, 0, 0.04), scale=(0.8, 1.1, 0.9)))
    objs.append(cyl(0.062, 0.062, 0.012, ma, loc=(0.055, 0, 0.04), rot=(0, math.pi / 2, 0)))
    # 로터 + 스풀 (앞쪽 +y 방향)
    objs.append(cyl(0.05, 0.065, 0.06, mb, loc=(0, -0.09, 0.04), rot=(math.pi / 2, 0, 0)))
    objs.append(cyl(0.06, 0.06, 0.07, ms, loc=(0, -0.155, 0.04), rot=(math.pi / 2, 0, 0)))
    objs.append(cyl(0.052, 0.052, 0.055, ml, loc=(0, -0.155, 0.04), rot=(math.pi / 2, 0, 0)))
    objs.append(cyl(0.066, 0.06, 0.012, ms, loc=(0, -0.195, 0.04), rot=(math.pi / 2, 0, 0)))
    objs.append(cyl(0.025, 0.012, 0.03, ma, loc=(0, -0.215, 0.04), rot=(math.pi / 2, 0, 0)))
    # 베일 (반원 철사)
    objs.append(torus(0.075, 0.004, ms, loc=(0, -0.12, 0.04), rot=(math.pi / 2, 0, 0), arc=math.pi, seg=24, mseg=8))
    # 손잡이: 팔 + 노브
    objs.append(cyl(0.012, 0.012, 0.06, ma, loc=(-0.075, 0, 0.04), rot=(0, math.pi / 2, 0)))
    arm = [(-0.1, 0, 0.04), (-0.1, 0.01, 0.09), (-0.1, 0.02, 0.13)]
    objs.append(tube(arm, 0.008, ma))
    objs.append(cyl(0.014, 0.016, 0.04, mk, loc=(-0.125, 0.02, 0.13), rot=(0, math.pi / 2, 0)))
    if double:
        arm2 = [(-0.1, 0, 0.04), (-0.1, -0.01, -0.01), (-0.1, -0.02, -0.05)]
        objs.append(tube(arm2, 0.008, ma))
        objs.append(cyl(0.014, 0.016, 0.04, mk, loc=(-0.125, -0.02, -0.05), rot=(0, math.pi / 2, 0)))
    o = join(objs, 'Reel')
    o.scale = (size, size, size)
    o.rotation_euler = (0, 0, math.radians(200))
    return [o], 'item'


def model_line(iid):
    objs = []
    mspool = mat('#2a3440', 0.4) if iid != 'line_pe' else mat('#1c1c22', 0.4)
    objs.append(cyl(0.11, 0.11, 0.018, mspool, loc=(0, 0, 0.0)))
    objs.append(cyl(0.11, 0.11, 0.018, mspool, loc=(0, 0, 0.12)))
    objs.append(cyl(0.03, 0.03, 0.14, mat('#e0e0e0', 0.3, metal=0.5), loc=(0, 0, 0.06)))
    if iid == 'line_nylon':
        objs.append(cyl(0.09, 0.09, 0.1, mat('#f2f6f2', 0.15, trans=0.6, coat=0.5), loc=(0, 0, 0.06), seg=40))
    elif iid == 'line_carbon':
        objs.append(cyl(0.09, 0.09, 0.1, mat('#b8c4cc', 0.08, metal=0.4, coat=1.0), loc=(0, 0, 0.06), seg=40))
    else:
        cols = ['#e8402a', '#2a7ae8', '#f0d020', '#30b040', '#f0f0f0']
        for i, c in enumerate(cols):
            objs.append(cyl(0.09, 0.09, 0.1 / len(cols), mat(c, 0.6), loc=(0, 0, 0.012 + 0.1 * (i + 0.5) / len(cols)), seg=40))
    # 풀린 줄 끝
    objs.append(tube([(0.09, 0.0, 0.08), (0.14, -0.04, 0.1), (0.18, -0.02, 0.16), (0.2, 0.03, 0.2)], 0.0025,
                     mat('#f0f0f0', 0.3) if iid != 'line_pe' else mat('#e8402a', 0.6)))
    o = join(objs, 'Line')
    o.rotation_euler = (math.radians(20), 0, 0)
    return [o], 'item'


def model_boat(iid):
    objs = []
    if iid == 'boat_rubber':
        mg, mo = mat('#5a5e64', 0.6), mat('#e8742a', 0.55)
        # U자 고무 튜브: 뒤쪽 두 개 + 앞 반원
        objs.append(cyl(0.09, 0.09, 0.9, mg, loc=(0.25, 0.1, 0.09), rot=(math.pi / 2, 0, 0)))
        objs.append(cyl(0.09, 0.09, 0.9, mg, loc=(-0.25, 0.1, 0.09), rot=(math.pi / 2, 0, 0)))
        objs.append(torus(0.25, 0.09, mg, loc=(0, -0.35, 0.09), rot=(0, 0, math.pi), arc=math.pi, seg=24, mseg=16))
        for x in (0.25, -0.25):
            objs.append(sphere(0.09, mg, loc=(x, 0.55, 0.09)))
            objs.append(cyl(0.092, 0.092, 0.04, mo, loc=(x, 0.3, 0.09), rot=(math.pi / 2, 0, 0)))
        objs.append(box((0.48, 0.95, 0.02), mat('#3a3e44', 0.7), loc=(0, 0.1, 0.02)))
        objs.append(box((0.5, 0.12, 0.03), mo, loc=(0, 0.15, 0.14), bevel=0.01))  # 좌석
        # 노
        objs.append(cyl(0.008, 0.008, 0.8, mat('#c8a060', 0.6), loc=(0.35, 0.1, 0.22), rot=(math.radians(80), 0, math.radians(10))))
    else:
        mw, mbl = mat('#f4f4f2', 0.25, coat=0.8), mat('#2a5ad8', 0.3, coat=0.6)
        bm = bmesh.new()
        # 선체: 뱃머리가 뾰족한 V자 단면
        prof = [(-0.5, 0.0), (-0.3, 0.75), (0.0, 1.0), (0.4, 1.0), (0.55, 0.95)]
        rings = []
        for y, w in prof:
            hw = 0.26 * w
            ring = [bm.verts.new((-hw, y, 0.16)), bm.verts.new((-hw * 0.9, y, 0.04)), bm.verts.new((0, y, -0.03 + 0.03 * (y < -0.3))),
                    bm.verts.new((hw * 0.9, y, 0.04)), bm.verts.new((hw, y, 0.16))]
            rings.append(ring)
        for i in range(len(rings) - 1):
            for k in range(4):
                bm.faces.new((rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]))
        bm.faces.new(rings[-1][::-1])
        ob = obj_from_bm(bm, 'Hull', mw)
        bpy.context.view_layer.objects.active = ob
        sm = ob.modifiers.new('S', 'SUBSURF')
        sm.levels = 2
        bpy.ops.object.modifier_apply(modifier='S')
        objs.append(ob)
        objs.append(box((0.5, 0.95, 0.03), mbl, loc=(0, 0.02, 0.1)))  # 파란 줄
        objs.append(box((0.42, 0.7, 0.02), mat('#d8d4c8', 0.6), loc=(0, 0.12, 0.13)))  # 갑판
        objs.append(box((0.3, 0.06, 0.16), mat('#88b8e0', 0.05, trans=0.8), loc=(0, -0.05, 0.22), rot=(math.radians(-20), 0, 0)))  # 앞유리
        objs.append(box((0.12, 0.12, 0.2), mat('#1a1a1c', 0.4), loc=(0, 0.62, 0.15), bevel=0.02))  # 엔진
        objs.append(box((0.04, 0.04, 0.22), mat('#2a2a2c', 0.4), loc=(0, 0.66, -0.02)))
        objs.append(box((0.14, 0.13, 0.05), mbl, loc=(0, 0.62, 0.27), bevel=0.015))
    o = join(objs, 'Boat')
    o.rotation_euler = (0, 0, math.radians(40 if iid == 'boat_rubber' else 65))
    return [o], 'item'


# =====================================================================
#  모델: 미끼 · 음식 · 재료
# =====================================================================

def model_worm(iid):
    objs = []
    objs.append(cyl(0.3, 0.32, 0.06, mat('#4a3424', 0.95), loc=(0, 0, -0.03), seg=40))  # 흙
    for k, (ph, z0) in enumerate(((0.0, 0.03), (2.1, 0.06), (4.2, 0.045))):
        pts = curve_pts(lambda t, ph=ph, z0=z0: (math.cos(ph + t * 3.6) * (0.08 + 0.12 * t), math.sin(ph + t * 3.6) * (0.08 + 0.12 * t),
                                                  z0 + 0.02 * math.sin(t * 9)), 40)
        def rad(t):
            return 0.018 * (0.7 + 0.3 * math.sin(math.pi * t)) * (1 + 0.12 * math.sin(t * 90)) * (1.15 if 0.25 < t < 0.33 else 1)
        objs.append(tube(pts, rad, mat('#c05a64', 0.35, coat=0.6), sides=12))
    return [join(objs, 'Worm')], 'item'


def model_paste(iid):
    objs = [lumpy(0.14, mat('#c8a878', 0.9), loc=(0, 0, 0.12), amp=0.12, freq=4, seed=1)]
    for i in range(14):
        a = i * 2.4
        objs.append(lumpy(0.018, mat('#d8bc8c', 0.9), loc=(math.cos(a) * (0.18 + 0.03 * (i % 3)), math.sin(a) * (0.18 + 0.03 * (i % 3)), 0.01), amp=0.3, seed=i, seg=10, rings=6))
    return [join(objs, 'Paste')], 'item'


def model_corn(iid):
    objs = []
    my = mat('#f2c22a', 0.35, coat=0.4)
    for i in range(9):
        a = i * 2.39
        r = 0.05 + 0.04 * (i % 4)
        o = box((0.07, 0.06, 0.05), my, loc=(math.cos(a) * r, math.sin(a) * r, 0.03 + 0.03 * (i % 2)), rot=(0.3 * i, 0.5 * i, a), bevel=0.02)
        objs.append(o)
    return [join(objs, 'Corn')], 'item'


def shrimp(m, scale=1.0, loc=(0, 0, 0), rot=0.0, legs=True, antenna=True):
    """굽은 새우: 마디가 있는 몸 + 꼬리 부채 + 더듬이."""
    objs = []
    pts = curve_pts(lambda t: (math.sin(t * 2.4) * 0.12, -math.cos(t * 2.4) * 0.12 + 0.12, 0.0), 24)
    def rad(t):
        return 0.04 * (1 - 0.65 * t) * (1 + 0.08 * math.sin(t * 50))
    objs.append(tube(pts, rad, m, sides=14))
    tail = pts[-1]
    for s in (-1, 1):
        objs.append(sphere(0.03, m, loc=(tail[0] - 0.01, tail[1] + 0.005, s * 0.02), scale=(1.2, 0.3, 0.8)))
    if antenna:
        head = pts[0]
        for s in (-1, 1):
            objs.append(tube([(head[0], head[1], s * 0.01), (head[0] - 0.08, head[1] + 0.06, s * 0.05), (head[0] - 0.15, head[1] + 0.15, s * 0.08)], 0.002, m))
    objs.append(sphere(0.008, mat('#101010', 0.2), loc=(pts[1][0] - 0.02, pts[1][1] + 0.02, 0.03)))
    objs.append(sphere(0.008, mat('#101010', 0.2), loc=(pts[1][0] - 0.02, pts[1][1] + 0.02, -0.03)))
    if legs:
        for i in range(5):
            p = pts[4 + i * 2]
            objs.append(tube([p, (p[0] + 0.03, p[1] - 0.04, 0.0)], 0.003, m))
    o = join(objs, 'Shrimp')
    o.scale = (scale, scale, scale)
    o.location = loc
    o.rotation_euler = (math.pi / 2, 0, rot)
    return o


def model_krill(iid):
    m = mat('#f0907a', 0.35, trans=0.3, coat=0.5)
    objs = [shrimp(m, 0.7, (0.0, 0.0, 0.03), 0.2), shrimp(m, 0.65, (0.12, 0.1, 0.05), 2.0), shrimp(m, 0.6, (-0.1, 0.12, 0.02), 4.0)]
    return objs, 'item'


def model_shrimp(iid):
    return [shrimp(mat('#a8b0a8', 0.25, trans=0.5, coat=0.6), 1.2, (0, 0, 0.05), 0.4)], 'item'


def model_seaworm(iid):
    m = mat('#4a7a6a', 0.35, coat=0.5)
    pts = curve_pts(lambda t: (math.sin(t * 5) * 0.1 * (1 - t * 0.3), -0.25 + t * 0.5, 0.03 + 0.01 * math.sin(t * 13)), 50)
    def rad(t):
        return 0.02 * (0.8 + 0.2 * math.sin(math.pi * t))
    objs = [tube(pts, rad, m, sides=12)]
    mleg = mat('#8ab0a0', 0.5)
    for i in range(2, 48, 2):
        p = V(pts[i])
        t = (V(pts[i + 1]) - V(pts[i - 1])).normalized()
        side = t.cross(V((0, 0, 1))).normalized()
        for s in (-1, 1):
            objs.append(tube([p, p + side * s * 0.035 + V((0, 0, -0.01))], 0.004, mleg, sides=5))
    return [join(objs, 'SeaWorm')], 'item'


def model_squid(iid):
    m = mat('#f4efe4', 0.3, trans=0.2, coat=0.5)
    objs = []
    for i in range(3):
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=16, y_segments=3, size=1.0)
        for v in bm.verts:
            x, y = v.co.x, v.co.y
            v.co = V((x * 0.2, y * 0.035, 0.03 * math.sin(x * 3 + i)))
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.015)
        o = obj_from_bm(bm, 'Strip', m)
        o.location = (0.0, -0.06 + i * 0.07, 0.02 + i * 0.01)
        o.rotation_euler = (0, 0, 0.3 * (i - 1))
        objs.append(o)
    return [join(objs, 'Squid')], 'item'


def small_fish(m, fin_m):
    """생미끼: build_fish의 피라미 몸을 작게 (뼈 없이)."""
    import fish_species as FS
    import build_fish as BF
    d = FS.resolve('zacco')
    B = BF.Body(d)
    v, f, uv = BF.build_body_mesh(d, B)
    body = BF.mesh_from('Body', v, f, uv)
    body.data.materials.append(m)
    fins = []
    for name, fv, ff, fu, kind in BF.make_fins(d, B):
        fo = BF.mesh_from(name, fv, ff, fu)
        fo.data.materials.append(fin_m)
        fins.append(fo)
    eyes = BF.bm_to_obj('Eyes', [BF.make_eye(d, B, 1), BF.make_eye(d, B, -1)])
    eyes.data.materials.append(mat('#101010', 0.1))
    o = join([body] + fins + [eyes], 'Minnow')
    return o


def model_minnow(iid):
    o = small_fish(mat('#c8d4d8', 0.2, metal=0.7, coat=0.4), mat('#a8b8bc', 0.4))
    o.scale = (0.5, 0.5, 0.5)
    o.rotation_euler = (0, 0, math.radians(-90))
    o.location = (0, 0, 0.05)
    return [o], 'item'


def model_lure(iid):
    objs = []
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=16, radius=1.0)
    for v in bm.verts:
        x, y, z = v.co
        v.co = V((x * 0.07, y * 0.17, z * 0.012 + 0.025 * (y * y)))  # 숟가락처럼 오목하게
    spoon = obj_from_bm(bm, 'Spoon', mat('#d8d8dc', 0.12, metal=1.0))
    objs.append(spoon)
    objs.append(box((0.05, 0.13, 0.003), mat('#e0a830', 0.2, metal=1.0), loc=(0, 0.0, 0.017)))
    ring_m = mat('#c8c8cc', 0.2, metal=1.0)
    objs.append(torus(0.015, 0.0025, ring_m, loc=(0, -0.185, 0.02), rot=(0, math.pi / 2, 0)))
    objs.append(torus(0.015, 0.0025, ring_m, loc=(0, 0.185, 0.02), rot=(0, math.pi / 2, 0)))
    # 트레블 훅: 세 갈래 바늘
    hook_m = mat('#2a2a2e', 0.3, metal=0.9)
    objs.append(cyl(0.003, 0.003, 0.07, hook_m, loc=(0, 0.23, 0.02), rot=(math.pi / 2, 0, 0)))
    for k in range(3):
        a = k * 2 * math.pi / 3
        dvec = V((math.cos(a), 0, math.sin(a)))
        base = V((0, 0.265, 0.02))
        pts = [base, base + dvec * 0.025 + V((0, 0.01, 0)), base + dvec * 0.035 - V((0, 0.02, 0)), base + dvec * 0.022 - V((0, 0.035, 0))]
        objs.append(tube(pts, 0.0028, hook_m, r_end=0.0012, sides=6))
    o = join(objs, 'Lure')
    o.rotation_euler = (math.radians(35), math.radians(-25), math.radians(-40))
    return [o], 'item'


def model_egi(iid):
    """에기: 새우 모양 나무 몸통(천을 감은 무늬) + 깃털 다리 + 꼬리 쪽 우산살 바늘 (주꾸미·갑오징어용)."""
    objs = []
    body_m = mat('#f08030', 0.35, coat=0.6)
    stripe = mat('#ffd040', 0.3, coat=0.6)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=12, radius=1.0)
    for v in bm.verts:
        x, y, z = v.co
        taper = 1.0 - 0.45 * max(0.0, y)  # 꼬리 쪽이 가늘다
        v.co = V((x * 0.035 * taper, y * 0.16, z * 0.04 * taper))
    objs.append(obj_from_bm(bm, 'EgiBody', body_m))
    for k in range(4):
        objs.append(torus(0.036 - k * 0.003, 0.004, stripe, loc=(0, -0.08 + k * 0.05, 0), rot=(math.pi / 2, 0, 0), seg=20, mseg=6))
    objs.append(sphere(0.012, mat('#101010', 0.1), loc=(0.02, -0.13, 0.015)))
    objs.append(sphere(0.012, mat('#101010', 0.1), loc=(-0.02, -0.13, 0.015)))
    # 앞쪽 납작한 머리판 (가라앉는 추)
    objs.append(box((0.05, 0.03, 0.012), mat('#c8c8cc', 0.25, metal=0.9), loc=(0, -0.16, -0.03), rot=(0.4, 0, 0), bevel=0.004))
    # 옆 깃털 (다리)
    feather = mat('#f0e0c0', 0.6)
    for s in (-1, 1):
        objs.append(tube([(s * 0.03, -0.02, -0.01), (s * 0.07, 0.02, -0.03), (s * 0.08, 0.06, -0.05)], 0.006, feather, r_end=0.001, sides=5))
    # 꼬리 우산살 바늘 (두 겹)
    hook = mat('#d8d8dc', 0.2, metal=1.0)
    for ring in range(2):
        for k in range(10):
            a = k * 2 * math.pi / 10 + ring * 0.3
            base = V((0, 0.16 + ring * 0.012, 0))
            d = V((math.cos(a), 0, math.sin(a)))
            objs.append(tube([base, base + d * 0.012 + V((0, 0.02, 0)), base + d * 0.022 + V((0, 0.012, 0))], 0.0018, hook, sides=4))
    o = join(objs, 'Egi')
    o.rotation_euler = (0, 0, math.radians(-50))
    return [o], 'item'


CUT_COL = ['#c8a070', '#b87860', '#9a4a3a', '#e8c8b0', '#a83a3a']


def model_cutbait(iid):
    k = int(iid[-1]) - 1
    size = 0.11 + 0.025 * k
    col = CUT_COL[k]
    objs = [lumpy(size, mat(col, 0.35 if k else 0.9, coat=0.5 if k else 0), loc=(0, 0, size * 0.7), scale=(1.3, 0.9, 0.6), amp=0.1 + 0.015 * k, freq=2.2 + 0.3 * k, seed=k * 3)]
    if k >= 1:
        # 지방·껍질 줄무늬
        objs.append(lumpy(size * 0.5, mat('#f2e2d2', 0.4), loc=(size * 0.4, 0, size * 1.0), scale=(1.4, 0.5, 0.3), amp=0.2, seed=k))
    if k == 2:
        for i in range(4):
            objs.append(tube(curve_pts(lambda t, i=i: (-0.1 + 0.06 * i + 0.03 * math.sin(t * 6), -0.12 + 0.24 * t, 0.03 + 0.04 * math.sin(t * 3)), 12),
                             0.012, mat('#7a2a2a', 0.3, coat=0.6)))
    if k == 4:
        objs.append(sphere(0.02, mat('#101010', 0.1), loc=(-size * 0.9, 0.02, size * 0.9)))
    return [join(objs, 'Cut')], 'item'


def model_kimbap(iid):
    objs = []
    for i in range(3):
        x = -0.16 + i * 0.16
        rot = (math.radians(70), 0, math.radians(10 * (i - 1)))
        parts = [cyl(0.075, 0.075, 0.05, mat('#141c14', 0.6), seg=32),
                 cyl(0.068, 0.068, 0.052, mat('#f4f2ea', 0.85), seg=32),
                 cyl(0.016, 0.016, 0.054, mat('#f0c020', 0.6), loc=(0.02, 0.018, 0)),
                 cyl(0.013, 0.013, 0.054, mat('#2a8a2a', 0.6), loc=(-0.022, 0.012, 0)),
                 cyl(0.014, 0.014, 0.054, mat('#f8d860', 0.5), loc=(0.0, -0.025, 0)),
                 cyl(0.012, 0.012, 0.054, mat('#e86a6a', 0.5), loc=(-0.025, -0.018, 0)),
                 cyl(0.011, 0.011, 0.054, mat('#e88a3a', 0.5), loc=(0.028, -0.012, 0))]
        o = join(parts, f'Slice{i}')
        o.location = (x, 0.02 * i, 0.07)
        o.rotation_euler = rot
        objs.append(o)
    return objs, 'item'


def model_scale(iid):
    objs = []
    m = mat('#d8e4ec', 0.08, metal=0.6, film=500, coat=1.0)
    for i in range(5):
        bm = bmesh.new()
        bmesh.ops.create_circle(bm, cap_ends=True, segments=32, radius=1.0)
        for v in bm.verts:
            v.co = V((v.co.x * 0.11, v.co.y * 0.09, -0.02 * (v.co.x ** 2 + v.co.y ** 2)))
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.004)
        o = obj_from_bm(bm, 'Scale', m)
        a = i * 1.3
        o.location = (math.cos(a) * 0.08, math.sin(a) * 0.06, 0.01 * i)
        o.rotation_euler = (0.2 * math.sin(i), 0.25 * math.cos(i), a)
        objs.append(o)
    return [join(objs, 'Scales')], 'item'


def star(r, m, loc, rot):
    bm = bmesh.new()
    pts = []
    for k in range(10):
        a = math.pi / 2 + k * math.pi / 5
        rr = r if k % 2 == 0 else r * 0.45
        pts.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, 0)))
    f = bm.faces.new(pts)
    bmesh.ops.solidify(bm, geom=[f], thickness=r * 0.35)
    o = obj_from_bm(bm, 'Star', m)
    o.location, o.rotation_euler = loc, rot
    return o


def model_starsand(iid):
    objs = []
    glass = mat('#e8f4ff', 0.02, trans=1.0)
    objs.append(cyl(0.09, 0.09, 0.2, glass, loc=(0, 0, 0.1), seg=40))
    objs.append(cyl(0.04, 0.04, 0.05, glass, loc=(0, 0, 0.225), seg=24))
    objs.append(cyl(0.042, 0.036, 0.05, mat('#a8784a', 0.8), loc=(0, 0, 0.265)))
    objs.append(cyl(0.08, 0.08, 0.09, mat('#f0d898', 0.9), loc=(0, 0, 0.05), seg=40))
    ms = mat('#ffe070', 0.2, emit=2.0)
    for i in range(10):
        a = i * 2.2
        objs.append(star(0.018, ms, (math.cos(a) * 0.05, math.sin(a) * 0.05, 0.03 + 0.012 * (i % 6)), (0.4 * i, 0.3 * i, 0)))
    for i in range(3):
        objs.append(star(0.03, ms, (0.12 + 0.05 * i, -0.08 + 0.07 * i, 0.02), (math.pi / 2 * 0.2, 0, i)))
    return [join(objs, 'StarSand')], 'item'


def model_conch(iid):
    """소라: 로그 나선(Raup 모형)으로 감긴 관. 어깨에 뿔(가시)이 줄지어 난다."""
    bm = bmesh.new()
    turns, n, sides = 4.5, 220, 20
    k = math.log(2.3) / (2 * math.pi)        # 한 바퀴에 2.3배 커진다
    tmax = turns * 2 * math.pi
    rings = []
    for i in range(n):
        th = tmax * i / (n - 1)
        r = math.exp(k * (th - tmax))        # 마지막 바퀴 = 1
        c = V((math.cos(th) * r, math.sin(th) * r, -1.25 * r))
        rr = 0.62 * r
        out, up = V((math.cos(th), math.sin(th), 0)), V((0, 0, 1))
        ring = []
        for q in range(sides):
            ph = 2 * math.pi * q / sides
            ex = 1.0
            # 어깨(바깥 위쪽)에 뿔: 나선을 따라 일정한 간격으로
            if 0.35 < ph < 1.2 and r > 0.25:
                ex += 0.35 * max(0.0, math.cos(th * 9)) ** 6
            ring.append(bm.verts.new(c + (out * math.cos(ph) + up * math.sin(ph) * 1.15) * rr * ex))
        rings.append(ring)
    for i in range(n - 1):
        for q in range(sides):
            bm.faces.new((rings[i][q], rings[i][(q + 1) % sides], rings[i + 1][(q + 1) % sides], rings[i + 1][q]))
    o = obj_from_bm(bm, 'Conch', mat('#c89a6a', 0.4, coat=0.5))
    # 입구 안쪽 (분홍빛 진주층)
    lip = sphere(0.5, mat('#f4c8b8', 0.15, coat=1.0, film=300), loc=(1.0, 0.0, -1.25), scale=(0.25, 0.9, 1.0))
    o = join([o, lip], 'Conch')
    o.scale = (0.12, 0.12, 0.12)
    o.rotation_euler = (math.radians(-70), math.radians(10), math.radians(200))
    return [o], 'item'


# =====================================================================
#  잡동사니 (아이콘 + FBX)
# =====================================================================

def model_can(iid):
    objs = [cyl(0.033, 0.033, 0.12, mat('#c83030', 0.3, metal=0.7), loc=(0, 0, 0.06), seg=32),
            cyl(0.031, 0.033, 0.006, mat('#c0c0c4', 0.2, metal=1.0), loc=(0, 0, 0.123), seg=32),
            torus(0.03, 0.003, mat('#c0c0c4', 0.2, metal=1.0), loc=(0, 0, 0.126)),
            cyl(0.0335, 0.0335, 0.035, mat('#f0f0f0', 0.4), loc=(0, 0, 0.07), seg=32),
            cyl(0.008, 0.008, 0.002, mat('#808084', 0.3, metal=1.0), loc=(0.01, 0, 0.127), seg=12)]
    o = join(objs, 'Can')
    o.rotation_euler = (math.radians(12), math.radians(-8), math.radians(30))
    return [o], 'item', 0.12


def model_fishcan(iid):
    objs = [cyl(0.05, 0.05, 0.035, mat('#d8d8dc', 0.2, metal=1.0), loc=(0, 0, 0.0175), seg=40),
            cyl(0.0505, 0.0505, 0.022, mat('#2a6ad8', 0.35), loc=(0, 0, 0.0175), seg=40),
            torus(0.048, 0.003, mat('#c8c8cc', 0.2, metal=1.0), loc=(0, 0, 0.036)),
            torus(0.012, 0.003, mat('#c8c8cc', 0.2, metal=1.0), loc=(0.03, 0, 0.038))]
    # 라벨의 물고기 그림
    objs.append(sphere(0.018, mat('#f0c040', 0.4), loc=(0, -0.05, 0.018), scale=(1.4, 0.15, 0.6)))
    o = join(objs, 'FishCan')
    o.rotation_euler = (math.radians(30), 0, math.radians(20))
    return [o], 'item', 0.1


def model_box(iid):
    wood, dark = mat('#a87848', 0.8), mat('#6a4a2a', 0.8)
    objs = []
    W, D, H = 0.4, 0.3, 0.26
    for side in (-1, 1):
        for k in range(3):
            objs.append(box((W, 0.012, H / 3.4), wood, loc=(0, side * D / 2, H * (k + 0.5) / 3), bevel=0.003))
            objs.append(box((0.012, D, H / 3.4), wood, loc=(side * W / 2, 0, H * (k + 0.5) / 3), bevel=0.003))
    objs.append(box((W, D, 0.012), dark, loc=(0, 0, 0.006)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            objs.append(box((0.03, 0.03, H), dark, loc=(sx * (W / 2 - 0.015), sy * (D / 2 - 0.015), H / 2)))
    o = join(objs, 'Box')
    o.rotation_euler = (0, 0, math.radians(25))
    return [o], 'item', 0.4


def model_trap(iid):
    net = mat('#3a5a4a', 0.6)
    objs = []
    L, R = 0.45, 0.13
    for i in range(7):
        objs.append(torus(R, 0.004, net, loc=(0, -L / 2 + L * i / 6, R), rot=(math.pi / 2, 0, 0), seg=28, mseg=6))
    for k in range(12):
        a = 2 * math.pi * k / 12
        objs.append(cyl(0.003, 0.003, L, net, loc=(math.cos(a) * R, 0, R + math.sin(a) * R), rot=(math.pi / 2, 0, 0), seg=6))
    # 입구 깔때기
    objs.append(cyl(R, 0.03, 0.12, mat('#4a6a5a', 0.6, alpha=0.6), loc=(0, -L / 2 + 0.06, R), rot=(math.pi / 2, 0, 0), cap=False, seg=20))
    objs.append(box((0.04, 0.1, 0.02), mat('#e8c020', 0.5), loc=(0, 0.1, 2 * R)))  # 부표 끈 매듭
    o = join(objs, 'Trap')
    o.rotation_euler = (0, 0, math.radians(30))
    return [o], 'item', 0.45


def model_shoe(iid):
    """낡은 운동화: 흰 밑창 + 남색 갑피(앞코 낮고 뒤꿈치 높게) + 신발끈 + 발목 구멍."""
    sole, upper, lace, inner = mat('#e8e6dc', 0.7), mat('#34507a', 0.75), mat('#f0eee6', 0.6), mat('#1a1e28', 0.9)
    bm = bmesh.new()
    n, sides = 28, 16
    rings = []
    for i in range(n):
        t = i / (n - 1)                       # 0 앞코 ~ 1 뒤꿈치
        y = -0.13 + 0.26 * t
        w = 0.052 * math.sin(math.pi * (0.06 + 0.88 * t)) ** 0.45 * (1.05 - 0.1 * t)
        h = 0.03 + 0.05 * min(1.0, t / 0.55) ** 0.8
        ring = []
        for q in range(sides):
            a = math.pi * q / (sides - 1)     # 0 오른쪽 바닥 → π 왼쪽 바닥
            ring.append(bm.verts.new((math.cos(a) * w, y, 0.022 + math.sin(a) ** 0.8 * h)))
        rings.append(ring)
    for i in range(n - 1):
        for q in range(sides - 1):
            bm.faces.new((rings[i][q], rings[i][q + 1], rings[i + 1][q + 1], rings[i + 1][q]))
    objs = [obj_from_bm(bm, 'Upper', upper)]
    bm = bmesh.new()
    outline = []
    for i in range(36):
        a = 2 * math.pi * i / 36
        outline.append(bm.verts.new((math.cos(a) * 0.056 * (1.0 + 0.1 * math.sin(a)), math.sin(a) * 0.14, 0)))
    f = bm.faces.new(outline)
    bmesh.ops.solidify(bm, geom=[f], thickness=0.024)
    objs.append(obj_from_bm(bm, 'Sole', sole))
    objs.append(sphere(0.034, inner, loc=(0, 0.075, 0.08), scale=(1.0, 1.5, 0.12)))     # 발목 구멍
    objs.append(torus(0.036, 0.007, upper, loc=(0, 0.075, 0.08), rot=(0, 0, 0), seg=24, mseg=8))
    for i in range(5):
        y = -0.06 + i * 0.022
        objs.append(box((0.06, 0.007, 0.006), lace, loc=(0, y, 0.062 + i * 0.004), rot=(math.radians(-25), 0, 0)))
    objs.append(box((0.002, 0.08, 0.02), mat('#f0f0f0', 0.4), loc=(0.05, 0.03, 0.05), rot=(0, 0, 0.05)))  # 옆 줄무늬
    o = join(objs, 'Shoe')
    o.rotation_euler = (0, 0, math.radians(-35))
    return [o], 'item', 0.27


def model_robotfish(iid):
    o = small_fish(mat('#b8c0c8', 0.25, metal=0.95), mat('#808890', 0.3, metal=0.9))
    objs = [o]
    for i in range(5):
        objs.append(box((0.03, 0.12, 0.08), mat('#8890a0', 0.3, metal=1.0), loc=(0.05, -0.25 + i * 0.1, 0.0), bevel=0.01))
        objs.append(sphere(0.008, mat('#505860', 0.3, metal=1.0), loc=(0.07, -0.25 + i * 0.1, 0.03)))
    objs.append(sphere(0.02, mat('#ff3030', 0.1, emit=4.0), loc=(0.04, -0.42, 0.03)))
    objs.append(sphere(0.02, mat('#ff3030', 0.1, emit=4.0), loc=(-0.04, -0.42, 0.03)))
    o = join(objs, 'RobotFish')
    o.rotation_euler = (0, 0, math.radians(-90))
    return [o], 'item', 1.0


ITEMS = {
    'rod_basic': model_rod, 'rod_carbon': model_rod, 'rod_bamboo': model_rod, 'rod_heavy': model_rod, 'rod_gege': model_rod,
    'reel_basic': model_reel, 'reel_power': model_reel, 'reel_hispeed': model_reel, 'reel_napal': model_reel,
    'line_nylon': model_line, 'line_carbon': model_line, 'line_pe': model_line,
    'boat_rubber': model_boat, 'boat_speed': model_boat,
    'bait_worm': model_worm, 'bait_paste': model_paste, 'bait_corn': model_corn, 'bait_krill': model_krill,
    'bait_seaworm': model_seaworm, 'bait_squid': model_squid, 'bait_shrimp': model_shrimp, 'bait_minnow': model_minnow,
    'bait_lure': model_lure, 'bait_egi': model_egi,
    'bait_cut1': model_cutbait, 'bait_cut2': model_cutbait, 'bait_cut3': model_cutbait, 'bait_cut4': model_cutbait, 'bait_cut5': model_cutbait,
    'food_kimbap': model_kimbap,
    'mat_scale': model_scale, 'mat_starsand': model_starsand, 'mat_conch': model_conch,
}

JUNK = {'can': model_can, 'trap': model_trap, 'box': model_box, 'fishcan': model_fishcan, 'robotfish': model_robotfish, 'shoe': model_shoe}


# =====================================================================
#  렌더 · 내보내기
# =====================================================================

def scene_setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _mats.clear()
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = 48
    sc.cycles.use_denoising = False
    sc.render.resolution_x = sc.render.resolution_y = 256
    sc.render.film_transparent = True
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('W')
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes['Background'].inputs['Color'].default_value = (0.55, 0.62, 0.7, 1)
    w.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.8
    # 왼쪽 위 주광 + 오른쪽 뒤 테두리 빛 (모든 아이콘 같은 조명)
    key = link(bpy.data.objects.new('Key', bpy.data.lights.new('Key', 'SUN')))
    key.data.energy = 3.4
    key.data.angle = math.radians(8)
    key.rotation_euler = (math.radians(45), math.radians(-30), math.radians(-40))
    rim = link(bpy.data.objects.new('Rim', bpy.data.lights.new('Rim', 'SUN')))
    rim.data.energy = 2.2
    rim.rotation_euler = (math.radians(-55), math.radians(20), math.radians(150))
    cam = link(bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')))
    cam.data.type = 'ORTHO'
    sc.camera = cam
    return cam


def frame(cam, objs, pad=1.12):
    """3/4 시점(앞-오른쪽 위)에서 물체가 칸에 꽉 차게."""
    bpy.context.view_layer.update()
    pts = []
    for o in objs:
        for c in o.bound_box:
            pts.append(o.matrix_world @ V(c))
    center = sum(pts, V()) / len(pts)
    dirv = V((0.62, -0.78, 0.62)).normalized()
    cam.location = center + dirv * 5
    cam.rotation_euler = (-dirv).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    xs, ys = [], []
    for p in pts:
        q = inv @ p
        xs.append(q.x)
        ys.append(q.y)
    w, h = max(xs) - min(xs), max(ys) - min(ys)
    cam.data.ortho_scale = max(w, h) * pad
    cam.data.shift_x = (max(xs) + min(xs)) / 2 / cam.data.ortho_scale
    cam.data.shift_y = (max(ys) + min(ys)) / 2 / cam.data.ortho_scale


def render(out):
    bpy.context.scene.render.filepath = out
    bpy.ops.render.render(write_still=True)


def export_junk(jid, objs, length):
    """잡동사니 FBX: 길이(가장 긴 축) = 1m로 맞춘다. Unity가 실제 cm로 줄인다."""
    bpy.context.view_layer.update()
    for o in objs:
        o.select_set(True)
    o = join(objs, jid) if len(objs) > 1 else objs[0]
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    xs = [V(c) for c in o.bound_box]
    size = max(max(c[i] for c in xs) - min(c[i] for c in xs) for i in range(3))
    s = 1.0 / max(size, 1e-4)
    o.scale = (s, s, s)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    # 가운데로
    xs = [V(c) for c in o.bound_box]
    ctr = sum(xs, V()) / 8
    o.data.transform(Matrix.Translation(-ctr))
    o.location = (0, 0, 0)
    os.makedirs(OUT_JUNK, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    out = os.path.join(OUT_JUNK, jid + '.fbx')
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'MESH'}, axis_forward='-Y', axis_up='Z',
                             apply_scale_options='FBX_SCALE_UNITS', mesh_smooth_type='FACE', bake_anim=False)
    return out


def contact_sheet(paths, out, cols=8, cell=192):
    from PIL import Image, ImageDraw
    rows = (len(paths) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cell, rows * (cell + 18)), (34, 46, 56, 255))
    for i, (iid, p) in enumerate(paths):
        im = Image.open(p).convert('RGBA').resize((cell, cell))
        x, y = (i % cols) * cell, (i // cols) * (cell + 18)
        sheet.alpha_composite(im, (x, y + 18))
        ImageDraw.Draw(sheet).text((x + 6, y + 3), iid, fill=(220, 230, 235, 255))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.convert('RGB').save(out)


def main():
    only = arg('--only')
    want = set(only.split(',')) if only else None
    done = []
    os.makedirs(OUT_ICON, exist_ok=True)
    os.makedirs(OUT_JUNK_ICON, exist_ok=True)
    for iid, fn in list(ITEMS.items()) + list(JUNK.items()):
        if want and iid not in want:
            continue
        cam = scene_setup()
        res = fn(iid)
        objs = res[0]
        frame(cam, objs)
        junk = iid in JUNK
        out = os.path.join(OUT_JUNK_ICON if junk else OUT_ICON, iid + '.png')
        render(out)
        done.append((iid, out))
        if junk:
            path = export_junk(iid, objs, res[2])
            print(f'[icons] {iid} → {os.path.relpath(path, ROOT)}', flush=True)
        else:
            print(f'[icons] {iid}', flush=True)
    sheet = arg('--sheet')
    if sheet:
        contact_sheet(done, sheet if os.path.isabs(sheet) else os.path.join(ROOT, sheet))
        print('[icons] sheet', sheet, flush=True)


if __name__ == '__main__':
    main()
    sys.stdout.flush()
    os._exit(0)
