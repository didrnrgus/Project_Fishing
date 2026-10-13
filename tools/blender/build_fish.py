"""
어종별 물고기 모델 + 애니메이션 5종 + 텍스처를 만든다. docs/09-art-plan.md 3장.

  python -I tools/blender/build_fish.py                    # 32종 모두 FBX·텍스처·아이콘
  python -I tools/blender/build_fish.py --only carp        # 하나만 (쉼표로 여러 개)
  python -I tools/blender/build_fish.py --preview --only carp --out Screenshots/fish   # 미리보기 렌더 (옆·위·비스듬히)
  python -I tools/blender/build_fish.py --sheet docs/images/32_fish_models.png         # 전체 어종 한 장 (도감용 확인)
  python -I tools/blender/build_fish.py --no-icons         # 아이콘 렌더 건너뛰기

결과 (Unity):
  Assets/Resources/Fish/Species/<id>.fbx    몸(Body) + 지느러미(Fins) + LOD1, 뼈, 클립 Swim_Slow/Swim_Fast/Thrash/Flop/Idle_Hover
  Assets/Resources/Fish/Textures/<id>.png, <id>_n.png(노멀), <id>_m.png(R 금속성, A 매끈함)
  Assets/Resources/Icons/Fish/<id>.png      도감 목록 아이콘 (256×128, 투명 배경, 옆모습)

모델 좌표 (Blender): 길이 1m, 주둥이 y = -0.5, 꼬리 끝 y = +0.5, 위 +Z.
FBX는 캐릭터와 같은 축(-Y 앞, Z 위)으로 내보내서 Unity에서는 머리가 +Z를 본다.
"""
import math
import os
import sys

ARGS = sys.argv[1:]
sys.argv = [sys.argv[0]]
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT_FBX = os.path.join(ROOT, 'Assets', 'Resources', 'Fish', 'Species')
OUT_TEX = os.path.join(ROOT, 'Assets', 'Resources', 'Fish', 'Textures')
OUT_ICON = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Fish')

import bpy  # noqa: E402
import bmesh  # noqa: E402
import mathutils  # noqa: E402
import numpy as np  # noqa: E402

import cephalopod as CP  # noqa: E402
import fish_species as FS  # noqa: E402
import fish_tex  # noqa: E402

NR, NA = 44, 28   # 몸 고리 수, 고리 둘레 버텍스 수
FPS = 30


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


# =====================================================================
#  몸 모양 (단면 로프트)
# =====================================================================

class Body:
    """어종 수치로 몸통 단면을 계산한다. s = 몸통 구간 0(주둥이) ~ 1(꼬리자루)."""

    def __init__(self, d):
        self.d = d
        self.H = d['depth']
        self.W = d['width']
        self.be = d['body_end']
        self.y0 = -0.5

    def y(self, s):
        return self.y0 + s * self.be

    def depth(self, s):
        d, H = self.d, self.H
        tm = d['tmax']
        en = 0.2 + d['snout'] * 0.7
        if s < tm:
            return H * math.sin(math.pi / 2 * s / tm) ** en
        t = (s - tm) / (1 - tm)
        return H * (d['ped'] + (1 - d['ped']) * math.cos(math.pi / 2 * t) ** 1.5)

    def width(self, s):
        d = self.d
        tm = d['tmax'] * 0.8
        en = (0.2 + d['snout'] * 0.7) * 0.85
        if s < tm:
            w = self.W * math.sin(math.pi / 2 * s / tm) ** en
        else:
            t = (s - tm) / (1 - tm)
            w = self.W * (0.32 + 0.68 * math.cos(math.pi / 2 * t) ** 1.3)
        hw = d.get('head_w', 1.0)
        if hw != 1.0 and s < 0.35:
            w *= 1 + (hw - 1) * (1 - s / 0.35) * min(1, s / 0.05)
        return w

    def zc(self, s):
        m = self.d.get('mouth', 0.0)
        return m * 0.32 * self.H * max(0.0, 1 - s / 0.32) ** 2

    def arch(self, s):
        a = self.d['arch']
        t = min(1, max(0, (s - 0.6) / 0.4))
        return a + (0.5 - a) * t * t * (3 - 2 * t)

    def top(self, s):
        return self.depth(s) * self.arch(s)

    def bot(self, s):
        return self.depth(s) * (1 - self.arch(s))

    def ring_point(self, s, a):
        """a: 0 = 배 가운데, π = 등 가운데, (0, π) 오른쪽(+x), (π, 2π) 왼쪽."""
        xs = self.d['xsec']
        cz, cx = -math.cos(a), math.sin(a)
        x = self.width(s) / 2 * math.copysign(abs(cx) ** (2 / xs), cx)
        h = self.top(s) if cz > 0 else self.bot(s)
        z = self.zc(s) + h * math.copysign(abs(cz) ** (2 / xs), cz)
        return (x, self.y(s), z)

    def surface_x(self, s, zrel):
        """옆면 x (zrel: -1 배 ~ +1 등)."""
        xs = self.d['xsec']
        return self.width(s) / 2 * max(0.0, 1 - abs(zrel) ** xs) ** (1 / xs)

    def z_at(self, s, zrel):
        return self.zc(s) + (self.top(s) if zrel > 0 else self.bot(s)) * zrel


