"""
아이템 아이콘 + 잡동사니 모델을 만든다. docs/09-art-plan.md 4장.

  python -I tools/blender/build_icons.py                  # 모든 아이콘 + 잡동사니 FBX
  python -I tools/blender/build_icons.py --only rod_basic,bait_worm
  python -I tools/blender/build_icons.py --sheet docs/images/33_item_icons.jpg

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


def mat_wound(color, axis='X', bands=90.0, rough=0.45, coat=0.4, name=None):
    """감긴 줄: 물체 좌표 axis 방향으로 촘촘한 띠(줄 가닥)와 결 요철. 릴 스풀·원줄 스풀."""
    key = ('wound', color, axis, bands, rough, coat)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(name or f'W{len(_mats)}')
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes['Principled BSDF']
    c = hexc(color) if isinstance(color, str) else color
    b.inputs['Roughness'].default_value = rough
    if 'Coat Weight' in b.inputs:
        b.inputs['Coat Weight'].default_value = coat
    tc = nt.nodes.new('ShaderNodeTexCoord')
    wv = nt.nodes.new('ShaderNodeTexWave')
    wv.wave_type = 'BANDS'
    wv.bands_direction = axis
    wv.inputs['Scale'].default_value = bands
    wv.inputs['Distortion'].default_value = 1.5
    wv.inputs['Detail'].default_value = 1.0
    nt.links.new(tc.outputs['Object'], wv.inputs['Vector'])
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = tuple(x * 0.62 for x in c[:3]) + (1,)
    ramp.color_ramp.elements[1].color = c
    nt.links.new(wv.outputs['Fac'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
    bump = nt.nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.35
    nt.links.new(wv.outputs['Fac'], bump.inputs['Height'])
    nt.links.new(bump.outputs['Normal'], b.inputs['Normal'])
    m.diffuse_color = c
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


def blob(r, m, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), amp=0.12, freq=2.0, grain=0.0, gfreq=24.0, square=1.0,
         seed=0, seg=48, rings=28, zcut=None, below=False, name='Blob'):
    """펄린 잡음 덩어리 (떡밥·어분·살점). square < 1이면 모서리가 둥근 상자꼴, grain은 알갱이 요철,
    zcut이면 그 높이(r 배율)보다 위만(below면 아래만) 남긴 껍질 (살점 껍질)."""
    from mathutils import noise
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    off = V((seed * 7.31, seed * 3.17, seed * 5.71))
    for v in bm.verts:
        p = v.co.normalized()
        if square != 1.0:
            p = V(tuple(math.copysign(abs(c) ** square, c) for c in p))
        d = amp * noise.noise(p * freq + off)
        if grain:
            d += grain * noise.noise(p * gfreq + off * 2.0)
        v.co = p * r * (1 + d)
    if zcut is not None:
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if (v.co.z > zcut * r if below else v.co.z < zcut * r)], context='VERTS')
    ob = obj_from_bm(bm, name, m)
    ob.location, ob.scale, ob.rotation_euler = loc, scale, rot
    return ob


def mat_spots(base, spot, scale=40.0, thresh=0.62, rough=0.4, coat=0.5, metal=0.0, trans=0.0):
    """점박이 재질 (오징어 껍질 색소포, 생선 껍질 점): 잡음이 thresh를 넘는 곳만 spot 색."""
    key = ('spots', base, spot, scale, thresh, rough, coat, metal, trans)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(f'S{len(_mats)}')
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes['Principled BSDF']
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    for nm, val in (('Coat Weight', coat), ('Transmission Weight', trans)):
        if nm in b.inputs and val:
            b.inputs[nm].default_value = val
    tc = nt.nodes.new('ShaderNodeTexCoord')
    nz = nt.nodes.new('ShaderNodeTexNoise')
    nz.inputs['Scale'].default_value = scale
    nz.inputs['Detail'].default_value = 3.0
    nt.links.new(tc.outputs['Object'], nz.inputs['Vector'])
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation = 'CONSTANT'
    ramp.color_ramp.elements[0].color = hexc(base)
    ramp.color_ramp.elements[1].position = thresh
    ramp.color_ramp.elements[1].color = hexc(spot)
    nt.links.new(nz.outputs['Fac'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
    m.diffuse_color = hexc(base)
    _mats[key] = m
    return m


def mat_bands(base, line, scale=6.0, width=0.12, distort=3.0, axis='X', rough=0.3, coat=0.7):
    """가는 줄무늬 재질 (생선 살의 흰 근육 막 결): base 바탕에 폭 width의 line 줄, distort로 W자처럼 휜다."""
    key = ('bands', base, line, scale, width, distort, axis, rough, coat)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(f'B{len(_mats)}')
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes['Principled BSDF']
    b.inputs['Roughness'].default_value = rough
    if 'Coat Weight' in b.inputs:
        b.inputs['Coat Weight'].default_value = coat
    tc = nt.nodes.new('ShaderNodeTexCoord')
    wv = nt.nodes.new('ShaderNodeTexWave')
    wv.wave_type = 'BANDS'
    wv.bands_direction = axis
    wv.wave_profile = 'SAW'
    wv.inputs['Scale'].default_value = scale
    wv.inputs['Distortion'].default_value = distort
    wv.inputs['Detail'].default_value = 0.5
    nt.links.new(tc.outputs['Object'], wv.inputs['Vector'])
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = hexc(line)
    ramp.color_ramp.elements[1].position = width
    ramp.color_ramp.elements[1].color = hexc(base)
    e = ramp.color_ramp.elements.new(width * 0.6)
    e.color = hexc(line)
    nt.links.new(wv.outputs['Fac'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
    m.diffuse_color = hexc(base)
    _mats[key] = m
    return m


def mat_bump(color, scale=60.0, strength=0.5, rough=0.8, coat=0.0, color2=None, split=None, metal=0.0):
    """잡음 요철 재질 (밥알·면 니트·튀긴 어묵 겉). split = (축, 경계값, color2)면 물체 좌표 그 축의 값이 경계보다 작은 쪽을 color2로
    칠한다 (목장갑 손바닥 쪽 빨간 코팅)."""
    key = ('bump', color, scale, strength, rough, coat, color2, split, metal)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(f'N{len(_mats)}')
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = hexc(color)
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    if coat and 'Coat Weight' in b.inputs:
        b.inputs['Coat Weight'].default_value = coat
    tc = nt.nodes.new('ShaderNodeTexCoord')
    nz = nt.nodes.new('ShaderNodeTexNoise')
    nz.inputs['Scale'].default_value = scale
    nz.inputs['Detail'].default_value = 4.0
    nt.links.new(tc.outputs['Object'], nz.inputs['Vector'])
    bump = nt.nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = strength
    nt.links.new(nz.outputs['Fac'], bump.inputs['Height'])
    nt.links.new(bump.outputs['Normal'], b.inputs['Normal'])
    if split:
        axis, edge, col2 = split
        sep = nt.nodes.new('ShaderNodeSeparateXYZ')
        nt.links.new(tc.outputs['Object'], sep.inputs[0])
        cmp = nt.nodes.new('ShaderNodeMath')
        cmp.operation = 'LESS_THAN'
        cmp.inputs[1].default_value = edge
        nt.links.new(sep.outputs['XYZ'.index(axis)], cmp.inputs[0])
        mix = nt.nodes.new('ShaderNodeMix')
        mix.data_type = 'RGBA'
        mix.inputs['A'].default_value = hexc(color)
        mix.inputs['B'].default_value = hexc(col2)
        nt.links.new(cmp.outputs[0], mix.inputs['Factor'])
        nt.links.new(mix.outputs['Result'], b.inputs['Base Color'])
        rr = nt.nodes.new('ShaderNodeMapRange')       # 코팅 쪽은 매끈하고 윤기
        rr.inputs['To Min'].default_value = rough
        rr.inputs['To Max'].default_value = 0.35
        nt.links.new(cmp.outputs[0], rr.inputs['Value'])
        nt.links.new(rr.outputs['Result'], b.inputs['Roughness'])
    m.diffuse_color = hexc(color)
    _mats[key] = m
    return m


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
    'rod_fly': ('#3e4a2c', '#bf9e4d', '#b88f5c', 3, 'fly'),
    'rod_gangbaram': ('#297ab3', '#d1d6e0', '#1a1a1c', 4, 'ring'),
    'rod_goldgege': ('#dba838', '#ebe6d6', '#4d2e1a', 5, 'pearl'),
    'rod_pungrim': ('#0d0a0a', '#bf1a14', '#c79a33', 5, 'carbon'),
    'rod_gt': ('#ebebe6', '#333338', '#141417', 3, 'heavy'),
    'rod_nile': ('#c7a873', '#26808c', '#593d26', 4, 'heavy'),
}


def model_rod(iid):
    """뽑기 릴대 (실물 사진 대조): 고무 엉덩이 마개 → 뒷손잡이(EVA·코르크) → 금속 릴시트(조임 너트 둘 + 릴 발 덮개) → 앞손잡이 →
    감음 마감 링·로고 띠 → 끝으로 가늘어지는 마디(이음 페룰), 줄 고리는 릴 쪽이 크고 끝으로 갈수록 작다 (다리 둘 + 세라믹 링). 끝에 톱 가이드."""
    blank, ferrule, grip, sections, kind = ROD_STYLE[iid]
    mb = mat(blank, 0.25, coat=0.6) if kind != 'pearl' else mat(blank, 0.2, film=350, coat=0.8)
    if kind == 'carbon':
        mb = mat_wound(blank, 'Z', bands=900.0, rough=0.3, coat=0.8)      # 카본 직조 결
    mf = mat(ferrule, 0.25, metal=0.9)
    mg = mat_bump(grip, 140.0, 0.4, 0.8) if kind not in ('bamboo', 'fly') else mat_bump(grip, 70.0, 0.8, 0.85)   # EVA / 코르크
    mk = mat('#141416', 0.6)
    mc = mat('#2a2a30', 0.15, metal=0.3, coat=1.0)                       # 세라믹 링
    L = 1.6
    rb, rt = 0.03, 0.007
    if kind == 'heavy':
        rb, rt = 0.036, 0.009
    objs = []
    z = 0.0
    objs.append(cyl(rb * 1.3, rb * 1.38, 0.035, mk, loc=(0, 0, 0.0175), seg=24))           # 엉덩이 마개
    z = 0.035
    rear = 0.16 if kind != 'fly' else 0.0
    if rear:
        objs.append(cyl(rb * 1.32, rb * 1.22, rear, mg, loc=(0, 0, z + rear / 2), seg=28))   # 뒷손잡이
        z += rear
    # 릴시트: 슬리브 + 조임 너트 둘(톱니) + 릴 발 덮개 (줄 고리와 같은 쪽 -Y)
    seat = 0.13
    objs.append(cyl(rb * 1.12, rb * 1.12, seat, mf, loc=(0, 0, z + seat / 2), seg=28))
    for nz in (z + 0.018, z + 0.046):
        objs.append(cyl(rb * 1.34, rb * 1.34, 0.022, mf, loc=(0, 0, nz), seg=28))
        for k in range(14):
            a = k * 2 * math.pi / 14
            objs.append(box((0.004, 0.004, 0.02), mf, loc=(math.cos(a) * rb * 1.36, math.sin(a) * rb * 1.36, nz)))
    for hz in (z + 0.07, z + seat - 0.012):
        objs.append(sphere(rb * 0.8, mf, loc=(0, -rb * 1.05, hz), scale=(0.9, 0.55, 1.1), seg=16, rings=10))
    z += seat
    fore = 0.07 if kind != 'fly' else 0.2
    objs.append(cyl(rb * 1.2, rb * 1.08, fore, mg, loc=(0, 0, z + fore / 2), seg=28))       # 앞손잡이
    z += fore
    objs.append(cyl(rb * 1.1, rb * 1.0, 0.012, mf, loc=(0, 0, z + 0.006), seg=24))         # 감음 마감 링
    z += 0.012
    # 로고 띠: 블랭크 밑동에 테 색 줄 두 개
    for dz in (0.05, 0.075):
        objs.append(cyl(rb * 1.03, rb * 1.03, 0.008, mf, loc=(0, 0, z + dz), seg=24))
    # 마디: 끝으로 갈수록 가늘다, 이음에 페룰, 마디마다 줄 고리 하나
    z0 = z
    seg_len = (L - z0) / sections
    nguide = sections + 2
    gi = 0
    for i in range(sections):
        r0 = rb + (rt - rb) * i / sections
        r1 = rb + (rt - rb) * (i + 1) / sections
        objs.append(cyl(r0, r1, seg_len, mb, loc=(0, 0, z + seg_len / 2), seg=20))
        if i < sections - 1:
            objs.append(cyl(r1 * 1.3, r1 * 1.15, 0.03, mf, loc=(0, 0, z + seg_len - 0.012), seg=20))
        if kind == 'bamboo':
            for k in range(2):
                bz = z + seg_len * (0.3 + 0.4 * k)
                rr = r0 + (r1 - r0) * (0.3 + 0.4 * k)
                objs.append(torus(rr, 0.004, mat('#4a2e18', 0.5), loc=(0, 0, bz), seg=20, mseg=6))
        z += seg_len
    # 줄 고리: 릴 쪽부터 커다란 것 → 작은 것 (간격도 끝으로 갈수록 좁아진다)
    for g in range(nguide):
        u = g / (nguide - 1)
        gz = z0 + 0.12 + (L - z0 - 0.2) * (1 - (1 - u) ** 1.35)
        rr = rb + (rt - rb) * (gz - z0) / (L - z0)
        gr = (0.034 if kind != 'heavy' else 0.042) * (1 - u) ** 1.2 + 0.008
        h = gr * 1.05 + rr
        ring = V((0, -(h + gr * 0.2), gz))
        objs.append(torus(gr, gr * 0.16, mc, loc=ring, rot=(0, math.pi / 2, 0), seg=20, mseg=6))     # 아이콘에서 링이 보이게 옆으로 세운다
        objs.append(torus(gr * 1.12, gr * 0.06, mf, loc=ring, rot=(0, math.pi / 2, 0), seg=20, mseg=5))
        for sdz in (-1, 1):     # 다리 둘: 링 아래 → 블랭크 (V자)
            objs.append(tube([ring + V((0, gr * 0.9, sdz * gr * 0.5)), V((0, -rr, gz + sdz * gr * 1.6))], 0.0022, mf, sides=5))
        for sdz in (-1, 1):     # 다리 발을 감은 실
            objs.append(cyl(rr * 1.1, rr * 1.1, 0.012, mk, loc=(0, 0, gz + sdz * gr * 1.6), seg=16))
    objs.append(torus(0.007, 0.0018, mc, loc=(0, -0.012, L), rot=(0, math.pi / 2, 0), seg=14, mseg=5))   # 톱 가이드
    objs.append(cyl(rt * 1.4, rt * 1.2, 0.02, mf, loc=(0, 0, L - 0.01), seg=12))
    o = join(objs, 'Rod')
    o.rotation_euler = (0, math.radians(-50), 0)
    return [o], 'long'


REEL_STYLE = {
    'reel_basic': ('#b3b5bd', '#333338', '#9ea1a8', 1.0, False),
    'reel_power': ('#141417', '#e6b340', '#d9a838', 1.2, False),
    'reel_hispeed': ('#264d99', '#d9dde6', '#bfc7d9', 1.1, True),
    'reel_napal': ('#edd9b3', '#b85c29', '#d98c4d', 1.15, False),
    'reel_victory5': ('#c71f29', '#e6e6ed', '#b3b8c2', 1.05, False),
    'reel_pungrim': ('#0d0a0a', '#c71a14', '#d9a838', 1.2, True),
    'reel_jigging': ('#1f242e', '#d98c26', '#ccd1db', 1.35, True),
}


def model_reel(iid):
    """스피닝 릴 (실물 사진 대조): 위 발판(풋) → 다리 → 옆이 납작한 기어 몸통, 앞에 로터 컵 + 두 팔 + 베일 철사,
    그 앞에 줄 감긴 스풀(치마·앞턱) + 드랙 노브, 옆에 크랭크 손잡이 + 노브. 앞 = +X, 위 = +Z, 손잡이 = -Y."""
    body, accent, spool, size, double = REEL_STYLE[iid]
    mb = mat(body, 0.3, metal=0.6, coat=0.5)
    ma = mat(accent, 0.28, metal=0.75)
    ms = mat(spool, 0.22, metal=0.85)
    ml = mat_wound('#e4ece4', 'X', bands=140.0)
    mk = mat('#18181a', 0.55)
    mw = mat('#d8dade', 0.15, metal=1.0)
    AX = (0, math.pi / 2, 0)          # 원통 축 Z → X
    AY = (math.pi / 2, 0, 0)          # 원통 축 Z → Y
    objs = []
    # 기어 몸통: 옆으로 납작한 물방울 (뒤가 좁다) + 양옆 덮개 + 덮개 테
    objs.append(sphere(0.062, mb, loc=(0.0, 0, 0.0), scale=(1.1, 0.62, 1.0), seg=32, rings=18))
    objs.append(sphere(0.046, mb, loc=(-0.05, 0, 0.012), scale=(1.0, 0.55, 0.85), seg=24, rings=14))
    for s in (-1, 1):
        objs.append(cyl(0.047, 0.044, 0.01, mb, loc=(-0.004, s * 0.037, 0.0), rot=AY, seg=36))
        objs.append(torus(0.046, 0.0035, ma, loc=(-0.004, s * 0.041, 0.0), rot=AY, seg=36, mseg=6))
    objs.append(sphere(0.012, ma, loc=(-0.098, 0, 0.018), scale=(0.8, 1.0, 1.0)))       # 뒤 마개
    # 다리: 앞으로 살짝 기운 넓적한 날 + 발판(풋)
    objs.append(box((0.034, 0.018, 0.12), mb, loc=(0.0, 0, 0.1), rot=(0, math.radians(-14), 0), bevel=0.007))
    objs.append(box((0.17, 0.028, 0.011), mb, loc=(0.02, 0, 0.165), bevel=0.004))
    # 로터 컵 (앞이 조금 넓다) + 두 팔 (±Y) + 라인 롤러
    objs.append(cyl(0.046, 0.055, 0.04, mb, loc=(0.075, 0, 0.0), rot=AX, seg=36))
    objs.append(torus(0.055, 0.004, ma, loc=(0.095, 0, 0.0), rot=AX, seg=36, mseg=6))
    for s in (-1, 1):
        objs.append(box((0.07, 0.009, 0.016), mb, loc=(0.12, s * 0.059, 0.0), rot=(0, 0, s * math.radians(-4)), bevel=0.003))
    objs.append(cyl(0.008, 0.008, 0.016, ma, loc=(0.152, -0.064, 0.0), rot=AX, seg=14))
    # 스풀: 치마(로터 앞을 덮음) → 감긴 줄 → 앞턱 → 드랙 노브 (돌기)
    objs.append(cyl(0.061, 0.061, 0.022, ms, loc=(0.104, 0, 0.0), rot=AX, seg=40))
    objs.append(cyl(0.05, 0.05, 0.046, ml, loc=(0.137, 0, 0.0), rot=AX, seg=48))
    objs.append(cyl(0.058, 0.055, 0.008, ms, loc=(0.163, 0, 0.0), rot=AX, seg=40))
    objs.append(cyl(0.03, 0.026, 0.026, mk, loc=(0.18, 0, 0.0), rot=AX, seg=24))
    objs.append(cyl(0.022, 0.016, 0.008, ma, loc=(0.196, 0, 0.0), rot=AX, seg=20))
    for k in range(12):
        a = k * 2 * math.pi / 12
        objs.append(box((0.022, 0.006, 0.006), mk, loc=(0.18, math.cos(a) * 0.029, math.sin(a) * 0.029), rot=(a, 0, 0), bevel=0.002))
    # 베일: 두 팔 끝을 잇는 반원 철사 (스풀 위로 넘어간다)
    bail = [(0.15, -math.cos(t) * 0.066, math.sin(t) * 0.066) for t in (math.pi * i / 24 for i in range(25))]
    objs.append(tube(bail, 0.0032, mw, sides=8))
    # 손잡이 (-Y 쪽): 축 + 크랭크 팔 (아래·뒤로 내려 놓은 자세) + 노브
    objs.append(cyl(0.011, 0.009, 0.034, ma, loc=(-0.004, -0.058, 0.0), rot=AY, seg=16))
    arms = [math.radians(-120)] if not double else [math.radians(-120), math.radians(60)]
    for a in arms:
        d = V((math.cos(a), 0, math.sin(a)))
        y = -0.078
        p0, p1 = V((-0.004, y, 0.0)), V((-0.004, y, 0.0)) + d * 0.085
        objs.append(tube([p0, (p0 + p1) / 2 + V((0, -0.004, 0)), p1], 0.0075, ma, r_end=0.0055, sides=10))
        if double:      # 더블 핸들: 납작한 T 노브
            objs.append(cyl(0.0035, 0.0035, 0.02, ma, loc=p1 + V((0, -0.01, 0)), rot=AY, seg=8))
            objs.append(sphere(0.02, mk, loc=p1 + V((0, -0.026, 0)), scale=(1.0, 0.45, 0.6), rot=(0, -a, 0)))
        else:           # 둥근 손잡이 노브
            objs.append(cyl(0.0035, 0.0035, 0.016, ma, loc=p1 + V((0, -0.008, 0)), rot=AY, seg=8))
            objs.append(sphere(0.016, mk, loc=p1 + V((0, -0.03, 0)), scale=(1.0, 1.45, 1.0)))
    o = join(objs, 'Reel')
    o.scale = (size, size, size)
    o.rotation_euler = (0, 0, math.radians(22))
    return [o], 'item'


def _trade_gear():
    """마을 교환 장비 색·수치를 GearDatabase.cs의 Trade(...) 줄에서 읽는다 (C#과 한곳에서 관리)."""
    import re
    src = open(os.path.join(ROOT, 'Assets', 'Scripts', 'Data', 'GearDatabase.cs'), encoding='utf-8').read()
    out = {}
    pat = re.compile(r'Trade\("(\w+)", "[^"]*", GearSlot\.(\w+), \d+, "\w+", \d+, (null|"#\w+"), (null|"#\w+"),\s*"[^"]*",([^)]*)\)')
    for m in pat.finditer(src):
        iid, slot, main, accent, rest = m.groups()
        if main == 'null':
            continue    # 따로 모양을 정한 장비 (나팔고동 릴·황금거거 낚싯대)
        kw = dict(re.findall(r'(\w+): ([\d.]+)f', rest))
        out[iid] = (slot, main.strip('"'), accent.strip('"'), {k: float(v) for k, v in kw.items()})
    return out


def _mix(a, b, t):
    a, b = a.lstrip('#'), b.lstrip('#')
    return '#' + ''.join(f'{round(int(a[i:i + 2], 16) * (1 - t) + int(b[i:i + 2], 16) * t):02x}' for i in (0, 2, 4))


TRADE = _trade_gear()
LINE_STYLE = {}
for _id, (_slot, _main, _accent, _kw) in TRADE.items():
    if _slot == 'Rod':      # TelescopicRod.StyleFor 기본 갈래와 같은 규칙: 경조면 굵은 4마디
        stiff = _kw.get('flex', 0.5) <= 0.45
        ROD_STYLE[_id] = (_main, _accent, '#1f1a17', 4 if stiff else 5, 'heavy' if stiff else 'ring')
    elif _slot == 'Reel':   # TelescopicRod.BuildReel 기본 갈래와 같은 규칙: 권사량이 클수록 크고, 200m부터 더블 핸들
        cap = _kw.get('capacity', 60)
        size = 1.05 + 0.35 * min(1.0, max(0.0, (cap - 80) / 220))
        REEL_STYLE[_id] = (_main, _accent, _mix(_accent, '#ccd1db', 0.5), size, cap >= 200)
    elif _slot == 'Line':
        LINE_STYLE[_id] = (_main, _accent)


LINE_LOOK = {  # 플랜지(스풀 테), 라벨, 줄 색들 (여러 색 = 10m마다 색이 바뀌는 합사), 풀린 끝 색
    'line_nylon': ('#3c7cc0', '#f2f2ee', ['#eef4ee'], '#f4f8f4'),
    'line_carbon': ('#1d2026', '#c8a040', ['#aab6be'], '#c8d2d8'),
    'line_pe': ('#1c1c22', '#e8402a', ['#e8402a', '#2a7ae8', '#f0d020', '#30b040', '#f0f0f0'], '#e8402a'),
    'line_pe8': ('#c89a2a', '#1c1c22', ['#30b040', '#f0d020', '#e8402a', '#2a7ae8', '#f0f0f0', '#a040c0'], '#e8f4f8'),
}


def line_spool(flange, label, cols, loose, bands=None):
    """원줄 스풀 (실물 포장 사진 대조): 큰 플랜지 두 장 사이에 줄이 가득 감기고(감긴 결), 바깥 면에 둥근 라벨 + 가운데 구멍,
    풀린 줄 끝이 위로 말려 나온다. 축 = Y."""
    mf = mat(flange, 0.35, coat=0.5)
    AY = (math.pi / 2, 0, 0)
    objs = []
    for s in (-1, 1):
        objs.append(cyl(0.125, 0.125, 0.012, mf, loc=(0, s * 0.068, 0), rot=AY, seg=56))
        objs.append(torus(0.124, 0.0065, mf, loc=(0, s * 0.072, 0), rot=AY, seg=56, mseg=8))
        objs.append(cyl(0.022, 0.022, 0.004, mat('#0e0e10', 0.6), loc=(0, s * 0.0755, 0), rot=AY, seg=24))
    # 앞(-Y) 라벨: 색 원판 + 띠 + 글자 자리 줄
    ink = '#1a1a1a' if label != '#1c1c22' else '#f0f0f0'
    objs.append(cyl(0.085, 0.085, 0.003, mat(label, 0.4, coat=0.6), loc=(0, -0.0755, 0), rot=AY, seg=48))
    objs.append(torus(0.07, 0.004, mat(flange if label == '#f2f2ee' else '#f6f6f2', 0.45), loc=(0, -0.0775, 0), rot=AY, seg=48, mseg=6))
    for k in range(3):
        objs.append(box((0.07 - 0.015 * k, 0.002, 0.006), mat(ink, 0.5), loc=(0, -0.078, 0.05 - 0.011 * k)))
    # 감긴 줄: 색 덩어리마다 한 마디 (줄 결은 원둘레 방향)
    bands = bands or cols
    n = len(bands)
    w = 0.124 / n
    for i, c in enumerate(bands):
        objs.append(cyl(0.106, 0.106, w + 0.0004, mat_wound(c, 'Y', bands=150.0, rough=0.4, coat=0.6), loc=(0, -0.062 + w * (i + 0.5), 0),
                        rot=AY, seg=64))
    # 풀린 줄 끝: 줄 위쪽에서 나와 크게 말린다
    pts = [(0.0, 0.03, 0.106), (0.06, 0.02, 0.13), (0.12, -0.01, 0.18), (0.13, -0.05, 0.25), (0.08, -0.08, 0.29)]
    objs.append(tube(pts, 0.0028, mat(loose, 0.3, coat=0.6), sides=8))
    o = join(objs, 'Line')
    o.rotation_euler = (math.radians(-8), 0, math.radians(-18))
    return [o], 'item'


def model_line(iid):
    if iid in LINE_STYLE:
        main, accent = LINE_STYLE[iid]     # 마을 교환 원줄: 테 색 스풀에 원줄 색 합사, 사이사이 테 색 띠 (PE 합사 표시)
        return line_spool(_mix(accent, '#101418', 0.35), accent, [main], main,
                          bands=[main, main, accent, main, main, accent, main])
    return line_spool(*LINE_LOOK[iid])


def model_boat(iid):
    """배: 게임이 띄우는 배 모델(boats.py, 사진 대조)을 그대로 렌더한다 (BoatController.BuildModel과 같은 짝).
    고무보트 = boat_rib, 고속보트 = boat_speed, 블랙스피어 = boat_speed를 검게, 크루저 = boat_kr,
    돛단배 = boat_dinghy + 돛대·붐·삼각 돛(남색 띠). 뱃머리 = -Y."""
    import boats
    boats.B._mats.clear()       # boats.py는 build_icons를 따로 불러 재질 캐시가 둘이다 (장면을 새로 열 때마다 비운다)
    src = {'boat_rubber': 'boat_rib', 'boat_speed': 'boat_speed', 'boat_blackspear': 'boat_speed',
           'boat_cruiser': 'boat_kr', 'boat_sail': 'boat_dinghy'}[iid]
    objs = boats.PROPS[src](src)
    if iid == 'boat_blackspear':        # BoatController.BuildBlackSpear는 색 × (0.22, 0.22, 0.26). 아이콘 조명이 밝아 더 검게
        seen = set()
        for ob in objs:
            for m in ob.data.materials:
                if m is None or m.name in seen:
                    continue
                seen.add(m.name)
                bsdf = m.node_tree.nodes.get('Principled BSDF') if m.use_nodes else None
                if bsdf:
                    c = bsdf.inputs['Base Color'].default_value
                    bsdf.inputs['Base Color'].default_value = (c[0] * 0.1, c[1] * 0.1, c[2] * 0.12, 1)
    if iid == 'boat_sail':              # BoatController.BuildSailBoat: 돛대 + 붐 + 흰 삼각 돛 (남색 띠)
        steel = mat('#d8d8dc', 0.2, metal=0.8)
        sail_m, navy = mat('#f8f6ee', 0.55), mat('#1e3a78', 0.5)
        my = -0.45
        objs.append(cyl(0.05, 0.035, 6.2, steel, loc=(0, my, 0.55 + 3.1), seg=12))
        objs.append(cyl(0.035, 0.035, 2.5, steel, loc=(0, my + 1.25, 1.25), rot=(math.pi / 2, 0, 0), seg=10))
        bm = bmesh.new()
        nu, nv = 10, 14
        grid = []
        for i in range(nv + 1):         # 아래(붐) → 위(돛대 끝): 앞전은 돛대, 뒷전은 붐 끝에서 꼭대기로
            v = i / nv
            z = 1.32 + v * 4.9
            length = 2.4 * (1 - v) ** 1.05 + 0.08
            row = []
            for j in range(nu + 1):
                u = j / nu
                belly = 0.22 * math.sin(math.pi * u) * (1 - v) ** 0.6       # 바람을 받은 배(볼록)
                row.append(bm.verts.new((belly, my + 0.06 + u * length, z)))
            grid.append(row)
        for i in range(nv):
            for j in range(nu):
                f = bm.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
                f.material_index = 1 if 3 <= i <= 4 else 0
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.02)
        sail = boats.flat(obj_from_bm(bm, 'Sail', sail_m))
        sail.data.materials.append(navy)
        objs.append(sail)
    o = join(objs, 'Boat')
    o.rotation_euler = (0, 0, math.radians({'boat_sail': -62, 'boat_cruiser': -48}.get(iid, -40)))
    return [o], 'item'


