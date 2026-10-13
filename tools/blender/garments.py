"""
Quaternius 베이스 몸에서 옷 메쉬를 만든다 (headless Blender, bpy).

옷은 몸 메쉬를 복사해서
  1) 부위(영역)만 남기고
  2) 법선 방향으로 띄우고, 원통 좌표로 "늘어뜨려" 근육 굴곡을 없애고 (셔츠는 가슴에서, 바지는 허벅지에서 곧게 떨어진다)
  3) 몸 밖으로 밀어내고, 단을 곧게 자르고, 두께를 준다.
뼈 가중치는 몸에서 그대로 물려받으므로 같은 애니메이션으로 몸과 함께 움직인다.

버텍스 컬러: R = 트림(목 리브, 소매단, 밑창, 지퍼, 주머니 덮개), G = 음영(1 = 밝음), B = 무늬 마스크(조끼 등판 망사 등), A = 1
"""
import math
import bmesh
import bpy
import mathutils
import numpy as np

from meshops import (bone_segments, bvh_world, closest_on_seg, neighbors, set_world_verts, smooth,
                     vertex_normals_world, weights, world_verts)

TORSO = ('pelvis', 'spine_', 'clavicle', 'neck_01')
ARM = ('upperarm', 'lowerarm')
HAND = ('hand', 'index', 'middle', 'ring', 'pinky', 'thumb')
LEG = ('thigh', 'calf')
FOOT = ('foot', 'ball')


def family(w, prefixes):
    return sum(v for b, v in w.items() if b.startswith(prefixes))


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


class Body:
    """몸 메쉬 + 뼈 기준점(휴식 자세, 월드 좌표). 캐릭터는 +Y를 보고, 왼쪽(_l)이 -X."""

    def __init__(self, body, arm, eyes=None):
        self.obj, self.arm, self.eyes = body, arm, eyes
        self.P = world_verts(body)
        self.N = vertex_normals_world(body)
        self.W = weights(body)
        self.nbr = neighbors(body)
        self.bvh = bvh_world(body)
        s = bone_segments(arm)
        self.seg = s
        self.shoulder_x = abs(s['upperarm_l'][0][0])
        self.elbow_x = abs(s['lowerarm_l'][0][0])
        self.wrist_x = abs(s['hand_l'][0][0])
        self.arm_z = s['upperarm_l'][0][2]
        self.arm_y = s['upperarm_l'][0][1]
        self.neck_z = s['neck_01'][0][2]
        self.neck_y = s['neck_01'][0][1]
        self.hip_z = s['thigh_l'][0][2]
        self.knee_z = s['calf_l'][0][2]
        self.ankle_z = s['foot_l'][0][2]
        self.chest_z = s['spine_03'][0][2]
        self.waist_z = s['spine_01'][0][2]
        self.leg_x = abs(s['thigh_l'][0][0])
        self.fam = {k: np.array([family(w, p) for w in self.W]) for k, p in
                    (('torso', TORSO), ('arm', ARM), ('hand', HAND), ('leg', LEG), ('foot', FOOT), ('head', ('Head',)))}
        head = self.P[self.fam['head'] > 0.6]
        self.head_min, self.head_max = head.min(0), head.max(0)
        band = (self.fam['torso'] > 0.8) & (self.P[:, 2] > self.chest_z) & (self.P[:, 2] < self.arm_z - 0.05)
        self.chest_half = float(np.abs(self.P[band, 0]).max())
        self.torso_y = float(np.median(self.P[(self.fam['torso'] > 0.8) & (np.abs(self.P[:, 2] - self.chest_z) < 0.1), 1]))

    def ax(self):
        return np.abs(self.P[:, 0])


# ---------------------------------------------------------------------------------------------
# 모양 연산

def drape(P, sel, origin, axis, up_window, bins=40, strength=1.0, below=None, max_grow=0.06):
    """축 둘레 원통 좌표에서, 각 점의 반지름을 '자기보다 위(up_window 이내)'의 최대 반지름으로 키운다.
    천이 위에서부터 곧게 떨어지는 모양. axis는 '위' 방향 단위벡터."""
    idx = np.where(sel)[0]
    if len(idx) == 0:
        return P
    axis = axis / np.linalg.norm(axis)
    rel = P[idx] - origin
    s = rel @ axis
    radial = rel - np.outer(s, axis)
    r = np.linalg.norm(radial, axis=1)
    ref = np.array([0.0, 1.0, 0.0]) if abs(axis[1]) < 0.9 else np.array([1.0, 0.0, 0.0])
    e1 = ref - axis * (ref @ axis); e1 /= np.linalg.norm(e1)
    e2 = np.cross(axis, e1)
    ang = np.arctan2(radial @ e2, radial @ e1)
    b = ((ang + math.pi) / (2 * math.pi) * bins).astype(int) % bins
    newr = r.copy()
    limit = below if below is not None else np.inf
    for k in range(bins):
        m = (b == k) | (b == (k + 1) % bins) | (b == (k - 1) % bins)
        ii = np.where(b == k)[0]
        cand = np.where(m)[0]
        cs, cr = s[cand], r[cand]
        ok = cs <= limit
        cs, cr = cs[ok], cr[ok]
        for j in ii:
            w = (cs >= s[j]) & (cs <= s[j] + up_window)
            if w.any():
                newr[j] = max(r[j], cr[w].max())
    newr = np.minimum(newr, r + max_grow)
    scale = np.where(r > 1e-6, (r + (newr - r) * strength) / np.maximum(r, 1e-6), 1.0)
    out = P.copy()
    out[idx] = origin + np.outer(s, axis) + radial * scale[:, None]
    return out


