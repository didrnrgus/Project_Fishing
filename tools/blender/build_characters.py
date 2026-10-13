"""
낚시꾼 캐릭터 FBX를 만든다: Quaternius Superhero 몸 → 보통 체형 + 진짜 옷 메쉬.

  python -I tools/blender/build_characters.py [--preview [--closeup]] [--only male|female] [--pose Idle_Loop]

  - bpy(PyPI의 headless Blender, `pip install bpy`)로 실행한다. Blender 실행 파일은 필요 없다.
  - 결과: Assets/Resources/Characters/Angler_Male.fbx, Angler_Female.fbx
    (몸 Body + 눈 + 눈썹 + 옷 메쉬들이 한 아마추어에 붙어 있다. CharacterFactory가 이름으로 켜고 끈다.)
  - --preview: FBX 대신 docs/images/character_preview_*.png 렌더(Cycles CPU)
"""
import os
import sys

ARGS = sys.argv[1:]
sys.argv = [sys.argv[0]]  # bpy가 명령줄 인자를 Blender 인자로 해석하지 않게
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
CHAR = os.path.join(ROOT, 'Assets', 'Resources', 'Characters')
SRC = os.path.join(ROOT, 'Assets', 'ThirdParty', 'Quaternius', 'Source')  # 원본 Superhero FBX (빌드에는 안 들어감)
UAL = os.path.join(ROOT, 'Assets', 'Resources', 'Animations', 'UAL1_Standard.fbx')

import bpy  # noqa: E402
import numpy as np  # noqa: E402

import garments as G  # noqa: E402
import preview as R  # noqa: E402
from meshops import bone_segments, closest_on_seg, neighbors, set_world_verts, smooth, weights, world_verts  # noqa: E402

# 근육질 Superhero → 보통 체형: 뼈 둘레 반지름 배율 (몸통은 좌우 x, 앞뒤 y 따로)
SLIM = {
    'male': {
        'neck_01': (0.84, 0.9), 'spine_03': (0.84, 0.93), 'spine_02': (0.88, 0.96), 'spine_01': (0.97, 1.02), 'pelvis': (0.97, 1.0),
        'clavicle_l': 0.7, 'clavicle_r': 0.7, 'upperarm_l': 0.77, 'upperarm_r': 0.77, 'lowerarm_l': 0.84, 'lowerarm_r': 0.84,
        'thigh_l': 0.92, 'thigh_r': 0.92, 'calf_l': 0.9, 'calf_r': 0.9,
    },
    'female': {
        'neck_01': (0.9, 0.95), 'spine_03': (0.92, 0.97), 'spine_02': (0.92, 0.98), 'spine_01': (0.98, 1.0),
        'clavicle_l': 0.82, 'clavicle_r': 0.82, 'upperarm_l': 0.84, 'upperarm_r': 0.84, 'lowerarm_l': 0.88, 'lowerarm_r': 0.88,
        'thigh_l': 0.96, 'thigh_r': 0.96, 'calf_l': 0.94, 'calf_r': 0.94,
    },
}
KEEP = ('Head', 'hand', 'index', 'middle', 'ring', 'pinky', 'thumb', 'foot', 'ball')

SOURCES = {
    'male': ('Superhero_Male_FullBody.fbx', 'T_Superhero_Male_Ligh.png'),
    'female': ('Superhero_Female_FullBody.fbx', 'T_Superhero_Female_Light_BaseColor.png'),
}


def slim(body, arm, factors, iters=12):
    P = world_verts(body)
    W = weights(body)
    seg = bone_segments(arm)
    D = np.zeros_like(P)
    for i, (p, w) in enumerate(zip(P, W)):
        for b, wt in w.items():
            f = factors.get(b)
            if f is None:
                continue
            q, _ = closest_on_seg(p, *seg[b])
            off = p - q
            if isinstance(f, tuple):
                D[i] += wt * np.array([(f[0] - 1) * off[0], (f[1] - 1) * off[1], 0.0])
            else:
                D[i] += wt * (f - 1) * off
    P = P + D
    mask = np.array([1.0 - sum(wt for b, wt in w.items() if b.startswith(KEEP)) for w in W])
    mask = np.clip(mask, 0, 1) ** 2
    P = smooth(P, neighbors(body), mask * 0.8, iters=iters)
    set_world_verts(body, P)


