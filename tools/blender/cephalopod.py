"""
두족류(주꾸미·갑오징어) 모델. build_fish.py가 type 'O'(문어형)·'Q'(갑오징어형)일 때 부른다.

좌표는 물고기와 같다: 길이 1m, 다리 끝(앞) y = -0.5, 몸통(외투막) 끝 y = +0.5, 위 +Z.
  - Body: 외투막 + 머리 + 눈 + 다리 8개(+ 갑오징어 촉완 2개)
  - Fins: 갑오징어 외투막 둘레의 지느러미 치마 (주꾸미는 없음 → 아주 작은 판 하나)
UV는 물고기 몸과 같은 원통 투영(u = 길이, v = 배 0 ~ 등 1)이라 fish_tex의 몸 무늬가 그대로 입혀진다.
다리마다 뼈 3개(Arm{i}_{0..2})를 따로 두고, 몸통 사슬(Head·Spine·Tail)과 함께 애니메이션한다.
"""
import math

import bmesh
import bpy
import mathutils

V = mathutils.Vector


def body_uv(p):
    u = min(0.749, max(0.0, (p.y + 0.5) * 0.75))
    v = abs(math.atan2(p.x, -p.z)) / math.pi
    return (u, v)


def ellipsoid(bm, uvl, center, radii, seg=28, rings=16):
    new = bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    verts = new['verts']
    for vv in verts:
        vv.co = V((vv.co.x * radii[0] + center[0], vv.co.y * radii[1] + center[1], vv.co.z * radii[2] + center[2]))
    faces = {f for vv in verts for f in vv.link_faces}
    for f in faces:
        for l in f.loops:
            l[uvl].uv = body_uv(l.vert.co)


def eye(bm, uvl, center, r, side):
    new = bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=9, radius=r)
    verts = new['verts']
    faces = {f for vv in verts for f in vv.link_faces}
    for f in faces:
        for l in f.loops:
            p = l.vert.co
            l[uvl].uv = (0.75 + 0.125 * (0.5 + p.y / (2 * r) * 0.98), 0.25 + 0.25 * (0.5 + p.z / (2 * r) * 0.98))
    for vv in verts:
        vv.co += V(center)


def arm_tube(bm, uvl, path, r0, r1, sides=8):
    """다리 하나 (관). 버텍스 리스트를 돌려준다 (가중치용)."""
    rings = []
    n = len(path)
    prev = None
    for i, p in enumerate(path):
        t = (path[min(i + 1, n - 1)] - path[max(i - 1, 0)]).normalized()
        a = (prev - t * prev.dot(t)).normalized() if prev is not None else t.orthogonal().normalized()
        prev = a
        b = t.cross(a)
        r = r0 + (r1 - r0) * (i / (n - 1)) ** 0.8
        rings.append([bm.verts.new(p + (a * math.cos(2 * math.pi * k / sides) + b * math.sin(2 * math.pi * k / sides)) * r) for k in range(sides)])
    for i in range(n - 1):
        for k in range(sides):
            f = bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k]))
            for l in f.loops:
                l[uvl].uv = body_uv(l.vert.co)
    tip = bm.verts.new(path[-1] + (path[-1] - path[-2]).normalized() * r1)
    for k in range(sides):
        f = bm.faces.new((rings[-1][k], rings[-1][(k + 1) % sides], tip))
        for l in f.loops:
            l[uvl].uv = body_uv(l.vert.co)
    return [v for ring in rings for v in ring] + [tip]


def arm_paths(d):
    """다리 경로: (점 리스트, 굵기 시작, 끝). 주꾸미는 길고 고르게, 갑오징어는 짧은 다리 8 + 긴 촉완 2."""
    out = []
    octo = d['type'] == 'O'
    base_y = -0.06 if octo else -0.2
    ring_r = 0.05 if octo else 0.04
    for i in range(8):
        a = (i + 0.5) * 2 * math.pi / 8
        radial = V((math.cos(a), 0, math.sin(a)))
        base = V((0, base_y, 0.01)) + radial * ring_r
        L = 0.43 if octo else 0.2 + 0.03 * (i % 2)
        pts = []
        for k in range(17):
            t = k / 16
            curl = 0.035 * math.sin(t * math.pi * 1.5 + i)
            p = base + V((0, -L * t, 0)) + radial * (0.07 * t + 0.03 * math.sin(t * math.pi)) + V((0, 0, curl))
            pts.append(p)
        out.append((pts, 0.03 if octo else 0.022, 0.004 if octo else 0.006))
    if not octo:
        # 촉완 2개: 가늘고 길게, 끝이 넓적
        for s in (-1, 1):
            base = V((s * 0.02, base_y, -0.01))
            pts = [base + V((s * 0.03 * t, -0.29 * t, -0.03 * math.sin(t * math.pi))) for t in [k / 16 for k in range(17)]]
            out.append((pts, 0.008, 0.012))
    return out


