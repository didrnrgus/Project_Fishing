"""반응 동작(환호·박수·펄쩍·손짓·움찔)을 UAL1 뼈대 위에 만들어 FBX로 내보낸다 (docs/10-animation.md).

  bpyenv/Scripts/python -I tools/blender/build_reactions.py [--preview <폴더>]

- 바탕은 Quaternius UAL1의 Idle_Loop(숨쉬기·무게 이동). 그 위에 손·발 자리를 시간에 따라 정하고
  두 관절 IK(어깨-팔꿈치-손목, 엉덩이-무릎-발목)로 팔다리를 맞춘 뒤 프레임마다 키로 굽는다.
- 출력: Assets/Resources/Animations/Reactions.fbx (뼈대만, 클립 = Cheer_ArmsUp · Clap_Loop · Excited · Wave_Call · Scared)
- 예전 Mixamo Cheering·Clapping·Excited·Rallying·Scared를 대신한다. 직접 만든 동작이라 프로젝트 라이선스를 따른다.
- --preview: 마네킹으로 클립마다 앞·옆 6프레임씩 찍은 시트(<폴더>/<클립>.png)
블렌더 축: 캐릭터는 +Y를 보고, 오른쪽 +X, 위 +Z (UAL1을 읽으면 아마추어가 Z로 180° 돌아 있다).
"""
import argparse
import math
import os
import sys

argv = sys.argv[1:]
ap = argparse.ArgumentParser()
ap.add_argument("--preview", default="")
args = ap.parse_args(argv)
sys.argv = sys.argv[:1]

import bpy  # noqa: E402
from mathutils import Matrix, Quaternion, Vector  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "Assets", "Resources", "Animations", "UAL1_Standard.fbx")
OUT = os.path.join(ROOT, "Assets", "Resources", "Animations", "Reactions.fbx")
IDLE_LEN = 75  # UAL1 Idle_Loop 길이 (프레임 1~76)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
mesh = next((o for o in bpy.data.objects if o.type == 'MESH'), None)
idle = next(a for a in bpy.data.actions if a.name.endswith("|Idle_Loop"))
scene = bpy.context.scene
scene.render.fps = 30
bpy.context.view_layer.objects.active = arm
P = arm.pose.bones

# 아마추어 공간 기준 방향 (월드 +Y 앞, +X 오른쪽, +Z 위를 아마추어 공간으로)
MWi = arm.matrix_world.to_3x3().inverted()
FWD = (MWi @ Vector((0, 1, 0))).normalized()
RIGHT = (MWi @ Vector((1, 0, 0))).normalized()
UP = (MWi @ Vector((0, 0, 1))).normalized()


def update():
    bpy.context.view_layer.update()


