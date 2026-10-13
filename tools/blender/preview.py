"""Preview renderer: import FBX files (or use current scene), apply simple materials, render front/34/side views."""
import bpy, math, mathutils, os

def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    return [o for o in bpy.data.objects if o not in before]

def tex_mat(name, img_path=None, color=(0.8,0.8,0.8,1), rough=0.6):
    m = bpy.data.materials.new(name); m.use_nodes = True
    bsdf = m.node_tree.nodes['Principled BSDF']
    bsdf.inputs['Roughness'].default_value = rough
    if img_path:
        t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(img_path)
        m.node_tree.links.new(t.outputs['Color'], bsdf.inputs['Base Color'])
    else:
        bsdf.inputs['Base Color'].default_value = color
    return m

def setup_scene(height=1.8, samples=24, res=(560, 900)):
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = samples
    sc.cycles.use_denoising = False
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = False
    w = bpy.data.worlds.new('W'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs['Color'].default_value = (0.55, 0.62, 0.7, 1)
    w.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.7
    sun = bpy.data.objects.new('Sun', bpy.data.lights.new('Sun', 'SUN')); sc.collection.objects.link(sun)
    sun.data.energy = 3.5; sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); sc.collection.objects.link(cam)
    cam.data.lens = 70; sc.camera = cam
    return cam

def shoot(cam, out, yaw_deg, height=1.8, dist=4.6, look_h=None):
    look_h = look_h if look_h is not None else height*0.52
    a = math.radians(yaw_deg)
    # character faces -Y in Blender after import (Unity +Z forward = Blender -Y?) -> we pass yaw relative to front
    cam.location = (math.sin(a)*dist, -math.cos(a)*dist, look_h + 0.1)
    d = mathutils.Vector((0, 0, look_h)) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = out
    bpy.ops.render.render(write_still=True)
