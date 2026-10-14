"""Mixamo FBX를 쪼갤 구간 찾기 (docs/10-animation.md).

  bpyenv/Scripts/python -I tools/blender/mixamo_probe.py <fbx> --out <폴더> [--step 8] [--loop 135-248:30-75]

- <out>/<이름>_sheet.png : 프레임마다 막대 인형(앞·옆), 아래 숫자는 Unity 프레임(0부터)
- <out>/<이름>_curve.txt : Unity 프레임별 움직임 양(엉덩이 기준 관절 이동 합), 오른손 높이·앞뒤
- --loop A-B:MIN-MAX    : Unity 프레임 A~B 안에서 길이 MIN~MAX 프레임인 루프 후보 (시작·끝 포즈·속도 차가 작은 순)
블렌더는 FBX 시작을 1프레임으로 읽으므로(anim_offset) Unity 프레임 = 블렌더 프레임 - 1.
"""
import argparse
import math
import os
import sys

argv = sys.argv[1:]
ap = argparse.ArgumentParser()
ap.add_argument("fbx")
ap.add_argument("--out", required=True)
ap.add_argument("--step", type=int, default=8)
ap.add_argument("--loop", default="")
args = ap.parse_args(argv)
sys.argv = sys.argv[:1]

import bpy  # noqa: E402
import numpy as np  # noqa: E402
from PIL import Image, ImageDraw  # noqa: E402

os.makedirs(args.out, exist_ok=True)
tag = os.path.splitext(os.path.basename(args.fbx))[0].replace("X Bot@", "").replace(" ", "_")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.abspath(args.fbx))
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
f0, f1 = [int(round(x)) for x in arm.animation_data.action.frame_range]
sc = bpy.context.scene
fps = sc.render.fps
bones = arm.pose.bones
names = [b.name for b in bones]
B = len(names)


def idx(suffix):
    return next(i for i, n in enumerate(names) if n.endswith(suffix))


hip, rh = idx("Hips"), idx("RightHand")
P = []
for f in range(f0, f1 + 1):
    sc.frame_set(f)
    mw = arm.matrix_world
    P.append([list(mw @ b.head) for b in bones] + [list(mw @ b.tail) for b in bones])
P = np.array(P)  # 프레임 x (머리 B + 꼬리 B) x 3
rel = P - P[:, hip:hip + 1, :]
vel = np.linalg.norm(np.diff(rel, axis=0), axis=2).sum(axis=1)
vel = np.concatenate([[vel[0]], vel])
n = len(P)
print(f"[{tag}] Unity 0..{n - 1} ({(n - 1) / fps:.2f}s, {fps}fps)")

with open(os.path.join(args.out, tag + "_curve.txt"), "w", encoding="utf-8") as fp:
    fp.write("unity\tmotion\thandR_up\thandR_fwd\n")
    for i in range(n):
        fp.write(f"{i}\t{vel[i]:.3f}\t{P[i, rh, 2]:.3f}\t{-P[i, rh, 1]:.3f}\n")

# 막대 인형 시트: 칸마다 앞(X-Z)·옆(Y-Z)
sel = list(range(0, n, args.step))
cell, cols = 150, 8
rows = math.ceil(len(sel) / cols)
img = Image.new("RGB", (cols * cell * 2, rows * (cell + 16)), "white")
d = ImageDraw.Draw(img)
lo, hi = P.reshape(-1, 3).min(axis=0), P.reshape(-1, 3).max(axis=0)
span = max(hi - lo) * 1.05
for k, i in enumerate(sel):
    cx, cy = (k % cols) * cell * 2, (k // cols) * (cell + 16)
    for view, (a, b) in enumerate([(0, 2), (1, 2)]):
        ox = cx + view * cell

        def pt(p):
            return (ox + (p[a] - lo[a]) / span * (cell - 10) + 5, cy + cell - (p[b] - lo[b]) / span * (cell - 10) - 5)

        for j in range(B):
            col = "#d33" if "Right" in names[j] else "#33d" if "Left" in names[j] else "#222"
            d.line([pt(P[i, j]), pt(P[i, B + j])], fill=col, width=2)
        d.rectangle([ox, cy, ox + cell - 1, cy + cell - 1], outline="#ccc")
    d.text((cx + 4, cy + cell + 2), f"{i}  ({i / fps:.1f}s)", fill="black")
img.save(os.path.join(args.out, tag + "_sheet.png"))

if args.loop:
    rng, lens = args.loop.split(":")
    a0, a1 = map(int, rng.split("-"))
    lmin, lmax = map(int, lens.split("-"))
    tails = rel[:, B:, :]
    cands = []
    for s in range(a0, a1 - lmin):
        for e in range(s + lmin, min(a1, s + lmax) + 1):
            pose = np.linalg.norm(tails[s] - tails[e], axis=1).mean()
            speed = np.linalg.norm((tails[s + 1] - tails[s]) - (tails[e + 1] - tails[e]), axis=1).mean() if e + 1 < n else 9
            cands.append((pose + 3 * speed, s, e, pose))
    cands.sort()
    for score, s, e, pose in cands[:8]:
        print(f"  루프 {s}-{e} ({(e - s) / fps:.2f}s) 포즈 차 {pose * 100:.1f}cm, 점수 {score * 100:.2f}")

sys.stdout.flush()
os._exit(0)
