"""캐릭터 동작을 UAL1 뼈대 위에 만들어 FBX로 내보낸다 (docs/10-animation.md 1~2장).

  bpyenv/Scripts/python -I tools/blender/build_motions.py [--preview <폴더>]

- 바탕은 Quaternius UAL1(CC0) 클립(대기·걷기·뛰기·말하기). 프레임마다 바탕 자세를 놓고 그 위에 손·발 자리를 시간에 따라 정해
  두 관절 IK(어깨-팔꿈치-손목, 엉덩이-무릎-발목)로 팔다리를 맞추고, 손 방향·손가락·골반·허리를 얹은 뒤 키로 굽는다.
- 대부분 "편한 대기"(relaxed_idle: UAL 대기의 넓은 다리·주먹 쥔 팔을 모으고 내려 손을 편다)를 바탕으로 쓴다.
- 출력: Assets/Resources/Animations/Motions.fbx (뼈대만, 클립은 CLIPS 표)
  - 기본 동작 Basic_*: 예전 Human Basic Motions FREE(Kevin Iglesias, 에셋 스토어)를 대신한다
  - 반응 동작(환호·박수·펄쩍·손짓·움찔): 예전 Mixamo를 대신한다
  직접 만든 동작이라 프로젝트 라이선스(CC BY-NC-SA 4.0)를 따른다. 바탕 UAL1은 CC0.
- --preview: 마네킹으로 클립마다 앞·옆 6프레임씩 찍은 시트(<폴더>/<클립>.png)와 손 확대(<폴더>/_hands.png)
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
OUT = os.path.join(ROOT, "Assets", "Resources", "Animations", "Motions.fbx")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
mesh = next((o for o in bpy.data.objects if o.type == 'MESH'), None)
BASE = {a.name.split("|")[-1]: a for a in bpy.data.actions}
scene = bpy.context.scene
scene.render.fps = 30
bpy.context.view_layer.objects.active = arm
P = arm.pose.bones

# 아마추어 공간 기준 방향 (월드 +Y 앞, +X 오른쪽, +Z 위를 아마추어 공간으로)
MWi = arm.matrix_world.to_3x3().inverted()
FWD = (MWi @ Vector((0, 1, 0))).normalized()
RIGHT = (MWi @ Vector((1, 0, 0))).normalized()
UP = (MWi @ Vector((0, 0, 1))).normalized()

FINGERS = ("index", "middle", "ring", "pinky")


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


def set_fingers(side, curl, w=1.0):
    """손가락을 쉬는 자세(뼈대 기본 = 편 손)에서 curl(0 편 손 ~ 1 주먹)만큼 감은 모양으로 (지금 손 모양에서 w 만큼)."""
    if w <= 1e-4:
        return
    names = [f"{n}_0{k}_{side}" for n in FINGERS for k in (1, 2, 3)] + [f"thumb_0{k}_{side}" for k in (1, 2, 3)]
    before = {n: P[n].rotation_quaternion.copy() for n in names}
    for n in names:
        P[n].rotation_quaternion = (1, 0, 0, 0)
    update()
    for name in FINGERS:
        for k, deg in ((1, 70), (2, 90), (3, 60)):
            pb = P[f"{name}_0{k}_{side}"]
            f, n = hand_frame(side)
            # f×n 축으로 + 돌리면 손가락 끝이 손바닥 쪽으로 감긴다
            rotate(pb, Quaternion(f.cross(n), math.radians(deg * curl)), pb.head.copy())
    for k, deg in ((2, 30), (3, 30)):
        pb = P[f"thumb_0{k}_{side}"]
        f, n = hand_frame(side)
        rotate(pb, Quaternion(f.cross(n), math.radians(deg * curl)), pb.head.copy())
    if w < 1.0:
        for n in names:
            P[n].rotation_quaternion = before[n].slerp(P[n].rotation_quaternion, w)
        update()


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


def feet_now():
    return {s: P["foot_" + s].head.copy() for s in "lr"}


def plant_feet(feet, lift=0.0):
    """골반을 옮긴 뒤 발을 feet 자리에 둔다. lift = 발을 그만큼 띄운다 (숫자 또는 {"l": , "r": })."""
    for side in ("l", "r"):
        h = lift[side] if isinstance(lift, dict) else lift
        ik("thigh_" + side, "calf_" + side, "foot_" + side, feet[side] + UP * h, FWD, 1.0)


def shoulder(side):
    return P["upperarm_" + side].head.copy()


def side_out(side):
    return RIGHT if side == "r" else -RIGHT


def arm_to(side, target, pole, w):
    ik("upperarm_" + side, "lowerarm_" + side, "hand_" + side, target, pole, w)


# ---------------- 바탕: 편한 대기 ----------------

def narrow_feet():
    """UAL 대기의 어깨보다 넓은 다리(슈퍼히어로 자세)를 골반 폭으로 모으고, 앞뒤로 엇갈린 발도 반쯤 맞춘다."""
    feet = feet_now()
    pel = P["pelvis"].head
    mid_y = (feet["l"].dot(FWD) + feet["r"].dot(FWD)) * 0.5
    want = {}
    rise = 0.0
    for s in "lr":
        p = feet[s]
        lat = (p - pel).dot(RIGHT)
        fy = p.dot(FWD)
        want[s] = p - RIGHT * lat * 0.45 - FWD * (fy - mid_y) * 0.5
        # 발을 모으면 같은 골반 높이에선 무릎이 더 굽는다 → 엉덩이~발목 거리를 지키도록 골반을 올린다 (+1.5cm 더 펴기)
        hip = P["thigh_" + s].head
        d0 = (p - hip).length
        flat = (want[s] - hip) - UP * (want[s] - hip).dot(UP)
        rise += (math.sqrt(max(0.0, d0 * d0 - flat.length_squared)) - (hip - want[s]).dot(UP)) * 0.5
    move_pelvis(UP * (rise + 0.015))
    plant_feet(want)
    lean(-5)  # UAL 대기는 고개·가슴이 앞으로 나와 있다


def relax_arms(w=1.0):
    """두 팔을 몸 옆으로 내리고(팔꿈치 살짝 굽혀 뒤로) 손바닥이 허벅지 쪽을 보게, 손가락은 살짝 감는다."""
    for s in "lr":
        target = shoulder(s) - UP * 0.49 + side_out(s) * 0.07 + FWD * 0.04
        arm_to(s, target, side_out(s) * 0.3 - FWD, w)
        orient_hand(s, -UP + FWD * 0.12, -side_out(s) + FWD * 0.2, w)
        set_fingers(s, 0.3, w)


def relaxed_idle(f):
    narrow_feet()
    relax_arms()


def relaxed(fn):
    """반응 동작을 편한 대기 위에."""
    def run(f):
        relaxed_idle(f)
        return fn(f)
    run.__name__ = fn.__name__
    return run


def fishing_hold(f):
    """
    낚싯대를 들고 선 몸 (HoldRod, 두 팔은 게임 코드의 IK가 낚싯대에 붙인다): 왼발을 앞으로 엇갈려 서고, 상체를 앞으로 숙이고
    가슴을 오른쪽으로 틀어 왼 어깨가 앞으로 나온다. 똑바로 선 편한 대기에 손 자리 표(RodArm)를 쓰면 허리 앞에 든 두 손이 배에 묻히고
    오른손 밑을 쥐는 왼팔이 몸통을 파고들었다 (예전 Mixamo Fishing Idle이 이런 자세였다).
    """
    relaxed_idle(f)
    feet = feet_now()
    plant_feet({"l": feet["l"] + FWD * 0.11 - RIGHT * 0.03, "r": feet["r"] - FWD * 0.06 + RIGHT * 0.03})
    lean(11)
    pb = P["spine_02"]
    rotate(pb, Quaternion(UP, math.radians(-16)), pb.head.copy())  # 위에서 봐서 시계 방향 = 왼 어깨가 앞으로
    return None


# ---------------- 기본 동작 (Human Basic Motions 대신) ----------------

def basic_idle(f):
    return None  # relaxed_idle 그대로, 길이 = 바탕(75)


def basic_walk(f):
    """걷기 (Walk_Formal_Loop, 원래 0.98 m/s): 주먹을 풀고 손바닥이 몸 쪽을 보게."""
    for s in "lr":
        set_fingers(s, 0.35)
    return None


def basic_run(f):
    """뛰기 (Jog_Fwd_Loop, 원래 5.9 m/s): 주먹을 느슨하게."""
    for s in "lr":
        set_fingers(s, 0.6)
    return None


def basic_sprint(f):
    """질주 (Sprint_Loop, 원래 8.9 m/s)."""
    for s in "lr":
        set_fingers(s, 0.7)
    return None


def basic_talk(f):
    """말하기 (Idle_Talking_Loop): 다리를 모으고 손을 편다."""
    narrow_feet()
    for s in "lr":
        set_fingers(s, 0.2)
    return None


def jump_begin(f):
    """점프 시작 (20프레임): 웅크렸다가 팔을 앞으로 휘두르며 발끝으로 펴 오른다."""
    length = 20
    feet = feet_now()
    h = -0.12 * ramp(f, 0, 6) + 0.15 * ramp(f, 6, 12)
    move_pelvis(UP * h)
    plant_feet(feet)
    lean(18 * ramp(f, 0, 6) * (1 - ramp(f, 6, 12)))
    for s in "lr":
        back = shoulder(s) - UP * 0.4 + side_out(s) * 0.1 - FWD * 0.22
        fwd = shoulder(s) + FWD * 0.35 - UP * 0.05 + side_out(s) * 0.12
        target = back.lerp(fwd, ramp(f, 6, 13))
        arm_to(s, target, side_out(s) * 0.4 - UP * 0.4 - FWD * 0.3, min(1.0, ramp(f, 0, 4) + 0.3))
    return length


def fall(f):
    """공중 (75프레임 반복): 무릎을 조금 접고(한 발 앞), 두 팔을 옆으로 벌려 균형을 잡는다."""
    feet = feet_now()
    sway = math.sin(2 * math.pi * f / 37.5)
    plant_feet({"l": feet["l"] + FWD * 0.08, "r": feet["r"] - FWD * 0.05}, {"l": 0.16 + 0.02 * sway, "r": 0.09 - 0.02 * sway})
    for s in "lr":
        target = shoulder(s) + side_out(s) * 0.28 - UP * 0.30 + FWD * 0.12 + UP * 0.03 * sway * (1 if s == "l" else -1)
        arm_to(s, target, -FWD - UP * 0.3, 1.0)
        set_fingers(s, 0.35)
    return 75


def turn(direction):
    """제자리 돌기 (75프레임 반복, 25프레임에 한 걸음씩 세 번): 몸 방향은 코드가 돌리고, 클립은 발만 번갈아 들어 옮긴다."""
    def run(f):
        feet = feet_now()
        p = (f % 25) / 25.0
        lead = "l" if direction < 0 else "r"
        other = "r" if lead == "l" else "l"
        # 앞 절반은 도는 쪽 발, 뒤 절반은 다른 발을 들어 도는 쪽으로 살짝 옮긴다
        la = math.sin(math.pi * min(1.0, p * 2)) if p < 0.5 else 0.0
        lb = math.sin(math.pi * (p - 0.5) * 2) if p >= 0.5 else 0.0
        step = RIGHT * 0.04 * (1 if direction > 0 else -1)
        plant_feet({lead: feet[lead] + step * la, other: feet[other] + step * lb}, {lead: 0.07 * la, other: 0.07 * lb})
        move_pelvis(-UP * 0.015 * (la + lb))
        return 75
    run.__name__ = "turn"
    return run


# ---------------- 반응 동작 (Mixamo 대신) ----------------

def cheer(f):
    """랜딩 환호 (78프레임): 두 팔을 V자로 들고 좌우로 흔든다, 무릎이 살짝 들썩인다."""
    length = 78
    w = envelope(f, length, 9, 12)
    feet = feet_now()
    bounce = -0.035 * (0.5 - 0.5 * math.cos(2 * math.pi * f / 15)) * w
    move_pelvis(UP * bounce)
    plant_feet(feet)
    lean(-6 * w)
    sway = math.sin(2 * math.pi * f / 15) * 0.10
    for s in "lr":
        target = shoulder(s) + UP * 0.47 + side_out(s) * 0.2 + FWD * 0.07 + RIGHT * sway
        arm_to(s, target, side_out(s) - FWD * 0.3, w)
        orient_hand(s, UP + side_out(s) * 0.3 + RIGHT * sway * 2, FWD - side_out(s) * 0.4, w * 0.8)
        set_fingers(s, 0.15, w)
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
        arm_to(s, target, side_out(s) - UP * 0.6, 1.0)
        orient_hand(s, FWD + UP * 0.9 - side_out(s) * 0.15, -side_out(s), 1.0)
        set_fingers(s, 0.05)
    return length


def excited(f):
    """괴물·전설 어종 (76프레임): 웅크렸다 펄쩍 뛰며 두 주먹을 치켜들고, 내려와 주먹을 두 번 당긴다."""
    length = 76
    feet = feet_now()
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
        arm_to(s, target, side_out(s) - UP * 0.5 - FWD * 0.2, w)
        set_fingers(s, 1.0, w)
    return length


def wave(f):
    """손짓해 부르기 (72프레임): 오른손을 머리 위로 들고 크게 세 번 흔든다."""
    length = 72
    w = envelope(f, length, 10, 14)
    sway = math.sin(2 * math.pi * (f - 10) / 18) * 0.15 * ramp(f, 8, 16)
    target = shoulder("r") + UP * 0.42 + RIGHT * (0.2 + sway) + FWD * 0.12
    arm_to("r", target, RIGHT - UP * 0.2 - FWD * 0.3, w)
    orient_hand("r", UP + RIGHT * sway * 2.5, FWD, w)
    set_fingers("r", 0.0, w)
    lean(3 * w, FWD)  # 반대쪽(왼쪽)으로 살짝 기운다
    return length


def scared(f):
    """줄이 끊겼을 때 움찔 (40프레임): 두 손을 가슴 앞으로 홱 올리며 뒤로 젖히고 무릎을 굽혔다가 풀린다."""
    length = 40
    jerk = min(ramp(f, 0, 4), 1 - ramp(f, 18, 38))
    feet = feet_now()
    move_pelvis(UP * (-0.06 * jerk) - FWD * 0.04 * jerk)
    plant_feet(feet)
    lean(-12 * jerk)
    shake = math.sin(f * 2.2) * 0.012 * jerk  # 놀라서 손이 떨린다
    for s in "lr":
        target = shoulder(s) + FWD * 0.24 - UP * 0.08 + side_out(s) * 0.04 + UP * shake
        arm_to(s, target, side_out(s) - UP * 0.7, jerk)
        orient_hand(s, UP + FWD * 0.3, FWD, jerk * 0.9)
        set_fingers(s, 0.5, jerk)
    return length


# (클립 이름, 바탕 UAL 클립, 함수). 함수가 None을 돌려주면 길이 = 바탕 클립 길이. 이름에 Loop가 있으면 Unity에서 반복.
CLIPS = [
    ("Basic_Idle_Loop", "Idle_Loop", relaxed(basic_idle)),
    ("Fishing_Hold_Loop", "Idle_Loop", fishing_hold),
    ("Basic_Walk_Loop", "Walk_Formal_Loop", basic_walk),
    ("Basic_Run_Loop", "Jog_Fwd_Loop", basic_run),
    ("Basic_Sprint_Loop", "Sprint_Loop", basic_sprint),
    ("Basic_Talk_Loop", "Idle_Talking_Loop", basic_talk),
    ("Basic_JumpBegin", "Idle_Loop", relaxed(jump_begin)),
    ("Basic_Fall_Loop", "Idle_Loop", relaxed(fall)),
    ("Basic_TurnLeft_Loop", "Idle_Loop", relaxed(turn(-1))),
    ("Basic_TurnRight_Loop", "Idle_Loop", relaxed(turn(1))),
    ("Cheer_ArmsUp", "Idle_Loop", relaxed(cheer)),
    ("Clap_Loop", "Idle_Loop", relaxed(clap)),
    ("Excited", "Idle_Loop", relaxed(excited)),
    ("Wave_Call", "Idle_Loop", relaxed(wave)),
    ("Scared", "Idle_Loop", relaxed(scared)),
]

CHANNELS = ("location", "rotation_quaternion", "scale")


def bake(name, base_name, fn):
    base = BASE[base_name]
    s, e = base.frame_range
    base_len = int(round(e - s))
    frames = []
    f = 0
    length = None
    while length is None or f < length:
        arm.animation_data.action = base
        for pb in P:
            pb.location = (0, 0, 0)
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.scale = (1, 1, 1)
        scene.frame_set(int(s) + (f % base_len))
        update()
        length = fn(f) or base_len
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
    print(f"[motions] {name}: {len(frames)}f (바탕 {base_name})")
    return act


made = [bake(n, b, fn) for n, b, fn in CLIPS]

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
    views = (((0, 4, 1.0), (math.radians(90), 0, math.radians(180))), ((4, 0, 1.0), (math.radians(90), 0, math.radians(90))))
    for act in made:
        arm.animation_data.action = act
        n = int(act.frame_range[1])
        picks = [1 + round(i * (n - 1) / 5) for i in range(6)]
        sheet = Image.new("RGB", (220 * 6, 300 * 2), (40, 40, 40))
        for row, (loc, rot) in enumerate(views):
            cam.location, cam.rotation_euler = loc, rot
            for col, fr in enumerate(picks):
                sc.frame_set(fr)
                sc.render.filepath = tmp
                bpy.ops.render.render(write_still=True)
                sheet.paste(Image.open(tmp).convert("RGB"), (col * 220, row * 300))
        sheet.save(os.path.join(args.preview, act.name + ".png"))
    # 손 확대: 편한 대기(손가락 살짝) · 펄쩍(주먹) · 손짓(편 손) — 손가락이 손바닥 쪽으로 감기는지
    cam.data.ortho_scale = 0.35
    sheet = Image.new("RGB", (220 * 3, 300), (40, 40, 40))
    for col, (clip, fr) in enumerate((("Basic_Idle_Loop", 1), ("Excited", 50), ("Wave_Call", 36))):
        act = next(a for a in made if a.name == clip)
        arm.animation_data.action = act
        sc.frame_set(fr)
        h = arm.matrix_world @ P["hand_r"].head
        cam.location = (h.x + 0.0, h.y + 1.0, h.z)
        cam.rotation_euler = (math.radians(90), 0, math.radians(180))
        sc.render.filepath = tmp
        bpy.ops.render.render(write_still=True)
        sheet.paste(Image.open(tmp).convert("RGB"), (col * 220, 0))
    sheet.save(os.path.join(args.preview, "_hands.png"))
    print("[motions] preview", args.preview)

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
print("[motions] wrote", OUT)