def body_uv(d, s, a):
    u = s * 0.75
    if d.get('flat'):
        v = a / (2 * math.pi)
    else:
        v = a / math.pi if a <= math.pi else (2 * math.pi - a) / math.pi
    return (u, v)


def build_body_mesh(d, B):
    verts, faces, uvs = [], [], []
    ss = [0.006 + (1 - 0.006) * (0.5 - 0.5 * math.cos(math.pi * i / (NR - 1))) for i in range(NR)]
    for s in ss:
        for k in range(NA):
            a = 2 * math.pi * k / NA
            verts.append(B.ring_point(s, a))
    # 주둥이 끝 점, 꼬리자루 끝 점
    nose = len(verts)
    verts.append((0.0, B.y(0.0), B.zc(0.0)))
    tail = len(verts)
    verts.append((0.0, B.y(1.0) + 0.002, B.zc(1.0)))
    loops_uv = []
    for i in range(NR - 1):
        for k in range(NA):
            k2 = (k + 1) % NA
            f = (i * NA + k, i * NA + k2, (i + 1) * NA + k2, (i + 1) * NA + k)
            faces.append(f)
            a1, a2 = 2 * math.pi * k / NA, 2 * math.pi * (k + 1) / NA
            loops_uv.append([body_uv(d, ss[i], a1), body_uv(d, ss[i], a2), body_uv(d, ss[i + 1], a2), body_uv(d, ss[i + 1], a1)])
    for k in range(NA):
        k2 = (k + 1) % NA
        a1, a2 = 2 * math.pi * k / NA, 2 * math.pi * (k + 1) / NA
        faces.append((nose, k2, k))
        loops_uv.append([(0.0, 0.5), body_uv(d, ss[0], a2), body_uv(d, ss[0], a1)])
        base = (NR - 1) * NA
        faces.append((tail, base + k, base + k2))
        loops_uv.append([(0.75, 0.5), body_uv(d, 1.0, a1), body_uv(d, 1.0, a2)])
    return verts, faces, loops_uv


# =====================================================================
#  지느러미
# =====================================================================

def interp(prof, x):
    if not prof:
        return 1.0
    if x <= prof[0][0]:
        return prof[0][1]
    for (x0, y0), (x1, y1) in zip(prof, prof[1:]):
        if x <= x1:
            t = (x - x0) / (x1 - x0 + 1e-9)
            t = t * t * (3 - 2 * t)
            return y0 + (y1 - y0) * t
    return prof[-1][1]


def fin_grid(base_fn, tip_fn, nu, nv, group, curl=None):
    """base_fn(u) → 밑동 점, tip_fn(u) → 끝 점. 격자 버텍스·면·UV."""
    x0 = 0.75 if group == 'up' else 0.875
    verts, faces, uvs = [], [], []
    for j in range(nv + 1):
        fv = j / nv
        for i in range(nu + 1):
            fu = i / nu
            b = mathutils.Vector(base_fn(fu))
            t = mathutils.Vector(tip_fn(fu))
            p = b.lerp(t, fv)
            if curl:
                p += mathutils.Vector(curl(fu, fv))
            verts.append(tuple(p))
    for j in range(nv):
        for i in range(nu):
            a = j * (nu + 1) + i
            faces.append((a, a + 1, a + nu + 2, a + nu + 1))
            uvs.append([(x0 + 0.125 * i / nu, 0.5 + 0.5 * j / nv), (x0 + 0.125 * (i + 1) / nu, 0.5 + 0.5 * j / nv),
                        (x0 + 0.125 * (i + 1) / nu, 0.5 + 0.5 * (j + 1) / nv), (x0 + 0.125 * i / nu, 0.5 + 0.5 * (j + 1) / nv)])
    return verts, faces, uvs