SHELLS = {}  # 이름 -> (P, sel): 두께를 주기 전 바깥면. 겹쳐 입는 옷을 그 밖으로 밀 때 쓴다.


def shell_tree(body, names):
    """먼저 만든 옷들의 바깥면 BVH (몸 다각형 중 옷 영역만, 옷 위치로)."""
    verts, polys = [], []
    for n in names:
        if n not in SHELLS:
            continue
        P, sel = SHELLS[n]
        base = len(verts)
        verts.extend(tuple(p) for p in P)
        for poly in body.obj.data.polygons:
            if all(sel[i] for i in poly.vertices):
                polys.append(tuple(base + i for i in poly.vertices))
    return mathutils.bvhtree.BVHTree.FromPolygons(verts, polys) if polys else None


def envelope(P, sel, origin, axis, window, bins=32, ang_spread=1):
    """축에 수직인 단면마다 각도별 반지름을 '앞뒤 window 안의 최댓값'으로: 발가락 사이 같은 홈을 메운다."""
    idx = np.where(sel)[0]
    if len(idx) == 0:
        return P
    axis = axis / np.linalg.norm(axis)
    rel = P[idx] - origin
    s = rel @ axis
    radial = rel - np.outer(s, axis)
    r = np.linalg.norm(radial, axis=1)
    ref = np.array([0.0, 0.0, 1.0]) if abs(axis[2]) < 0.9 else np.array([1.0, 0.0, 0.0])
    e1 = ref - axis * (ref @ axis)
    e1 /= np.linalg.norm(e1)
    e2 = np.cross(axis, e1)
    ang = np.arctan2(radial @ e2, radial @ e1)
    b = ((ang + math.pi) / (2 * math.pi) * bins).astype(int) % bins
    newr = r.copy()
    for j in range(len(idx)):
        da = np.abs(b - b[j])
        da = np.minimum(da, bins - da)
        w = (da <= ang_spread) & (np.abs(s - s[j]) <= window)
        newr[j] = max(r[j], r[w].max())
    out = P.copy()
    out[idx] = origin + np.outer(s, axis) + radial * (newr / np.maximum(r, 1e-6))[:, None]
    return out


def push_out(P, sel, body, gap, tree=None, max_depth=None, ref_normals=None):
    """몸(또는 tree) 표면에서 gap(배열 또는 스칼라)보다 가까운 점을 밖으로 민다.
    다른 옷 위로 밀 때는 max_depth(그보다 깊이 박힌 점은 다른 부위로 보고 무시)와
    ref_normals(점의 원래 법선과 반대쪽 면이면 무시)로 겨드랑이처럼 팔 안쪽 면에 끌려가는 것을 막는다."""
    gap = np.broadcast_to(np.asarray(gap, dtype=float), (len(P),))
    out = P.copy()
    tree = tree or body.bvh
    for i in np.where(sel)[0]:
        loc, nrm, _, _ = tree.find_nearest(mathutils.Vector(out[i]))
        if loc is None:
            continue
        q, n = np.array(loc), np.array(nrm)
        d = (out[i] - q) @ n
        if max_depth is not None and d < -max_depth:
            continue
        if ref_normals is not None and n @ ref_normals[i] < 0.3:
            continue
        if d < gap[i]:
            out[i] = out[i] + n * (gap[i] - d)
    return out


def layer_over(P, sel, body, under, gap=0.008):
    """under(먼저 만든 옷 이름들) 바깥으로 민다."""
    tree = shell_tree(body, under or [])
    return push_out(P, sel, body, gap, tree=tree, max_depth=0.05, ref_normals=body.N) if tree is not None else P


def shell(body, sel, offset, smooth_iters=6):
    """기본 껍데기: 법선 방향 오프셋 + 굴곡을 줄이는 스무딩."""
    off = np.broadcast_to(np.asarray(offset, dtype=float), (len(body.P),))[:, None]
    P = body.P + body.N * off
    P = smooth(P, body.nbr, sel.astype(float) * 0.9, iters=smooth_iters)
    return P


# ---------------------------------------------------------------------------------------------
# 메쉬 오브젝트 만들기

