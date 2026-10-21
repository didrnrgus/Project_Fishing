"""실사풍 나무·덤불·바위·풀을 만든다 (docs/17). 에셋 스토어 식생(Conifers [BOTD]·Realistic Tree 9·10·Nature Starter Kit 2·
Rock and Boulders 2·Grass Flowers Pack Free)을 대신한다.

  bpyenv/Scripts/python -I tools/blender/build_vegetation.py [--only 이름,...] [--preview <폴더>]

- 나무·덤불: ez-tree(MIT, Daniel Greenheck — tools/eztree, 처음 한 번 npm ci)로 형상을 뽑아(가지 = 관, 잎 = 십자로 겹친 사각형)
  잎 법선을 수관 가운데에서 바깥으로 바꾸고(부드러운 수관 음영), 먼 거리(LOD1)는 나무를 앞에서 찍은 그림을 십자로 세운 판 두 장.
  껍질·잎 텍스처는 ez-tree에 든 것 (껍질: Poly Haven·TextureCan CC0, 잎: ez-tree MIT).
- 바위: Poly Haven(CC0) 모델의 LOD 중 삼각형이 적당한 것 + 더 줄인 것. 모델은 Temp/polyhaven/models/<id>/ 에 한 번만 받는다.
- 풀·들꽃: Poly Haven 모델을 옆에서 찍어 투명 배경 카드 그림(grass0N·grassFlower0N.png)으로 (GrassField가 교차한 판 두 장에 붙인다).
- 출력: Assets/Vegetation/<종류>/<이름>.fbx (오브젝트 이름 = "LOD0_재질" · "LOD1_재질") + <이름>_<재질>_diff.png(잎은 알파 포함)·_nor.png.
  재질·LODGroup·프리팹은 Unity 쪽 Editor/VegetationBake가 만든다. 출처는 Assets/Vegetation/SOURCES.md.
"""
import argparse
import json
import math
import os
import random
import shutil
import subprocess
import sys
import urllib.request

argv = sys.argv[1:]
ap = argparse.ArgumentParser()
ap.add_argument("--only", default="")
ap.add_argument("--preview", default="")
args = ap.parse_args(argv)
sys.argv = sys.argv[:1]

import bpy  # noqa: E402
import bmesh  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402
from PIL import Image  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CACHE = os.path.join(ROOT, "Temp", "polyhaven", "models")
OUT = os.path.join(ROOT, "Assets", "Vegetation")
UA = {"User-Agent": "ProjectFishing-build/1.0"}  # 기본 urllib 요청은 Poly Haven이 막는다(403)

EZTREE = os.path.join(ROOT, "tools", "eztree")
EZ_CACHE = os.path.join(ROOT, "Temp", "eztree")

# (출력 이름, 원본, 종류, 설정)
#  conifer·tree·bush: 원본 = ez-tree 프리셋 이름, seed = 같은 프리셋의 다른 모양
#  rock: 원본 = Poly Haven id, tris = LOD0 삼각형 수 목표 / card: 원본 = Poly Haven id
ASSETS = [
    ("Conifer_Tall", "Pine Large", "conifer", {"seed": 44166}),
    ("Conifer_Medium", "Pine Medium", "conifer", {"seed": 13977}),
    ("Conifer_Small", "Pine Small", "conifer", {}),
    ("Broadleaf_1", "Oak Large", "tree", {}),
    ("Broadleaf_2", "Oak Medium", "tree", {}),
    ("Broadleaf_3", "Ash Medium", "tree", {}),
    ("Broadleaf_4", "Aspen Medium", "tree", {}),
    ("Small_1", "Oak Small", "tree", {}),
    ("Small_2", "Ash Small", "tree", {}),
    ("Small_3", "Aspen Small", "tree", {}),
    ("Small_4", "Oak Small", "tree", {"seed": 2024}),
    ("Bush_1", "Bush 1", "bush", {}),
    ("Bush_2", "Bush 2", "bush", {}),
    ("Bush_3", "Bush 3", "bush", {}),
    ("Bush_4", "Bush 1", "bush", {"seed": 777}),
    ("Bush_5", "Bush 2", "bush", {"seed": 4242}),
    ("Bush_6", "Bush 3", "bush", {"seed": 9001}),
    ("Rock_1", "boulder_01", "rock", {"tris": 5000}),
    ("Rock_2", "namaqualand_boulder_02", "rock", {"tris": 5000}),
    ("Rock_3", "namaqualand_boulder_03", "rock", {"tris": 5000}),
    ("Rock_4", "namaqualand_boulder_04", "rock", {"tris": 5000}),
    ("Rock_5", "namaqualand_boulder_05", "rock", {"tris": 4000}),
    ("Rock_6", "namaqualand_boulder_06", "rock", {"tris": 4000}),
    ("Rock_7", "rock_07", "rock", {"tris": 3000}),
    ("Rock_8", "rock_09", "rock", {"tris": 3000}),
    ("Rock_9", "stone_01", "rock", {"tris": 3000}),
    ("grass01", "grass_medium_01", "card", {}),
    ("grass02", "grass_medium_02", "card", {}),
    ("grassFlower01", "flower_gazania", "card", {}),
    ("grassFlower02", "flower_ursinia", "card", {}),
    ("grassFlower03", "dandelion_01", "card", {}),
    ("grassFlower04", "flower_empodium", "card", {}),
    ("grassFlower05", "periwinkle_plant", "card", {}),
]
FOLDER = {"conifer": "Conifers", "tree": "Broadleaf", "bush": "Bushes", "rock": "Rocks", "card": "Grass"}