def make_fins(d, B):
    out = []  # (name, verts, faces, uvs, kind)
    H = B.H
    for idx, f in enumerate(d['fins']):
        kind = f['kind']
        grp = f['group']
        if kind in ('dorsal', 'anal', 'adipose'):
            s0, s1 = f['s']
            s1 = min(s1, 1.0)
            span = s1 - s0
            up = kind != 'anal'
            sweep = math.radians(38 if not f.get('spine') else 15)
            h = f['h'] * H
            nu = max(6, int(span * 70))
            spines = max(3, int(span * 42))
            def base(u, s0=s0, span=span, up=up):
                s = s0 + span * u
                z = B.zc(s) + (B.top(s) - 0.0025 if up else -(B.bot(s) - 0.0025))
                return (0.0, B.y(s), z)
            def tip(u, s0=s0, span=span, up=up, h=h, f=f, spines=spines, sweep=sweep):
                s = s0 + span * u
                L = h * interp(f.get('prof'), u)
                if f.get('spiny'):
                    L *= 1 - 0.28 * math.sin(math.pi * u * spines) ** 2
                if s > 0.97 and d['type'] == 'G':
                    L *= max(0.0, (1.0 - s) / 0.03) * 0.5 + 0.5
                bz = B.zc(s) + (B.top(s) if up else -B.bot(s))
                return (0.0, B.y(s) + L * math.sin(sweep), bz + (L if up else -L) * math.cos(sweep))
            v, fc, uv = fin_grid(base, tip, nu, 4, grp)
            out.append((f'{kind}{idx}', v, fc, uv, kind))
        elif kind == 'caudal':
            yb = B.y(1.0) - 0.004
            zb = B.zc(1.0)
            ph = B.depth(1.0) * 0.48
            C = 0.5 - yb
            hc = f['h'] / 2
            shape = f.get('shape', 'fork')
            def cl(vc, shape=shape, C=C):
                a = abs(vc)
                return C * {'fork': 0.62 + 0.38 * a ** 1.2, 'deepfork': 0.38 + 0.62 * a ** 1.05, 'emarg': 0.86 + 0.14 * a,
                            'truncate': 0.95 + 0.05 * a, 'round': 1 - 0.32 * a * a, 'point': max(0.05, 1 - a)}.get(shape, 1.0)
            def ctz(vc, shape=shape, hc=hc):
                k = {'round': 0.85, 'point': 0.5, 'truncate': 0.95}.get(shape, 1.0)
                return vc * hc * k
            def base(u, yb=yb, zb=zb, ph=ph):
                vc = u * 2 - 1
                return (0.0, yb, zb + vc * ph)
            def tip(u, yb=yb, zb=zb):
                vc = u * 2 - 1
                return (0.0, yb + cl(vc), zb + ctz(vc))
            v, fc, uv = fin_grid(base, tip, 18, 5, grp)
            out.append(('caudal', v, fc, uv, kind))
        elif kind in ('pectoral', 'pelvic'):
            s = f['s'][0]
            h = f['h']
            for side in (1, -1):
                if kind == 'pectoral':
                    zr0, zr1 = -0.4, -0.2
                    if d.get('flat'):
                        zr0, zr1 = -0.05, 0.08
                    def base(u, s=s, side=side, zr0=zr0, zr1=zr1):
                        zr = zr0 + (zr1 - zr0) * u
                        return (side * (B.surface_x(s, zr) - 0.002), B.y(s), B.z_at(s, zr))
                    def tip(u, s=s, side=side, zr0=zr0, zr1=zr1, h=h, f=f):
                        # 부채꼴: 위쪽 살은 위로, 아래쪽 살은 아래로 벌어진다. 끝은 둥글게
                        if f.get('long'):
                            L = h * (0.3 + 0.7 * u ** 1.6)
                        else:
                            L = h * (0.55 + 0.45 * math.sin(math.pi * (0.15 + 0.7 * u)))
                        fan = (u - 0.5) * 0.9
                        dvec = mathutils.Vector((0.42 * side, 1.0, -0.3 + fan)).normalized()
                        b = mathutils.Vector(base(u))
                        return tuple(b + dvec * L)
                else:
                    def base(u, s=s, side=side):
                        ss = s + 0.03 * (u - 0.5)
                        return (side * B.width(ss) * (0.08 + 0.12 * u), B.y(ss), B.zc(ss) - B.bot(ss) + 0.002)
                    def tip(u, s=s, side=side, h=h):
                        L = h * (0.6 + 0.4 * math.sin(math.pi * u))
                        dvec = mathutils.Vector((0.35 * side, 1.0, -0.55)).normalized()
                        b = mathutils.Vector(base(u))
                        return tuple(b + dvec * L)
                v, fc, uv = fin_grid(base, tip, 8, 4, grp)
                out.append((f'{kind}_{"L" if side < 0 else "R"}', v, fc, uv, kind + ('_L' if side < 0 else '_R')))
    return out


# =====================================================================
#  눈, 수염, 부리
# =====================================================================

def make_eye(d, B, side):
    e = d['eye']
    r = e['r']
    s, zr = e['s'], e['z']
    flat = d.get('flat')
    if flat:
        # 납작한 고기: 두 눈이 모두 눈 있는 쪽에 (위쪽 눈은 등 가장자리 가까이)
        eside = -1 if flat == 'left' else 1
        zr = 0.35 if side > 0 else -0.05
        s = e['s'] if side > 0 else e['s'] + 0.025
        side = eside
    x = B.surface_x(s, zr) - r * 0.35
    c = mathutils.Vector((side * x, B.y(s), B.z_at(s, zr)))
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=9, radius=r)
    uvl = bm.loops.layers.uv.new('UV')
    for f in bm.faces:
        for l in f.loops:
            p = l.vert.co
            # 옆에서 투영: 바깥쪽 면이 눈 텍스처(홍채·동공)
            l[uvl].uv = (0.75 + 0.125 * (0.5 + p.y / (2 * r) * 0.98), 0.25 + 0.25 * (0.5 + p.z / (2 * r) * 0.98))
    for vv in bm.verts:
        vv.co += c
    return bm