def build(gender):
    fbx, _ = SOURCES[gender]
    objs = R.import_fbx(os.path.join(SRC, fbx))
    arm = next(o for o in objs if o.type == 'ARMATURE')
    body = next(o for o in objs if o.type == 'MESH' and 'Super' in o.name)
    eyes = next(o for o in objs if o.type == 'MESH' and o.name.startswith('Eyes'))
    slim(body, arm, SLIM[gender])
    body.name = body.data.name = 'Body'
    b = G.Body(body, arm, eyes)
    made = {}
    # 겹쳐 입는 순서대로: 바지 → 상의(바지 허리 밖) → 웨이더(상의 밖) → 조끼(상의·웨이더 밖)
    made['Pants_Cargo'] = G.pants(b, 'Pants_Cargo', 'cargo')
    made['Pants_Jeans'] = G.pants(b, 'Pants_Jeans', 'jeans')
    belts = ['Pants_Cargo', 'Pants_Jeans']
    made['Top_Tee'] = G.top(b, 'Top_Tee', 'short', under=belts)
    made['Top_Long'] = G.top(b, 'Top_Long', 'long', loose=0.8, base=0.011, under=belts)
    made['Top_Rash'] = G.top(b, 'Top_Rash', 'long', tight=True, base=0.005, hem_drop=0.05, neck_front=0.045, under=belts)
    made['Top_Shirt'] = G.shirt(b, 'Top_Shirt', under=belts)
    tops = ['Top_Tee', 'Top_Long', 'Top_Rash', 'Top_Shirt']
    made['Pants_Wader'] = G.pants(b, 'Pants_Wader', 'wader', under=tops)
    made['Vest_Fishing'] = G.vest(b, 'Vest_Fishing', 'fishing', under=tops + ['Pants_Wader'])
    made['Vest_Life'] = G.vest(b, 'Vest_Life', 'life', under=tops + ['Pants_Wader'])
    made['Shoes_Sneaker'] = G.shoes(b, 'Shoes_Sneaker', 'sneaker')
    made['Shoes_Boots'] = G.shoes(b, 'Shoes_Boots', 'boots')
    # 몸 가림: R 상의(셋 다), G 하의(셋 다), B 신발(둘 다), A 긴소매(둘 다)
    G.bake_cover(b, [['Top_Tee', 'Top_Long', 'Top_Rash', 'Top_Shirt'], ['Pants_Cargo', 'Pants_Jeans', 'Pants_Wader'],
                     ['Shoes_Sneaker', 'Shoes_Boots'], ['Top_Long', 'Top_Rash', 'Top_Shirt']])
    made['Gloves'] = G.gloves(b, 'Gloves')
    made['Hat_Bucket'] = G.hat(b, 'Hat_Bucket', 'bucket')
    made['Hat_Cap'] = G.hat(b, 'Hat_Cap', 'cap')
    made['Hat_Straw'] = G.hat(b, 'Hat_Straw', 'straw')
    made['Glasses'] = G.glasses(b, 'Glasses')
    # 음영(AO): 겨드랑이·가랑이·주머니 테두리·모자 챙 밑이 어두워져 옷이 납작해 보이지 않는다
    for obj in made.values():
        G.bake_ao(obj, b)
    return arm, body, made, objs


# ---------------------------------------------------------------------------------------------
# 미리보기

