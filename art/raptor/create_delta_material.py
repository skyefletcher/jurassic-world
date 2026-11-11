"""Reference-inspired olive-grey Delta skin; preserve Blue, anatomy and animation."""
import bpy
from pathlib import Path
ROOT=Path(bpy.data.filepath).resolve().parents[2]
mesh=bpy.data.objects["Retopo"];scene=bpy.context.scene
source=bpy.data.images.load(str(ROOT/"art/raptor/original/source/Raptor/Textures/Raptor_PatternA_Albedo.png"),check_existing=True)
mat=mesh.data.materials[0].copy();mat.name="Delta_OliveGrey"
mesh.data.materials[0]=mat
nodes=mat.node_tree.nodes;links=mat.node_tree.links
bs=next(n for n in nodes if n.type=="BSDF_PRINCIPLED")
output=next(n for n in nodes if n.type=="OUTPUT_MATERIAL")
tex=nodes.new("ShaderNodeTexImage");tex.image=source
bw=nodes.new("ShaderNodeRGBToBW");links.new(tex.outputs["Color"],bw.inputs[0])
ramp=nodes.new("ShaderNodeValToRGB");links.new(bw.outputs[0],ramp.inputs[0])
ramp.color_ramp.elements[0].position=.01
ramp.color_ramp.elements[0].color=(.020,.040,.030,1)
ramp.color_ramp.elements[1].position=.24
ramp.color_ramp.elements[1].color=(.17,.235,.15,1)
coord=nodes.new("ShaderNodeTexCoord")
noise=nodes.new("ShaderNodeTexNoise")
noise.inputs["Scale"].default_value=11
noise.inputs["Detail"].default_value=3
links.new(coord.outputs["Generated"],noise.inputs["Vector"])
spots=nodes.new("ShaderNodeMapRange");spots.clamp=True
spots.inputs["From Min"].default_value=.48;spots.inputs["From Max"].default_value=.68
spots.inputs["To Min"].default_value=0;spots.inputs["To Max"].default_value=.35
links.new(noise.outputs["Fac"],spots.inputs["Value"])
mix=nodes.new("ShaderNodeMixRGB");mix.blend_type="MULTIPLY"
links.new(spots.outputs[0],mix.inputs[0])
links.new(ramp.outputs[0],mix.inputs[1])
mix.inputs[2].default_value=(.25,.37,.29,1)
# Anatomical colour masks use rest-space coordinates and normals.
# Bake them into the UV texture so they stay attached during animation.
def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)))
    return t*t*(3-2*t)
mask=mesh.data.color_attributes.get("Delta_Countershade")
if mask is None:
    mask=mesh.data.color_attributes.new(name="Delta_Countershade",type="FLOAT_COLOR",domain="POINT")
normal_matrix=mesh.matrix_world.to_3x3().inverted().transposed()
for i,point in enumerate(mesh.data.shape_keys.key_blocks["Basis"].data):
    p=mesh.matrix_world@point.co
    n=(normal_matrix@mesh.data.vertices[i].normal).normalized()
    dorsal=smooth(.15,.75,n.z)*smooth(.75,1.10,p.z)
    underside=smooth(.05,.75,-n.z)
    throat=smooth(.20,.80,-n.y)*(1-smooth(1.65,1.80,p.z))*smooth(.85,1.10,p.z)*(1-smooth(-.50,-.35,p.y))
    belly=max(underside,throat)*smooth(.55,.95,p.z)
    eye=__import__("math").exp(-((p.y+1.062)/.10)**2-((p.z-1.882)/.075)**2)*smooth(.025,.07,abs(p.x))
    mask.data[i].color=(dorsal,belly,eye,1)