def make_object(body, name, P, sel, snap=None, trim=None, pattern=None, thickness=0.005, shade=None):
    """몸을 복사해 sel 버텍스만 남긴 새 오브젝트. snap(i, p, boundary)->p 로 단을 곧게 맞춘다."""
    src = body.obj
    obj = src.copy()
    obj.data = src.data.copy()
    obj.name = obj.data.name = name
    src.users_collection[0].objects.link(obj)
    set_world_verts(obj, P)

    me = obj.data
    n = len(me.vertices)
    col = np.ones((n, 4))
    if trim is not None:
        col[:, 0] = np.clip(trim, 0, 1)
    else:
        col[:, 0] = 0
    if pattern is not None:
        col[:, 2] = np.clip(pattern, 0, 1)
    else:
        col[:, 2] = 0
    if shade is not None:
        col[:, 1] = np.clip(shade, 0, 1)
    col[:, 3] = np.clip(body.P[:, 2] / 2.0, 0, 1)  # 휴식 자세 높이/2: 장화를 신으면 바지 아랫단을 셰이더에서 잘라낸다
    SHELLS[name] = (P.copy(), sel.copy())
    attr = me.color_attributes.new('Col', 'FLOAT_COLOR', 'POINT')
    for i in range(n):
        attr.data[i].color = tuple(col[i])

    bm = bmesh.new()
    bm.from_mesh(me)
    bm.verts.ensure_lookup_table()
    kill = [v for v in bm.verts if not sel[v.index]]
    bmesh.ops.delete(bm, geom=kill, context='VERTS')
    # 고립된 조각 제거 (몇 개짜리 섬)
    bm.verts.ensure_lookup_table()
    islands = []
    seen = set()
    for v in bm.verts:
        if v.index in seen:
            continue
        stack, isl = [v], []
        seen.add(v.index)
        while stack:
            x = stack.pop()
            isl.append(x)
            for e in x.link_edges:
                o = e.other_vert(x)
                if o.index not in seen:
                    seen.add(o.index)
                    stack.append(o)
        islands.append(isl)
    biggest = max(len(i) for i in islands) if islands else 0
    small = [v for isl in islands if len(isl) < max(30, biggest * 0.05) for v in isl]
    if small:
        bmesh.ops.delete(bm, geom=small, context='VERTS')
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context='VERTS')
    if snap is not None:
        mw = obj.matrix_world
        inv = mw.inverted()
        bnd = [v for v in bm.verts if v.is_boundary]
        for v in bnd:
            p = np.array(mw @ v.co)
            p2 = snap(p)
            if p2 is not None:
                v.co = inv @ mathutils.Vector(p2)
        # 단 근처 한 줄 더 부드럽게
        for _ in range(2):
            for v in bnd:
                nb = [e.other_vert(v) for e in v.link_edges if e.other_vert(v).is_boundary]
                if len(nb) == 2:
                    v.co = v.co * 0.5 + (nb[0].co + nb[1].co) * 0.25
    bm.to_mesh(me)
    bm.free()
    me.update()
    for p in me.polygons:
        p.use_smooth = True

    if thickness > 0:
        mod = obj.modifiers.new('Solidify', 'SOLIDIFY')
        mod.thickness = thickness
        mod.offset = -1
        mod.use_rim = True
        mod.use_even_offset = False
        mod.thickness_clamp = 1.0  # 밑창처럼 납작하게 눌린 곳에서 가시가 튀지 않게
        with bpy.context.temp_override(object=obj, active_object=obj, selected_objects=[obj]):
            bpy.ops.object.modifier_move_to_index(modifier=mod.name, index=0)
            bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def add_box(obj, body_ref, center, normal, up, size, color=(1, 1, 1, 1), bevel=0.004):
    """옷 표면에 붙는 상자(주머니·버클). 가장 가까운 옷 버텍스의 가중치를 물려받는다."""
    me = obj.data
    mw = obj.matrix_world
    inv = mw.inverted()
    n = mathutils.Vector(normal).normalized()
    u = (mathutils.Vector(up) - n * mathutils.Vector(up).dot(n)).normalized()
    r = u.cross(n)
    c = mathutils.Vector(center)

    # 가장 가까운 옷 버텍스
    best, bd = None, 1e9
    for v in me.vertices:
        d = ((mw @ v.co) - c).length
        if d < bd:
            best, bd = v, d
    groups = [(g.group, g.weight) for g in best.groups]

    bm = bmesh.new()
    bm.from_mesh(me)
    start = len(bm.verts)
    res = bmesh.ops.create_cube(bm, size=1.0)
    verts = res['verts']
    sx, sy, sz = size
    for v in verts:
        lp = v.co.copy()
        wp = c + r * (lp.x * sx) + u * (lp.y * sy) + n * ((lp.z + 0.5) * sz)
        v.co = inv @ wp
    if bevel > 0:
        edges = list({e for v in verts for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=bevel, segments=2, affect='EDGES')
    bm.to_mesh(me)
    bm.free()
    me.update()
    attr = me.color_attributes['Col']
    for i in range(start, len(me.vertices)):
        v = me.vertices[i]
        for g in list(v.groups):
            obj.vertex_groups[g.group].remove([i])
        for gi, w in groups:
            obj.vertex_groups[gi].add([i], w, 'REPLACE')
        attr.data[i].color = color
    for p in me.polygons:
        if all(i >= start for i in p.vertices):
            p.use_smooth = False


def surface_hit_tree(tree, origin, direction):
    loc, nrm, _, _ = tree.ray_cast(mathutils.Vector(origin), mathutils.Vector(direction).normalized())
    return (np.array(loc), np.array(nrm)) if loc is not None else (None, None)


def surface_hit(obj, origin, direction):
    """옷 메쉬 표면 위치/법선 (광선)."""
    mw = obj.matrix_world
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.transform(mw)
    tree = mathutils.bvhtree.BVHTree.FromBMesh(bm)
    bm.free()
    loc, nrm, _, _ = tree.ray_cast(mathutils.Vector(origin), mathutils.Vector(direction).normalized())
    return (np.array(loc), np.array(nrm)) if loc is not None else (None, None)


# ---------------------------------------------------------------------------------------------
# 옷 종류


def neck_hole(b, P, rx=0.075, ry=0.07, back_drop=0.025, front_drop=0.07):
    """목둘레 구멍: 목 축 둘레 타원 안쪽에서, 깃선(뒤는 얕게, 앞은 깊게)보다 높은 점. True = 잘라낸다.
    깃선 높이 배열도 돌려준다(목 리브 트림용)."""
    dx = P[:, 0] / rx
    dy = (P[:, 1] - (b.neck_y + 0.012)) / ry
    inside = dx * dx + dy * dy < 1.0
    front = smoothstep(b.neck_y - 0.02, b.neck_y + ry, P[:, 1])
    collar = b.neck_z - back_drop - (front_drop - back_drop) * front
    return inside & (P[:, 2] > collar), collar, inside


def top(b, name, sleeve='short', loose=1.0, base=0.012, hem_drop=0.045, neck_front=0.075, tight=False, under=None):
    P0, ax = b.P, b.ax()
    if sleeve == 'short':
        cut_x = b.shoulder_x + 0.42 * (b.elbow_x - b.shoulder_x)
    elif sleeve == 'long':
        cut_x = b.wrist_x - 0.035
    else:
        cut_x = b.shoulder_x - 0.03
    hem_z = b.hip_z - hem_drop
    hole, collar, inside = neck_hole(b, P0, front_drop=neck_front)
    sel = (P0[:, 2] > hem_z) & ~hole & (ax < cut_x) & (b.fam['head'] < 0.3) & (b.fam['hand'] < 0.05)
    sel &= ~((b.fam['torso'] < 0.5) & (b.fam['arm'] < 0.5))  # 목 위·다리
    off = np.full(len(P0), base)
    if not tight:
        # 아랫단으로 갈수록 살짝 더 뜬다
        off += 0.012 * smoothstep(b.waist_z + 0.05, hem_z, P0[:, 2])
    P = shell(b, sel, off, smooth_iters=8 if not tight else 3)
    if not tight:
        torso = sel & (ax < b.shoulder_x + 0.01) & (P0[:, 2] < b.arm_z - 0.02)
        P = drape(P, torso, np.array([0, b.torso_y, 0]), np.array([0, 0, 1.0]), up_window=0.45, strength=0.9 * loose,
                  below=b.arm_z - 0.06)
        for side in (-1, 1):
            arm = sel & (P0[:, 0] * side > b.shoulder_x - 0.02)
            o = np.array([side * b.shoulder_x, b.arm_y, b.arm_z])
            P = drape(P, arm, o, np.array([-side, 0, 0.0]), up_window=0.16, strength=0.6 * loose, bins=24)
        P = smooth(P, b.nbr, sel * 0.7, iters=4)
    P = push_out(P, sel, b, base * 0.8)
    P = layer_over(P, sel & (P0[:, 2] < b.hip_z + 0.12), b, under, 0.007)  # 바지 허리 밖으로

    def snap(p):
        if abs(p[0]) > cut_x - 0.04:
            return np.array([math.copysign(cut_x, p[0]), p[1], p[2]])
        if p[2] < hem_z + 0.04:
            return np.array([p[0], p[1], hem_z])
        return None

    trim = np.zeros(len(P0))
    trim[inside & (np.abs(P0[:, 2] - collar) < 0.02)] = 1.0        # 목 리브
    if sleeve == 'long':
        trim[np.abs(ax - cut_x) < 0.025] = 1.0      # 소맷부리
    return make_object(b, name, P, sel, snap=snap, trim=trim)


def vest(b, name, kind='fishing', under=None):
    P0, ax = b.P, b.ax()
    life = kind == 'life'
    hem_z = b.hip_z + (0.05 if life else 0.035)
    hole, collar, inside = neck_hole(b, P0, rx=0.09 if not life else 0.085, ry=0.085, back_drop=0.02,
                                     front_drop=0.2 if not life else 0.12)
    armhole = b.chest_half * (0.93 if not life else 0.98)
    sel = (P0[:, 2] > hem_z) & ~hole & (b.fam['head'] < 0.2) & (b.fam['torso'] > 0.45) & (b.fam['arm'] < 0.3)
    sel &= ax < armhole
    # 겨드랑이 아래로 둥근 진동둘레
    side = np.abs(P0[:, 1] - b.torso_y) < 0.065
    sel &= ~((P0[:, 2] > b.arm_z - 0.16) & (P0[:, 2] < b.arm_z + 0.03) & (ax > b.chest_half * 0.62) & side)
    base = 0.032 if not life else 0.05
    P = shell(b, sel, base, smooth_iters=10)
    torso = sel & (P0[:, 2] < b.arm_z - 0.02)
    P = drape(P, torso, np.array([0, b.torso_y, 0]), np.array([0, 0, 1.0]), up_window=0.5, strength=1.0, below=b.arm_z - 0.08)
    P = smooth(P, b.nbr, sel * 0.8, iters=6)
    P = push_out(P, sel, b, base * 0.85)
    for _ in range(2):
        P = layer_over(P, sel, b, under, 0.008)
        P = smooth(P, b.nbr, sel * 0.5, iters=2)
    P = layer_over(P, sel, b, under, 0.008)

    def snap(p):
        if p[2] < hem_z + 0.035:
            return np.array([p[0], p[1], hem_z])
        return None

    front = P0[:, 1] > b.torso_y
    trim = np.zeros(len(P0))
    trim[(np.abs(P0[:, 0]) < 0.012) & front] = 1.0                                     # 지퍼
    trim[inside & (np.abs(P0[:, 2] - collar) < 0.022)] = 1.0                           # 깃 테두리
    pattern = (~front).astype(float) * smoothstep(b.waist_z, b.chest_z, P0[:, 2])     # 등판 망사
    obj = make_object(b, name, P, sel, snap=snap, trim=trim, pattern=None if life else pattern, thickness=0.008 if not life else 0.012)

    # 주머니
    if not life:
        pockets = [(0.075, b.chest_z + 0.03, (0.07, 0.075, 0.018)), (0.078, b.waist_z + 0.035, (0.085, 0.09, 0.024)),
                   (0.14, b.waist_z + 0.045, (0.05, 0.08, 0.02))]
        for x, z, size in pockets:
            for side in (-1, 1):
                loc, nrm = surface_hit(obj, (side * x, 1.0, z), (0, -1, 0))
                if loc is None:
                    continue
                add_box(obj, b, loc - nrm * 0.004, nrm, (0, 0, 1), size, color=(0.0, 0.85, 0.0, 1))
                # 덮개
                add_box(obj, b, loc + np.array([0, 0, size[1] * 0.38]) + nrm * (size[2] - 0.006), nrm, (0, 0, 1),
                        (size[0] * 1.05, size[1] * 0.3, 0.007), color=(1.0, 1.0, 0.0, 1))
        # 고리 (D링)
        for side in (-1, 1):
            loc, nrm = surface_hit(obj, (side * 0.06, 1.0, b.arm_z - 0.02), (0, -1, 0))
            if loc is not None:
                add_box(obj, b, loc, nrm, (0, 0, 1), (0.02, 0.02, 0.008), color=(1.0, 0.4, 0.0, 1), bevel=0.003)
    else:
        # 구명조끼 버클 띠 2줄
        for z in (b.waist_z + 0.02, b.chest_z - 0.02):
            loc, nrm = surface_hit(obj, (0, 1.0, z), (0, -1, 0))
            if loc is not None:
                add_box(obj, b, loc - nrm * 0.003, nrm, (0, 0, 1), (0.06, 0.03, 0.012), color=(1.0, 0.3, 0.0, 1), bevel=0.003)
    return obj


def pants(b, name, kind='cargo', under=None):
    P0, ax = b.P, b.ax()
    shorts = kind == 'shorts'
    wader = kind == 'wader'
    top_z = b.chest_z - 0.03 if wader else b.hip_z + 0.075
    hem_z = (b.knee_z + 0.06) if shorts else (b.ankle_z + 0.03)
    sel = (P0[:, 2] < top_z) & (P0[:, 2] > hem_z) & (b.fam['foot'] < 0.4) & (b.fam['arm'] < 0.05) & (b.fam['hand'] < 0.01)
    if wader:
        sel &= (b.fam['arm'] < 0.02) & ~((P0[:, 2] > b.arm_z - 0.14) & (ax > b.shoulder_x - 0.08))
    base = {'cargo': 0.014, 'jeans': 0.008, 'tech': 0.01, 'wader': 0.03, 'shorts': 0.012}[kind]
    loose = {'cargo': 1.0, 'jeans': 0.55, 'tech': 0.7, 'wader': 1.0, 'shorts': 1.0}[kind]
    off = np.full(len(P0), base)
    # 허리는 몸에 붙게
    off = np.where(P0[:, 2] > b.hip_z + 0.03, np.minimum(off, 0.008 if not wader else 0.03), off)
    P = shell(b, sel, off, smooth_iters=8)
    for side in (-1, 1):
        leg = sel & (P0[:, 0] * side > 0.015) & (P0[:, 2] < b.hip_z - 0.04)
        a, t = b.seg['thigh_l' if side < 0 else 'thigh_r'][0], b.seg['calf_l' if side < 0 else 'calf_r'][1]
        axis = a - t
        o = t
        P = drape(P, leg, o, axis, up_window=0.38 if kind != 'jeans' else 0.18, strength=loose, bins=32,
                  below=(b.hip_z - 0.1 - t[2]))
    if wader:
        torso = sel & (P0[:, 2] > b.hip_z)
        P = drape(P, torso, np.array([0, b.torso_y, 0]), np.array([0, 0, 1.0]), up_window=0.3, strength=1.0)
    P = smooth(P, b.nbr, sel * 0.7, iters=5)
    P = push_out(P, sel, b, base * 0.8)
    if under:
        for _ in range(2):
            P = layer_over(P, sel & (P0[:, 2] > b.hip_z - 0.12), b, under, 0.008)
            P = smooth(P, b.nbr, sel * 0.5, iters=2)
        P = layer_over(P, sel & (P0[:, 2] > b.hip_z - 0.12), b, under, 0.008)

    def snap(p):
        if p[2] < hem_z + 0.05:
            return np.array([p[0], p[1], hem_z])
        if p[2] > top_z - 0.04:
            return np.array([p[0], p[1], top_z])
        return None

    trim = np.zeros(len(P0))
    trim[P0[:, 2] > top_z - 0.035] = 1.0      # 허리 밴드
    obj = make_object(b, name, P, sel, snap=snap, trim=trim, thickness=0.005)
    if kind == 'cargo' or shorts:
        for side in (-1, 1):
            z = b.knee_z + 0.16 if not shorts else b.knee_z + 0.17
            loc, nrm = surface_hit(obj, (side * 1.0, -0.04, z), (-side, 0, 0))
            if loc is not None:
                add_box(obj, b, loc - nrm * 0.004, nrm, (0, 0, 1), (0.11, 0.13, 0.022), color=(0.0, 0.85, 0.0, 1))
                add_box(obj, b, loc + np.array([0, 0, 0.05]) + nrm * 0.014, nrm, (0, 0, 1), (0.115, 0.04, 0.007),
                        color=(1.0, 1.0, 0.0, 1))
    if wader:
        # 멜빵: 가슴판 위 모서리에서 어깨 너머로
        for side in (-1, 1):
            for z in np.linspace(top_z + 0.03, b.arm_z + 0.02, 4):
                loc, nrm = surface_hit_tree(shell_tree(b, under or []) or b.bvh, (side * 0.085, 1.0, z), (0, -1, 0))
                if loc is not None:
                    add_box(obj, b, loc, nrm, (0, 0, 1), (0.028, (b.arm_z - top_z) / 3 + 0.02, 0.006), color=(1.0, 1.0, 0.0, 1), bevel=0.0015)
            for z in np.linspace(top_z + 0.03, b.arm_z + 0.02, 4):
                loc, nrm = surface_hit_tree(shell_tree(b, under or []) or b.bvh, (side * 0.085, -1.0, z), (0, 1, 0))
                if loc is not None:
                    add_box(obj, b, loc, nrm, (0, 0, 1), (0.028, (b.arm_z - top_z) / 3 + 0.02, 0.006), color=(1.0, 1.0, 0.0, 1), bevel=0.0015)
            loc, nrm = surface_hit(obj, (side * 0.085, 1.0, top_z - 0.02), (0, -1, 0))
            if loc is not None:
                add_box(obj, b, loc, nrm, (0, 0, 1), (0.03, 0.025, 0.008), color=(1.0, 0.3, 0.0, 1), bevel=0.002)
    return obj


def shoes(b, name, kind='sneaker'):
    P0 = b.P
    boots = kind == 'boots'
    top_z = (b.knee_z - 0.1) if boots else (b.ankle_z + 0.045)
    sel = (P0[:, 2] < top_z) & (((b.fam['foot'] + b.fam['leg']) > 0.5) | (P0[:, 2] < b.ankle_z))
    base = 0.011 if not boots else 0.014
    P = P0 + b.N * base
    # 발가락 사이 홈을 메운다: 뒤꿈치→앞코 축 둘레 단면 외곽선
    foot = sel & (P0[:, 2] < b.ankle_z + 0.035)
    for side in (-1, 1):
        f = foot & (P0[:, 0] * side > 0)
        heel = b.seg['foot_l' if side < 0 else 'foot_r'][0].copy()
        tip = b.seg['ball_leaf_l' if side < 0 else 'ball_leaf_r'][1].copy()
        heel[1] -= 0.06
        heel[2], tip[2] = 0.06, 0.016  # 발등은 높고 발가락 쪽은 낮게
        P = envelope(P, f, heel, tip - heel, window=0.035, bins=28, ang_spread=2)
    P = smooth(P, b.nbr, foot * 0.8, iters=4)
    P = push_out(P, sel, b, base)
    # 앞코를 조금 길고 둥글게
    ball_y = b.seg['ball_l'][0][1]
    front = sel & (P0[:, 1] > ball_y)
    P[front, 1] += 0.012 * smoothstep(ball_y, ball_y + 0.12, P0[front, 1])
    # 밑창: 바닥 평평하게
    sole_z = -0.014
    low = sel & (P[:, 2] < 0.04)
    P[low, 2] = sole_z + (P[low, 2] - sole_z) * smoothstep(0.0, 0.04, P[low, 2]) * 1.0
    P[sel & (P0[:, 2] < 0.015), 2] = sole_z
    if boots:
        for side in (-1, 1):
            leg = sel & (P0[:, 0] * side > 0) & (P0[:, 2] > b.ankle_z - 0.01)
            a = b.seg['calf_l' if side < 0 else 'calf_r'][0]
            t = b.seg['calf_l' if side < 0 else 'calf_r'][1]
            P = drape(P, leg, t, a - t, up_window=0.4, strength=1.0, bins=28, max_grow=0.03)
            # 윗단이 살짝 벌어지게
            rim = leg & (P0[:, 2] > top_z - 0.08)
            P = drape(P, rim, t, a - t, up_window=0.0, strength=0.0, bins=28)
    P = smooth(P, b.nbr, sel * 0.5, iters=3)
    P = push_out(P, sel & (P0[:, 2] > 0.02), b, base * 0.8)

    def snap(p):
        if p[2] > top_z - 0.04:
            return np.array([p[0], p[1], top_z])
        return None

    trim = np.zeros(len(P0))
    trim[P[:, 2] < 0.012] = 1.0                       # 밑창
    if boots:
        trim[P0[:, 2] > top_z - 0.025] = 1.0          # 장화 윗단
    else:
        trim[(P0[:, 2] > b.ankle_z + 0.015)] = 0.6    # 발목 칼라
    return make_object(b, name, P, sel, snap=snap, trim=trim, thickness=0.004)


# ---------------------------------------------------------------------------------------------
# 머리 소품 (Head 뼈 100%)

def _head_object(b, name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    b.obj.users_collection[0].objects.link(obj)
    obj.parent = b.arm
    obj.matrix_world = mathutils.Matrix.Identity(4)
    # 몸과 같은 부모 공간으로 (월드 좌표로 만든 버텍스를 로컬로)
    inv = obj.matrix_world.inverted()
    g = obj.vertex_groups.new(name='Head')
    g.add(list(range(len(me.vertices))), 1.0, 'REPLACE')
    mod = obj.modifiers.new('Armature', 'ARMATURE')
    mod.object = b.arm
    for p in me.polygons:
        p.use_smooth = True
    # 원통 UV (밀짚 엮음 무늬용): u = 둘레 각도, v = 높이 + 반지름 → 약 1cm 칸 (셰이더 _PatternScale 24)
    uv = me.uv_layers.new(name='UVMap')
    c = (b.head_min + b.head_max) / 2
    for loop in me.loops:
        p = me.vertices[loop.vertex_index].co
        dx, dy = p.x - c[0], p.y - c[1]
        ang = math.atan2(dy, dx)
        uv.data[loop.index].uv = (ang / (2 * math.pi) * 2.5, (p.z + math.hypot(dx, dy)) * 4.2)
    return obj


def lathe(bm, profile, center, scale_xy=(1.0, 1.0), segs=40, tilt=0.0, color_fn=None):
    """profile: [(r, z)] 아래→위. 원통 둘레로 돌려 면을 만든다."""
    rings = []
    cx, cy, cz = center
    for (r, z) in profile:
        ring = []
        for k in range(segs):
            a = 2 * math.pi * k / segs
            x, y = math.cos(a) * r * scale_xy[0], math.sin(a) * r * scale_xy[1]
            zz = z + tilt * y
            ring.append(bm.verts.new((cx + x, cy + y, cz + zz)))
        rings.append(ring)
    for i in range(len(rings) - 1):
        for k in range(segs):
            a, b2 = rings[i][k], rings[i][(k + 1) % segs]
            c, d = rings[i + 1][(k + 1) % segs], rings[i + 1][k]
            bm.faces.new((a, b2, c, d))
    return rings


def hat(b, name, style):
    hx = (b.head_max[0] - b.head_min[0]) / 2 + 0.018
    hy = (b.head_max[1] - b.head_min[1]) / 2 + 0.02
    cx = 0.0
    cy = (b.head_max[1] + b.head_min[1]) / 2 - 0.005
    top = b.head_max[2]
    bm = bmesh.new()
    if style == 'bucket':
        base = top - 0.085
        prof = [(1.75, -0.035), (1.55, -0.02), (1.08, 0.0), (1.0, 0.012), (1.0, 0.06), (0.97, 0.085), (0.88, 0.1), (0.6, 0.108),
                (0.0, 0.11)]
        prof = [(r, z) for r, z in prof]
        rings = lathe(bm, [(r, z) for r, z in prof], (cx, cy, base), (hx, hy))
        # 반지름 단위가 hx, hy 이므로 z는 미터
        bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.004)
    elif style == 'straw':
        base = top - 0.075
        prof = [(2.35, -0.03), (2.2, -0.018), (1.6, -0.004), (1.04, 0.002), (1.0, 0.01), (1.0, 0.07), (0.92, 0.09), (0.0, 0.094)]
        lathe(bm, prof, (cx, cy, base), (hx, hy))
        bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.004)
    elif style == 'cap':
        base = top - 0.075
        prof = [(1.0, 0.0), (1.0, 0.03), (0.96, 0.06), (0.84, 0.085), (0.6, 0.102), (0.3, 0.108), (0.0, 0.11)]
        lathe(bm, prof, (cx, cy - 0.004, base), (hx * 0.98, hy * 0.98))
        # 챙: 앞쪽 반원판, 살짝 휘게
        n = 16
        front_y = cy + hy * 0.95
        pts = []
        for k in range(n + 1):
            a = math.pi * k / n
            x = math.cos(a) * hx * 0.92
            ext = math.sin(a) * 0.075
            pts.append((x, ext))
        inner = [bm.verts.new((cx + x, front_y - 0.02 * math.sin(math.pi * k / n), base + 0.002 - 0.02 * (x / hx) ** 2 * 0))
                 for k, (x, e) in enumerate(pts)]
        outer = [bm.verts.new((cx + x * 0.95, front_y + e, base - 0.006 - 0.012 * (x / hx) ** 2)) for (x, e) in pts]
        for k in range(n):
            bm.faces.new((inner[k], inner[k + 1], outer[k + 1], outer[k]))
        bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.005)
        # 꼭지 단추
        bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=6, radius=0.009,
                                  matrix=mathutils.Matrix.Translation((cx, cy - 0.004, base + 0.11)))
    obj = _head_object(b, name, bm)
    me = obj.data
    attr = me.color_attributes.new('Col', 'FLOAT_COLOR', 'POINT')
    mw = obj.matrix_world
    for i, v in enumerate(me.vertices):
        p = mw @ v.co
        dz = p.z - (top - 0.085 if style == 'bucket' else top - 0.075)
        trim = 0.0
        if style == 'bucket' and 0.008 < dz < 0.03:
            trim = 1.0  # 띠
        if style == 'straw' and 0.006 < dz < 0.026:
            trim = 1.0  # 리본
        if style == 'cap' and p.y > cy + hy * 0.9 and dz < 0.01:
            trim = 1.0  # 챙
        attr.data[i].color = (trim, 1.0, 1.0 if style == 'straw' else 0.0, 1.0)
    return obj


