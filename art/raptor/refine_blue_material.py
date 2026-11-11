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
centers=[(-1.6,1.76),(-1.35,1.81),(-1.05,1.865),(-.8,1.86),(-.65,1.62),(-.52,1.39),(-.3,1.29),(0,1.22),(.4,1.14),(.8,1.10),(1.2,1.125),(1.7,1.19),(2.4,1.235)]
widths=[(-1.6,.018),(-1.2,.025),(-.8,.032),(-.55,.038),(-.2,.047),(.3,.048),(.8,.028),(1.4,.017),(2.4,.009)]
side_widths=[(-1.6,.02),(-1.3,.07),(-.8,.10),(-.55,.12),(-.2,.22),(.3,.20),(.8,.09),(1.4,.045),(2.4,.015)]
field=mesh.data.color_attributes.get("ReferenceStripeField")
if field is None:
    field=mesh.data.color_attributes.new(name="ReferenceStripeField",type="FLOAT_COLOR",domain="POINT")
claws=mesh.data.color_attributes.get("ReferenceClawMask")
if claws is None:
    claws=mesh.data.color_attributes.new(name="ReferenceClawMask",type="FLOAT_COLOR",domain="POINT")
eye_field=mesh.data.color_attributes.get("ReferenceEyeField")
if eye_field is None:
    eye_field=mesh.data.color_attributes.new(name="ReferenceEyeField",type="FLOAT_COLOR",domain="POINT")
surface_field=mesh.data.color_attributes.get("ReferenceSurfacePosition")
if surface_field is None:
    surface_field=mesh.data.color_attributes.new(name="ReferenceSurfacePosition",type="FLOAT_COLOR",domain="POINT")