def make_tube(points, r0, r1, uv_rect, sides=6):
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UV')
    rings = []
    n = len(points)
    for i, p in enumerate(points):
        p = mathutils.Vector(p)
        t = (mathutils.Vector(points[min(i + 1, n - 1)]) - mathutils.Vector(points[max(i - 1, 0)])).normalized()
        a = t.orthogonal().normalized()
        b = t.cross(a)
        r = r0 + (r1 - r0) * i / (n - 1)
        ring = [bm.verts.new(p + (a * math.cos(2 * math.pi * k / sides) + b * math.sin(2 * math.pi * k / sides)) * r) for k in range(sides)]
        rings.append(ring)
    u0, v0, u1, v1 = uv_rect
    for i in range(n - 1):
        for k in range(sides):
            f = bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k]))
            for l in f.loops:
                l[uvl].uv = ((u0 + u1) / 2, (v0 + v1) / 2)
    tipv = bm.verts.new(mathutils.Vector(points[-1]))
    for k in range(sides):
        f = bm.faces.new((rings[-1][k], rings[-1][(k + 1) % sides], tipv))
        for l in f.loops:
            l[uvl].uv = ((u0 + u1) / 2, (v0 + v1) / 2)
    return bm


def make_barbels(d, B):
    out = []
    for (s, zr, L, th, kind) in d['barbels']:
        for side in (1, -1):
            x = B.surface_x(s, zr) * 0.9
            start = mathutils.Vector((side * x, B.y(s), B.z_at(s, zr)))
            dirs = {'down': (0.35, -0.2, -1.0), 'side': (1.0, -0.35, -0.25), 'up': (0.8, -0.3, 0.45)}[kind]
            d0 = mathutils.Vector((dirs[0] * side, dirs[1], dirs[2])).normalized()
            pts = []
            for i in range(9):
                t = i / 8
                p = start + d0 * L * t + mathutils.Vector((0, 0.55, -0.15)) * L * t * t
                pts.append(tuple(p))
            out.append(make_tube(pts, th, th * 0.3, (0.875, 0.25, 0.9375, 0.375)))
    if d.get('beak'):
        L = d['beak']
        z0 = B.zc(0.02) - B.bot(0.02) * 0.4
        pts = [(0.0, B.y(0.03) - L * t, z0 - 0.004 * t) for t in np.linspace(0, 1, 6)]
        out.append(make_tube(pts, B.depth(0.03) * 0.18, 0.0015, (0.9375, 0.25, 1.0, 0.375), sides=5))
    return out


# =====================================================================
#  bpy 오브젝트
# =====================================================================

def mesh_from(name, verts, faces, loops_uv):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    uvl = me.uv_layers.new(name='UV')
    li = 0
    for poly, fuv in zip(me.polygons, loops_uv):
        for k, loop_index in enumerate(poly.loop_indices):
            uvl.data[loop_index].uv = fuv[k]
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def bm_to_obj(name, bms):
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UV')
    for src in bms:
        me = bpy.data.meshes.new('tmp')
        src.to_mesh(me)
        bm.from_mesh(me)
        bpy.data.meshes.remove(me)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    o.data.name = name
    return o


def solidify(ob, thick):
    m = ob.modifiers.new('Solid', 'SOLIDIFY')
    m.thickness = thick
    m.offset = 0.0
    m.use_rim = True
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=m.name)


# =====================================================================
#  뼈와 가중치
# =====================================================================

