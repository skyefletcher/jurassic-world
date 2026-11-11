"""Reversible reference-inspired blue/grey material variant.
Run in the approved Blender scene. Uses Blender material nodes and UV baking;
the original texture files, body geometry, rig and actions are not modified.
"""
import bpy
import math
from pathlib import Path
ROOT = Path(bpy.data.filepath).resolve().parents[2]
mesh = bpy.data.objects["Retopo"]
scene = bpy.context.scene
assert "Neck_Back_Transition" not in mesh.data.shape_keys.key_blocks
out = ROOT / "art/raptor/textures"
out.mkdir(parents=True, exist_ok=True)
def smooth(a, b, value):
    t = max(0.0, min(1.0, (value-a)/(b-a)))
    return t*t*(3-2*t)
def interpolate(stations, y):
    if y <= stations[0][0]: return stations[0][1]
    for (a,x),(b,z) in zip(stations, stations[1:]):
        if y <= b:
            t=smooth(a,b,y)
            return x+(z-x)*t
    return stations[-1][1]
centers=[(-1.6,1.76),(-1.35,1.83),(-1.05,1.91),(-.8,1.91),(-.65,1.66),(-.52,1.42),(-.3,1.32),(0,1.25),(.4,1.17),(.8,1.13),(1.2,1.145),(1.7,1.21),(2.4,1.25)]
widths=[(-1.6,.025),(-1.2,.042),(-.8,.045),(-.55,.05),(-.2,.065),(.3,.07),(.8,.04),(1.4,.025),(2.4,.013)]
side_widths=[(-1.6,.02),(-1.3,.07),(-.8,.10),(-.55,.12),(-.2,.22),(.3,.20),(.8,.09),(1.4,.045),(2.4,.015)]
mask = mesh.data.color_attributes.get("ReferenceBlueMask")
if mask is None:
    mask=mesh.data.color_attributes.new(name="ReferenceBlueMask",type="FLOAT_COLOR",domain="POINT")
claws=mesh.data.color_attributes.get("ReferenceClawMask")
if claws is None:
    claws=mesh.data.color_attributes.new(name="ReferenceClawMask",type="FLOAT_COLOR",domain="POINT")
for i,v in enumerate(mesh.data.shape_keys.key_blocks["Basis"].data):
    p=mesh.matrix_world@v.co
    center=interpolate(centers,p.y)
    half=interpolate(widths,p.y)
    waviness=.008*math.sin(p.y*39.0+abs(p.x)*23.0)
    band=1-smooth(half*.7,half*1.5,abs(p.z-center-waviness))
    side=interpolate(side_widths,p.y)
    lateral=smooth(side*.22,side*.65,abs(p.x))
    stripe=band*lateral
    mask.data[i].color=(stripe,stripe,stripe,1)
    dark=1-smooth(1.15,1.5,p.z)
    claws.data[i].color=(dark,dark,dark,1)
original_engine=scene.render.engine
original_samples=scene.cycles.samples
scene.render.engine="CYCLES"
scene.cycles.samples=1
scene.cycles.device="CPU"
original_selection=list(bpy.context.selected_objects)
original_active=bpy.context.view_layer.objects.active
for obj in original_selection: obj.select_set(False)
mesh.select_set(True)
bpy.context.view_layer.objects.active=mesh
outputs=[]
new_mats=[]
for index,slot in enumerate(mesh.material_slots):
    source_material=bpy.data.materials["Body_Mat" if index==0 else "Other_Mat"]
    source_material.use_fake_user=True
    mat=source_material.copy()
    mat.name="BlueGrey_Body" if index==0 else "BlueGrey_Details"
    slot.material=mat
    new_mats.append(mat)
    nodes=mat.node_tree.nodes; links=mat.node_tree.links
    bsdf=next(n for n in nodes if n.type=="BSDF_PRINCIPLED")
    output=next(n for n in nodes if n.type=="OUTPUT_MATERIAL")
    original_color=bsdf.inputs["Base Color"].links[0].from_socket
    bw=nodes.new("ShaderNodeRGBToBW")
    links.new(original_color,bw.inputs["Color"])
    ramp=nodes.new("ShaderNodeValToRGB")
    links.new(bw.outputs[0],ramp.inputs[0])
    ramp.color_ramp.elements[0].position=.0
    ramp.color_ramp.elements[0].color=(.08,.10,.11,1)
    ramp.color_ramp.elements[1].position=.18
    ramp.color_ramp.elements[1].color=(.65,.70,.72,1)
    if index==0:
        attr=nodes.new("ShaderNodeAttribute");attr.attribute_name="ReferenceBlueMask"
        blue=nodes.new("ShaderNodeMixRGB");blue.blend_type="MULTIPLY"
        blue.inputs[0].default_value=1
        links.new(ramp.outputs["Color"],blue.inputs[1])
        blue.inputs[2].default_value=(.055,.28,.66,1)
        mix=nodes.new("ShaderNodeMixRGB")
        links.new(attr.outputs["Fac"],mix.inputs[0])
        links.new(ramp.outputs["Color"],mix.inputs[1])
        links.new(blue.outputs["Color"],mix.inputs[2])
    else:
        attr=nodes.new("ShaderNodeAttribute");attr.attribute_name="ReferenceClawMask"
        mix=nodes.new("ShaderNodeMixRGB")
        links.new(attr.outputs["Fac"],mix.inputs[0])
        links.new(original_color,mix.inputs[1])
        mix.inputs[2].default_value=(.025,.032,.036,1)
    emission=nodes.new("ShaderNodeEmission")
    links.new(mix.outputs["Color"],emission.inputs["Color"])
    links.new(emission.outputs[0],output.inputs["Surface"])
    size=2048 if index==0 else 1024
    image=bpy.data.images.new("Raptor_BlueGrey_BaseColor" if index==0 else "Raptor_BlueGrey_Details",width=size,height=size,alpha=False)
    image.filepath_raw=str(out/("raptor-bluegrey-body.png" if index==0 else "raptor-bluegrey-details.png"))
    image.file_format="PNG"
    target=nodes.new("ShaderNodeTexImage");target.image=image
    for n in nodes: n.select=False
    target.select=True;nodes.active=target
    outputs.append((mat,bsdf,output,target,image))
try:
    bpy.ops.object.bake(type="EMIT",margin=16,use_clear=True)
    for mat,bsdf,output,target,image in outputs:
        image.save()
        links=mat.node_tree.links
        links.new(target.outputs["Color"],bsdf.inputs["Base Color"])
        links.new(bsdf.outputs["BSDF"],output.inputs["Surface"])
        # Skin is rough; keep the source's scale normal map and amber eye atlas.
        bsdf.inputs["Roughness"].default_value=.78 if mat.name.startswith("BlueGrey_Body") else .48
    mesh["appearance_note"]="Reference-inspired pale grey skin with blue lateral stripe and charcoal claws. Original neck and body retained."
    scene.render.engine=original_engine
    scene.cycles.samples=original_samples
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/"art/raptor/raptor-blue-review.blend"),relative_remap=True)
finally:
    scene.render.engine=original_engine
    scene.cycles.samples=original_samples
    for obj in bpy.context.selected_objects: obj.select_set(False)
    for obj in original_selection: obj.select_set(True)
    bpy.context.view_layer.objects.active=original_active
result={"saved":bpy.data.filepath,"textures":[str(image.filepath) for *_,image in outputs],"geometry_changed":False}