def glasses(b, name):
    e = b.eyes
    P = world_verts(e)
    cz = P[:, 2].mean()
    fy = P[:, 1].max() + 0.012
    ex = (P[:, 0].max() - P[:, 0].min()) / 2  # 눈 바깥쪽 끝까지 반폭
    lx = ex * 0.52
    bm = bmesh.new()
    for side in (-1, 1):
        c = (side * lx, fy, cz)
        bmesh.ops.create_cube(bm, size=1.0, matrix=mathutils.Matrix.Translation(c) @ mathutils.Matrix.Diagonal((0.048, 0.006, 0.03, 1)))
        # 다리
        hx = (b.head_max[0] - b.head_min[0]) / 2
        bmesh.ops.create_cube(bm, size=1.0, matrix=mathutils.Matrix.Translation((side * (hx + 0.002), fy - 0.06, cz + 0.006))
                              @ mathutils.Matrix.Diagonal((0.004, 0.12, 0.006, 1)))
        bmesh.ops.create_cube(bm, size=1.0, matrix=mathutils.Matrix.Translation((side * (lx + 0.03), fy - 0.002, cz + 0.008))
                              @ mathutils.Matrix.Diagonal((0.016, 0.006, 0.008, 1)))
    bmesh.ops.create_cube(bm, size=1.0, matrix=mathutils.Matrix.Translation((0, fy, cz + 0.008)) @ mathutils.Matrix.Diagonal((0.026, 0.005, 0.005, 1)))
    obj = _head_object(b, name, bm)
    me = obj.data
    attr = me.color_attributes.new('Col', 'FLOAT_COLOR', 'POINT')
    mw = obj.matrix_world
    for i, v in enumerate(me.vertices):
        p = mw @ v.co
        lens = abs(abs(p.x) - lx) < 0.0245 and abs(p.z - cz) < 0.0155 and p.y > fy - 0.004
        attr.data[i].color = (0.0 if lens else 1.0, 1.0, 0.0, 1.0)  # R=1 테
    for p in me.polygons:
        p.use_smooth = False
    return obj