def get(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA)) as r:
        return r.read()


def fetch(asset):
    """FBX 1k와 텍스처를 받아 둔다. FBX 경로."""
    d = os.path.join(CACHE, asset)
    fbx = os.path.join(d, asset + ".fbx")
    if os.path.exists(fbx):
        return fbx
    info = json.loads(get(f"https://api.polyhaven.com/files/{asset}"))["fbx"]["1k"]["fbx"]
    os.makedirs(d, exist_ok=True)
    for rel, v in info.get("include", {}).items():
        p = os.path.join(d, rel)
        os.makedirs(os.path.dirname(p), exist_ok=True)
        with open(p, "wb") as f:
            f.write(get(v["url"]))
    print("  받는 중", info["url"], f"{info['size'] / 1e6:.0f}MB")
    tmp = fbx + ".part"
    with open(tmp, "wb") as f:
        f.write(get(info["url"]))
    os.replace(tmp, fbx)
    return fbx


def textures_for(asset, mat_name):
    """재질 이름으로 받은 텍스처 찾기: <재질>_diff·_nor_gl·_alpha (예: island_tree_02_leaves_diff_1k.png)."""
    d = os.path.join(CACHE, asset, "textures")
    found = {}
    if not os.path.isdir(d):
        return found
    base = mat_name.split(".")[0]
    for f in os.listdir(d):
        low = f.lower()
        for key, tag in (("diff", "_diff"), ("nor", "_nor_gl"), ("alpha", "_alpha")):
            if low.startswith(base.lower() + tag) and key not in found:
                found[key] = os.path.join(d, f)
    return found


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(path):
    """FBX를 읽어 메쉬만. 부모(빈 오브젝트, 축 회전·0.01 배율)를 떼어 월드 변환을 오브젝트에 직접 둔다."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    meshes = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    for o in meshes:
        if o.parent is not None:
            mw = o.matrix_world.copy()
            o.parent = None
            o.matrix_world = mw
    return meshes


def apply_all(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def decimate(obj, target):
    t = tri_count(obj)
    if t <= target or t == 0:
        return
    m = obj.modifiers.new("dec", 'DECIMATE')
    m.ratio = max(0.002, target / t)
    m.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=m.name)


def save_rgba(diff_path, alpha_path, out_path, size):
    im = Image.open(diff_path).convert("RGB")
    if alpha_path:
        a = Image.open(alpha_path).convert("L").resize(im.size)
        im = im.convert("RGBA")
        im.putalpha(a)
    im = im.resize((size, size), Image.LANCZOS) if max(im.size) > size else im
    im.save(out_path)


def save_tex(path, out_path, size):
    if path.lower().endswith(".exr"):
        # PIL은 EXR을 못 읽는다 → 블렌더로 (법선 맵은 색 공간 변환 없이)
        img = bpy.data.images.load(path)
        img.colorspace_settings.name = "Non-Color"
        if max(img.size) > size:
            img.scale(size, size)
        img.filepath_raw = out_path
        img.file_format = "PNG"
        img.save()
        return
    im = Image.open(path)
    im = im.convert("RGB")
    if max(im.size) > size:
        im = im.resize((size, size), Image.LANCZOS)
    im.save(out_path)


def render_material(mat_name, tex):
    """찍기용 재질: 알베도(+알파) 텍스처, 알파는 잘라 낸다."""
    m = bpy.data.materials.new("R_" + mat_name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.8
    if "diff" in tex:
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = bpy.data.images.load(tex["diff"])
        nt.links.new(t.outputs["Color"], bsdf.inputs["Base Color"])
        if tex.get("rgba"):
            nt.links.new(t.outputs["Alpha"], bsdf.inputs["Alpha"])
    if "alpha" in tex:
        ta = nt.nodes.new("ShaderNodeTexImage")
        ta.image = bpy.data.images.load(tex["alpha"])
        ta.image.colorspace_settings.name = "Non-Color"
        nt.links.new(ta.outputs["Color"], bsdf.inputs["Alpha"])
        m.blend_method = 'CLIP' if hasattr(m, "blend_method") else None
    return m


def setup_render(size):
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = 24
    sc.cycles.use_denoising = False
    sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.film_transparent = True
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.75, 0.8, 0.85, 1)
    w.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.9
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
    sun.data.energy = 2.2
    sun.rotation_euler = (math.radians(40), 0, math.radians(25))
    sc.collection.objects.link(sun)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type = 'ORTHO'
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def shoot(cam, objs, out_png, side, size, pad=1.02):
    """정면(-Y 쪽에서) 또는 옆(+X 쪽에서) 직교 사진. 아래 가운데가 원점이 되게 화면에 맞춘다."""
    mn, mx = bounds(objs)
    w = (mx.x - mn.x) if not side else (mx.y - mn.y)
    h = mx.z - mn.z
    ext = max(w, h) * pad
    cx = (mn.x + mx.x) / 2 if not side else (mn.y + mx.y) / 2
    cam.data.ortho_scale = ext
    zc = mn.z + ext / 2  # 아래를 화면 바닥에
    if side:
        cam.location = (mx.x + 50, cx, zc)
        cam.rotation_euler = (math.radians(90), 0, math.radians(90))
    else:
        cam.location = (cx, mn.y - 50, zc)
        cam.rotation_euler = (math.radians(90), 0, 0)
    cam.data.clip_end = 200
    bpy.context.scene.render.filepath = out_png
    bpy.ops.render.render(write_still=True)
    return ext


def to_origin(objs):
    """밑면 가운데를 원점으로."""
    mn, mx = bounds(objs)
    off = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    for o in objs:
        o.location -= off
    apply_all(objs)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')


def billboard(name, ext, h_ratio, tex_png):
    """십자 판 두 장 (LOD1). ext = 사진 한 변(m), 판은 그 크기, 밑이 원점."""
    s = ext / 2
    verts, faces, uvs = [], [], []
    for k, (ax, ay) in enumerate(((1, 0), (0, 1))):
        i0 = len(verts)
        verts += [(-s * ax, -s * ay, 0), (s * ax, s * ay, 0), (s * ax, s * ay, ext), (-s * ax, -s * ay, ext)]
        uvs += [(0, 0), (1, 0), (1, 1), (0, 1)]
        faces.append((i0, i0 + 1, i0 + 2, i0 + 3))
    me = bpy.data.meshes.new("LOD1_billboard")
    me.from_pydata(verts, [], faces)
    me.update()
    uv = me.uv_layers.new(name="UVMap")
    for poly in me.polygons:
        for li in poly.loop_indices:
            uv.data[li].uv = uvs[me.loops[li].vertex_index]
    me.normals_split_custom_set([(0, 0, 1)] * len(me.loops))  # 판은 위에서 빛을 받게
    mat = bpy.data.materials.new("billboard")
    me.materials.append(mat)
    o = bpy.data.objects.new("LOD1_billboard", me)
    bpy.context.scene.collection.objects.link(o)
    return o


def export(objs, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'}, apply_scale_options='FBX_SCALE_UNITS',
                             mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False,
                             path_mode='STRIP', axis_forward='-Z', axis_up='Y')


def ez_dump(name, preset, seed):
    """ez-tree 형상 JSON (tools/eztree/dump.mjs, 처음엔 npm ci)."""
    out = os.path.join(EZ_CACHE, name + ".json")
    if not os.path.isdir(os.path.join(EZTREE, "node_modules")):
        subprocess.run("npm ci --no-audit --no-fund", cwd=EZTREE, shell=True, check=True)
    job = f"{name}:{preset}" + (f":{seed}" if seed else "")
    subprocess.run(["node", os.path.join(EZTREE, "dump.mjs"), EZ_CACHE, job], check=True)
    with open(out, encoding="utf-8") as f:
        return json.load(f)


def ez_texture(kind, name):
    """ez-tree에 든 텍스처: kind = bark(oak_color_1k.jpg 등) / leaves(oak_color.png)."""
    return os.path.join(EZTREE, "node_modules", "@dgreenheck", "ez-tree", "src", "lib", "assets", kind, name)


def ez_mesh(obj_name, part, center=None):
    """ez-tree 버퍼(Y 위) → 블렌더 메쉬(Z 위). center가 있으면 법선을 거기서 바깥으로 (잎)."""
    pos = np.array(part["position"], dtype=np.float64).reshape(-1, 3)
    uv = np.array(part["uv"], dtype=np.float64).reshape(-1, 2)
    idx = np.array(part["index"], dtype=np.int64).reshape(-1, 3)
    verts = np.c_[pos[:, 0], -pos[:, 2], pos[:, 1]]
    if center is not None:
        nrm = verts - np.array(center)
        nrm /= np.linalg.norm(nrm, axis=1, keepdims=True) + 1e-6
    else:
        n = np.array(part["normal"], dtype=np.float64).reshape(-1, 3)
        nrm = np.c_[n[:, 0], -n[:, 2], n[:, 1]]
    me = bpy.data.meshes.new(obj_name)
    me.from_pydata([tuple(v) for v in verts], [], [tuple(t) for t in idx])
    me.update()
    layer = me.uv_layers.new(name="UVMap")
    loops_v = np.zeros(len(me.loops), dtype=np.int64)
    me.loops.foreach_get("vertex_index", loops_v)
    layer.data.foreach_set("uv", uv[loops_v].ravel())
    me.normals_split_custom_set([tuple(nrm[i]) for i in loops_v])
    o = bpy.data.objects.new(obj_name, me)
    bpy.context.scene.collection.objects.link(o)
    return o


def build_plant(name, preset, kind, opt):
    clear()
    dump = ez_dump(name, preset, opt.get("seed"))
    bark_type, leaf_type = dump["options"]["bark"], dump["options"]["leaves"]
    bark = ez_mesh("LOD0_bark", dump["bark"])
    mn, mx = bounds([bark])
    leaves = None
    if dump["leaves"]["index"]:
        lv = ez_mesh("LOD0_leaves", dump["leaves"])
        lmn, lmx = bounds([lv])
        center = ((lmn.x + lmx.x) / 2, (lmn.y + lmx.y) / 2, lmn.z + (lmx.z - lmn.z) * 0.45)
        bpy.data.objects.remove(lv)
        leaves = ez_mesh("LOD0_leaves", dump["leaves"], center)
    objs = [o for o in (bark, leaves) if o]
    to_origin(objs)
    # 줄기·가지(관)는 덤불 3천·나무 6천 삼각형까지 줄인다 (덤불 3은 1만 4천이었다)
    decimate(bark, 3000 if kind == "bush" else 6000)
    out_dir = os.path.join(OUT, FOLDER[kind])
    os.makedirs(out_dir, exist_ok=True)
    tex = {
        "bark": {"diff": ez_texture("bark", f"{bark_type}_color_1k.jpg"), "nor": ez_texture("bark", f"{bark_type}_normal_1k.jpg")},
        "leaves": {"diff": ez_texture("leaves", f"{leaf_type}_color.png"), "rgba": True},
    }
    # 껍질·잎 텍스처는 종류마다 하나 (Assets/Vegetation/Textures/Bark_oak_diff.png · Leaves_oak.png), 재질도 종류마다 하나 (VegetationBake)
    tex_dir = os.path.join(OUT, "Textures")
    os.makedirs(tex_dir, exist_ok=True)
    for src, dst in ((tex["bark"]["diff"], f"Bark_{bark_type}_diff.png"), (tex["bark"]["nor"], f"Bark_{bark_type}_nor.png")):
        if not os.path.exists(os.path.join(tex_dir, dst)):
            save_tex(src, os.path.join(tex_dir, dst), 1024)
    if leaves and not os.path.exists(os.path.join(tex_dir, f"Leaves_{leaf_type}.png")):
        shutil.copyfile(tex["leaves"]["diff"], os.path.join(tex_dir, f"Leaves_{leaf_type}.png"))
    with open(os.path.join(out_dir, name + ".json"), "w", encoding="utf-8") as f:
        json.dump({"bark": bark_type, "leaves": leaf_type if leaves else None, "preset": preset, "seed": dump["options"]["seed"]}, f)
    print(f"  {name} ({preset}): " + ", ".join(f"{o.name} {tri_count(o)}" for o in objs))
    # LOD1: 앞에서 찍은 그림을 십자 판에
    cam = setup_render(512)
    for o in objs:
        o.data.materials.append(render_material(o.name, tex[o.name[5:]]))
    png = os.path.join(out_dir, f"{name}_billboard_diff.png")
    ext = shoot(cam, objs, png, False, 512)
    bb = billboard(name, ext, 1.0, png)
    if args.preview:
        os.makedirs(args.preview, exist_ok=True)
        bb.hide_render = True
        shoot(cam, objs, os.path.join(args.preview, name + "_side.png"), True, 512)
    for o in objs:
        o.data.materials.clear()
        o.data.materials.append(bpy.data.materials.new(o.name[5:]))
    export(objs + [bb], os.path.join(out_dir, name + ".fbx"))


def build_rock(name, asset, opt):
    clear()
    objs = import_fbx(fetch(asset))
    # 원본 LOD 중 목표 삼각형 수에 가장 가까운 것(넘으면 줄인다)
    objs.sort(key=lambda o: tri_count(o))
    pick = next((o for o in objs if tri_count(o) >= opt["tris"] * 0.7), objs[-1])
    for o in objs:
        if o is not pick:
            bpy.data.objects.remove(o)
    apply_all([pick])
    decimate(pick, opt["tris"])
    to_origin([pick])
    pick.name = pick.data.name = "LOD0_rock"
    lod1 = pick.copy()
    lod1.data = pick.data.copy()
    bpy.context.scene.collection.objects.link(lod1)
    decimate(lod1, max(300, opt["tris"] // 5))
    lod1.name = lod1.data.name = "LOD1_rock"
    mat = pick.data.materials[0].name if pick.data.materials else asset
    tex = textures_for(asset, mat) or textures_for(asset, asset)
    out_dir = os.path.join(OUT, FOLDER["rock"])
    os.makedirs(out_dir, exist_ok=True)
    if "diff" in tex:
        save_tex(tex["diff"], os.path.join(out_dir, f"{name}_rock_diff.png"), 1024)
    if "nor" in tex:
        save_tex(tex["nor"], os.path.join(out_dir, f"{name}_rock_nor.png"), 1024)
    for o in (pick, lod1):
        o.data.materials.clear()
        o.data.materials.append(bpy.data.materials.new("rock"))
    export([pick, lod1], os.path.join(out_dir, name + ".fbx"))
    print(f"  {name}: LOD0 {tri_count(pick)}, LOD1 {tri_count(lod1)}")


def build_card(name, asset):
    """풀·들꽃 카드: 옆에서 찍은 투명 배경 그림. 포기 하나만 (여러 개면 가장 큰 것)."""
    clear()
    objs = import_fbx(fetch(asset))
    lod0 = [o for o in objs if "_LOD" not in o.name or o.name.endswith("_LOD0")] or objs
    for o in objs:
        if o not in lod0:
            bpy.data.objects.remove(o)
    objs = lod0
    apply_all(objs)
    # 모델에 크기가 다른 포기 여러 개가 한 줄로 들어 있다 → 한 오브젝트면 떨어진 조각으로 나눠, 가장 큰 포기 하나만 찍는다
    if len(objs) == 1:
        bpy.ops.object.select_all(action='DESELECT')
        objs[0].select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.separate(type='LOOSE')
        bpy.ops.object.mode_set(mode='OBJECT')
        objs = [o for o in bpy.context.selected_objects if o.type == 'MESH']
        if len(objs) > 40:  # 잎 하나하나가 조각이면 나누지 말고 덩어리 무리로: x 위치로 묶는다
            xs = sorted(objs, key=lambda o: bounds([o])[0].x)
            groups, cur, edge = [], [], None
            for o in xs:
                mn, mx = bounds([o])
                if edge is not None and mn.x > edge + 0.05:
                    groups.append(cur)
                    cur = []
                cur.append(o)
                edge = mx.x if edge is None else max(edge, mx.x)
            groups.append(cur)
            keep = max(groups, key=lambda g: (lambda b: b[1].z - b[0].z)(bounds(g)))
        else:
            keep = [max(objs, key=lambda o: o.dimensions.z)]
    else:
        keep = [max(objs, key=lambda o: o.dimensions.z)]
    for o in objs:
        if o not in keep:
            bpy.data.objects.remove(o)
    objs = keep
    # 한 포기만 찍으면 게임에서 성긴 점처럼 보였다 → 풀은 다섯, 들꽃은 둘을 돌리고 줄여 겹친 포기로
    rnd = random.Random(sum(map(ord, name)))
    to_origin(objs)  # 밑면 가운데가 원점 (조각으로 나눈 오브젝트는 원점이 원래 모델 자리에 남아 있었다)
    mn, mx = bounds(objs)
    w = max(mx.x - mn.x, mx.y - mn.y, 0.05)
    print(f"    포기 {len(objs)}개, 폭 {w:.2f}m")
    for i in range(2 if name.startswith("grass0") else 1):
        for o in list(keep):
            d = o.copy()
            bpy.context.scene.collection.objects.link(d)
            d.rotation_euler = (0, 0, rnd.uniform(0, 6.283))
            k = rnd.uniform(0.7, 0.95)
            d.scale = (k, k, k)
            d.location = (rnd.uniform(-0.45, 0.45) * w, rnd.uniform(-0.3, 0.3) * w, 0)
            objs.append(d)
    cam = setup_render(512)
    for o in objs:
        for i, m in enumerate(o.data.materials):
            tex = textures_for(asset, m.name) if m else {}
            if not tex:
                tex = textures_for(asset, asset)
            o.data.materials[i] = render_material(m.name if m else "m", tex)
    out_dir = os.path.join(OUT, FOLDER["card"])
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, name + ".png")
    shoot(cam, objs, out, False, 512, pad=1.0)
    # 게임 풀 셰이더(위를 보는 법선)에서 어둡게 보여 조금 밝게. 풀은 마른 짚 색이라 잔디(밝은 초록) 위에서 갈색 덩어리로 튀었다 → 초록 쪽으로
    im = Image.open(out).convert("RGBA")
    a = im.getchannel("A")
    hsv = np.asarray(im.convert("RGB").convert("HSV"), dtype=np.float32)
    if name.startswith("grass0"):
        hsv[..., 0] = hsv[..., 0] * 0.4 + 70 * 0.6           # 색상 → 초록(약 100°)
        hsv[..., 1] = np.clip(hsv[..., 1] * 1.3 + 25, 0, 255)
    hsv[..., 2] = np.clip(hsv[..., 2] * 1.18, 0, 255)
    rgb = Image.fromarray(hsv.astype(np.uint8), "HSV").convert("RGB")
    rgb.putalpha(a)
    rgb.save(out)
    print(f"  {name}: 카드")


def main():
    only = set(filter(None, args.only.split(",")))
    for name, asset, kind, opt in ASSETS:
        if only and name not in only:
            continue
        print(name, "<-", asset)
        if kind == "rock":
            build_rock(name, asset, opt)
        elif kind == "card":
            build_card(name, asset)
        else:
            build_plant(name, asset, kind, opt)


if __name__ == "__main__":
    main()