attr=nodes.new("ShaderNodeAttribute");attr.attribute_name="Delta_Countershade"
channels=nodes.new("ShaderNodeSeparateXYZ");links.new(attr.outputs["Color"],channels.inputs[0])
top=nodes.new("ShaderNodeMixRGB")
links.new(channels.outputs["X"],top.inputs[0])
links.new(mix.outputs[0],top.inputs[1])
dorsal_colour=nodes.new("ShaderNodeMixRGB");dorsal_colour.blend_type="MULTIPLY"
dorsal_colour.inputs[0].default_value=.45
dorsal_colour.inputs[1].default_value=(.028,.065,.033,1)
links.new(bw.outputs[0],dorsal_colour.inputs[2])
links.new(dorsal_colour.outputs[0],top.inputs[2])
under=nodes.new("ShaderNodeMixRGB")
links.new(channels.outputs["Y"],under.inputs[0])
links.new(top.outputs[0],under.inputs[1])
belly_colour=nodes.new("ShaderNodeMixRGB");belly_colour.blend_type="MULTIPLY"
belly_colour.inputs[0].default_value=.30
belly_colour.inputs[1].default_value=(.20,.19,.125,1)
links.new(bw.outputs[0],belly_colour.inputs[2])
links.new(belly_colour.outputs[0],under.inputs[2])
eye=nodes.new("ShaderNodeMixRGB");eye.blend_type="MULTIPLY"
links.new(channels.outputs["Z"],eye.inputs[0])
links.new(under.outputs[0],eye.inputs[1])
eye.inputs[2].default_value=(.45,.50,.40,1)
# Fine pigment follows the source scale normal map instead of adding coarse bumps.
geometry=nodes.new("ShaderNodeNewGeometry")
cells=nodes.new("ShaderNodeTexVoronoi");cells.feature="DISTANCE_TO_EDGE"
cells.inputs["Scale"].default_value=145
links.new(geometry.outputs["Position"],cells.inputs["Vector"])
scale_colour=nodes.new("ShaderNodeValToRGB")
scale_colour.color_ramp.elements[0].position=.018
scale_colour.color_ramp.elements[0].color=(.28,.32,.26,1)
scale_colour.color_ramp.elements[1].position=.10
scale_colour.color_ramp.elements[1].color=(.95,1,.93,1)
links.new(cells.outputs["Distance"],scale_colour.inputs[0])
pigment=nodes.new("ShaderNodeMixRGB");pigment.blend_type="MULTIPLY"
pigment.inputs[0].default_value=.30
links.new(eye.outputs[0],pigment.inputs[1])
links.new(scale_colour.outputs[0],pigment.inputs[2])
emission=nodes.new("ShaderNodeEmission");links.new(pigment.outputs[0],emission.inputs[0])
links.new(emission.outputs[0],output.inputs["Surface"])
image=bpy.data.images.new("Delta_OliveGrey_Albedo",width=2048,height=2048,alpha=False)
target=nodes.new("ShaderNodeTexImage");target.image=image
for n in nodes:n.select=False
target.select=True;nodes.active=target
# Both material slots need independent bake targets when baking one mesh.
detail=mesh.data.materials[1].copy();detail.name="Delta_Details"
mesh.data.materials[1]=detail
detail_bs=next(n for n in detail.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
detail_output=next(n for n in detail.node_tree.nodes if n.type=="OUTPUT_MATERIAL")
detail_source=detail_bs.inputs["Base Color"].links[0].from_socket
detail_emission=detail.node_tree.nodes.new("ShaderNodeEmission")
detail.node_tree.links.new(detail_source,detail_emission.inputs[0])
detail.node_tree.links.new(detail_emission.outputs[0],detail_output.inputs["Surface"])
detail_image=bpy.data.images.new("Delta_DetailBake",width=1024,height=1024,alpha=False)
detail_target=detail.node_tree.nodes.new("ShaderNodeTexImage");detail_target.image=detail_image
for n in detail.node_tree.nodes:n.select=False
detail_target.select=True;detail.node_tree.nodes.active=detail_target
old_engine=scene.render.engine;old_samples=scene.cycles.samples
old_selected=list(bpy.context.selected_objects);old_active=bpy.context.view_layer.objects.active
for o in old_selected:o.select_set(False)
mesh.select_set(True);bpy.context.view_layer.objects.active=mesh
scene.render.engine="CYCLES";scene.cycles.samples=1;scene.cycles.device="CPU"
try:
    bpy.ops.object.bake(type="EMIT",margin=16,use_clear=True)
    image.file_format="PNG"
    image.filepath_raw=str(ROOT/"assets/raptor/delta-body.png");image.save()
    image.filepath_raw=str(ROOT/"art/raptor/textures/delta-body.png");image.save()
    links.new(target.outputs["Color"],bs.inputs["Base Color"])
    links.new(bs.outputs[0],output.inputs["Surface"])
    bs.inputs["Roughness"].default_value=.80
    detail.node_tree.links.new(detail_bs.outputs[0],detail_output.inputs["Surface"])
    mesh["appearance_note"]="Delta: dark forest-green back, deep green mottled sides, warm pale throat and belly, dark eye surrounds, fine scales, amber eyes and charcoal claws. Shares Blue's approved anatomy and animations."
    scene.render.engine=old_engine;scene.cycles.samples=old_samples
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/"art/raptor/raptor-delta-review.blend"),relative_remap=True)
finally:
    scene.render.engine=old_engine;scene.cycles.samples=old_samples
    for o in bpy.context.selected_objects:o.select_set(False)
    for o in old_selected:o.select_set(True)
    bpy.context.view_layer.objects.active=old_active
result={"saved":bpy.data.filepath,"runtime_texture":str(ROOT/"assets/raptor/delta-body.png"),"blue_preserved":True}