def build_rig(d, B, body, fins):
    nb = d['bones']
    be = B.be
    yR = -0.5 + 0.3 * be
    yE = B.y(1.0)
    arm_data = bpy.data.armatures.new('Rig')
    arm = bpy.data.objects.new('Rig', arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    root = eb.new('Root')
    root.head, root.tail = (0, yR, 0), (0, yR + 0.04, 0)
    head = eb.new('Head')
    head.head, head.tail = (0, yR, 0), (0, -0.5, 0)
    head.parent = root
    segs = [('Head', -0.5, yR)]
    prev = root
    n_sp = nb - 1
    for i in range(n_sp):
        a = yR + (yE - yR) * i / n_sp
        b = yR + (yE - yR) * (i + 1) / n_sp
        sp = eb.new(f'Spine{i + 1}')
        sp.head, sp.tail = (0, a, 0), (0, b, 0)
        sp.parent = prev
        sp.use_connect = i > 0
        prev = sp
        segs.append((sp.name, a, b))
    tail = eb.new('Tail')
    tail.head, tail.tail = (0, yE, 0), (0, 0.5, 0)
    tail.parent = prev
    tail.use_connect = True
    segs.append(('Tail', yE, 0.5))
    pecs = {}
    for f in d['fins']:
        if f['kind'] == 'pectoral':
            s = f['s'][0]
            for side, nm in ((1, 'Pec_R'), (-1, 'Pec_L')):
                b = eb.new(nm)
                x = B.surface_x(s, -0.3)
                b.head = (side * x, B.y(s), B.z_at(s, -0.3))
                b.tail = (side * (x + 0.03), B.y(s) + 0.06, B.z_at(s, -0.3) - 0.015)
                b.parent = root
                pecs[nm] = True
    for b in eb:
        b.roll = 0.0
    bpy.ops.object.mode_set(mode='OBJECT')

    centers = [(n, (a + b) / 2) for n, a, b in segs]

    def chain_weights(y):
        if y <= centers[0][1]:
            return {centers[0][0]: 1.0}
        if y >= centers[-1][1]:
            return {centers[-1][0]: 1.0}
        for (n0, c0), (n1, c1) in zip(centers, centers[1:]):
            if c0 <= y <= c1:
                t = (y - c0) / (c1 - c0)
                t = t * t * (3 - 2 * t)
                return {n0: 1 - t, n1: t}
        return {centers[-1][0]: 1.0}

    def assign(ob, extra=None):
        groups = {}
        for v in ob.data.vertices:
            w = chain_weights(v.co.y)
            if extra:
                w = extra(v, w)
            for n, wt in w.items():
                if wt <= 0:
                    continue
                if n not in groups:
                    groups[n] = ob.vertex_groups.new(name=n)
                groups[n].add([v.index], wt, 'REPLACE')
        ob.parent = arm
        mod = ob.modifiers.new('Armature', 'ARMATURE')
        mod.object = arm

    def body_extra(v, w):
        # 눈·수염·부리(주둥이 앞쪽)는 머리에 단단히
        if v.co.y < -0.5 + 0.06:
            return {'Head': 1.0}
        return w

    assign(body, body_extra)
    pec_info = []
    for f in d['fins']:
        if f['kind'] == 'pectoral':
            s = f['s'][0]
            pec_info.append((B.y(s), f['h']))

    def fin_extra(v, w):
        if pec_info and abs(v.co.x) > 0.003:
            py, ph = pec_info[0]
            # 가슴지느러미: 몸 옆으로 튀어나온 부분
            if py - 0.01 <= v.co.y <= py + ph + 0.02 and abs(v.co.x) > B.surface_x((v.co.y + 0.5) / B.be, -0.3) * 0.9:
                t = min(1.0, max(0.0, (v.co.y - py) / max(0.01, ph * 0.5)))
                nm = 'Pec_R' if v.co.x > 0 else 'Pec_L'
                return {nm: t, 'Root': 1 - t}
        return w

    assign(fins, fin_extra)
    return arm


# =====================================================================
#  애니메이션 (몸 마디를 사인파로 돌린다)
# =====================================================================

CLIPS = {
    # 이름: (프레임 수, 주기 수, 꼬리 폭(rad), 파장 위상, 몸 비틀기, 가슴지느러미 폭, C자 굽힘)
    'Swim_Slow': (50, 2, 0.16, 0.9, 0.0, 0.35, 0.0),
    'Swim_Fast': (30, 3, 0.3, 1.0, 0.0, 0.2, 0.0),
    'Thrash': (36, 3, 0.55, 0.55, 0.22, 0.6, 0.0),
    'Flop': (40, 2, 0.12, 0.3, 0.0, 0.5, 0.42),
    'Idle_Hover': (60, 1, 0.035, 0.8, 0.0, 0.5, 0.0),
}


def animate(d, arm):
    nb = d['bones']
    amp_k = d['anim'].get('amp', 1.0)
    freq_k = d['anim'].get('freq', 1.0)
    axis = 0 if d.get('flat') else 2  # 납작한 고기는 위아래로 물결친다
    chain = ['Head'] + [f'Spine{i + 1}' for i in range(nb - 1)] + ['Tail']
    arm.animation_data_create()
    sc = bpy.context.scene
    sc.render.fps = FPS
    for clip, (frames, cycles, amp, wave, roll, pec, cbend) in CLIPS.items():
        # 어종 빈도 배율은 클립 길이로 (주기 수는 그대로라 반복이 이어진다)
        frames = max(12, int(round(frames / freq_k)))
        act = bpy.data.actions.new(clip)
        act.use_fake_user = True
        arm.animation_data.action = act
        for pb in arm.pose.bones:
            pb.rotation_mode = 'XYZ'
        for fr in range(frames + 1):
            ph = 2 * math.pi * cycles * fr / frames
            for i, name in enumerate(chain):
                pb = arm.pose.bones.get(name)
                if pb is None:
                    continue
                e = [0.0, 0.0, 0.0]
                if name == 'Head':
                    # 머리는 반대로 살짝 (몸이 흔들려도 머리가 덜 흔들리게)
                    ang = -0.35 * amp * amp_k * math.sin(ph + 0.6) - cbend * 0.6 * math.sin(ph)
                    e[axis] = -ang
                else:
                    k = i / (len(chain) - 1)
                    env = (0.2 + 0.8 * k ** 1.4) * amp * amp_k / max(1.0, (nb - 1) / 5.0)
                    ang = env * math.sin(ph - k * 2 * math.pi * wave)
                    ang += cbend / (nb - 1) * 1.6 * math.sin(ph)
                    e[axis] = ang
                pb.rotation_euler = e
                pb.keyframe_insert('rotation_euler', frame=fr)
            r = arm.pose.bones['Root']
            re = [0.0, 0.0, 0.0]
            if roll:
                re[1] = roll * math.sin(ph * 0.5 + 0.3)  # 몸을 비튼다 (몸길이 축)
                re[2 if axis == 2 else 0] = roll * 0.6 * math.sin(ph)
            if clip == 'Flop':
                re[0 if axis == 2 else 2] = 0.18 * math.sin(ph * 2)  # 펄떡: 위아래로 들썩
            r.rotation_euler = re
            r.keyframe_insert('rotation_euler', frame=fr)
            # 두족류 다리: 다리마다 위상을 달리해 꿈틀 (빠르게 헤엄칠 땐 뒤로 모아 쭉)
            arm_amp = {'Swim_Slow': 0.22, 'Swim_Fast': 0.08, 'Thrash': 0.6, 'Flop': 0.45, 'Idle_Hover': 0.15}[clip]
            for pb in arm.pose.bones:
                if not pb.name.startswith('Arm'):
                    continue
                ai, aj = (int(x) for x in pb.name[3:].split('_'))
                w = ph * (1.5 if clip == 'Thrash' else 1.0) + ai * 0.785 + aj * 0.9
                pb.rotation_euler = (arm_amp * math.sin(w), 0.0, arm_amp * 0.5 * math.cos(w * 0.7 + ai))
                pb.keyframe_insert('rotation_euler', frame=fr)
            for nm, sgn in (('Pec_R', 1), ('Pec_L', -1)):
                pb = arm.pose.bones.get(nm)
                if pb is None:
                    continue
                pb.rotation_euler = (pec * math.sin(ph * (2 if clip == 'Idle_Hover' else 1) + 0.5), 0.0, sgn * pec * 0.4 * math.sin(ph))
                pb.keyframe_insert('rotation_euler', frame=fr)
        for fc in act.fcurves if hasattr(act, 'fcurves') else []:
            for kp in fc.keyframe_points:
                kp.interpolation = 'LINEAR'
    arm.animation_data.action = bpy.data.actions['Swim_Slow']


# =====================================================================
#  만들기
# =====================================================================

def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def build(sid):
    d = FS.resolve(sid)
    clear()
    B = Body(d)
    if d.get('ceph'):
        return build_cephalopod(d, B)
    v, f, uv = build_body_mesh(d, B)
    body = mesh_from('Body', v, f, uv)
    extras = [make_eye(d, B, 1), make_eye(d, B, -1)] + make_barbels(d, B)
    ex = bm_to_obj('Extras', extras)
    body = join([body, ex], 'Body')
    fins = []
    for name, fv, ff, fu, kind in make_fins(d, B):
        fins.append(mesh_from(name, fv, ff, fu))
    fins = join(fins, 'Fins')
    solidify(fins, 0.0028 if d['type'] != 'G' else 0.0018)
    for p in fins.data.polygons:
        p.use_smooth = True
    if d.get('flat'):
        ang = math.radians(90 if d['flat'] == 'left' else -90)
        rot = mathutils.Matrix.Rotation(ang, 4, 'Y')
        for ob in (body, fins):
            ob.data.transform(rot)
            ob.data.update()
    arm = build_rig(d, B, body, fins)
    # LOD1 (수족관에 여러 마리): 몸·지느러미를 줄인 복사본
    lods = []
    for ob, ratio in ((body, 0.28), (fins, 0.35)):
        c = ob.copy()
        c.data = ob.data.copy()
        c.name = ob.name + '_LOD1'
        c.data.name = c.name
        bpy.context.scene.collection.objects.link(c)
        dm = c.modifiers.new('Dec', 'DECIMATE')
        dm.ratio = ratio
        bpy.context.view_layer.objects.active = c
        c.modifiers.move(len(c.modifiers) - 1, 0)
        bpy.ops.object.modifier_apply(modifier=dm.name)
        lods.append(c)
    animate(d, arm)
    tris = sum(len(p.vertices) - 2 for ob in (body, fins) for p in ob.data.polygons)
    return d, arm, body, fins, lods, tris


def make_lods(body, fins):
    lods = []
    for ob, ratio in ((body, 0.28), (fins, 0.35)):
        c = ob.copy()
        c.data = ob.data.copy()
        c.name = ob.name + '_LOD1'
        c.data.name = c.name
        bpy.context.scene.collection.objects.link(c)
        dm = c.modifiers.new('Dec', 'DECIMATE')
        dm.ratio = ratio
        bpy.context.view_layer.objects.active = c
        c.modifiers.move(len(c.modifiers) - 1, 0)
        bpy.ops.object.modifier_apply(modifier=dm.name)
        lods.append(c)
    return lods


def build_cephalopod(d, B):
    """주꾸미·갑오징어 (cephalopod.py): 몸통 사슬 + 다리 뼈."""
    body, fins, paths, arm_idx = CP.build(d)
    solidify(fins, 0.002)
    arm = build_rig(d, B, body, fins)
    CP.add_arm_bones(arm, paths)
    CP.weight_arms(body, paths, arm_idx)
    lods = make_lods(body, fins)
    animate(d, arm)
    tris = sum(len(p.vertices) - 2 for ob in (body, fins) for p in ob.data.polygons)
    return d, arm, body, fins, lods, tris


def export(d, arm, objs):
    os.makedirs(OUT_FBX, exist_ok=True)
    out = os.path.join(OUT_FBX, d['id'] + '.fbx')
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'},
                             axis_forward='-Y', axis_up='Z', apply_scale_options='FBX_SCALE_UNITS',
                             add_leaf_bones=False, use_armature_deform_only=True,
                             bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, bake_anim_step=1.0,
                             mesh_smooth_type='FACE', use_mesh_modifiers=False)
    return out