def skin_mat(tex, clip):
    """Unity Fishing/Skin과 같은 규칙: dot(버텍스 컬러, clip) > 0.5인 피부는 옷 속이라 그리지 않는다."""
    m = bpy.data.materials.new('skin')
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes['Principled BSDF']
    bsdf.inputs['Roughness'].default_value = 0.55
    ti = nt.nodes.new('ShaderNodeTexImage')
    ti.image = bpy.data.images.load(tex)
    nt.links.new(ti.outputs['Color'], bsdf.inputs['Base Color'])
    attr = nt.nodes.new('ShaderNodeAttribute')
    attr.attribute_name = 'Col'
    dot = nt.nodes.new('ShaderNodeVectorMath')
    dot.operation = 'DOT_PRODUCT'
    nt.links.new(attr.outputs['Color'], dot.inputs[0])
    dot.inputs[1].default_value = clip[:3]
    add = nt.nodes.new('ShaderNodeMath')
    add.operation = 'MULTIPLY_ADD'
    nt.links.new(attr.outputs['Alpha'], add.inputs[0])
    add.inputs[1].default_value = clip[3]
    nt.links.new(dot.outputs['Value'], add.inputs[2])
    cmp = nt.nodes.new('ShaderNodeMath')
    cmp.operation = 'GREATER_THAN'
    nt.links.new(add.outputs[0], cmp.inputs[0])
    cmp.inputs[1].default_value = 0.5
    tr = nt.nodes.new('ShaderNodeBsdfTransparent')
    mix = nt.nodes.new('ShaderNodeMixShader')
    nt.links.new(cmp.outputs[0], mix.inputs['Fac'])
    nt.links.new(bsdf.outputs[0], mix.inputs[1])
    nt.links.new(tr.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], nt.nodes['Material Output'].inputs['Surface'])
    return m


def cloth_mat(name, color, trim, pattern=None, rough=0.85, clip_below=None):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes['Principled BSDF']
    bsdf.inputs['Roughness'].default_value = rough
    attr = nt.nodes.new('ShaderNodeAttribute')
    attr.attribute_name = 'Col'
    sep = nt.nodes.new('ShaderNodeSeparateColor')
    nt.links.new(attr.outputs['Color'], sep.inputs['Color'])
    mix = nt.nodes.new('ShaderNodeMix')
    mix.data_type = 'RGBA'
    mix.inputs['A'].default_value = color
    mix.inputs['B'].default_value = trim
    nt.links.new(sep.outputs['Red'], mix.inputs['Factor'])
    out = mix.outputs['Result']
    if pattern is not None:
        mix2 = nt.nodes.new('ShaderNodeMix')
        mix2.data_type = 'RGBA'
        nt.links.new(out, mix2.inputs['A'])
        mix2.inputs['B'].default_value = pattern
        nt.links.new(sep.outputs['Blue'], mix2.inputs['Factor'])
        out = mix2.outputs['Result']
    mul = nt.nodes.new('ShaderNodeMix')
    mul.data_type = 'RGBA'
    mul.blend_type = 'MULTIPLY'
    mul.inputs['Factor'].default_value = 1.0
    nt.links.new(out, mul.inputs['A'])
    nt.links.new(sep.outputs['Green'], mul.inputs['B'])
    nt.links.new(mul.outputs['Result'], bsdf.inputs['Base Color'])
    if clip_below is not None:
        # Unity 셰이더의 _ClipBelow와 같은 규칙: 알파(휴식 자세 높이/2)가 기준보다 낮으면 버린다
        cmp = nt.nodes.new('ShaderNodeMath')
        cmp.operation = 'GREATER_THAN'
        nt.links.new(attr.outputs['Alpha'], cmp.inputs[0])
        cmp.inputs[1].default_value = clip_below
        tr = nt.nodes.new('ShaderNodeBsdfTransparent')
        mixs = nt.nodes.new('ShaderNodeMixShader')
        outn = nt.nodes['Material Output']
        nt.links.new(cmp.outputs[0], mixs.inputs['Fac'])
        nt.links.new(tr.outputs[0], mixs.inputs[1])
        nt.links.new(bsdf.outputs[0], mixs.inputs[2])
        nt.links.new(mixs.outputs[0], outn.inputs['Surface'])
    return m


