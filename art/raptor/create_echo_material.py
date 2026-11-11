"""Scar-free Echo: olive-brown skin with broken tan markings, based on the supplied reference."""
import bpy
from pathlib import Path
ROOT=Path(bpy.data.filepath).resolve().parents[2]
mesh=bpy.data.objects["Retopo"];scene=bpy.context.scene
source=bpy.data.images.load(str(ROOT/"art/raptor/original/source/Raptor/Textures/Raptor_PatternA_Albedo.png"),check_existing=True)
mat=mesh.data.materials[0].copy();mat.name="Echo_GoldenTeal"
mesh.data.materials[0]=mat
nodes=mat.node_tree.nodes;links=mat.node_tree.links
bs=next(n for n in nodes if n.type=="BSDF_PRINCIPLED")
output=next(n for n in nodes if n.type=="OUTPUT_MATERIAL")
tex=nodes.new("ShaderNodeTexImage");tex.image=source
bw=nodes.new("ShaderNodeRGBToBW");links.new(tex.outputs["Color"],bw.inputs[0])
ramp=nodes.new("ShaderNodeValToRGB");links.new(bw.outputs[0],ramp.inputs[0])
ramp.color_ramp.elements[0].position=.01
ramp.color_ramp.elements[0].color=(.14,.09,.032,1)
ramp.color_ramp.elements[1].position=.24
ramp.color_ramp.elements[1].color=(.43,.29,.115,1)
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
mix.inputs[2].default_value=(.65,.48,.27,1)
# Anatomical colour masks use rest-space coordinates and normals.
# Bake them into the UV texture so they stay attached during animation.
def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)))
    return t*t*(3-2*t)
mask=mesh.data.color_attributes.get("Echo_Countershade")
if mask is None:
    mask=mesh.data.color_attributes.new(name="Echo_Countershade",type="FLOAT_COLOR",domain="POINT")
normal_matrix=mesh.matrix_world.to_3x3().inverted().transposed()
photo_field=mesh.data.color_attributes.get("Echo_ReferencePosition")
if photo_field is None:
    photo_field=mesh.data.color_attributes.new(name="Echo_ReferencePosition",type="FLOAT_COLOR",domain="POINT")
photo_mask=mesh.data.color_attributes.get("Echo_ReferenceMasks")
if photo_mask is None:
    photo_mask=mesh.data.color_attributes.new(name="Echo_ReferenceMasks",type="FLOAT_COLOR",domain="POINT")
for i,point in enumerate(mesh.data.shape_keys.key_blocks["Basis"].data):
    p=mesh.matrix_world@point.co
    n=(normal_matrix@mesh.data.vertices[i].normal).normalized()
    dorsal=smooth(.15,.75,n.z)*smooth(.75,1.10,p.z)
    underside=smooth(.05,.75,-n.z)
    throat=smooth(.20,.80,-n.y)*(1-smooth(1.65,1.80,p.z))*smooth(.85,1.10,p.z)*(1-smooth(-.50,-.35,p.y))
    belly=max(underside,throat)*smooth(.55,.95,p.z)
    eye=__import__("math").exp(-((p.y+1.062)/.10)**2-((p.z-1.882)/.075)**2)*smooth(.025,.07,abs(p.x))
    mask.data[i].color=(dorsal,belly,eye,1)
    photo_field.data[i].color=((p.y+2)/5,p.z/3,0,1)
    band_region=1.0-belly*.80 # Stripes on the entire body, fading into a cream belly.
    muzzle=(1-smooth(-1.45,-1.10,p.y))*(1-smooth(1.84,1.98,p.z))
    photo_mask.data[i].color=(band_region,muzzle*.20,0,1)