# ---------------------------------------------------------------------------------------------
# 몸 가림 마스크

def erode(mask, nbr, rings=2):
    m = mask.copy()
    for _ in range(rings):
        m = m & np.array([all(m[j] for j in nb) for nb in nbr])
    return m


def bake_cover(b, groups):
    """몸 버텍스마다 '이 옷 묶음 중 무엇을 입어도 덮이는가'를 RGBA로 굽는다 (단 근처 2줄은 빼서 틈이 안 보이게).
    groups: [[이름, ...] x 4] → R, G, B, A. Unity 피부 셰이더가 입은 부위의 채널만 잘라낸다."""
    n = len(b.P)
    col = np.zeros((n, 4))
    for c, names in enumerate(groups):
        m = np.ones(n, bool)
        for name in names:
            m &= SHELLS[name][1]
        col[:, c] = erode(m, b.nbr, 2)
    me = b.obj.data
    for a in list(me.color_attributes):
        me.color_attributes.remove(a)
    attr = me.color_attributes.new('Col', 'FLOAT_COLOR', 'POINT')
    for i in range(n):
        attr.data[i].color = tuple(col[i])
    return col


# ---------------------------------------------------------------------------------------------
# 음영(AO): 버텍스마다 법선 반구로 광선을 쏴서 가까이 막히는 비율 → 버텍스 컬러 G에 곱한다