def write_textures(d):
    os.makedirs(OUT_TEX, exist_ok=True)
    alb, nrm, mt = fish_tex.paint(d)
    base = os.path.join(OUT_TEX, d['id'])
    alb.save(base + '.png')
    nrm.save(base + '_n.png')
    mt.save(base + '_m.png')
    return base + '.png'


# =====================================================================
#  렌더 (미리보기, 아이콘, 한 장 모음)
# =====================================================================

def apply_preview_materials(d, body, fins, tex_path):
    img = bpy.data.images.load(tex_path)
    nimg = bpy.data.images.load(tex_path.replace('.png', '_n.png'))
    nimg.colorspace_settings.name = 'Non-Color'
    for ob, cut in ((body, False), (fins, True)):
        m = bpy.data.materials.new(ob.name + 'Mat')
        m.use_nodes = True
        nt = m.node_tree
        bsdf = nt.nodes['Principled BSDF']
        t = nt.nodes.new('ShaderNodeTexImage')
        t.image = img
        nt.links.new(t.outputs['Color'], bsdf.inputs['Base Color'])
        if cut:
            nt.links.new(t.outputs['Alpha'], bsdf.inputs['Alpha'])
        nm = nt.nodes.new('ShaderNodeNormalMap')
        nt2 = nt.nodes.new('ShaderNodeTexImage')
        nt2.image = nimg
        nt.links.new(nt2.outputs['Color'], nm.inputs['Color'])
        nt.links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
        bsdf.inputs['Roughness'].default_value = 0.35
        bsdf.inputs['Metallic'].default_value = d['colors'].get('metallic', 0.0) * 0.5 + d['colors'].get('silver', 0) * 0.25
        ob.data.materials.clear()
        ob.data.materials.append(m)