def build(d):
    """Body, Fins 오브젝트와 다리 경로(뼈용)를 만든다."""
    octo = d['type'] == 'O'
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UV')
    if octo:
        ellipsoid(bm, uvl, (0, 0.27, 0.035), (0.12, 0.23, 0.13))     # 외투막 (머리 뒤로 늘어진 주머니)
        ellipsoid(bm, uvl, (0, 0.0, 0.0), (0.1, 0.1, 0.09))          # 머리
        eye_c, eye_r = (0.082, -0.01, 0.045), 0.03
    else:
        ellipsoid(bm, uvl, (0, 0.18, 0.0), (0.13, 0.31, 0.095), seg=32)  # 납작한 외투막 (갑)
        ellipsoid(bm, uvl, (0, -0.15, 0.0), (0.085, 0.07, 0.065))        # 머리
        eye_c, eye_r = (0.07, -0.15, 0.03), 0.032
    for s in (1, -1):
        eye(bm, uvl, (s * eye_c[0], eye_c[1], eye_c[2]), eye_r, s)
    paths = arm_paths(d)
    arm_verts = []
    for pts, r0, r1 in paths:
        arm_verts.append(arm_tube(bm, uvl, pts, r0, r1))
    me = bpy.data.meshes.new('Body')
    # 다리 버텍스 인덱스를 기억해 두고 메쉬로
    bm.verts.index_update()
    arm_idx = [[v.index for v in vs] for vs in arm_verts]
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    body = bpy.data.objects.new('Body', me)
    bpy.context.scene.collection.objects.link(body)

    # 지느러미: 갑오징어 치마 / 주꾸미는 외투막 끝의 아주 작은 판(지느러미가 없지만 머티리얼 슬롯을 맞추려고)
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UV')
    if octo:
        segs = [((0, 0.47, 0.03), (0, 0.5, 0.03))]
        width = 0.01
    else:
        segs = None
    rows = []
    nu, nv = 24, 3
    for side in ((1, -1) if not octo else (1,)):
        grid = []
        for j in range(nv + 1):
            row = []
            for i in range(nu + 1):
                t = i / nu
                if octo:
                    y = 0.47 + 0.03 * t
                    p = V((side * 0.005 * j, y, 0.03))
                else:
                    y = -0.08 + 0.57 * t
                    # 외투막 가장자리 x (타원)
                    ex = 0.13 * math.sqrt(max(0.0, 1 - ((y - 0.18) / 0.31) ** 2))
                    w = 0.04 * math.sin(math.pi * min(1.0, t * 1.1)) ** 0.5 + 0.004
                    p = V((side * (ex - 0.004 + w * j / nv), y, 0.004 * math.sin(t * 20) * j / nv))
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
    return body, fins, paths, arm_idx


def add_arm_bones(arm_obj, paths):
    """다리마다 뼈 3개 (머리에 붙는다)."""
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_obj.data.edit_bones
    head = eb['Head']
    names = []
    for i, (pts, _, _) in enumerate(paths):
        prev = head
        idx = [0, 5, 10, 16]
        for j in range(3):
            b = eb.new(f'Arm{i}_{j}')
            b.head = pts[idx[j]]
            b.tail = pts[idx[j + 1]]
            b.parent = prev
            b.use_connect = j > 0
            prev = b
            names.append(b.name)
    bpy.ops.object.mode_set(mode='OBJECT')
    return names


def weight_arms(body, paths, arm_idx):
    """다리 버텍스는 다리 뼈에만 (경로를 따라 이웃 뼈와 섞는다)."""
    me = body.data
    all_arm = [vi for idx in arm_idx for vi in idx]
    for g in body.vertex_groups:
        if not g.name.startswith('Arm'):
            g.remove(all_arm)   # 몸통 사슬 가중치는 빼고 다리 뼈에만
    for i, idx in enumerate(arm_idx):
        pts = paths[i][0]
        cuts = [0, 5, 10, 16]
        centers = [(pts[cuts[j]] + pts[cuts[j + 1]]) / 2 for j in range(3)]
        groups = [body.vertex_groups.get(f'Arm{i}_{j}') or body.vertex_groups.new(name=f'Arm{i}_{j}') for j in range(3)]
        for vi in idx:
            co = me.vertices[vi].co
            # 가장 가까운 경로 점의 비율
            k = min(range(len(pts)), key=lambda q: (pts[q] - co).length_squared)
            t = k / (len(pts) - 1) * 3 - 0.5
            j0 = max(0, min(2, int(math.floor(t))))
            f = max(0.0, min(1.0, t - j0))
            j1 = min(2, j0 + 1)
            groups[j0].add([vi], 1 - f if j1 != j0 else 1.0, 'REPLACE')
            if j1 != j0:
                groups[j1].add([vi], f, 'REPLACE')
