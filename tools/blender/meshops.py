"""Mesh helpers working in armature rest space (world coords, rest pose)."""
import bpy, bmesh, mathutils, numpy as np
from mathutils.bvhtree import BVHTree

def world_verts(obj):
    mw = obj.matrix_world
    return np.array([tuple(mw @ v.co) for v in obj.data.vertices])

def set_world_verts(obj, P):
    inv = obj.matrix_world.inverted()
    for v, p in zip(obj.data.vertices, P):
        v.co = inv @ mathutils.Vector(p)
    obj.data.update()

def weights(obj):
    """list of dict bone->weight per vertex"""
    names = {g.index: g.name for g in obj.vertex_groups}
    out = []
    for v in obj.data.vertices:
        d = {}
        for g in v.groups:
            if g.weight > 1e-4: d[names[g.group]] = g.weight
        s = sum(d.values()) or 1.0
        out.append({k: w / s for k, w in d.items()})
    return out

def bone_segments(arm):
    mw = arm.matrix_world
    return {b.name: (np.array(mw @ b.head_local), np.array(mw @ b.tail_local)) for b in arm.data.bones}

def closest_on_seg(p, a, b):
    ab = b - a; t = np.clip(np.dot(p - a, ab) / max(np.dot(ab, ab), 1e-9), 0, 1)
    return a + ab * t, t

def neighbors(obj):
    n = [set() for _ in obj.data.vertices]
    for e in obj.data.edges:
        a, b = e.vertices; n[a].add(b); n[b].add(a)
    return [list(s) for s in n]

def smooth(P, nbr, mask, iters=10, lam=0.5, mu=-0.53):
    """Taubin smoothing; mask in [0,1] per vertex scales the step."""
    P = P.copy(); m = np.asarray(mask)[:, None]
    for it in range(iters):
        for f in (lam, mu):
            avg = np.array([P[nb].mean(axis=0) if nb else P[i] for i, nb in enumerate(nbr)])
            P = P + f * m * (avg - P)
    return P

def vertex_normals_world(obj):
    obj.data.update()
    nm = obj.matrix_world.to_3x3().inverted().transposed()
    return np.array([tuple((nm @ v.normal).normalized()) for v in obj.data.vertices])

def bvh_world(obj):
    """월드 좌표 BVH. 면 법선 방향이 버텍스 법선과 같도록 다각형 순서를 그대로 쓴다."""
    P = world_verts(obj)
    polys = [tuple(p.vertices) for p in obj.data.polygons]
    return BVHTree.FromPolygons([tuple(v) for v in P], polys)