# =====================================================================
#  모델: 미끼 · 음식 · 재료
# =====================================================================

def earthworm(path, R, seed=0):
    """지렁이 한 마리 (사진 대조): 머리가 뾰족하고 진한 적갈색, 머리 쪽 1/3에 굵고 밝은 환대(띠),
    몸은 고리 마디(환절)가 도드라진 분홍빛 갈색, 꼬리는 연하고 납작하게 가늘어진다."""
    def rad(t):
        head = min(1.0, (t + 0.015) / 0.07) ** 0.55
        tail = min(1.0, (1.01 - t) / 0.16) ** 0.6
        ring = 1 + 0.07 * abs(math.sin(t * math.pi * 75 + seed))
        band = 1.2 if 0.27 < t < 0.36 else 1.0
        return R * head * tail * ring * band
    n = 120
    out = []
    for t0, t1, col in ((0.0, 0.27, '#8a3a3c'), (0.265, 0.365, '#c8785e'), (0.355, 1.0, '#b8646a')):
        k0, k1 = int(t0 * (n - 1)), int(math.ceil(t1 * (n - 1)))
        pts = [path(i / (n - 1)) for i in range(k0, k1 + 1)]
        tt = [i / (n - 1) for i in range(k0, k1 + 1)]
        out.append(tube(pts, lambda u, tt=tt: rad(tt[0] + u * (tt[-1] - tt[0])), mat(col, 0.3, coat=0.9), sides=14))
    return out