def render_setup(res, transparent, samples=24):
    sc = bpy.context.scene
    for nm in ('Key', 'Rim', 'Cam'):
        if nm in bpy.data.objects:
            bpy.data.objects.remove(bpy.data.objects[nm])
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = samples
    sc.cycles.use_denoising = False
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = transparent
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('W')
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes['Background'].inputs['Color'].default_value = (0.42, 0.55, 0.62, 1)
    w.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.9
    key = bpy.data.objects.new('Key', bpy.data.lights.new('Key', 'SUN'))
    key.data.energy = 3.2
    key.rotation_euler = (math.radians(40), math.radians(-25), math.radians(-30))
    sc.collection.objects.link(key)
    rim = bpy.data.objects.new('Rim', bpy.data.lights.new('Rim', 'SUN'))
    rim.data.energy = 1.6
    rim.rotation_euler = (math.radians(-60), math.radians(30), math.radians(160))
    sc.collection.objects.link(rim)
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def look(cam, pos, target=(0, 0, 0), ortho=None):
    cam.location = pos
    dvec = mathutils.Vector(target) - mathutils.Vector(pos)
    cam.rotation_euler = dvec.to_track_quat('-Z', 'Y').to_euler()
    if ortho:
        cam.data.type = 'ORTHO'
        cam.data.ortho_scale = ortho
    else:
        cam.data.type = 'PERSP'
        cam.data.lens = 60


