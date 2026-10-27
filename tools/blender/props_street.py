"""
해안도로 소품 (2026-10-27, CoastProps.Streetlight · CoastMap 해안도로). build_props.py가 불러 PROPS에 더한다.
길이는 실제 미터, 위 = +Z, 앞 = -Y (Unity +Z). 원점은 바닥 가운데.

  street_lamp       해안 가로등 (8m): 아연 도금 회색 팔각 기둥(아래 굵고 위로 가늘게), 밑동 받침 상자, 위에서 앞(-Y)으로 휘어 나온 팔,
                    끝에 납작한 LED 등기구(아래 발광면은 따로 'Lens' 재질 — C#이 밤에 빛나게 바꾼다)
  street_guardrail  W형 가드레일 4m 한 칸: 기둥 둘(H형), 간격재, 물결 단면 강판 (+X 방향으로 이어 붙인다)
"""
import math

import bmesh
import build_icons as B
from mathutils import Vector as V

mat, cyl, box, tube = B.mat, B.cyl, B.box, B.tube


def lamp(_):
    zinc = mat('#9a9fa4', 0.45, 0.7)
    dark = mat('#4a4e52', 0.5, 0.6)
    lens = mat('#f4f0e2', 0.2, name='Lens')
    h = 8.0
    objs = []
    # 팔각 기둥: 아래 0.11 → 위 0.06
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=8, radius1=0.11, radius2=0.06, depth=h)
    ob = B.obj_from_bm(bm, 'Pole', zinc)
    for p in ob.data.polygons:
        p.use_smooth = False
    ob.location = (0, 0, h / 2)
    objs.append(ob)
    objs.append(box((0.32, 0.32, 0.6), zinc, loc=(0, 0, 0.3), bevel=0.02))          # 밑동 받침
    objs.append(box((0.14, 0.04, 0.3), dark, loc=(0, -0.17, 0.35)))                   # 점검 문
    objs.append(box((0.4, 0.4, 0.05), dark, loc=(0, 0, 0.025)))                       # 밑판
    # 휘어 나온 팔 (위로 솟았다가 앞으로)
    pts = [(0, 0, h - 0.4), (0, 0, h - 0.05), (0, -0.25, h + 0.25), (0, -0.8, h + 0.42), (0, -1.6, h + 0.5)]
    objs.append(tube(pts, 0.045, zinc, r_end=0.035, sides=10, name='Arm'))
    # LED 등기구: 납작한 상자 + 아래 발광면
    head = box((0.28, 0.62, 0.1), dark, loc=(0, -1.85, h + 0.48), rot=(-0.08, 0, 0), bevel=0.02)
    objs.append(head)
    objs.append(box((0.22, 0.5, 0.012), lens, loc=(0, -1.87, h + 0.42), rot=(-0.08, 0, 0)))
    return objs


def guardrail(_):
    steel = mat('#b8bcc0', 0.35, 0.8)
    post = mat('#8e9398', 0.45, 0.7)
    L = 4.0
    objs = []
    # 물결(W) 단면 강판: 높이 0.31, 두 골
    bm = bmesh.new()
    prof = []
    n = 16
    for i in range(n + 1):
        t = i / n
        z = 0.45 + t * 0.31
        y = -0.02 * math.cos(t * math.pi * 4) - 0.03
        prof.append((y, z))
    rows = []
    for x in (-L / 2 - 0.15, L / 2 + 0.15):
        rows.append([bm.verts.new((x, y, z)) for (y, z) in prof])
    for i in range(n):
        bm.faces.new((rows[0][i], rows[1][i], rows[1][i + 1], rows[0][i + 1]))
    bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.006)
    objs.append(B.obj_from_bm(bm, 'Beam', steel))
    for x in (-L / 2 + 0.1, L / 2 - 0.1):
        objs.append(box((0.1, 0.1, 0.8), post, loc=(x, 0.12, 0.4)))              # 기둥 (레일 윗선까지)
        objs.append(box((0.08, 0.14, 0.18), post, loc=(x, 0.03, 0.6)))           # 간격재
        objs.append(cyl(0.02, 0.02, 0.05, post, loc=(x, -0.06, 0.6), rot=(math.pi / 2, 0, 0), seg=6))  # 볼트
    return objs


PROPS = {
    'street_lamp': lamp,
    'street_guardrail': guardrail,
}