def model_worm(iid):
    R = 0.019
    objs = []
    objs += earthworm(lambda t: (-0.32 + 0.64 * t, 0.1 + 0.07 * math.sin(t * 2 * math.pi * 1.1), R), R, 0)
    objs += earthworm(lambda t: (0.28 - 0.5 * t, -0.1 + 0.09 * math.sin(t * 2 * math.pi * 0.8 + 1.0) - 0.06 * t, R), R * 0.9, 2)
    for i in range(6):     # 흙 부스러기 몇 알
        a = i * 2.3
        objs.append(blob(0.012 + 0.004 * (i % 3), mat('#4a3626', 0.95), loc=(math.cos(a) * 0.3, math.sin(a) * 0.22, 0.008), amp=0.3, freq=3, seed=i,
                         seg=12, rings=8))
    return [join(objs, 'Worm')], 'item'


def model_paste(iid):
    """떡밥 (사진 대조): 연한 크림빛 반죽을 콩알만 하게 뭉쳐 바늘 굽은 곳을 감싼다 + 옆에 뭉친 알 두 개 + 부스러기."""
    dough = mat('#e6cf96', 0.85)
    hook = mat('#2c2c30', 0.25, metal=0.9)
    objs = []
    # 바늘: 귀(고리) → 축 → 굽은 곳 → 끝(미늘)
    pts = [V((0, 0, 0.24)), V((0, 0, 0.12))] + [V((0.05 - math.cos(a) * 0.05, 0, 0.12 - math.sin(a) * 0.05)) for a in
                                                (math.pi * k / 10 for k in range(1, 11))] + [V((0.1, 0, 0.18))]
    objs.append(tube(pts, 0.008, hook, sides=8))
    objs.append(torus(0.016, 0.005, hook, loc=(0, 0, 0.258), rot=(math.pi / 2, 0, 0), seg=16, mseg=6))
    objs.append(tube([V((0.1, 0, 0.18)), V((0.086, 0, 0.162))], 0.006, hook, sides=6))
    objs.append(blob(0.07, dough, loc=(0.05, 0, 0.08), amp=0.1, freq=2.5, grain=0.035, gfreq=30, seed=1))
    for i, (x, y, r) in enumerate(((0.2, 0.08, 0.05), (0.13, -0.12, 0.043))):
        objs.append(blob(r, dough, loc=(x, y, r * 0.85), scale=(1, 1, 0.9), amp=0.1, freq=2.5, grain=0.035, gfreq=30, seed=3 + i))
    for i in range(12):
        a = i * 2.4
        rr = 0.12 + 0.04 * (i % 3)
        objs.append(blob(0.01 + 0.003 * (i % 3), dough, loc=(0.12 + math.cos(a) * rr, math.sin(a) * rr * 0.8, 0.006), amp=0.3, seed=i,
                         seg=10, rings=6))
    return [join(objs, 'Paste')], 'item'