OUTFITS = [
    # (이름, 켤 메쉬, 색)
    ('basic', ['Top_Tee', 'Vest_Fishing', 'Pants_Cargo', 'Shoes_Sneaker', 'Hat_Bucket', 'Gloves'],
     {'Top_Tee': (0.9, 0.9, 0.92, 1), 'Vest_Fishing': (0.25, 0.4, 0.3, 1), 'Pants_Cargo': (0.32, 0.36, 0.3, 1),
      'Shoes_Sneaker': (0.2, 0.2, 0.22, 1), 'Hat_Bucket': (0.9, 0.84, 0.62, 1), 'Gloves': (0.1, 0.1, 0.12, 1)}),
    ('pro', ['Top_Shirt', 'Vest_Fishing', 'Pants_Jeans', 'Shoes_Boots', 'Hat_Cap', 'Glasses'],
     {'Top_Shirt': (0.65, 0.25, 0.22, 1), 'Vest_Fishing': (0.55, 0.52, 0.38, 1), 'Pants_Jeans': (0.22, 0.3, 0.5, 1),
      'Shoes_Boots': (0.15, 0.25, 0.2, 1), 'Hat_Cap': (0.15, 0.25, 0.5, 1), 'Glasses': (0.05, 0.05, 0.06, 1)}),
    ('sea', ['Top_Rash', 'Vest_Life', 'Pants_Wader', 'Shoes_Boots', 'Hat_Straw'],
     {'Top_Rash': (0.12, 0.14, 0.2, 1), 'Vest_Life': (0.95, 0.45, 0.1, 1), 'Pants_Wader': (0.2, 0.28, 0.22, 1),
      'Shoes_Boots': (0.15, 0.15, 0.17, 1), 'Hat_Straw': (0.93, 0.82, 0.5, 1)}),
]

BOOT_CLIP = 0.2  # 장화를 신으면 휴식 자세 높이 0.4m 아래의 바지는 숨긴다 (장화 윗단 ≈ 무릎 - 0.1m)

TRIM = {'Top_Tee': 0.85, 'Top_Long': 0.8, 'Top_Shirt': 0.8, 'Gloves': None, 'Top_Rash': 1.4, 'Vest_Fishing': 0.6, 'Vest_Life': 0.15, 'Pants_Cargo': 0.75,
        'Pants_Jeans': 0.6, 'Pants_Wader': 0.6, 'Shoes_Sneaker': None, 'Shoes_Boots': 0.7, 'Hat_Bucket': 0.6, 'Hat_Cap': 0.8,
        'Hat_Straw': None, 'Glasses': None}


def preview(gender, pose_name, out_prefix):
    R.clear()
    acts = None
    if os.path.exists(UAL):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=UAL)
        for o in [o for o in bpy.data.objects if o not in before]:
            bpy.data.objects.remove(o, do_unlink=True)
        acts = {a.name.split('|')[-1]: a for a in bpy.data.actions}
    _, tex = SOURCES[gender]
    skin = R.tex_mat('skin', os.path.join(CHAR, 'Textures', tex), rough=0.55)
    skin_long = skin_mat(os.path.join(CHAR, 'Textures', tex), (1, 1, 1, 1))
    skin_short = skin_mat(os.path.join(CHAR, 'Textures', tex), (1, 1, 1, 0))
    hair_tex = os.path.join(CHAR, 'Hair', 'T_Hair_1_BaseColor.png' if gender == 'male' else 'T_Hair_2_BaseColor.png')
    rigs = []
    for k, (oname, on, colors) in enumerate(OUTFITS):
        arm, body, made, objs = build(gender)
        body.data.materials.clear()
        body.data.materials.append(skin_long if ('Top_Long' in on or 'Top_Rash' in on or 'Top_Shirt' in on) else skin_short)
        for o in objs:
            if o.type == 'MESH' and o is not body:
                o.data.materials.clear()
                o.data.materials.append(R.tex_mat('eye', color=(0.15, 0.1, 0.08, 1)))
        for name, obj in made.items():
            if name not in on:
                bpy.data.objects.remove(obj, do_unlink=True)
                continue
            c = colors[name]
            t = TRIM[name]
            if name == 'Shoes_Sneaker':
                trim = (0.92, 0.92, 0.9, 1)
            elif name == 'Hat_Straw':
                trim = (0.7, 0.12, 0.1, 1)
            elif name == 'Glasses':
                trim = (0.03, 0.03, 0.03, 1)
            elif name == 'Gloves':
                trim = (0.95, 0.55, 0.15, 1)
            else:
                trim = tuple(min(1, x * t) for x in c[:3]) + (1,)
            pat = tuple(x * 0.55 for x in c[:3]) + (1,) if name == 'Vest_Fishing' else None
            if name == 'Shoes_Sneaker':
                pat = (0.9, 0.9, 0.88, 1)  # 끈
            if name == 'Hat_Straw':
                pat = c
            obj.data.materials.clear()
            clip = BOOT_CLIP if name.startswith('Pants') and 'Shoes_Boots' in on else None
            obj.data.materials.append(cloth_mat(name, c, trim, pat, rough=0.15 if name == 'Glasses' else 0.85, clip_below=clip))
        # 머리카락
        hair_file = os.path.join(CHAR, 'Hair', 'Hair_SimpleParted.fbx' if gender == 'male' else 'Hair_Long.fbx')
        hobjs = R.import_fbx(hair_file)
        harm = next((o for o in hobjs if o.type == 'ARMATURE'), None)
        for o in hobjs:
            if o.type == 'MESH':
                o.data.materials.clear()
                o.data.materials.append(R.tex_mat('hair', hair_tex, rough=0.6))
                o.parent = arm
                for m in o.modifiers:
                    if m.type == 'ARMATURE':
                        m.object = arm
        if harm is not None:
            bpy.data.objects.remove(harm, do_unlink=True)
        rigs.append(arm)
    for k, arm in enumerate(rigs):
        if acts and pose_name in acts:
            R_pose(arm, acts[pose_name], 20)
        e = bpy.data.objects.new('off%d' % k, None)
        bpy.context.scene.collection.objects.link(e)
        arm.parent = e
        e.location.x = (k - 1) * -0.95
    cam = R.setup_scene(res=(1500, 1000), samples=32)
    cam.data.lens = 60
    if '--closeup' in ARGS:
        # 발(운동화·장화)과 상체(깃·장갑·조끼) 근접
        R.shoot(cam, f'{out_prefix}_feet.png', 160, dist=2.8, look_h=0.12)
        R.shoot(cam, f'{out_prefix}_chest.png', 170, dist=3.2, look_h=1.2)
        return
    for yaw in (180, 145, 0):
        R.shoot(cam, f'{out_prefix}_{yaw}.png', yaw, dist=7.0)