attr=nodes.new("ShaderNodeAttribute");attr.attribute_name="Echo_Countershade"
channels=nodes.new("ShaderNodeSeparateXYZ");links.new(attr.outputs["Color"],channels.inputs[0])
top=nodes.new("ShaderNodeMixRGB")
links.new(channels.outputs["X"],top.inputs[0])
links.new(mix.outputs[0],top.inputs[1])
dorsal_colour=nodes.new("ShaderNodeMixRGB");dorsal_colour.blend_type="MULTIPLY"
dorsal_colour.inputs[0].default_value=.45
dorsal_colour.inputs[1].default_value=(.36,.24,.082,1)
links.new(bw.outputs[0],dorsal_colour.inputs[2])
links.new(dorsal_colour.outputs[0],top.inputs[2])
under=nodes.new("ShaderNodeMixRGB")
links.new(channels.outputs["Y"],under.inputs[0])
links.new(top.outputs[0],under.inputs[1])
belly_colour=nodes.new("ShaderNodeMixRGB");belly_colour.blend_type="MULTIPLY"
belly_colour.inputs[0].default_value=.30
belly_colour.inputs[1].default_value=(.47,.43,.30,1)
links.new(bw.outputs[0],belly_colour.inputs[2])
links.new(belly_colour.outputs[0],under.inputs[2])
eye=nodes.new("ShaderNodeMixRGB");eye.blend_type="MULTIPLY"
links.new(channels.outputs["Z"],eye.inputs[0])
links.new(under.outputs[0],eye.inputs[1])
eye.inputs[2].default_value=(.06,.16,.13,1)
# Broken ochre/tan bands follow rest-space anatomy, then bake into the UVs.
def calc(op,a,b=None):
    node=nodes.new("ShaderNodeMath");node.operation=op
    for i,value in enumerate([a,b] if b is not None else [a]):
        if isinstance(value,(int,float)):node.inputs[i].default_value=value
        else:links.new(value,node.inputs[i])
    return node.outputs[0]