for i,v in enumerate(mesh.data.shape_keys.key_blocks["Basis"].data):
    p=mesh.matrix_world@v.co
    center=interpolate(centers,p.y)
    half=interpolate(widths,p.y)
    side=interpolate(side_widths,p.y)
    lateral=smooth(side*.22,side*.65,abs(p.x))*smooth(-1.50,-1.12,p.y)
    face_gate=smooth(-1.34,-1.23,p.y)*(1-smooth(-.91,-.81,p.y))*smooth(1.67,1.79,p.z)
    lateral*=1-face_gate
    neck_zone=smooth(-.99,-.90,p.y)*(1-smooth(-.40,-.27,p.y))
    lateral*=1-neck_zone
    surface_field.data[i].color=((p.y+2)/5,p.z/3,smooth(.015,.055,abs(p.x)),1)
    eye_field.data[i].color=(max(0,min(1,.5+(p.y+1.06209)/.5)),max(0,min(1,.5+(p.z-1.88184)/.5)),smooth(.025,.075,abs(p.x)),1)
    # Interpolate a distance field, rather than per-vertex stripe coverage.
    # This keeps narrow markings smooth across the mesh's large triangles.
    field.data[i].color=(max(0.0,min(1.0,p.z-center+.5)),half,lateral,1)
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
    mat.name="BlueDetailed_Body" if index==0 else "BlueDetailed_Details"
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
    ramp.color_ramp.elements[0].position=.01
    ramp.color_ramp.elements[0].color=(.065,.075,.085,1)
    ramp.color_ramp.elements[1].position=.24
    ramp.color_ramp.elements[1].color=(.52,.56,.60,1)
    if index==0:
        geometry=nodes.new("ShaderNodeNewGeometry")
        noise=nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value=28
        noise.inputs["Detail"].default_value=4
        noise.inputs["Roughness"].default_value=.72
        links.new(geometry.outputs["Position"],noise.inputs["Vector"])
        def calc(op,a,b=None):
            n=nodes.new("ShaderNodeMath");n.operation=op
            for i,value in enumerate([a,b] if b is not None else [a]):
                if isinstance(value,(int,float)):n.inputs[i].default_value=value
                else:links.new(value,n.inputs[i])
            return n.outputs[0]
        def remap(value,low,high):
            n=nodes.new("ShaderNodeMapRange")
            n.interpolation_type="SMOOTHSTEP";n.clamp=True
            links.new(value,n.inputs["Value"])
            n.inputs["From Min"].default_value=low;n.inputs["From Max"].default_value=high
            return n.outputs[0]
        attr=nodes.new("ShaderNodeAttribute");attr.attribute_name="ReferenceStripeField"
        separate=nodes.new("ShaderNodeSeparateXYZ")
        links.new(attr.outputs["Color"],separate.inputs[0])
        signed=calc("SUBTRACT",separate.outputs["X"],.5)
        noise_edge=calc("MULTIPLY",calc("SUBTRACT",noise.outputs["Fac"],.5),.06)
        edge=calc("ABSOLUTE",calc("ADD",signed,noise_edge))
        half=separate.outputs["Y"]
        ratio=calc("DIVIDE",edge,half)
        stripe=calc("MULTIPLY",calc("SUBTRACT",1,remap(ratio,.72,1.0)),separate.outputs["Z"])
        outline=calc("MULTIPLY",calc("SUBTRACT",1,remap(ratio,1.0,1.24)),separate.outputs["Z"])
        eye_attr=nodes.new("ShaderNodeAttribute");eye_attr.attribute_name="ReferenceEyeField"
        eye_parts=nodes.new("ShaderNodeSeparateXYZ")
        links.new(eye_attr.outputs["Color"],eye_parts.inputs[0])
        ey=calc("MULTIPLY",calc("SUBTRACT",eye_parts.outputs["X"],.5),.5)
        ez=calc("MULTIPLY",calc("SUBTRACT",eye_parts.outputs["Y"],.5),.5)
        # Filled eye patch traced from the user's red reference outline.
        # Pixel coordinates are mapped around the eye, retaining the inward silver notch.
        outline_pixels=[
            (0,59),(51,47),(111,43),(151,50),(171,76),(211,96),
            (252,88),(296,88),(320,111),(339,149),(359,267),
            (340,257),(321,235),(309,190),(299,158),(282,146),
            (262,151),(246,172),(228,170),(216,163),(206,168),
            (194,158),(177,163),(162,173),(144,163),(157,149),
            (176,136),(151,130),(129,125),(121,133),(127,149),
            (148,174),(183,197),(219,210),(253,214),(229,220),
            (199,221),(163,214),(127,201),(104,185),(86,161),
            (74,132),(67,107),(56,89),(44,82),(29,89),(4,102)]
        polygon=[((277-x)*.00065,(181-z)*.00065) for x,z in outline_pixels]
        wy=calc("ADD",ey,calc("MULTIPLY",calc("SUBTRACT",noise.outputs["Fac"],.5),.0015))
        wz=calc("ADD",ez,calc("MULTIPLY",calc("SUBTRACT",noise.outputs["Fac"],.5),.0015))
        crossings=0
        for start,end in zip(polygon,polygon[1:]+polygon[:1]):
            if abs(end[1]-start[1])<1e-8:continue
            side_a=calc("GREATER_THAN",start[1],wz)
            side_b=calc("GREATER_THAN",end[1],wz)
            straddles=calc("ABSOLUTE",calc("SUBTRACT",side_a,side_b))
            slope=(end[0]-start[0])/(end[1]-start[1])
            intersection=calc("ADD",start[0]-start[1]*slope,calc("MULTIPLY",wz,slope))
            crosses=calc("MULTIPLY",straddles,calc("LESS_THAN",wy,intersection))
            crossings=calc("ADD",crossings,crosses)
        eye_shape=calc("MODULO",crossings,2)
        # Trace a narrow white border along the exact outer and inner polygon edges.
        point=nodes.new("ShaderNodeCombineXYZ")
        links.new(wy,point.inputs["X"]);links.new(wz,point.inputs["Y"])
        nearest_edge=100
        for start,end in zip(polygon,polygon[1:]+polygon[:1]):
            dx=end[0]-start[0];dz=end[1]-start[1]
            length_sq=dx*dx+dz*dz
            if length_sq<1e-12:continue
            offset=nodes.new("ShaderNodeVectorMath");offset.operation="SUBTRACT"
            links.new(point.outputs[0],offset.inputs[0])
            offset.inputs[1].default_value=(start[0],start[1],0)
            dot=nodes.new("ShaderNodeVectorMath");dot.operation="DOT_PRODUCT"
            links.new(offset.outputs["Vector"],dot.inputs[0])
            dot.inputs[1].default_value=(dx,dz,0)
            along=nodes.new("ShaderNodeMapRange");along.clamp=True
            along.inputs["From Min"].default_value=0
            along.inputs["From Max"].default_value=length_sq
            links.new(dot.outputs["Value"],along.inputs["Value"])
            projection=nodes.new("ShaderNodeVectorMath");projection.operation="SCALE"
            projection.inputs[0].default_value=(dx,dz,0)
            links.new(along.outputs[0],projection.inputs["Scale"])
            distance=nodes.new("ShaderNodeVectorMath");distance.operation="DISTANCE"
            links.new(offset.outputs["Vector"],distance.inputs[0])
            links.new(projection.outputs["Vector"],distance.inputs[1])
            nearest_edge=calc("MINIMUM",nearest_edge,distance.outputs["Value"])
        eye_border=calc("MULTIPLY",calc("SUBTRACT",1,remap(nearest_edge,.0020,.0045)),calc("SUBTRACT",1,eye_shape))
        eye_coverage=remap(eye_parts.outputs["Z"],.005,.04)
        eye_border=calc("MULTIPLY",eye_border,eye_coverage)

        # Route the neck stripe down the back edge and around the base into the flank.
        position_attr=nodes.new("ShaderNodeAttribute");position_attr.attribute_name="ReferenceSurfacePosition"
        position_parts=nodes.new("ShaderNodeSeparateXYZ")
        links.new(position_attr.outputs["Color"],position_parts.inputs[0])
        sy=calc("SUBTRACT",calc("MULTIPLY",position_parts.outputs["X"],5),2)
        sz=calc("MULTIPLY",position_parts.outputs["Y"],3)
        def neck_segment(start,end,r0,r1):
            dy=end[0]-start[0];dz=end[1]-start[1]
            py=calc("SUBTRACT",sy,start[0]);pz=calc("SUBTRACT",sz,start[1])
            dot=calc("ADD",calc("MULTIPLY",py,dy),calc("MULTIPLY",pz,dz))
            t=calc("MINIMUM",1,calc("MAXIMUM",0,calc("DIVIDE",dot,dy*dy+dz*dz)))
            qy=calc("SUBTRACT",py,calc("MULTIPLY",t,dy))
            qz=calc("SUBTRACT",pz,calc("MULTIPLY",t,dz))
            distance=calc("SQRT",calc("ADD",calc("MULTIPLY",qy,qy),calc("MULTIPLY",qz,qz)))
            radius=calc("ADD",r0,calc("MULTIPLY",t,r1-r0))
            variation=calc("MULTIPLY",calc("SUBTRACT",noise.outputs["Fac"],.5),.06)
            ratio=calc("DIVIDE",calc("ADD",distance,variation),radius)
            return calc("SUBTRACT",1,remap(ratio,.72,1.0)),calc("SUBTRACT",1,remap(ratio,1.0,1.24))
        route=[(-.87,1.93),(-.77,1.86),(-.70,1.77),(-.70,1.61),(-.69,1.49),(-.62,1.36),(-.51,1.33),(-.32,1.29)]
        neck_blue=0;neck_outline=0
        for i,(start,end) in enumerate(zip(route,route[1:])):
            r0=.024+i*.002;r1=r0+.002
            core,rim=neck_segment(start,end,r0,r1)
            neck_blue=calc("MAXIMUM",neck_blue,core)
            neck_outline=calc("MAXIMUM",neck_outline,rim)
        neck_blue=calc("MULTIPLY",neck_blue,position_parts.outputs["Z"])
        neck_outline=calc("MULTIPLY",neck_outline,position_parts.outputs["Z"])
        stripe=calc("MAXIMUM",stripe,neck_blue)
        outline=calc("MAXIMUM",outline,neck_outline)
        fine_noise=nodes.new("ShaderNodeTexNoise")
        fine_noise.inputs["Scale"].default_value=180
        fine_noise.inputs["Detail"].default_value=2
        links.new(geometry.outputs["Position"],fine_noise.inputs["Vector"])
        breaks=calc("SUBTRACT",1,calc("MULTIPLY",remap(fine_noise.outputs["Fac"],.58,.76),.65))
        eye_blue=calc("MULTIPLY",eye_shape,eye_coverage)
        # One shared, opaque paint layer for every blue marking.
        # Saturate only the eye's lateral coverage; preserve its traced silhouette.
        marking_mask=calc("MAXIMUM",stripe,eye_blue)
        marking_border=calc("MAXIMUM",outline,eye_border)
        border=nodes.new("ShaderNodeMixRGB")
        links.new(marking_border,border.inputs[0])
        links.new(ramp.outputs["Color"],border.inputs[1])
        border.inputs[2].default_value=(.40,.43,.43,1)
        blue=nodes.new("ShaderNodeRGB")
        blue.name="Shared_Tail_Blue"
        blue.outputs["Color"].default_value=(.024,.070,.105,1)
        mix=nodes.new("ShaderNodeMixRGB")
        mix.name="Unified_Blue_Markings"
        links.new(marking_mask,mix.inputs[0])
        links.new(border.outputs[0],mix.inputs[1])
        links.new(blue.outputs["Color"],mix.inputs[2])
        # Add restrained fine-scale pigment, without painting lighting into the albedo.
        cells=nodes.new("ShaderNodeTexVoronoi");cells.feature="DISTANCE_TO_EDGE"
        cells.inputs["Scale"].default_value=145
        links.new(geometry.outputs["Position"],cells.inputs["Vector"])
        cell_ramp=nodes.new("ShaderNodeValToRGB")
        cell_ramp.color_ramp.elements[0].position=.018
        cell_ramp.color_ramp.elements[0].color=(.32,.36,.30,1)
        cell_ramp.color_ramp.elements[1].position=.10
        cell_ramp.color_ramp.elements[1].color=(.95,1,.93,1)
        links.new(cells.outputs["Distance"],cell_ramp.inputs[0])
        pigment=nodes.new("ShaderNodeMixRGB");pigment.blend_type="MULTIPLY"
        pigment.inputs[0].default_value=.26
        links.new(mix.outputs[0],pigment.inputs[1])
        links.new(cell_ramp.outputs[0],pigment.inputs[2])
        mix=pigment
        detailed=bpy.data.images.load(str(ROOT/"art/raptor/original/source/Raptor/Textures/Raptor_NormalDetailed.png"),check_existing=True)
        detailed.colorspace_settings.name="Non-Color"
        normal_node=next(n for n in nodes if n.type=="NORMAL_MAP")
        normal_node.inputs["Strength"].default_value=.9
        tex=nodes.new("ShaderNodeTexImage");tex.image=detailed
        links.new(tex.outputs["Color"],normal_node.inputs["Color"])
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
    image=bpy.data.images.new("Raptor_BlueDetailed_BaseColor" if index==0 else "Raptor_BlueDetailed_Details",width=size,height=size,alpha=False)
    image.filepath_raw=str(out/("raptor-blue-detailed-body.png" if index==0 else "raptor-blue-detailed-details.png"))
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
        for alpha_link in list(bsdf.inputs["Alpha"].links):
            links.remove(alpha_link)
        bsdf.inputs["Alpha"].default_value=1
        links.new(bsdf.outputs["BSDF"],output.inputs["Surface"])
        # Skin is rough; keep the source's scale normal map and amber eye atlas.
        bsdf.inputs["Roughness"].default_value=.80 if mat.name.startswith("BlueDetailed_Body") else .55
    mesh["appearance_note"]="Silver-grey scales with one shared opaque tail-style blue paint layer and pale edging across all markings, white-outlined blue eye patch traced from the supplied outline, with a continuous neck stripe routed down the rear neck and around its base, blue lateral stripe, detailed scale normals and charcoal claws. Original anatomy retained."
    scene.render.engine=original_engine
    scene.cycles.samples=original_samples
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/"art/raptor/raptor-blue-detailed-review.blend"),relative_remap=True)
finally:
    scene.render.engine=original_engine
    scene.cycles.samples=original_samples
    for obj in bpy.context.selected_objects: obj.select_set(False)
    for obj in original_selection: obj.select_set(True)
    bpy.context.view_layer.objects.active=original_active
result={"saved":bpy.data.filepath,"textures":[str(image.filepath) for *_,image in outputs],"geometry_changed":False}