def corn_kernel(m, pale, loc, rot, size=1.0, seed=0):
    """옥수수 알 (사진 대조): 넓적하고 둥근 머리에서 밑동으로 좁아지는 쐐기꼴, 밑동 끝은 하얗다. 길이 = +X."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=16, radius=1.0)
    for v in bm.verts:
        p = V(tuple(math.copysign(abs(c) ** 0.7, c) for c in v.co))
        t = (p.x + 1) / 2                         # 0 = 밑동, 1 = 머리
        w = 0.55 + 0.45 * t ** 0.6
        v.co = V((p.x * 0.05, p.y * 0.042 * w, p.z * 0.03 * w)) * size
    k = obj_from_bm(bm, 'Kernel', m)
    tip = sphere(0.016 * size, pale, loc=(-0.045 * size, 0, 0), scale=(0.7, 1.1, 0.8), seg=12, rings=8)
    o = join([k, tip], 'Kernel')
    o.location, o.rotation_euler = loc, rot
    return o


def model_corn(iid):
    my = mat('#f6b82a', 0.25, coat=0.8)
    pale = mat('#f6ecc8', 0.5)
    objs = []
    for i in range(10):
        a = i * 2.39
        r = 0.03 + 0.035 * (i % 4)
        objs.append(corn_kernel(my, pale, (math.cos(a) * r, math.sin(a) * r, 0.03 + 0.025 * (i % 3)), (0.4 * (i % 3) - 0.4, 0.3 * (i % 2), a * 1.3),
                                1.0 + 0.08 * (i % 3), i))
    return [join(objs, 'Corn')], 'item'


def shrimp(m, scale=1.0, loc=(0, 0, 0), rot=0.0, legs=True, antenna=True, eye=0.008, lie=False):
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
    objs.append(sphere(eye, mat('#101010', 0.2), loc=(pts[1][0] - 0.02, pts[1][1] + 0.02, 0.03)))
    objs.append(sphere(eye, mat('#101010', 0.2), loc=(pts[1][0] - 0.02, pts[1][1] + 0.02, -0.03)))
    if legs:
        for i in range(5):
            p = pts[4 + i * 2]
            objs.append(tube([p, (p[0] + 0.03, p[1] - 0.04, 0.0)], 0.003, m))
    o = join(objs, 'Shrimp')
    o.scale = (scale, scale, scale)
    o.location = loc
    o.rotation_euler = (0.25 if lie else math.pi / 2, 0, rot)   # lie: 옆으로 누운 새우
    return o


def model_krill(iid):
    """크릴 (사진 대조): 분홍·주황 반투명 작은 새우 한 줌, 까만 눈이 크다."""
    m = mat('#f2967a', 0.35, trans=0.35, coat=0.6)
    spots = [(0.0, 0.0, 0.03, 0.2), (0.13, 0.09, 0.04, 2.0), (-0.12, 0.11, 0.03, 4.0), (0.05, -0.13, 0.03, 1.1),
             (-0.08, -0.06, 0.07, 5.2), (0.16, -0.06, 0.03, 3.3), (0.03, 0.06, 0.08, 0.7)]
    objs = [shrimp(m, 0.6 + 0.04 * (i % 3), (x, y, z), r, eye=0.016, lie=True) for i, (x, y, z, r) in enumerate(spots)]
    return objs, 'item'


def model_shrimp(iid):
    return [shrimp(mat('#a8b0a8', 0.25, trans=0.5, coat=0.6), 1.2, (0, 0, 0.05), 0.4)], 'item'


def ragworm(path, R, seed=0):
    """청갯지렁이 한 마리 (사진 대조): 올리브·청록빛 등(무지갯빛 윤기), 마디마다 양옆에 주황·분홍 다리(측각) + 센털,
    머리에 짧은 촉수."""
    body = mat('#5e6e3a', 0.3, coat=0.8, film=320)
    leg = mat('#d8845a', 0.45, coat=0.4)
    bristle = mat('#f0c8a0', 0.5)
    n = 90
    pts = [path(i / (n - 1)) for i in range(n)]
    def rad(t):
        return R * min(1.0, (t + 0.02) / 0.06) ** 0.5 * min(1.0, (1.01 - t) / 0.25) ** 0.7
    objs = [tube(pts, rad, body, sides=12)]
    for i in range(3, n - 2, 2):
        p = V(pts[i])
        t = (V(pts[i + 1]) - V(pts[i - 1])).normalized()
        side = t.cross(V((0, 0, 1))).normalized()
        rr = rad(i / (n - 1))
        for sgn in (-1, 1):
            d = side * sgn
            rot = d.to_track_quat('X', 'Z').to_euler()
            objs.append(sphere(rr * 0.75, leg, loc=p + d * rr * 1.25 + V((0, 0, -rr * 0.2)), scale=(1.0, 0.45, 0.35), rot=rot, seg=10, rings=6))
            objs.append(tube([p + d * rr * 1.6, p + d * rr * 2.6 + t * rr * 0.4], rr * 0.12, bristle, sides=4))
    h = V(pts[0])
    t0 = (V(pts[1]) - h).normalized()
    for k in range(4):
        a = (k - 1.5) * 0.45
        dirv = (-t0 * math.cos(a) + t0.cross(V((0, 0, 1))) * math.sin(a)).normalized()
        objs.append(tube([h, h + dirv * R * 1.6 + V((0, 0, R * 0.3))], R * 0.18, leg, r_end=R * 0.05, sides=5))
    return objs


def model_seaworm(iid):
    R = 0.017
    objs = []
    objs += ragworm(lambda t: (-0.3 + 0.6 * t, 0.09 + 0.08 * math.sin(t * 2 * math.pi * 1.2), R), R, 0)
    objs += ragworm(lambda t: (0.26 - 0.48 * t, -0.11 + 0.07 * math.sin(t * 2 * math.pi * 0.9 + 1.3), R + 0.004 * math.sin(t * 9)), R * 0.92, 1)
    return [join(objs, 'SeaWorm')], 'item'


def model_squid(iid):
    """오징어살 (사진 대조): 하얀 반투명 살을 길쭉한 세모 띠로 썰고 끝을 제비꼬리로 가른다. 겉면에 자잘한 적갈색 색소 점."""
    m = mat_spots('#fbf6ee', '#9a5060', scale=55.0, thresh=0.7, rough=0.3, coat=0.8)
    objs = []
    for i in range(3):
        L, w0 = 0.42 - 0.04 * i, 0.055
        bm = bmesh.new()
        nu, nv = 28, 4
        grid = []
        for a in range(nu + 1):
            u = a / nu
            row = []
            for b in range(nv + 1):
                v = b / nv
                h = w0 * (1 - 0.72 * u)
                x = u * L
                if u > 0.78 and b == nv // 2:      # 가운데를 뒤로 당겨 제비꼬리 홈
                    x = 0.78 * L + (u - 0.78) * L * 0.25
                y = (v - 0.5) * 2 * h
                z = 0.03 * math.sin(u * math.pi * 1.4 + i * 1.7) + 0.01 * (v - 0.5) * math.sin(u * 5)
                row.append(bm.verts.new((x, y, z)))
            grid.append(row)
        for a in range(nu):
            for b in range(nv):
                bm.faces.new((grid[a][b], grid[a + 1][b], grid[a + 1][b + 1], grid[a][b + 1]))
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.012)
        o = obj_from_bm(bm, 'Strip', m)
        o.location = (-0.2 + 0.02 * i, -0.08 + i * 0.08, 0.02 + i * 0.012)
        o.rotation_euler = (0, 0, 0.25 * (i - 1))
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
    """스푼 루어 (사진 대조): 한쪽이 넓은 물방울꼴 오목 금속판(망치 자국), 양끝 스플릿 링, 넓은 쪽 끝에 트레블 훅."""
    objs = []
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=40, v_segments=20, radius=1.0)
    for v in bm.verts:
        x, y, z = v.co
        w = 0.055 + 0.05 * (y + 1) / 2                    # 앞(-y) 좁고 뒤(+y) 넓다
        v.co = V((x * w, y * 0.16, z * 0.01 + 0.022 * (1 - x * x)))     # 가로로 휘어 오목하다
    objs.append(obj_from_bm(bm, 'Spoon', mat_bump('#e2ac34', 90.0, 0.35, 0.18, metal=1.0)))
    ring_m = mat('#c8c8cc', 0.2, metal=1.0)
    objs.append(torus(0.015, 0.0025, ring_m, loc=(0, -0.172, 0.02), rot=(0, math.pi / 2, 0)))
    objs.append(torus(0.015, 0.0025, ring_m, loc=(0, 0.172, 0.02), rot=(0, math.pi / 2, 0)))
    # 트레블 훅: 세 갈래 바늘
    hook_m = mat('#2a2a2e', 0.3, metal=0.9)
    objs.append(cyl(0.003, 0.003, 0.07, hook_m, loc=(0, 0.217, 0.02), rot=(math.pi / 2, 0, 0)))
    for k in range(3):
        a = k * 2 * math.pi / 3
        dvec = V((math.cos(a), 0, math.sin(a)))
        base = V((0, 0.252, 0.02))
        pts = [base, base + dvec * 0.025 + V((0, 0.01, 0)), base + dvec * 0.035 - V((0, 0.02, 0)), base + dvec * 0.022 - V((0, 0.035, 0))]
        objs.append(tube(pts, 0.0028, hook_m, r_end=0.0012, sides=6))
    o = join(objs, 'Lure')
    o.rotation_euler = (math.radians(30), 0, math.radians(-40))     # 오목한 면이 카메라를 보게
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


def model_fly(iid):
    """플라이 (드라이 플라이): 바늘 + 털 몸통 + 목털(해클) 고리 + 꼬리털 + 날개."""
    objs = []
    hook = mat('#30302e', 0.25, metal=0.9)
    pts = [V((0, -0.12, 0.02)), V((0, 0.06, 0.02)), V((0, 0.11, -0.01)), V((0, 0.11, -0.06)), V((0, 0.07, -0.08)), V((0, 0.04, -0.05))]
    objs.append(tube(pts, 0.004, hook, r_end=0.002, sides=6))
    objs.append(torus(0.012, 0.003, hook, loc=(0, -0.13, 0.02), rot=(0, math.pi / 2, 0), seg=16, mseg=6))
    objs.append(tube([V((0, -0.06, 0.02)), V((0, 0.05, 0.02))], 0.012, mat('#c8a060', 0.8), r_end=0.008, sides=10))
    hackle = mat('#7a5030', 0.7)
    for k in range(18):
        a = k * 2 * math.pi / 18
        d = V((math.cos(a), 0, math.sin(a)))
        base = V((0, -0.07, 0.02))
        objs.append(tube([base, base + d * 0.05 + V((0, 0.01, 0))], 0.0015, hackle, sides=4))
    for k in range(5):
        a = (k - 2) * 0.12
        objs.append(tube([V((0, 0.05, 0.02)), V((math.sin(a) * 0.03, 0.13, 0.03))], 0.0012, mat('#a08060', 0.6), sides=4))
    wing = mat('#e8e4d8', 0.5, alpha=0.9)
    for s in (-1, 1):
        objs.append(sphere(0.03, wing, loc=(s * 0.015, -0.08, 0.07), scale=(0.25, 0.6, 1.0), rot=(0, s * 0.3, 0)))
    o = join(objs, 'Fly')
    o.rotation_euler = (0, 0, math.radians(-40))
    return [o], 'item'


def model_cutbait(iid):
    """해체 미끼 5단계 (사진 대조) — 모양으로 구별한다.
    1 어분덩이: 갈색 알갱이를 뭉친 공 + 가루 / 2 악취나는살점: 분홍 살 토막(옆면 흰 근육 결) 위에 은빛 껍질
    3 덩어리진생선내장: 검붉은 창자 뭉치 + 간 + 흰 지방 / 4 촉촉한이리: 하얗고 구불구불한 덩어리 여러 갈래
    5 비릿한볼살: 점박이 껍질이 덮인 동그랗고 두툼한 살"""
    k = int(iid[-1])
    objs = []
    if k == 1:
        meal = mat('#a8743c', 0.95)
        objs.append(blob(0.13, meal, loc=(0, 0, 0.11), scale=(1, 1, 0.88), amp=0.08, freq=2.2, grain=0.06, gfreq=38, seed=2, seg=64, rings=36))
        for i in range(16):
            a = i * 2.4
            rr = 0.17 + 0.04 * (i % 4)
            objs.append(blob(0.008 + 0.004 * (i % 3), mat('#b88a4c', 0.95), loc=(math.cos(a) * rr, math.sin(a) * rr, 0.005), amp=0.3, seed=i,
                             seg=10, rings=6))
    elif k == 2:
        flesh = mat_bands('#e27a68', '#f8e2da', scale=7.0, width=0.14, distort=2.5)
        skin = mat_spots('#8e9aa6', '#38424c', scale=18.0, thresh=0.62, rough=0.25, metal=0.6, coat=0.8)
        objs.append(blob(0.14, flesh, loc=(0, 0, 0.06), scale=(1.35, 0.8, 0.45), square=0.75, amp=0.16, freq=2.0, seed=4))
        objs.append(blob(0.142, skin, loc=(0, 0, 0.058), scale=(1.35, 0.8, 0.45), square=0.75, amp=0.16, freq=2.0, seed=4, zcut=-0.2, below=True))
    elif k == 3:
        gut = mat('#8a2a2c', 0.3, coat=0.8)
        gut2 = mat('#b05a48', 0.35, coat=0.7)
        for j, (cx, cy, ph, col) in enumerate(((-0.05, 0.02, 0.0, gut), (0.06, -0.03, 1.7, gut2), (0.0, 0.07, 3.1, gut))):
            pts = [V((cx + 0.09 * math.cos(ph + t * 3.2 * math.pi) * (0.6 + 0.4 * t), cy + 0.07 * math.sin(ph + t * 3.2 * math.pi),
                      0.03 + 0.03 * t + 0.015 * math.sin(t * 11))) for t in (q / 59 for q in range(60))]
            objs.append(tube(pts, lambda u: 0.016 * (1 + 0.15 * math.sin(u * 40)), col, sides=12))
        objs.append(blob(0.08, mat('#5a1e18', 0.25, coat=0.9), loc=(-0.1, -0.08, 0.04), scale=(1.4, 0.9, 0.45), amp=0.1, seed=7))   # 간
        objs.append(blob(0.06, mat('#c88a78', 0.35, coat=0.6), loc=(0.12, 0.08, 0.05), scale=(1.3, 0.8, 0.7), amp=0.12, seed=8))    # 위
        for j in range(4):
            objs.append(blob(0.028, mat('#e8d49a', 0.45, coat=0.5), loc=(-0.02 + 0.06 * j, -0.1 + 0.05 * (j % 2), 0.05), scale=(1.2, 1, 0.45), amp=0.25, seed=j,
                             seg=14, rings=8))
    elif k == 4:
        milt = mat('#f0d6d0', 0.18, coat=1.0)
        for j in range(11):
            a = j * 2.3
            rr = 0.0 if j == 0 else 0.07 + 0.03 * (j % 3)
            c = V((math.cos(a) * rr, math.sin(a) * rr, 0.035 + (0.05 if j < 4 else 0.0)))
            d = V((math.cos(a * 1.7 + 1), math.sin(a * 1.7 + 1), 0)).normalized()
            pts = [c + d * (0.11 * (t - 0.5)) + d.cross(V((0, 0, 1))) * 0.03 * math.sin(t * 2 * math.pi + j) + V((0, 0, 0.012 * math.sin(t * 7)))
                   for t in (q / 15 for q in range(16))]
            objs.append(tube(pts, lambda u: 0.022 * (math.sin(math.pi * u) ** 0.35) * (1 + 0.22 * abs(math.sin(u * 22))), milt, sides=14))
    else:
        # 사진: 볼살은 가리비 관자처럼 동그랗고 두툼한 살. 한 점은 살 쪽, 한 점은 점박이 비늘 껍질 쪽이 위
        flesh = mat_bands('#f4d2c6', '#fff4ee', scale=9.0, width=0.1, distort=1.2, axis='Y', rough=0.25, coat=0.8)
        skin = mat_spots('#7a5c3c', '#24180e', scale=30.0, thresh=0.56, rough=0.3, coat=0.8)
        objs.append(blob(0.1, flesh, loc=(-0.07, 0.02, 0.05), scale=(1.1, 0.95, 0.55), amp=0.08, freq=1.6, seed=11))
        objs.append(blob(0.09, flesh, loc=(0.1, -0.04, 0.045), scale=(1.15, 0.95, 0.55), rot=(0, 0, 0.6), amp=0.08, freq=1.6, seed=12))
        objs.append(blob(0.0915, skin, loc=(0.1, -0.04, 0.047), scale=(1.15, 0.95, 0.55), rot=(0, 0, 0.6), amp=0.08, freq=1.6, seed=12, zcut=0.25))
    return [join(objs, 'Cut')], 'item'


def model_kimbap(iid):
    """김밥 (사진 대조): 김 테두리 안에 밥알이 오톨도톨한 흰 밥, 가운데 단무지·당근·계란·시금치·햄·우엉. 썬 조각이 비스듬히 기대 줄지어 있다."""
    nori = mat_bump('#10160f', 90.0, 0.3, 0.55, coat=0.4)
    rice = mat_bump('#f6f3ea', 140.0, 0.9, 0.75)
    fill = [((0.02, 0.022, 0.017), '#f2c424'), ((-0.024, 0.014, 0.014), '#2a7a28'), ((0.0, -0.026, 0.017), '#f6d860'),
            ((-0.027, -0.016, 0.013), '#e86a72'), ((0.03, -0.012, 0.012), '#ec7a2a'), ((-0.004, 0.004, 0.01), '#6a4422')]
    objs = []
    for i in range(4):
        parts = [cyl(0.075, 0.075, 0.05, nori, seg=40), cyl(0.069, 0.069, 0.052, rice, seg=40)]
        for (x, y, r), c in fill:
            parts.append(cyl(r, r, 0.054, mat(c, 0.45, coat=0.3), loc=(x, y, 0), rot=(0, 0, i), seg=16))
        o = join(parts, f'Slice{i}')
        o.location = (-0.21 + i * 0.13, 0.03 * i, 0.072)
        o.rotation_euler = (math.radians(62), 0, math.radians(6 * (i - 1.5)))
        objs.append(o)
    return objs, 'item'


def model_ramen(iid):
    """컵라면 (사진 대조): 위가 넓은 종이 사발, 띠 무늬, 뚜껑을 반쯤 뜯어 뒤로 젖혔고 안에 꼬불꼬불한 면 덩어리."""
    cup = cyl(0.125, 0.165, 0.2, mat('#d8302a', 0.5), loc=(0, 0, 0.1), seg=48, cap=False)
    wall = cyl(0.12, 0.16, 0.196, mat('#f4ecd8', 0.6), loc=(0, 0, 0.1), seg=48, cap=False)       # 안쪽 흰 종이 벽
    floor = cyl(0.125, 0.125, 0.004, mat('#f4ecd8', 0.6), loc=(0, 0, 0.004), seg=48)
    band = cyl(0.147, 0.158, 0.05, mat('#f6f0e0', 0.6), loc=(0, 0, 0.135), seg=48)
    band2 = cyl(0.135, 0.14, 0.018, mat('#1a1a1a', 0.5), loc=(0, 0, 0.04), seg=48)
    rim = torus(0.165, 0.006, mat('#f6f0e0', 0.5), loc=(0, 0, 0.2), seg=48, mseg=6)
    inner = cyl(0.152, 0.152, 0.03, mat('#d8b060', 0.7), loc=(0, 0, 0.16), seg=48)                # 면 덩어리 윗면
    objs = [cup, wall, floor, band, band2, rim, inner]
    noodle = mat('#ecc878', 0.5, coat=0.3)
    for k in range(9):      # 꼬불꼬불한 면 가닥 (덩어리 위를 지그재그로)
        y0 = -0.12 + 0.03 * k
        half = math.sqrt(max(0.0, 0.15 ** 2 - y0 ** 2))
        pts = [V((-half + 2 * half * t, y0 + 0.012 * math.sin(t * 40 + k), 0.182 + 0.008 * math.sin(t * 40 + k + 1.5))) for t in (q / 59 for q in range(60))]
        objs.append(tube(pts, 0.007, noodle, sides=6))
    # 반쯤 뜯어 젖힌 뚜껑 (사진): 뒤 반원(+y)은 붙어 있고, 앞 반원을 뜯어 가운데 선을 축으로 세워 뒤로 젖혔다 → 앞쪽에 면이 보인다
    bm = bmesh.new()
    nu, nv = 48, 12
    grid = []
    for i in range(nu):
        a = 2 * math.pi * i / nu
        row = []
        for j in range(1, nv + 1):
            rr = 0.17 * j / nv
            x, y = math.cos(a) * rr, math.sin(a) * rr
            if y < 0:
                th = math.radians(100 + 45 * (-y / 0.17))         # 가운데에서 멀수록 더 말린다
                p = (x, (-y) * -math.cos(th), 0.203 + (-y) * math.sin(th))
            else:
                p = (x, y, 0.203)
            row.append(bm.verts.new(p))
        grid.append(row)
    for i in range(nu):
        for j in range(nv - 1):
            bm.faces.new((grid[i][j], grid[(i + 1) % nu][j], grid[(i + 1) % nu][j + 1], grid[i][j + 1]))
    bm.faces.new([grid[i][0] for i in range(nu)])
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.003)
    lid = obj_from_bm(bm, 'Lid', mat('#e8e2d8', 0.3, metal=0.4))
    for p in lid.data.polygons:
        p.use_smooth = False
    objs.append(lid)
    o = join(objs, 'Ramen')
    o.rotation_euler = (0, 0, math.radians(-30))
    return [o], 'item'


def model_fishcake(iid):
    """살살녹는어묵 (사진 대조): 얇은 사각 어묵 판을 꼬치에 아코디언처럼 접어 꽂았다. 꼬치 두 개."""
    cake = mat_bump('#e6b878', 50.0, 0.2, 0.5, coat=0.4)
    stick = mat('#d2b07a', 0.7)
    objs = []
    for k in range(2):
        parts = [cyl(0.0055, 0.0055, 0.58, stick, loc=(0, 0, 0.0), seg=10)]
        bm = bmesh.new()
        nz, nx = 90, 8
        W = 0.17
        grid = []
        for i in range(nz + 1):
            z = -0.15 + 0.32 * i / nz
            ph = (z + 0.15) / 0.32 * 3.0 * 2 * math.pi           # 세 번 접힘
            y = 0.065 * math.sin(ph) * (1 + 0.15 * math.sin(z * 31 + k))
            row = []
            for j in range(nx + 1):
                x = (j / nx - 0.5) * W
                row.append(bm.verts.new((x, y + 0.01 * math.sin(x * 40 + ph), z)))
            grid.append(row)
        for i in range(nz):
            for j in range(nx):
                bm.faces.new((grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1]))
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.012)
        parts.append(obj_from_bm(bm, 'Cake', cake))
        o = join(parts, f'Skewer{k}')
        o.location = (-0.08 + 0.16 * k, 0.03 * k, 0.0)
        o.rotation_euler = (0, math.radians(-14 + 28 * k), math.radians(-20 + 15 * k))
        objs.append(o)
    return objs, 'item'


def model_coffee(iid):
    """보온병 믹스커피: 은색 보온병 + 컵 뚜껑 + 노란 막대 커피."""
    body = cyl(0.07, 0.07, 0.38, mat('#c8ccd2', 0.18, metal=0.9), loc=(0, 0, 0.19), seg=40)
    band = cyl(0.072, 0.072, 0.07, mat('#2a5a9a', 0.45), loc=(0, 0, 0.22), seg=40)
    cap = cyl(0.074, 0.068, 0.09, mat('#2a2a2a', 0.5), loc=(0, 0, 0.425), seg=40)
    cup = cyl(0.06, 0.05, 0.07, mat('#2a2a2a', 0.5), loc=(0.17, -0.03, 0.035), seg=32)
    coffee = cyl(0.055, 0.055, 0.005, mat('#7a4a22', 0.2), loc=(0.17, -0.03, 0.068), seg=32)
    stick = box((0.03, 0.13, 0.006), mat('#f0c020', 0.45), loc=(-0.15, -0.05, 0.004), rot=(0, 0, math.radians(30)), bevel=0.002)
    return [body, band, cap, cup, coffee, stick], 'item'


def model_dosirak(iid):
    """바다 도시락 (사진 대조): 검은 칸막이 상자, 왼쪽 칸 밥(밥알 요철 + 매실), 오른쪽 위 칸 흰 지방 결이 있는 연어회 다섯 점,
    오른쪽 아래 칸 채소·레몬·와사비."""
    blk = mat('#16161a', 0.35, coat=0.6)
    red_in = mat('#5a1414', 0.4, coat=0.6)
    objs = [box((0.46, 0.32, 0.016), blk, loc=(0, 0, 0.008), bevel=0.006),
            box((0.44, 0.3, 0.004), red_in, loc=(0, 0, 0.017))]
    for sx, sy, w, d in ((0, 0.155, 0.46, 0.01), (0, -0.155, 0.46, 0.01), (0.225, 0, 0.01, 0.32), (-0.225, 0, 0.01, 0.32)):
        objs.append(box((w, d, 0.07), blk, loc=(sx, sy, 0.04), bevel=0.003))
    objs.append(box((0.008, 0.29, 0.06), blk, loc=(-0.045, 0, 0.04)))                  # 세로 칸막이
    objs.append(box((0.25, 0.008, 0.06), blk, loc=(0.0875, -0.04, 0.04)))              # 가로 칸막이
    rice = mat_bump('#f7f4ec', 160.0, 1.0, 0.75)
    objs.append(blob(0.1, rice, loc=(-0.13, 0, 0.045), scale=(0.85, 1.35, 0.32), square=0.45, amp=0.06, seed=3))
    objs.append(blob(0.016, mat('#b8202a', 0.4, coat=0.6), loc=(-0.13, 0.0, 0.078), amp=0.2, seed=5, seg=14, rings=8))
    salmon = mat_bands('#f2864e', '#fde6d2', scale=12.0, width=0.18, distort=2.0, axis='X', rough=0.25, coat=0.9)
    for i in range(4):     # 넓적한 회 네 점: 가로로 눕혀 뒤로 갈수록 높게 계단처럼 겹친다 (윗면의 흰 결이 보이게)
        objs.append(blob(0.06, salmon, loc=(0.09, 0.0 + 0.04 * i, 0.03 + 0.01 * i), scale=(1.45, 0.62, 0.14),
                         rot=(math.radians(-14), 0, math.radians(4 * (i - 1.5))), square=0.45, amp=0.05, seed=20 + i))
    objs.append(cyl(0.032, 0.032, 0.008, mat('#f2e070', 0.4, coat=0.4), loc=(0.02, -0.095, 0.05), rot=(math.radians(30), 0, 0), seg=24))
    objs.append(torus(0.032, 0.004, mat('#e8c020', 0.4), loc=(0.02, -0.095, 0.05), rot=(math.radians(30), 0, 0), seg=24, mseg=6))
    for i in range(5):
        objs.append(blob(0.022, mat('#4a9a2e', 0.5, coat=0.3), loc=(0.08 + 0.028 * i, -0.1 + 0.02 * (i % 2), 0.045), amp=0.3, freq=3, seed=30 + i,
                         seg=16, rings=10))
    objs.append(blob(0.016, mat('#9ac050', 0.6), loc=(0.17, -0.07, 0.05), amp=0.25, seed=40, seg=14, rings=8))
    return objs, 'item'


def glove(coated_up):
    """반코팅 목장갑 한 짝 (사진 대조): 흰 면 니트, 손바닥·손가락 앞면(-Y)만 빨간 고무 코팅, 손목 시보리 + 테두리 박음질.
    손바닥 평면 = XZ, 손가락 = +Z."""
    knit = mat_bump('#f2efe8', 120.0, 0.6, 0.95, split=('Y', -0.004, '#d8242a'))
    cuff = mat_bump('#f2efe8', 120.0, 0.6, 0.95)
    stitch = mat('#2a5ab0', 0.7)
    parts = [blob(0.06, knit, loc=(0, 0, 0.07), scale=(0.95, 0.32, 1.05), square=0.6, amp=0.04, seed=1)]
    for k, (fx, ln, lean) in enumerate(((-0.038, 0.085, -0.12), (-0.013, 0.1, -0.04), (0.013, 0.095, 0.04), (0.037, 0.075, 0.12))):
        base = V((fx, 0, 0.125))
        tip = base + V((math.sin(lean) * ln, 0, math.cos(lean) * ln))
        parts.append(tube([base, (base + tip) / 2, tip], lambda u: 0.0135 * (1 - 0.1 * u), knit, sides=14))
        parts.append(sphere(0.0122, knit, loc=tip, seg=14, rings=8))
    tb, tt = V((-0.05, 0, 0.06)), V((-0.105, 0, 0.12))
    parts.append(tube([tb, (tb + tt) / 2 + V((-0.006, 0, -0.004)), tt], lambda u: 0.016 * (1 - 0.15 * u), knit, sides=14))
    parts.append(sphere(0.0136, knit, loc=tt, seg=14, rings=8))
    parts.append(cyl(0.058, 0.055, 0.06, cuff, loc=(0, 0, -0.012), seg=28))
    parts[-1].scale = (1.0, 0.42, 1.0)
    parts.append(torus(0.058, 0.0035, stitch, loc=(0, 0, -0.041), seg=28, mseg=6))
    parts[-1].scale = (1.0, 0.42, 1.0)
    o = join(parts, 'Glove')
    return o


def model_gloves(iid):
    """미끄러지지 않는 목장갑: 한 짝은 빨간 코팅 손바닥이, 한 짝은 흰 손등이 보이게 겹쳐 놓는다."""
    g1 = glove(True)
    g1.location = (-0.03, 0.0, 0.03)
    g1.rotation_euler = (math.radians(-62), 0, math.radians(15))      # 손바닥(-Y)이 위·앞을 본다
    g2 = glove(False)
    g2.location = (0.1, 0.06, 0.0)
    g2.rotation_euler = (math.radians(65), 0, math.radians(-20))      # 손등이 위
    return [g1, g2], 'item'


def model_coin(iid):
    """대물 코인: 금화 세 닢, 가운데에 물고기 무늬 돋을새김."""
    gold = mat('#e8b838', 0.22, metal=1.0)
    objs = []
    for i, (x, y, z, rx) in enumerate(((-0.1, 0.02, 0.02, 0), (0.08, -0.04, 0.05, 15), (0.0, 0.06, 0.13, 60))):
        parts = [cyl(0.12, 0.12, 0.022, gold, seg=48), torus(0.11, 0.008, gold, loc=(0, 0, 0.011), seg=48)]
        # 물고기 (타원 몸 + 삼각 꼬리)
        parts.append(sphere(0.05, gold, loc=(-0.01, 0, 0.012), scale=(1.0, 0.45, 0.12)))
        parts.append(box((0.03, 0.05, 0.008), gold, loc=(0.055, 0, 0.012), rot=(0, 0, math.radians(45))))
        o = join(parts, f'Coin{i}')
        o.location = (x, y, z)
        o.rotation_euler = (math.radians(rx), 0, math.radians(20 * i))
        objs.append(o)
    return objs, 'item'


def mat_fish_scale():
    """물고기 비늘 (사진 대조): 은빛 무지개 반사 + 가운데(초점)를 두른 동심 성장선."""
    key = ('fishscale',)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new('FishScale')
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes['Principled BSDF']
    b.inputs['Roughness'].default_value = 0.12
    b.inputs['Metallic'].default_value = 0.55
    for nm, val in (('Coat Weight', 1.0), ('Thin Film Thickness', 480.0), ('Transmission Weight', 0.25)):
        if nm in b.inputs:
            b.inputs[nm].default_value = val
    tc = nt.nodes.new('ShaderNodeTexCoord')
    wv = nt.nodes.new('ShaderNodeTexWave')
    wv.wave_type = 'RINGS'
    wv.rings_direction = 'Z'
    wv.inputs['Scale'].default_value = 28.0
    wv.inputs['Distortion'].default_value = 0.8
    mp = nt.nodes.new('ShaderNodeMapping')           # 비늘마다 제 초점(-0.02, 0)을 중심으로
    mp.inputs['Location'].default_value = (0.02, 0, 0)
    nt.links.new(tc.outputs['Object'], mp.inputs['Vector'])
    nt.links.new(mp.outputs['Vector'], wv.inputs['Vector'])
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = hexc('#aebcc8')
    ramp.color_ramp.elements[1].color = hexc('#e6eef4')
    nt.links.new(wv.outputs['Fac'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
    bump = nt.nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.4
    nt.links.new(wv.outputs['Fac'], bump.inputs['Height'])
    nt.links.new(bump.outputs['Normal'], b.inputs['Normal'])
    _mats[key] = m
    return m


def model_scale(iid):
    """반짝이는 비늘 (사진 대조): 둥근 부채꼴 비늘 다섯 장. 뒤쪽 가장자리는 둥글고 앞쪽(박혀 있던 쪽)은 물결진 톱니."""
    objs = []
    m = mat_fish_scale()
    for i in range(5):
        bm = bmesh.new()
        n = 40
        ring = []
        for k in range(n):
            a = 2 * math.pi * k / n
            r = 1.0 + (0.06 * abs(math.sin(a * 4)) if math.cos(a) < -0.2 else 0.0)    # 앞쪽 물결 톱니
            ring.append(bm.verts.new((math.cos(a) * 0.11 * r, math.sin(a) * 0.095 * r, 0)))
        c = bm.verts.new((-0.02, 0, 0.012))
        for k in range(n):
            bm.faces.new((ring[k], ring[(k + 1) % n], c))
        bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if c in e.verts], cuts=4, use_grid_fill=True)
        for v in bm.verts:
            v.co.z = 0.006 * (1 - (v.co.x ** 2 / 0.11 ** 2 + v.co.y ** 2 / 0.095 ** 2))
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.003)
        o = obj_from_bm(bm, 'Scale', m)
        a = i * 1.3
        o.location = (math.cos(a) * 0.08, math.sin(a) * 0.06, 0.008 * i)
        o.rotation_euler = (0.2 * math.sin(i), 0.25 * math.cos(i), a)
        objs.append(o)
    return objs, 'item'     # 따로 둔다 (성장선이 비늘마다 제 중심을 돈다)


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


def model_napal(iid):
    """나팔고동 (나팔고둥): 소라와 같은 나선 관이지만 탑이 높고 길쭉하다. 크림색 바탕에 갈색 초승달 무늬, 주황빛 입구."""
    bm = bmesh.new()
    turns, n, sides = 6.0, 320, 40
    k = math.log(1.75) / (2 * math.pi)       # 한 바퀴에 1.75배 (소라보다 천천히 커져서 탑이 길다)
    tmax = turns * 2 * math.pi
    rings, ths = [], []
    for i in range(n):
        th = tmax * i / (n - 1)
        r = math.exp(k * (th - tmax))
        c = V((math.cos(th) * r * 0.8, math.sin(th) * r * 0.8, -2.4 * r))
        rr = 0.62 * r
        flare = 1.0 + 0.4 * max(0.0, (th - (tmax - 0.8)) / 0.8) ** 2   # 입구 쪽 입술이 벌어진다
        out, up = V((math.cos(th), math.sin(th), 0)), V((0, 0, 1))
        ring = []
        for q in range(sides):
            ph = 2 * math.pi * q / sides
            ex = 1.0 + 0.05 * math.cos(ph * 7)                         # 나선을 따라 도는 굵은 줄(나륵)
            ring.append(bm.verts.new(c + (out * math.cos(ph) * flare + up * math.sin(ph) * 1.35) * rr * ex))
        rings.append(ring)
        ths.append(th)
    for i in range(n - 1):
        for q in range(sides):
            f = bm.faces.new((rings[i][q], rings[i][(q + 1) % sides], rings[i + 1][(q + 1) % sides], rings[i + 1][q]))
            ph = 2 * math.pi * q / sides
            f.material_index = 1 if math.sin(ths[i] * 2.5 + 1.4 * math.sin(ph)) > 0.45 else 0
    o = obj_from_bm(bm, 'Napal', mat('#efe2c4', 0.35, coat=0.6))
    o.data.materials.append(mat('#8a4a22', 0.4, coat=0.6))
    end = V((math.cos(tmax) * 0.8, math.sin(tmax) * 0.8, -2.4))
    lip = sphere(0.5, mat('#f2a46a', 0.15, coat=1.0, film=300), loc=end + V((0.35, 0.0, 0.0)), scale=(0.3, 0.75, 1.15))
    o = join([o, lip], 'Napal')
    o.scale = (0.1, 0.1, 0.1)
    o.rotation_euler = (math.radians(70), 0, math.radians(135))   # 탑(+z)이 화면을 가로질러 오른쪽 위로 눕게
    return [o], 'item'


def model_goldgege(iid):
    """황금거거: 금빛 대왕조개(거거) 한 쌍. 부채꼴 껍데기에 굵은 주름 다섯 줄, 벌어진 틈으로 청록 외투막."""
    gold = mat('#e8b838', 0.22, metal=1.0)

    def valve(sign):
        bm = bmesh.new()
        na, ns = 120, 24
        grid = []
        for i in range(na + 1):
            a = math.pi * (0.06 + 0.88 * i / na)
            row = []
            for j in range(ns + 1):
                s = 0.03 + 0.97 * j / ns
                fold = math.cos(10 * (a - math.pi * 0.06) / 0.88)       # 굵은 주름 다섯 줄
                x = math.cos(a) * s * 1.0
                y = math.sin(a) * s * 0.8
                dome = 0.42 * math.sin(math.pi * 0.5 * s) * math.sin(a) ** 0.6
                z = (dome + 0.13 * s ** 1.3 * fold) * sign                # 가장자리로 갈수록 주름이 깊어져 입이 물결친다
                row.append(bm.verts.new((x, y, z)))
            grid.append(row)
        for i in range(na):
            for j in range(ns):
                f = bm.faces.new((grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1]))
                if sign < 0:
                    f.normal_flip()
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.045)
        return obj_from_bm(bm, 'Valve', gold)

    top, bot = valve(1), valve(-1)
    top.rotation_euler = (math.radians(14), 0, 0)     # 경첩(x축)을 축으로 위 껍데기가 살짝 벌어진다
    mantle = sphere(0.5, mat('#1f8f9a', 0.3, coat=0.8, film=250), loc=(0, 0.42, 0.04), scale=(1.6, 0.62, 0.1),
                    rot=(math.radians(7), 0, 0))
    o = join([top, bot, mantle], 'GoldGege')
    o.rotation_euler = (math.radians(55), 0, math.radians(25))    # 경첩이 뒤, 불룩한 위 껍데기와 물결 입이 보이게
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
    net = mat('#7fa08c', 0.55)  # 밝은 물 바랜 초록 (짙으면 밤 결과 화면에서 배경에 묻혔다)
    objs = []
    L, R = 0.45, 0.13
    for i in range(7):
        objs.append(torus(R, 0.0055, net, loc=(0, -L / 2 + L * i / 6, R), rot=(math.pi / 2, 0, 0), seg=28, mseg=6))
    for k in range(12):
        a = 2 * math.pi * k / 12
        objs.append(cyl(0.0045, 0.0045, L, net, loc=(math.cos(a) * R, 0, R + math.sin(a) * R), rot=(math.pi / 2, 0, 0), seg=6))
    # 입구 깔때기: 그물로 된 원뿔 (예전 반투명 막은 FBX에서 불투명해져 회색 원뿔 덩어리로 보였다)
    for k in range(10):
        a = 2 * math.pi * k / 10
        p0 = V((math.cos(a) * R, -L / 2, R + math.sin(a) * R))
        p1 = V((math.cos(a) * 0.035, -L / 2 + 0.14, R + math.sin(a) * 0.035))
        objs.append(tube([p0, p1], 0.0035, net, sides=6))
    for f in (0.35, 0.7):
        objs.append(torus(R + (0.035 - R) * f, 0.0035, net, loc=(0, -L / 2 + 0.14 * f, R), rot=(math.pi / 2, 0, 0), seg=24, mseg=6))
    objs.append(box((0.04, 0.1, 0.02), mat('#e8c020', 0.5), loc=(0, 0.1, 2 * R)))  # 부표 끈 매듭
    o = join(objs, 'Trap')
    o.rotation_euler = (0, 0, math.radians(30))
    return [o], 'item', 0.45


def _shoe_round(t, front=0.2, back=0.12):
    """앞코·뒤꿈치 끝을 둥글게 오므리는 비율 (끝 0 → 안쪽 1, 사분원). 폭과 높이에 함께 곱해 끝이 타원체처럼 닫힌다
    (예전에는 폭만 줄고 높이는 그대로라 앞코가 세운 판처럼 뭉툭하게 잘려 보였다)."""
    r = 1.0
    if t < front:
        u = 1 - t / front
        r = math.sqrt(max(0.0, 1 - u * u))
    if t > 1 - back:
        u = 1 - (1 - t) / back
        r = min(r, math.sqrt(max(0.0, 1 - u * u)))
    return r


def _shoe_hw(t):
    """운동화 발바닥 윤곽의 반폭 (t: 0 앞코 ~ 1 뒤꿈치). 앞볼이 넓고 장심(아치)이 잘록하고, 앞코·뒤꿈치는 위에서 보면 둥글다."""
    base = 0.043 + 0.005 * math.exp(-((t - 0.26) / 0.14) ** 2) - 0.007 * math.exp(-((t - 0.6) / 0.12) ** 2) - 0.005 * fish_smooth(0.7, 1.0, t)
    return base * _shoe_round(t)


def _shoe_zb(t):
    """밑창 바닥 높이: 앞코가 살짝 들리고(토 스프링) 뒤꿈치 끝도 조금 둥글게."""
    return 0.011 * max(0.0, (0.3 - t) / 0.3) ** 2.2 + 0.003 * max(0.0, (t - 0.88) / 0.12) ** 2


SOLE_H = 0.024


def _shoe_H(t):
    """갑피 높이 (밑창 위에서): 앞코(토박스)는 낮고 납작, 끈 자리를 따라 발등이 오르고, 뒤꿈치 컵이 가장 높다."""
    return 0.024 + 0.048 * fish_smooth(0.14, 0.58, t) + 0.006 * fish_smooth(0.8, 1.0, t)


def _shoe_dip(t, xr):
    """발목 구멍: 갑피 윗면을 아래로 파낸 깊이 (xr = x / 반폭)."""
    w = fish_smooth(0.6, 0.68, t) * fish_smooth(0.97, 0.9, t)
    return 0.05 * w * max(0.0, 1 - (xr / 0.74) ** 2) ** 0.55


def fish_smooth(e0, e1, x):
    k = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return k * k * (3 - 2 * k)


def _loft(rings_fn, n, sides, mats, face_mat, end=0.004):
    """t를 따라 닫힌 단면(sides개 점)을 이어 붙인 몸. 양 끝은 부채꼴로 막는다. face_mat(t, a) → 머티리얼 번호."""
    bm = bmesh.new()
    ts = [end + (1 - 2 * end) * (0.5 - 0.5 * math.cos(math.pi * i / (n - 1))) for i in range(n)]
    rings = [[bm.verts.new(p) for p in rings_fn(t)] for t in ts]
    for i in range(n - 1):
        for k in range(sides):
            k2 = (k + 1) % sides
            f = bm.faces.new((rings[i][k], rings[i][k2], rings[i + 1][k2], rings[i + 1][k]))
            f.material_index = face_mat((ts[i] + ts[i + 1]) / 2, 2 * math.pi * (k + 0.5) / sides)
    for ring, flip in ((rings[0], True), (rings[-1], False)):
        c = bm.verts.new(sum((v.co for v in ring), V()) / sides)
        for k in range(sides):
            vs = (ring[k], ring[(k + 1) % sides], c)
            f = bm.faces.new(vs[::-1] if flip else vs)
            f.material_index = face_mat(0.0 if flip else 1.0, 2 * math.pi * (k + 0.5) / sides)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new('Loft')
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    ob = link(bpy.data.objects.new('Loft', me))
    for m in mats:
        ob.data.materials.append(m)
    return ob


def model_shoe(iid):
    """
    강에서 건진 낡은 운동화 한 짝 (캔버스 운동화). 예전 모델은 밑창이 안 보이고 끈이 갑피에 묻혀 파란 민달팽이 같았고,
    그다음엔 앞코가 폭만 줄고 높이는 그대로라 세운 판처럼 뭉툭하게 잘리고 들려서 주걱 같았으며 앞코 고무가 옆에 붙은 회색 조각이었다.
    지금: 위에서 보면 둥근 앞코, 낮게 내려와 밑창에 닿는 토박스(끝을 폭·높이 함께 타원체로 오므림), 살짝 들린 토 스프링,
    밑창보다 조금 앞에서 끝나는 갑피를 감싸는 흰 고무 앞코 캡(밑창 위 같은 높이의 띠 + 앞코 윗면 방패꼴), 둘레 전체 흰 밑창(남색 줄·바닥 고무),
    파인 발목 구멍과 짙은 안감, 혀, X자 끈·구멍·매듭, 옆 줄무늬 두 줄, 뒤꿈치 보강(힐 카운터)·고리, 이끼 몇 점.
    """
    sole_w, sole_line, sole_gum = mat('#e4ddcb', 0.7), mat('#2b3a5a', 0.6), mat('#5a5048', 0.9)
    upper, stripe, toe_rub = mat('#46658a', 0.85), mat('#e6e2d6', 0.7), mat('#e4ddcb', 0.55)
    counter = mat('#34507a', 0.8)
    lining, lace, eyelet = mat('#26242a', 0.95), mat('#ece8dc', 0.65), mat('#8a8c90', 0.35, metal=0.8)
    tongue_m, moss = mat('#52709a', 0.9), mat('#6b7a3a', 0.95)
    sides = 64
    L0, L = -0.13, 0.26
    SOLE_OUT = 0.005          # 밑창이 갑피보다 앞뒤로 더 나온 길이

    def y_of(t):
        return L0 + L * t

    def se(c, p):
        return math.copysign(abs(c) ** (2 / p), c)

    # ---- 밑창: 납작한 둥근 사각 단면. 끝에서 폭·두께가 함께 오므라들어 둥근 코가 된다 ----
    def sole_ring(t):
        r = _shoe_round(t)
        hw = _shoe_hw(t) + 0.0035 * r ** 0.5
        # 앞뒤 끝: 바닥은 위로 둥글게 말려 오르고 윗면은 거의 그대로 → 코가 밑창 윗면 쪽에서 둥글게 닫힌다
        k = 1 - r ** 0.7
        zb = _shoe_zb(t)
        z0, z1 = zb + SOLE_H * 0.65 * k, zb + SOLE_H * (1 - 0.3 * k)
        zm, hh = (z0 + z1) / 2, (z1 - z0) / 2
        y = L0 - SOLE_OUT + (L + 2 * SOLE_OUT) * t
        out = []
        for k in range(sides):
            a = 2 * math.pi * k / sides
            out.append((hw * se(math.cos(a), 3.2), y, zm + hh * se(math.sin(a), 3.2)))
        return out

    def sole_mat(t, a):
        zr = 0.5 * (1 + se(math.sin(a), 3.2))     # 0 바닥 ~ 1 윗면
        if zr < 0.08:
            return 2                               # 바닥 고무
        return 1 if 0.5 < zr < 0.6 else 0          # 가운데 남색 줄
    objs = [_loft(sole_ring, 72, sides, [sole_w, sole_line, sole_gum], sole_mat, end=0.0004)]

    # ---- 갑피: 아래는 밑창에 묻히고 위는 둥근 돔, 발목 쪽은 파여서 구멍. 앞코는 밑창 윗면 높이 쪽으로 오므린다 ----
    def sole_top(t):
        return _shoe_zb(t) + SOLE_H

    def upper_z(t, xr, top=True):
        zt = sole_top(t) - 0.003
        if top:
            z = zt + _shoe_H(t) * (1 - abs(xr) ** 2.6) ** (1 / 2.6) - _shoe_dip(t, xr)
        else:
            z = zt - 0.005
        zc = _shoe_zb(t) + SOLE_H * 0.65          # 앞코 끝이 모이는 높이 (밑창 속)
        return zc + (z - zc) * _shoe_round(t) ** 0.9

    def upper_pt(t, a):
        hw = _shoe_hw(t) - 0.001 * _shoe_round(t)
        xr = se(math.cos(a), 2.6)
        s = math.sin(a)
        z = upper_z(t, xr) if s >= 0 else upper_z(t, xr, False)
        if s >= 0:
            # 옆면은 위로 갈수록 살짝 안으로 (신발 볼이 위에서 좁아진다)
            xr *= 1 - 0.12 * s ** 2
        return V((hw * xr, y_of(t), z))

    def upper_ring(t):
        return [tuple(upper_pt(t, 2 * math.pi * k / sides)) for k in range(sides)]

    def upper_mat(t, a):
        xr = se(math.cos(a), 2.6)
        if math.sin(a) >= 0 and _shoe_dip(t, xr) > 0.012:
            return 1                                # 발목 구멍 안감
        return 0
    objs.append(_loft(upper_ring, 96, sides, [upper, lining], upper_mat, end=0.0004))

    # 앞코 고무 캡 · 뒤꿈치 보강: 갑피 위에 덧댄 얇은 껍질. 밑창 윗면에서 잰 높이 hmax(t)까지 덮는다
    # (면마다 색을 칠하면 경계가 계단처럼 보였다). 높이가 갑피 꼭대기보다 높으면 좌우가 위에서 만난다
    def overlay(t0, t1, hmax, m, ctr_t, n=40, na=14):
        def inside(t, a):   # 덮을 자리: 높이 아래이고 발목 구멍(파인 곳)이 아님
            return upper_pt(t, a).z - sole_top(t) < hmax(t) and _shoe_dip(t, se(math.cos(a), 2.6)) < 0.0005

        def a_edge(t):
            lo, hi = 0.0, math.pi / 2
            for k in range(1, 41):
                a = math.pi / 2 * k / 40
                if not inside(t, a):
                    hi = a
                    break
                lo = a
            else:
                return hi
            for _ in range(20):
                mid = (lo + hi) / 2
                if inside(t, mid):
                    lo = mid
                else:
                    hi = mid
            return lo
        for side in (1, -1):
            bm = bmesh.new()
            rows = []
            for i in range(n):
                t = t0 + (t1 - t0) * (0.5 - 0.5 * math.cos(math.pi * i / (n - 1)))
                ae = a_edge(t)
                ctr = V((0, y_of(ctr_t), sole_top(t) - 0.006))
                row = []
                for j in range(na):
                    a = -0.35 + (ae + 0.35) * j / (na - 1)
                    if side < 0:
                        a = math.pi - a
                    q = upper_pt(t, a)
                    row.append(bm.verts.new(q + (q - ctr).normalized() * 0.0013))
                rows.append(row)
            for i in range(n - 1):
                for j in range(na - 1):
                    f = (rows[i][j], rows[i + 1][j], rows[i + 1][j + 1], rows[i][j + 1])
                    bm.faces.new(f if side > 0 else f[::-1])
            bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
            objs.append(obj_from_bm(bm, 'Overlay', m))
    # 앞코 캡: 밑창 위 1.3cm 띠로 앞을 고르게 감싸고 뒤로 가며 낮아져 끝난다. 앞코 윗면은 통째로 덮인다
    overlay(0.0004, 0.27, lambda t: 0.013 * fish_smooth(0.27, 0.17, t) + 0.0004, toe_rub, 0.3)
    # 뒤꿈치 보강: 위 가장자리가 뒤로 갈수록 올라간다
    overlay(0.78, 0.9996, lambda t: 0.006 + 0.026 * fish_smooth(0.78, 0.97, t), counter, 0.7)

    def surf(t, xr, lift=0.0):
        return V((_shoe_hw(t) * xr, y_of(t), upper_z(t, xr) + lift))

    # 옆면 흰 줄무늬 두 줄: 표면을 따라가는 얇은 띠 (면 색칠은 계단처럼 보였다). 아래에서 위로 갈수록 뒤로 눕는다
    for side in (1, -1):
        for t0 in (0.4, 0.5):
            bm = bmesh.new()
            rows = []
            for j in range(13):
                u = j / 12
                a = math.asin(0.12 + 0.7 * u)
                if side < 0:
                    a = math.pi - a
                tc = t0 + 0.15 * u
                row = []
                for dt in (-0.017, 0.017):
                    q = upper_pt(tc + dt, a)
                    ctr = V((0, q.y, _shoe_zb(tc) + SOLE_H + 0.02))
                    row.append(bm.verts.new(q + (q - ctr).normalized() * 0.0012))
                rows.append(row)
            for j in range(12):
                f = (rows[j][0], rows[j][1], rows[j + 1][1], rows[j + 1][0])
                bm.faces.new(f if side > 0 else f[::-1])
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
            objs.append(obj_from_bm(bm, 'Stripe', stripe))
    # 혀: 발목 구멍 앞에서 끝만 살짝 나온다 (크게 세우면 뚜껑처럼 보였다). 둥근 판을 뒤로 눕힌다
    p = surf(0.6, 0.0, 0.0)
    objs.append(sphere(0.022, tongue_m, loc=(0, p.y + 0.006, p.z + 0.006), scale=(0.85, 0.28, 1.0), rot=(math.radians(-35), 0, 0), seg=20, rings=12))
    # 신발끈 구멍 + X자 끈 (발등 경사를 따라)
    for i in range(5):
        t0 = 0.31 + i * 0.06
        pl, pr = surf(t0, -0.5, 0.001), surf(t0, 0.5, 0.001)
        for q in (pl, pr):
            objs.append(sphere(0.0042, eyelet, loc=tuple(q), scale=(1, 1, 0.45), seg=12, rings=8))
        nl, nr = surf(t0 + 0.06, -0.5, 0.001), surf(t0 + 0.06, 0.5, 0.001)
        for a0, b0 in ((pl, nr), (pr, nl)):
            mid = (a0 + b0) / 2 + V((0, 0, 0.0035))
            objs.append(tube([a0 + V((0, 0, 0.002)), mid, b0 + V((0, 0, 0.002))], 0.0024, lace, sides=8))
    # 매듭과 늘어진 끈 두 가닥
    k = surf(0.6, 0.0, 0.005)
    objs.append(sphere(0.007, lace, loc=tuple(k), scale=(1.3, 1, 0.7), seg=12, rings=8))
    for sgn in (-1, 1):
        pts = [k + V((sgn * 0.004, 0, 0)), k + V((sgn * 0.02, -0.006, -0.004)), k + V((sgn * 0.036, -0.01, -0.02)),
               k + V((sgn * 0.046, -0.004, -0.04))]
        objs.append(tube(pts, 0.0026, lace, sides=8))
    # 뒤꿈치 고리
    h = surf(0.985, 0.0, 0.0)
    objs.append(box((0.012, 0.004, 0.014), stripe, loc=(0, h.y + 0.003, h.z - 0.006), bevel=0.0015))
    # 이끼·흙 몇 점 (밑창 가장자리)
    for i, (t0, side) in enumerate(((0.2, 1), (0.48, -1), (0.75, 1), (0.9, -1))):
        hw = _shoe_hw(t0) + 0.003
        objs.append(lumpy(0.007, moss, loc=(side * (hw + 0.0005), y_of(t0), _shoe_zb(t0) + SOLE_H * 0.85), scale=(0.35, 1.8, 0.45), amp=0.2, seed=i))
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
    'rod_basic': model_rod, 'rod_carbon': model_rod, 'rod_bamboo': model_rod, 'rod_heavy': model_rod, 'rod_gege': model_rod, 'rod_fly': model_rod, 'rod_gangbaram': model_rod, 'rod_goldgege': model_rod, 'rod_pungrim': model_rod, 'rod_gt': model_rod, 'rod_nile': model_rod,
    'reel_basic': model_reel, 'reel_power': model_reel, 'reel_hispeed': model_reel, 'reel_napal': model_reel, 'reel_victory5': model_reel, 'reel_pungrim': model_reel, 'reel_jigging': model_reel, 'line_pe8': model_line, 'boat_blackspear': model_boat, 'coin_bigfish': model_coin,
    'line_nylon': model_line, 'line_carbon': model_line, 'line_pe': model_line,
    'boat_rubber': model_boat, 'boat_speed': model_boat, 'boat_sail': model_boat, 'boat_cruiser': model_boat,
    'bait_worm': model_worm, 'bait_paste': model_paste, 'bait_corn': model_corn, 'bait_krill': model_krill,
    'bait_seaworm': model_seaworm, 'bait_squid': model_squid, 'bait_shrimp': model_shrimp, 'bait_minnow': model_minnow,
    'bait_lure': model_lure, 'bait_egi': model_egi, 'bait_fly': model_fly,
    'bait_cut1': model_cutbait, 'bait_cut2': model_cutbait, 'bait_cut3': model_cutbait, 'bait_cut4': model_cutbait, 'bait_cut5': model_cutbait,
    'food_kimbap': model_kimbap, 'food_ramen': model_ramen, 'food_fishcake': model_fishcake,
    'food_coffee': model_coffee, 'food_dosirak': model_dosirak,
    'acc_cottongloves': model_gloves,
    'mat_scale': model_scale, 'mat_starsand': model_starsand, 'mat_conch': model_conch,
    'mat_napal': model_napal, 'mat_goldgege': model_goldgege,
}
# 마을 교환 장비 (GearDatabase.cs Trade 줄)
ITEMS.update({iid: {'Rod': model_rod, 'Reel': model_reel, 'Line': model_line}[slot] for iid, (slot, *_r) in TRADE.items()})

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