def _ray_dirs(n=14):
    # 코사인 가중 반구 방향(위 = +Z) 고정 집합
    rng = np.random.default_rng(7)
    d = []
    for k in range(n):
        u, v = (k + 0.5) / n, rng.random()
        r, a = math.sqrt(u), 2 * math.pi * v
        d.append((r * math.cos(a), r * math.sin(a), math.sqrt(max(0.0, 1 - u))))
    return np.array(d)


def bake_ao(obj, body, extra=(), dist=0.12, strength=0.6, samples=14):
    """obj 버텍스 컬러 'Col'.G *= AO. 몸과 obj 자신(+extra)에 가리는 정도를 본다 (휴식 자세)."""
    me = obj.data
    P = world_verts(obj)
    obj.data.update()
    nm = obj.matrix_world.to_3x3().inverted().transposed()
    N = np.array([tuple((nm @ v.normal).normalized()) for v in me.vertices])
    trees = [body.bvh, mathutils.bvhtree.BVHTree.FromPolygons([tuple(p) for p in P], [tuple(p.vertices) for p in me.polygons])]
    for o in extra:
        Q = world_verts(o)
        trees.append(mathutils.bvhtree.BVHTree.FromPolygons([tuple(p) for p in Q], [tuple(p.vertices) for p in o.data.polygons]))
    D = _ray_dirs(samples)
    attr = me.color_attributes['Col']
    # 주머니·버클·안경처럼 각진(flat) 면의 버텍스는 법선이 모서리 대각선이라 옆 면에 막혀 시커메진다 → 건너뜀
    smooth_v = set()
    for p in me.polygons:
        if p.use_smooth:
            smooth_v.update(p.vertices)
    for i in range(len(P)):
        if i not in smooth_v:
            continue
        n = N[i]
        t = np.array([1.0, 0, 0]) if abs(n[0]) < 0.9 else np.array([0, 1.0, 0])
        t = t - n * (t @ n)
        t /= np.linalg.norm(t)
        bt = np.cross(n, t)
        o = mathutils.Vector(P[i] + n * 0.003)
        hit = 0
        for d in D:
            w = mathutils.Vector(t * d[0] + bt * d[1] + n * d[2])
            for tr in trees:
                loc, _, _, _ = tr.ray_cast(o, w, dist)
                if loc is not None:
                    hit += 1
                    break
        ao = 1.0 - strength * hit / len(D)
        c = list(attr.data[i].color)
        c[1] *= ao
        attr.data[i].color = c
