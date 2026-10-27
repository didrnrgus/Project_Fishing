"""
해외 낚시터 소품 (docs/09-art-plan.md 6장). 길이는 실제 미터, 위 = +Z(Blender) → Unity +Y, 앞 = -Y(Blender) → Unity +Z.

  bpyenv/Scripts/python -I tools/blender/build_props.py                 # 모두
  bpyenv/Scripts/python -I tools/blender/build_props.py --only palm_a --preview Screenshots/props

결과: Assets/Resources/Props/<id>.fbx (정적 모델, 머티리얼은 FBX 색 그대로 — AssetImportRules의 Junk·Kenney와 같은 규칙)
  palm_a, palm_b   야자수 (하롱베이 · 나일강). 줄기가 휘고 잎 9~11장, 코코넛 송이
  felucca          나일강 펠루카: 흰 나무배 + 비스듬한 긴 활대에 큰 삼각돛
  junk             하롱베이 정크선: 갈색 나무배, 높은 고물, 대나무 살을 댄 주황 돛 세 장
  (한강 둔치 텐트·그늘막은 props_camp.py)
  (방파제·호안 계선주·방충재·인명구조함은 props_harbor.py)
  (새 해외 맵 소품 — 대불·머라이언·독수리·롱테일 보트·열기구·파라솔·망루·사자상·난파선은 props_sea.py)
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_icons as B  # noqa: E402  (bpy 도구: mat, cyl, box, sphere, tube, join, obj_from_bm, scene_setup, render)
import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector as V, Matrix  # noqa: E402

ROOT = B.ROOT
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Props')
ARGS = B.ARGS
mat, cyl, box, sphere, tube, join = B.mat, B.cyl, B.box, B.sphere, B.tube, B.join


def frond(base, yaw, pitch, length, m, seed):
    """야자 잎 한 장: 활처럼 휘는 잎줄기 + 양옆으로 늘어진 잔잎 띠 (끝이 톱니처럼 갈라진다)."""
    bm = bmesh.new()
    n = 14
    d = V((math.cos(yaw), math.sin(yaw), 0))
    side = V((-math.sin(yaw), math.cos(yaw), 0))
    pts = []
    for i in range(n + 1):
        t = i / n
        # 위로 뻗다가 끝이 아래로 처진다
        ang = pitch - t * t * 1.6
        horiz = length * t
        z = length * (math.sin(pitch) * t - 0.55 * t * t * t)
        pts.append(base + d * horiz * math.cos(pitch * 0.4) + V((0, 0, z)))
    rows = []
    for i, p in enumerate(pts):
        t = i / n
        w = length * 0.2 * math.sin(math.pi * min(1.0, t * 1.15)) * (1.0 - 0.15 * t)
        droop = -w * 0.55
        jag = 0.12 * w * (1 if (i + seed) % 2 else -1)
        rows.append([bm.verts.new(p + side * (w + jag) + V((0, 0, droop))), bm.verts.new(p + V((0, 0, 0.02))),
                     bm.verts.new(p - side * (w - jag) + V((0, 0, droop)))])
    for i in range(n):
        a, b = rows[i], rows[i + 1]
        bm.faces.new((a[0], a[1], b[1], b[0]))
        bm.faces.new((a[1], a[2], b[2], b[1]))
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.02)
    return B.obj_from_bm(bm, 'Frond', m)


def palm(pid):
    tall = 8.5 if pid == 'palm_a' else 6.5
    lean = 0.9 if pid == 'palm_a' else 1.6
    bark = mat('#8a6e4c', 0.85)
    ring = mat('#6a5238', 0.9)
    leaf = mat('#3e7a2a', 0.6)
    leaf2 = mat('#5a8e30', 0.6)
    nut = mat('#5a4020', 0.5)
    objs = []
    pts = [V((lean * (t ** 1.8), 0, tall * t)) for t in [i / 12 for i in range(13)]]
    objs.append(tube(pts, lambda u: 0.24 - 0.1 * u, bark, sides=10, name='Trunk'))
    for i in range(1, 12):
        p = pts[i]
        objs.append(B.torus(0.23 - 0.1 * i / 12, 0.025, ring, loc=tuple(p), seg=12, mseg=5))
    top = pts[-1]
    count = 10 if pid == 'palm_a' else 9
    for k in range(count):
        yaw = 2 * math.pi * k / count + 0.3 * math.sin(k * 1.7)
        pitch = 0.35 + 0.35 * ((k * 37) % 5) / 5
        objs.append(frond(top + V((0, 0, 0.1)), yaw, pitch, 3.6 + 0.6 * ((k * 13) % 3) / 2, leaf if k % 2 else leaf2, k))
    for k in range(5):
        a = 2 * math.pi * k / 5
        objs.append(sphere(0.14, nut, loc=tuple(top + V((math.cos(a) * 0.25, math.sin(a) * 0.25, -0.25)))))
    return objs


def felucca(_):
    hull_m, trim, wood, sail = mat('#f2efe6', 0.55), mat('#2a6aa8', 0.5), mat('#7a5a3a', 0.7), mat('#f6f2e8', 0.75)
    objs = []
    L, W = 7.0, 1.9
    bm = bmesh.new()
    prof = [(-0.5, 0.15), (-0.4, 0.75), (-0.1, 1.0), (0.25, 0.95), (0.45, 0.6), (0.5, 0.05)]
    rings = []
    for y, w in prof:
        hw = W * 0.5 * w
        rise = 0.25 * (abs(y) / 0.5) ** 3
        rings.append([bm.verts.new((-hw, y * L, 0.75 + rise)), bm.verts.new((-hw * 0.85, y * L, 0.1)), bm.verts.new((0, y * L, -0.12)),
                      bm.verts.new((hw * 0.85, y * L, 0.1)), bm.verts.new((hw, y * L, 0.75 + rise))])
    for i in range(len(rings) - 1):
        for k in range(4):
            bm.faces.new((rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]))
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.06)
    objs.append(B.obj_from_bm(bm, 'Hull', hull_m))
    objs.append(box((W * 0.98, L * 0.86, 0.12), trim, loc=(0, 0, 0.6)))
    objs.append(box((W * 0.8, L * 0.7, 0.05), wood, loc=(0, 0, 0.32)))
    for y in (-1.5, 0.6):
        objs.append(box((W * 0.85, 0.3, 0.06), wood, loc=(0, y, 0.55)))
    # 돛대 + 활대 (앞쪽이 낮고 뒤쪽이 높은 긴 활대)
    mast_y = -0.9
    objs.append(cyl(0.07, 0.05, 6.0, wood, loc=(0, mast_y, 3.3)))
    a, b = V((0, mast_y - 2.6, 1.6)), V((0, mast_y + 3.6, 8.6))
    objs.append(tube([a, b], 0.05, wood, r_end=0.03, sides=8))
    # 큰 삼각돛 (활대 아래로 늘어진 곡면)
    bm = bmesh.new()
    foot = V((0, mast_y + 3.0, 1.1))
    nu = 10
    top_row = [bm.verts.new(a + (b - a) * (i / nu)) for i in range(nu + 1)]
    mid = []
    for i in range(nu + 1):
        t = i / nu
        p = (a + (b - a) * t) * 0.5 + (a.lerp(foot, t)) * 0.5
        mid.append(bm.verts.new(p + V((0.45 * math.sin(math.pi * t), 0, 0))))
    bot = [bm.verts.new(a.lerp(foot, i / nu)) for i in range(nu + 1)]
    for i in range(nu):
        bm.faces.new((top_row[i], top_row[i + 1], mid[i + 1], mid[i]))
        bm.faces.new((mid[i], mid[i + 1], bot[i + 1], bot[i]))
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.02)
    objs.append(B.obj_from_bm(bm, 'Sail', sail))
    return objs


def junk(_):
    hull_m, dark, sail_m, batten = mat('#6a4428', 0.7), mat('#3a2616', 0.8), mat('#d8622a', 0.8), mat('#4a3420', 0.7)
    objs = []
    L, W = 12.0, 3.4
    bm = bmesh.new()
    prof = [(-0.5, 0.55), (-0.35, 0.95), (0.0, 1.0), (0.3, 0.9), (0.5, 0.35)]
    rings = []
    for y, w in prof:
        hw = W * 0.5 * w
        rise = 1.4 * max(0.0, -y - 0.25) / 0.25 + 0.5 * max(0.0, y - 0.3) / 0.2
        rings.append([bm.verts.new((-hw, y * L, 1.4 + rise)), bm.verts.new((-hw * 0.9, y * L, 0.2)), bm.verts.new((0, y * L, -0.4)),
                      bm.verts.new((hw * 0.9, y * L, 0.2)), bm.verts.new((hw, y * L, 1.4 + rise))])
    for i in range(len(rings) - 1):
        for k in range(4):
            bm.faces.new((rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]))
    bm.faces.new(rings[0][::-1])
    bm.faces.new(rings[-1])
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.1)
    objs.append(B.obj_from_bm(bm, 'Hull', hull_m))
    objs.append(box((W * 0.9, L * 0.82, 0.1), dark, loc=(0, 0, 1.15)))
    objs.append(box((W * 0.75, 3.0, 1.6), hull_m, loc=(0, -4.2, 2.0), bevel=0.05))     # 고물 선실
    objs.append(box((W * 0.85, 3.4, 0.12), dark, loc=(0, -4.2, 2.85)))
    # 돛 세 장 (대나무 살)
    for (y, h, w) in ((-1.6, 9.0, 4.2), (2.0, 7.5, 3.6), (4.8, 5.5, 2.6)):
        objs.append(cyl(0.09, 0.06, h + 0.6, dark, loc=(0, y, 1.2 + (h + 0.6) / 2)))
        objs.append(box((0.06, w, h * 0.82), sail_m, loc=(0.12, y + w * 0.32, 1.9 + h * 0.48), bevel=0.0))
        for k in range(6):
            zz = 1.9 + h * 0.07 + k * h * 0.82 / 5.5
            objs.append(cyl(0.035, 0.035, w + 0.2, batten, loc=(0.16, y + w * 0.32, zz), rot=(math.pi / 2, 0, 0), seg=6))
    return objs


PROPS = {'palm_a': palm, 'palm_b': palm, 'felucca': felucca, 'junk': junk}
import props_sea  # noqa: E402
PROPS.update(props_sea.PROPS)
import sculpt  # noqa: E402  (석상·난파선을 한 덩어리 조각으로 다시 만든 판 — 같은 id를 덮어쓴다)
PROPS.update(sculpt.PROPS)
import boats  # noqa: E402  (배: 한국 연안 어선·모터보트·고무보트·나무배·카이크·요트·딩기)
PROPS.update(boats.PROPS)
import props_camp  # noqa: E402  (한강 둔치: 원터치 돔 텐트·그늘막)
PROPS.update(props_camp.PROPS)
import props_harbor  # noqa: E402  (한국 포구 방파제·호안: 계선주·쌍계주·타이어/고무 방충재·인명구조함)
PROPS.update(props_harbor.PROPS)


def export(pid, objs):
    o = join(objs, pid) if len(objs) > 1 else objs[0]
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    # 기준점을 원점(바닥 가운데)으로: join은 첫 물체의 원점을 이어받아서, 파라솔은 기둥 가운데(1.4m 위)가 기준이라 땅에 묻혔다
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    os.makedirs(OUT, exist_ok=True)
    out = os.path.join(OUT, pid + '.fbx')
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'MESH'}, axis_forward='-Y', axis_up='Z',
                             apply_scale_options='FBX_SCALE_UNITS', mesh_smooth_type='FACE', bake_anim=False)
    return o, out


def main():
    only = B.arg('--only')
    want = set(only.split(',')) if only else None
    prev = B.arg('--preview')
    prev3 = B.arg('--preview3')   # 앞·3/4·옆 세 장 (원근)
    for pid, fn in PROPS.items():
        if want and pid not in want:
            continue
        cam = B.scene_setup()
        objs = fn(pid)
        o, out = export(pid, objs)
        print(f'[props] {pid} → {os.path.relpath(out, ROOT)}', flush=True)
        if prev:
            d = prev if os.path.isabs(prev) else os.path.join(ROOT, prev)
            os.makedirs(d, exist_ok=True)
            B.frame(cam, [o])
            B.render(os.path.join(d, pid + '.png'))
        if prev3:
            d = prev3 if os.path.isabs(prev3) else os.path.join(ROOT, prev3)
            os.makedirs(d, exist_ok=True)
            sculpt.preview3([o], os.path.join(d, pid + '_3.png'))


if __name__ == '__main__':
    main()
    sys.stdout.flush()
    os._exit(0)