position_attr=nodes.new("ShaderNodeAttribute");position_attr.attribute_name="Echo_ReferencePosition"
position_parts=nodes.new("ShaderNodeSeparateXYZ");links.new(position_attr.outputs["Color"],position_parts.inputs[0])
sy=calc("SUBTRACT",calc("MULTIPLY",position_parts.outputs["X"],5),2)
sz=calc("MULTIPLY",position_parts.outputs["Y"],3)
region_attr=nodes.new("ShaderNodeAttribute");region_attr.attribute_name="Echo_ReferenceMasks"
regions=nodes.new("ShaderNodeSeparateXYZ");links.new(region_attr.outputs["Color"],regions.inputs[0])
# Distort stripes at several scales to create tapered, branching markings.
stripe_geometry=nodes.new("ShaderNodeNewGeometry")
warp=nodes.new("ShaderNodeTexNoise");warp.inputs["Scale"].default_value=5.5;warp.inputs["Detail"].default_value=2.5
links.new(stripe_geometry.outputs["Position"],warp.inputs["Vector"])
phase=calc("ADD",calc("ADD",calc("MULTIPLY",sy,25),calc("MULTIPLY",calc("SINE",calc("MULTIPLY",sz,5)),1.8)),calc("MULTIPLY",warp.outputs["Fac"],9))
wave=calc("ABSOLUTE",calc("SINE",phase))
band=nodes.new("ShaderNodeMapRange");band.clamp=True;band.interpolation_type="SMOOTHSTEP"
band.inputs["From Min"].default_value=.55;band.inputs["From Max"].default_value=.80
band.inputs["To Min"].default_value=1;band.inputs["To Max"].default_value=0
links.new(wave,band.inputs["Value"])
pattern_geometry=nodes.new("ShaderNodeNewGeometry")
breakup=nodes.new("ShaderNodeTexNoise");breakup.inputs["Scale"].default_value=35;breakup.inputs["Detail"].default_value=3
links.new(pattern_geometry.outputs["Position"],breakup.inputs["Vector"])
broken=nodes.new("ShaderNodeMapRange");broken.clamp=True
broken.inputs["From Min"].default_value=.30;broken.inputs["From Max"].default_value=.52
links.new(breakup.outputs["Fac"],broken.inputs["Value"])
band_strength=calc("MULTIPLY",calc("MULTIPLY",band.outputs[0],broken.outputs[0]),regions.outputs["X"])
tan=nodes.new("ShaderNodeMixRGB");tan.blend_type="MULTIPLY"
tan.inputs[0].default_value=.22;tan.inputs[1].default_value=(.43,.29,.105,1)
links.new(bw.outputs[0],tan.inputs[2])
marked=nodes.new("ShaderNodeMixRGB")
links.new(band_strength,marked.inputs[0]);links.new(eye.outputs[0],marked.inputs[1]);links.new(tan.outputs[0],marked.inputs[2])
# Broad dark stripes sit inside irregular tan borders, retaining fine scale pigment.
core=nodes.new("ShaderNodeMapRange");core.clamp=True;core.interpolation_type="SMOOTHSTEP"
core.inputs["From Min"].default_value=.42;core.inputs["From Max"].default_value=.68
core.inputs["To Min"].default_value=1;core.inputs["To Max"].default_value=0
links.new(wave,core.inputs["Value"])
core_strength=calc("MULTIPLY",calc("MULTIPLY",core.outputs[0],broken.outputs[0]),regions.outputs["X"])
# Short secondary forks join selected main stripes instead of repeating parallel lines.
fork_wave=calc("ABSOLUTE",calc("SINE",calc("ADD",calc("MULTIPLY",phase,1.45),calc("MULTIPLY",sz,12))))
fork=nodes.new("ShaderNodeMapRange");fork.clamp=True
fork.inputs["From Min"].default_value=.12;fork.inputs["From Max"].default_value=.32
fork.inputs["To Min"].default_value=1;fork.inputs["To Max"].default_value=0
links.new(fork_wave,fork.inputs["Value"])
fork_gate=nodes.new("ShaderNodeMapRange");fork_gate.clamp=True
fork_gate.inputs["From Min"].default_value=.51;fork_gate.inputs["From Max"].default_value=.65
links.new(warp.outputs["Fac"],fork_gate.inputs["Value"])
core_strength=calc("MAXIMUM",core_strength,calc("MULTIPLY",calc("MULTIPLY",fork.outputs[0],fork_gate.outputs[0]),regions.outputs["X"]))
stripe_tone=nodes.new("ShaderNodeMixRGB");stripe_tone.blend_type="MULTIPLY"
stripe_tone.inputs[0].default_value=.20;stripe_tone.inputs[1].default_value=(.018,.075,.070,1)
links.new(bw.outputs[0],stripe_tone.inputs[2])
striped=nodes.new("ShaderNodeMixRGB");striped.name="Echo_Mottled_Stripes"
links.new(core_strength,striped.inputs[0]);links.new(marked.outputs[0],striped.inputs[1]);links.new(stripe_tone.outputs[0],striped.inputs[2])
photo_mix=nodes.new("ShaderNodeMixRGB")
links.new(regions.outputs["Y"],photo_mix.inputs[0]);links.new(striped.outputs[0],photo_mix.inputs[1])
photo_mix.inputs[2].default_value=(.48,.38,.20,1)
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
links.new(photo_mix.outputs[0],pigment.inputs[1])
links.new(scale_colour.outputs[0],pigment.inputs[2])
# Small irregular pigment dots: scattered colour variation rather than raised bumps.
dots=nodes.new("ShaderNodeTexVoronoi");dots.feature="F1";dots.distance="EUCLIDEAN"
dots.inputs["Scale"].default_value=38
links.new(geometry.outputs["Position"],dots.inputs["Vector"])
dot_mask=nodes.new("ShaderNodeMapRange");dot_mask.clamp=True;dot_mask.interpolation_type="SMOOTHSTEP"
dot_mask.inputs["From Min"].default_value=.20;dot_mask.inputs["From Max"].default_value=.38
dot_mask.inputs["To Min"].default_value=.18;dot_mask.inputs["To Max"].default_value=0
links.new(dots.outputs["Distance"],dot_mask.inputs["Value"])
dot_random=nodes.new("ShaderNodeRGBToBW");links.new(dots.outputs["Color"],dot_random.inputs[0])
dot_palette=nodes.new("ShaderNodeValToRGB");dot_palette.color_ramp.interpolation="CONSTANT"
dot_palette.color_ramp.elements[0].position=.18;dot_palette.color_ramp.elements[0].color=(.045,.032,.018,1)
dot_palette.color_ramp.elements[1].position=.68;dot_palette.color_ramp.elements[1].color=(.28,.22,.13,1)
dot_palette.color_ramp.elements.new(.40).color=(.115,.13,.095,1)
dot_palette.color_ramp.elements.new(.54).color=(.19,.16,.105,1)
links.new(dot_random.outputs[0],dot_palette.inputs[0])
speckled=nodes.new("ShaderNodeMixRGB");speckled.name="Echo_Multicolour_Speckles"
links.new(dot_mask.outputs[0],speckled.inputs[0]);links.new(pigment.outputs[0],speckled.inputs[1]);links.new(dot_palette.outputs[0],speckled.inputs[2])
# Echo remains free of scars.
emission=nodes.new("ShaderNodeEmission");links.new(speckled.outputs[0],emission.inputs[0])
links.new(emission.outputs[0],output.inputs["Surface"])
image=bpy.data.images.new("Echo_OliveGrey_Albedo",width=2048,height=2048,alpha=False)
target=nodes.new("ShaderNodeTexImage");target.image=image
for n in nodes:n.select=False
target.select=True;nodes.active=target
# Both material slots need independent bake targets when baking one mesh.
detail=mesh.data.materials[1].copy();detail.name="Echo_Details"
mesh.data.materials[1]=detail
detail_bs=next(n for n in detail.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
detail_output=next(n for n in detail.node_tree.nodes if n.type=="OUTPUT_MATERIAL")
detail_source=detail_bs.inputs["Base Color"].links[0].from_socket
detail_emission=detail.node_tree.nodes.new("ShaderNodeEmission")
detail.node_tree.links.new(detail_source,detail_emission.inputs[0])
detail.node_tree.links.new(detail_emission.outputs[0],detail_output.inputs["Surface"])
detail_image=bpy.data.images.new("Echo_DetailBake",width=1024,height=1024,alpha=False)
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
    image.filepath_raw=str(ROOT/"assets/raptor/echo-body.png");image.save()
    image.filepath_raw=str(ROOT/"art/raptor/textures/echo-body.png");image.save()
    links.new(target.outputs["Color"],bs.inputs["Base Color"])
    links.new(bs.outputs[0],output.inputs["Surface"])
    bs.inputs["Roughness"].default_value=.80
    detail.node_tree.links.new(detail_bs.outputs[0],detail_output.inputs["Surface"])
    mesh["appearance_note"]="Echo: reference-inspired golden-tan skin, branching, tapered dark teal-green markings with mottled tan borders across the entire body, golden back, golden muzzle and cream underside, larger multicoloured spots across the whole body, scales and no scars. Other raptors and animation remain unchanged."
    scene.render.engine=old_engine;scene.cycles.samples=old_samples
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/"art/raptor/raptor-echo-review.blend"),relative_remap=True)
finally:
    scene.render.engine=old_engine;scene.cycles.samples=old_samples
    for o in bpy.context.selected_objects:o.select_set(False)
    for o in old_selected:o.select_set(True)
    bpy.context.view_layer.objects.active=old_active
result={"saved":bpy.data.filepath,"runtime_texture":str(ROOT/"assets/raptor/echo-body.png"),"blue_preserved":True}