def R_pose(arm, action, frame):
    arm.animation_data_create()
    arm.animation_data.action = action
    if hasattr(arm.animation_data, 'action_slot') and action.slots:
        arm.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)


# ---------------------------------------------------------------------------------------------
# 내보내기

def export(gender):
    R.clear()
    arm, body, made, objs = build(gender)
    out = os.path.join(CHAR, 'Angler_%s.fbx' % gender.capitalize())
    for o in bpy.data.objects:
        o.select_set(o.type in ('ARMATURE', 'MESH'))
    # 옷에는 몸에서 복사된 색 레이어가 딸려 온다. Unity는 첫 레이어만 쓰므로 'Col' 하나만 남긴다.
    for name, obj in made.items():
        for c in [c for c in obj.data.color_attributes if c.name != 'Col']:
            obj.data.color_attributes.remove(c)
    # 원본 Quaternius FBX와 같은 축(Z 위, -Y 앞)·단위(UnitScaleFactor 100) → Unity에서 원본과 같은 루트 회전·크기로 들어온다
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'},
                             axis_forward='-Y', axis_up='Z', apply_scale_options='FBX_SCALE_UNITS',
                             add_leaf_bones=False, bake_anim=False, use_armature_deform_only=False,
                             mesh_smooth_type='FACE', colors_type='LINEAR', use_mesh_modifiers=False)
    print('exported', out)


def main():
    gender_list = ['male', 'female']
    if '--only' in ARGS:
        gender_list = [ARGS[ARGS.index('--only') + 1]]
    pose_name = ARGS[ARGS.index('--pose') + 1] if '--pose' in ARGS else 'Idle_Loop'
    for g in gender_list:
        if '--preview' in ARGS:
            out = ARGS[ARGS.index('--out') + 1] if '--out' in ARGS else os.path.join(ROOT, 'docs', 'images', 'character_preview')
            preview(g, pose_name, out + '_' + g)
        else:
            R.clear()
            export(g)


if __name__ == '__main__':
    main()
    sys.stdout.flush()
    os._exit(0)  # bpy 모듈 종료 시 충돌 회피