def smooth(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def ramp(f, a, b):
    """a~b 프레임에서 0→1 (부드럽게)."""
    return smooth((f - a) / max(1e-6, b - a))


def envelope(f, length, fade_in, fade_out):
    return min(ramp(f, 0, fade_in), 1.0 - ramp(f, length - fade_out, length))


def rotate(pb, q, pivot):
    m = pb.matrix.copy()
    r = q.to_matrix().to_4x4()
    pb.matrix = Matrix.Translation(pivot) @ r @ Matrix.Translation(-pivot) @ m
    update()


def ik(upper, lower, end, target, pole, w):
    """두 관절 IK: end의 머리를 target 쪽으로 (w 만큼), 가운데 관절은 pole 쪽으로 굽힌다."""
    if w <= 1e-4:
        return
    a, b, c = P[upper].head.copy(), P[lower].head.copy(), P[end].head.copy()
    t = c.lerp(target, w)
    l1, l2 = (b - a).length, (c - b).length
    to = t - a
    d = min(max(to.length, 1e-4), (l1 + l2) * 0.999)
    u = to.normalized()
    # 굽는 쪽: 지금 팔꿈치(무릎) 쪽에서 pole 쪽으로 w만큼 (w가 작을 때 튀지 않게)
    cur = (b - a) - u * u.dot(b - a)
    want = pole - u * u.dot(pole)
    n = cur.normalized().lerp(want.normalized(), w) if cur.length > 1e-5 else want
    n.normalize()
    ca = max(-1.0, min(1.0, (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d)))
    b2 = a + u * (l1 * ca) + n * (l1 * math.sqrt(1 - ca * ca))
    rotate(P[upper], (b - a).rotation_difference(b2 - a), a)
    b, c = P[lower].head.copy(), P[end].head.copy()
    rotate(P[lower], (c - b).rotation_difference(a + u * d - b), b)


def hand_frame(side):
    """손가락 방향, 손바닥이 보는 방향 (아마추어 공간)."""
    h = P["hand_" + side].head
    f = (P["middle_01_" + side].head - h).normalized()
    across = P["index_01_" + side].head - P["pinky_01_" + side].head  # 엄지 쪽
    n = across.cross(f) if side == "r" else f.cross(across)
    return f, n.normalized()


def orient_hand(side, fingers, palm, w):
    """손가락을 fingers, 손바닥을 palm 쪽으로 (w 만큼)."""
    if w <= 1e-4:
        return
    f, n = hand_frame(side)
    n = (n - f * f.dot(n)).normalized()
    fd = fingers.normalized()
    nd = (palm - fd * fd.dot(palm)).normalized()
    b1 = Matrix((f, n, f.cross(n))).transposed()
    b2 = Matrix((fd, nd, fd.cross(nd))).transposed()
    q = (b2 @ b1.transposed()).to_quaternion()
    rotate(P["hand_" + side], Quaternion().slerp(q, w), P["hand_" + side].head.copy())


def curl_fingers(side, amount):
    """주먹 쥐기: 손가락 마디를 손바닥 쪽으로 감는다 (0~1)."""
    if amount <= 1e-4:
        return
    for name in ("index", "middle", "ring", "pinky"):
        for k, deg in ((1, 70), (2, 85), (3, 60)):
            pb = P[f"{name}_0{k}_{side}"]
            f, n = hand_frame(side)
            axis = f.cross(n)  # 손가락을 손바닥 쪽으로 감는 축
            rotate(pb, Quaternion(axis, math.radians(deg * amount) * (1 if side == "r" else -1) * -1), pb.head.copy())
    pb = P[f"thumb_02_{side}"]
    f, n = hand_frame(side)
    rotate(pb, Quaternion(f, math.radians(25 * amount) * (1 if side == "r" else -1)), pb.head.copy())


def move_pelvis(offset):
    pb = P["pelvis"]
    m = pb.matrix.copy()
    m.translation = m.translation + offset
    pb.matrix = m
    update()


def lean(deg, axis=None):
    """허리(spine_01)를 앞(+)·뒤(-)로 숙이거나 젖힌다. axis=FWD면 +가 왼쪽으로 기울기."""
    pb = P["spine_01"]
    rotate(pb, Quaternion(axis or RIGHT, -math.radians(deg)), pb.head.copy())


def plant_feet(feet, lift=0.0):
    """골반을 옮긴 뒤 발을 원래 자리(feet)에 둔다. lift = 발을 그만큼 띄운다."""
    for side in ("l", "r"):
        ik("thigh_" + side, "calf_" + side, "foot_" + side, feet[side] + UP * lift, FWD, 1.0)


def shoulder(side):
    return P["upperarm_" + side].head.copy()


def side_out(side):
    return RIGHT if side == "r" else -RIGHT


# ---------------- 클립 ----------------

def cheer(f):
    """랜딩 환호 (78프레임): 두 팔을 V자로 들고 좌우로 흔든다, 무릎이 살짝 들썩인다."""
    length = 78
    w = envelope(f, length, 9, 12)
    feet = {s: P["foot_" + s].head.copy() for s in "lr"}
    bounce = -0.035 * (0.5 - 0.5 * math.cos(2 * math.pi * f / 15)) * w
    move_pelvis(UP * bounce)
    plant_feet(feet)
    lean(-6 * w)
    sway = math.sin(2 * math.pi * f / 15) * 0.10
    for s in "lr":
        target = shoulder(s) + UP * 0.47 + side_out(s) * 0.2 + FWD * 0.07 + RIGHT * sway
        ik("upperarm_" + s, "lowerarm_" + s, "hand_" + s, target, side_out(s) - FWD * 0.3, w)
        orient_hand(s, UP + side_out(s) * 0.3 + RIGHT * sway * 2, FWD - side_out(s) * 0.4, w * 0.8)
    return length


def clap(f):
    """박수 (75프레임 반복 = 대기 한 바퀴, 박수 5번): 가슴 앞에서 손바닥을 마주친다."""
    length = 75
    p = (f % 15) / 15.0
    gap = 0.03 + 0.14 * math.sin(math.pi * p) ** 0.8
    center = (shoulder("l") + shoulder("r")) * 0.5 + FWD * 0.33 - UP * 0.17
    lean(4)
    for s in "lr":
        target = center + side_out(s) * gap
        ik("upperarm_" + s, "lowerarm_" + s, "hand_" + s, target, side_out(s) - UP * 0.6, 1.0)
        orient_hand(s, FWD + UP * 0.9 - side_out(s) * 0.15, -side_out(s), 1.0)
    return length


def excited(f):
    """괴물·전설 어종 (76프레임): 웅크렸다 펄쩍 뛰며 두 주먹을 치켜들고, 내려와 주먹을 두 번 당긴다."""
    length = 76
    feet = {s: P["foot_" + s].head.copy() for s in "lr"}
    # 골반 높이: 웅크림(0~8) → 뛰어오름(8~26, 정점 +0.24) → 착지 웅크림(26~33) → 펌프 들썩임 → 원래대로
    lift = 0.0
    if f < 8:
        h = -0.13 * ramp(f, 0, 8)
    elif f < 26:
        t = (f - 8) / 18.0
        h = -0.13 * (1 - t) ** 2 + 0.24 * math.sin(math.pi * t) - 0.10 * t * t
        lift = max(0.0, h) * 0.8  # 공중에서는 발이 따라 뜨고 무릎을 살짝 접는다
    else:
        pump = 0.5 - 0.5 * math.cos(2 * math.pi * (f - 33) / 13) if 33 <= f < 59 else 0.0
        h = -0.10 * min(ramp(f, 26, 29), 1 - ramp(f, 31, 38)) - 0.06 * pump
    move_pelvis(UP * h)
    plant_feet(feet, lift)
    w = envelope(f, length, 6, 14)
    lean((14 if f < 8 else -8 if f < 26 else 6) * w)
    for s in "lr":
        down = shoulder(s) - UP * 0.42 + side_out(s) * 0.12 - FWD * 0.08  # 웅크릴 때 팔을 뒤로
        up = shoulder(s) + UP * 0.5 + side_out(s) * 0.14 + FWD * 0.05      # 공중: 두 주먹 위로
        pump_hi = shoulder(s) + FWD * 0.2 + UP * 0.02 + side_out(s) * 0.12  # 펌프: 주먹을 어깨 앞에서
        pump_lo = shoulder(s) + FWD * 0.22 - UP * 0.22 + side_out(s) * 0.12
        if f < 8:
            target = down
        elif f < 26:
            target = down.lerp(up, ramp(f, 8, 14))
        elif f < 33:
            target = up.lerp(pump_hi, ramp(f, 26, 33))
        else:
            k = 0.5 + 0.5 * math.cos(2 * math.pi * (f - 33) / 13) if f < 59 else 1.0
            target = pump_lo.lerp(pump_hi, k)
        ik("upperarm_" + s, "lowerarm_" + s, "hand_" + s, target, side_out(s) - UP * 0.5 - FWD * 0.2, w)
        curl_fingers(s, w)
    return length


def wave(f):
    """손짓해 부르기 (72프레임): 오른손을 머리 위로 들고 크게 세 번 흔든다."""
    length = 72
    w = envelope(f, length, 10, 14)
    sway = math.sin(2 * math.pi * (f - 10) / 18) * 0.15 * ramp(f, 8, 16)
    target = shoulder("r") + UP * 0.42 + RIGHT * (0.2 + sway) + FWD * 0.12
    ik("upperarm_r", "lowerarm_r", "hand_r", target, RIGHT - UP * 0.2 - FWD * 0.3, w)
    orient_hand("r", UP + RIGHT * sway * 2.5, FWD, w)
    lean(3 * w, FWD)  # 반대쪽(왼쪽)으로 살짝 기운다
    return length


def scared(f):
    """줄이 끊겼을 때 움찔 (40프레임): 두 손을 가슴 앞으로 홱 올리며 뒤로 젖히고 무릎을 굽혔다가 풀린다."""
    length = 40
    jerk = min(ramp(f, 0, 4), 1 - ramp(f, 18, 38))
    feet = {s: P["foot_" + s].head.copy() for s in "lr"}
    move_pelvis(UP * (-0.06 * jerk) - FWD * 0.04 * jerk)
    plant_feet(feet)
    lean(-12 * jerk)
    shake = math.sin(f * 2.2) * 0.012 * jerk  # 놀라서 손이 떨린다
    for s in "lr":
        target = shoulder(s) + FWD * 0.24 - UP * 0.08 + side_out(s) * 0.04 + UP * shake
        ik("upperarm_" + s, "lowerarm_" + s, "hand_" + s, target, side_out(s) - UP * 0.7, jerk)
        orient_hand(s, UP + FWD * 0.3, FWD, jerk * 0.9)
    return length


CLIPS = [("Cheer_ArmsUp", cheer), ("Clap_Loop", clap), ("Excited", excited), ("Wave_Call", wave), ("Scared", scared)]

CHANNELS = ("location", "rotation_quaternion", "scale")


def bake(name, fn):
    frames = []
    f = 0
    length = None
    while length is None or f < length:
        arm.animation_data.action = idle
        for pb in P:
            pb.location = (0, 0, 0)
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.scale = (1, 1, 1)
        scene.frame_set(1 + (f % IDLE_LEN))
        update()
        length = fn(f)
        frames.append({pb.name: (pb.location.copy(), pb.rotation_quaternion.copy(), pb.scale.copy()) for pb in P})
        f += 1
    act = bpy.data.actions.new(name)
    arm.animation_data.action = act
    prev = {}
    for i, pose in enumerate(frames):
        for pb in P:
            loc, rot, scl = pose[pb.name]
            if pb.name in prev and prev[pb.name].dot(rot) < 0:
                rot = -rot
            prev[pb.name] = rot
            pb.location, pb.rotation_quaternion, pb.scale = loc, rot, scl
            for ch in CHANNELS:
                pb.keyframe_insert(ch, frame=i + 1)
    print(f"[reactions] {name}: {len(frames)}f")
    return act


made = [bake(n, fn) for n, fn in CLIPS]

if args.preview:
    from PIL import Image
    os.makedirs(args.preview, exist_ok=True)
    sc = scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.render.resolution_x, sc.render.resolution_y = 220, 300
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = 2.3
    sc.camera = cam
    tmp = os.path.join(args.preview, "_f.png")
    for act in made:
        arm.animation_data.action = act
        n = int(act.frame_range[1])
        picks = [1 + round(i * (n - 1) / 5) for i in range(6)]
        sheet = Image.new("RGB", (220 * 6, 300 * 2), (40, 40, 40))
        for row, (loc, rot) in enumerate((((0, 4, 1.0), (math.radians(90), 0, math.radians(180))),
                                          ((4, 0, 1.0), (math.radians(90), 0, math.radians(90))))):
            cam.location, cam.rotation_euler = loc, rot
            for col, fr in enumerate(picks):
                sc.frame_set(fr)
                sc.render.filepath = tmp
                bpy.ops.render.render(write_still=True)
                sheet.paste(Image.open(tmp).convert("RGB"), (col * 220, row * 300))
        sheet.save(os.path.join(args.preview, act.name + ".png"))
        print("[reactions] preview", act.name, picks)

# 내보내기: 뼈대와 새 동작만
for a in list(bpy.data.actions):
    if a not in made:
        bpy.data.actions.remove(a)
if mesh is not None:
    bpy.data.objects.remove(mesh)
arm.animation_data.action = made[0]
bpy.ops.object.select_all(action='DESELECT')
arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, object_types={'ARMATURE'}, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0)
print("[reactions] wrote", OUT)