def pose_frame(arm, clip, frame):
    arm.animation_data.action = bpy.data.actions[clip]
    bpy.context.scene.frame_set(frame)


def side_dir(d):
    """카메라가 보는 옆면 (+x). 납작한 고기는 위(+z)에서 본다."""
    if d.get('flat'):
        return (0, 0, 1)
    return (1, 0, 0)


def render_icon(d, arm, body, fins, out):
    cam = render_setup((256, 128), True, samples=20)
    pose_frame(arm, 'Idle_Hover', 0)
    sd = side_dir(d)
    look(cam, (sd[0] * 3, sd[1], sd[2] * 3), (0, 0, 0), ortho=1.06)
    if d.get('flat'):
        cam.rotation_euler = (0, 0, math.radians(-90))
    bpy.context.scene.render.filepath = out
    bpy.ops.render.render(write_still=True)


def render_preview(d, arm, out_dir):
    cam = render_setup((900, 520), False, samples=32)
    os.makedirs(out_dir, exist_ok=True)
    views = {'side': ((2.2, 0, 0.05), None), 'top': ((0.001, 0, 2.4), None), 'three_q': ((1.5, -1.5, 0.7), None)}
    pose_frame(arm, 'Swim_Slow', 12)
    for name, (pos, _) in views.items():
        look(cam, pos, (0, 0, 0))
        bpy.context.scene.render.filepath = os.path.join(out_dir, f'{d["id"]}_{name}.png')
        bpy.ops.render.render(write_still=True)


def render_anim_strip(d, arm, out_dir):
    """클립마다 위에서 본 모습 4프레임 (몸이 제대로 휘는지 확인)."""
    from PIL import Image
    cam = render_setup((300, 300), False, samples=12)
    os.makedirs(out_dir, exist_ok=True)
    tiles = []
    for clip in CLIPS:
        act = bpy.data.actions[clip]
        n = int(act.frame_range[1])
        for k in range(4):
            pose_frame(arm, clip, int(n * k / 4))
            if d.get('flat'):
                look(cam, (2.2, 0.0, 0.3), (0, 0, 0))
            else:
                look(cam, (0.001, 0.0, 2.2), (0, 0, 0))
            p = os.path.join(out_dir, f'_{clip}_{k}.png')
            bpy.context.scene.render.filepath = p
            bpy.ops.render.render(write_still=True)
            tiles.append(p)
    sheet = Image.new('RGB', (1200, 300 * len(CLIPS)))
    for i, p in enumerate(tiles):
        sheet.paste(Image.open(p), ((i % 4) * 300, (i // 4) * 300))
        os.remove(p)
    sheet.save(os.path.join(out_dir, f'{d["id"]}_anim.png'))


def contact_sheet(paths, out, cols=6, cw=256, ch=150):
    """아이콘 모음 한 장 (docs/images). 칸마다 어종 Id."""
    from PIL import Image, ImageDraw
    rows = (len(paths) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cw, rows * ch), (34, 46, 56, 255))
    for i, (sid, p) in enumerate(paths):
        im = Image.open(p).convert('RGBA')
        x, y = (i % cols) * cw, (i // cols) * ch
        sheet.alpha_composite(im, (x, y + 18))
        ImageDraw.Draw(sheet).text((x + 8, y + 4), sid, fill=(220, 230, 235, 255))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.convert('RGB').save(out)


def main():
    only = arg('--only')
    ids = only.split(',') if only else list(FS.SPECIES.keys())
    preview = '--preview' in ARGS
    icons = '--no-icons' not in ARGS
    sheet = arg('--sheet')
    out_dir = os.path.abspath(arg('--out', os.path.join(ROOT, 'Screenshots', 'fish')))
    done = []
    for sid in ids:
        d, arm, body, fins, lods, tris = build(sid)
        tex = write_textures(d)
        if not preview:
            path = export(d, arm, [body, fins] + lods)
            print(f'[fish] {sid}: {tris} tris → {os.path.relpath(path, ROOT)}', flush=True)
        apply_preview_materials(d, body, fins, tex)
        for c in lods:
            c.hide_render = True
        if preview:
            render_preview(d, arm, out_dir)
            if '--anim' in ARGS:
                render_anim_strip(d, arm, out_dir)
            print(f'[fish] preview {sid}', flush=True)
        if icons or sheet:
            os.makedirs(OUT_ICON, exist_ok=True)
            ip = os.path.join(OUT_ICON, sid + '.png')
            render_icon(d, arm, body, fins, ip)
            done.append((sid, ip))
    if sheet:
        contact_sheet(done, os.path.join(ROOT, sheet) if not os.path.isabs(sheet) else sheet)
        print('[fish] sheet', sheet, flush=True)


if __name__ == '__main__':
    main()
    sys.stdout.flush()
    os._exit(0)
