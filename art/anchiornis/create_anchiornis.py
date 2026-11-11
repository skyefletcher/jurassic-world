"""Editable Anchiornis model. Coordinates match the game's articulated Bird pivots."""
import bpy, math, random
from pathlib import Path
from mathutils import Vector
ROOT=Path('C:/Users/SkyeFletcher/code/jurassic-world')
random.seed(411)
# A separate scene preserves the currently open raptor artwork.
scene=bpy.data.scenes.new('Anchiornis Studio')
bpy.context.window.scene=scene
def xyz(p): return (p[0],-p[2],p[1])
def material(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Roughness'].default_value=.82
    return m
dark=material('Blue-grey contour plumage',(.13,.19,.23))
pale=material('Ivory flight feathers',(.68,.61,.44))
red=material('Golden swept crest',(.72,.38,.018))
horn=material('Slate snout and feet',(.10,.105,.095))
black=material('Obsidian claws and pupils',(.006,.008,.009))
amber=material('Amber iris',(.48,.24,.045))
def pivot(name,parent,pos):
    o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);o.parent=parent;o.location=xyz(pos);return o
def blob(name,parent,pos,size,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12)
    o=bpy.context.object;o.name=name;o.parent=parent;o.location=xyz(pos);o.scale=(size[0],size[2],size[1]);o.data.materials.append(mat)
    for f in o.data.polygons:f.use_smooth=True
    return o
def feather(name,parent,root,along,length,width,mat):
    r=Vector(root);d=Vector(along).normalized();a=Vector((0,1,0)).cross(d).normalized()
    verts=[];faces=[];rows=24;columns=8
    spotted=mat==pale
    spots=[(random.uniform(.15,.95),random.uniform(-.7,.7),random.uniform(.035,.075),random.uniform(.20,.38)) for _ in range(7)] if spotted else []
    for j in range(rows+1):
        t=j/rows
        # Narrow pointed tips, gently scalloped vanes and a raised central shaft.
        w=width*max(.006,math.sin(math.pi*t)**.65)
        for k in range(columns+1):
            side=k/columns*2-1
            serration=1-.035*math.sin(t*160+abs(side)*3)**2
            p=r+d*(length*t)+a*(side*w*serration)+Vector((0,.003*math.sin(math.pi*t)*(1-abs(side)),0))
            verts.append(xyz(p))
    for j in range(rows):
        for k in range(columns):
            i=j*(columns+1)+k
            faces.append((i,i+1,i+columns+2,i+columns+1))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.materials.append(mat)
    if spotted:
        mesh.materials.append(black)
        for poly in mesh.polygons:
            j=poly.index//columns;k=poly.index%columns;t=(j+.5)/rows;side=(k+.5)/columns*2-1
            if any(((t-u)/rh)**2+((side-v)/rw)**2<1+.2*math.sin(t*140+side*20) for u,v,rh,rw in spots):
                poly.material_index=1
    for f in mesh.polygons:f.use_smooth=True
    o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);o.parent=parent
    mod=o.modifiers.new('Vane thickness','SOLIDIFY');mod.thickness=.0006
    return o
frame=pivot('Frame',None,(0,0,0));body=pivot('Body',frame,(0,.45,0))
blob('Torso',body,(0,0,0),(.115,.125,.28),dark)
blob('Breast',body,(0,-.045,-.10),(.10,.105,.19),pale)
neck=pivot('Neck',body,(0,.14,-.25));blob('Throat',neck,(0,0,0),(.052,.19,.065),dark)
head=pivot('Head',neck,(0,.27,-.08));blob('Skull',head,(0,0,0),(.063,.065,.115),dark)
blob('Toothed snout',head,(0,-.02,-.12),(.044,.030,.087),horn)
jaw=pivot('Jaw',head,(0,-.04,0));blob('Lower jaw',jaw,(0,0,-.08),(.040,.012,.125),horn)
for s in (-1,1):
    blob('Amber eye',head,(s*.061,.015,-.060),(.008,.011,.012),amber)
    blob('Pupil',head,(s*.068,.015,-.064),(.003,.008,.006),black)
    blob('Nostril',head,(s*.038,-.007,-.182),(.004,.004,.007),black)
for j in range(23):
    feather('Rust crown plume',head,((j%3-1)*.009,.050,-.055+j*.005),(0,.22,1),.10+random.random()*.025,.006,red)
# Fine overlapping blue-grey feathers follow the curved body and upright neck.
for row in range(13):
    z=-.25+row*.040
    taper=math.sqrt(max(.08,1-(z/.30)**2))
    for k in range(26):
        a=k*math.tau/26; x=.118*taper*math.cos(a); y=.128*taper*math.sin(a)
        feather('Body covert',body,(x,y,z),(x*.5,y*.4,1),.062,.012,dark)
for row in range(17):
    h=-.12+row*.025
    bend=-.08*max(0,(h+.1)/.4)
    for k in range(17):
        a=k*math.tau/17
        feather('Neck filament',neck,(.054*math.cos(a),h,bend+.061*math.sin(a)),(.1*math.cos(a),.95,.20),.048,.006,dark)
for row in range(9):
    z=-.10+row*.022
    taper=math.sqrt(max(.12,1-(z/.13)**2))
    for k in range(20):
        a=k*math.tau/20
        feather('Face filament',head,(.065*taper*math.cos(a),.065*taper*math.sin(a),z),(0,.12,1),.031,.006,dark)
tail=pivot('Tail',body,(0,0,.22))
blob('Tail core',tail,(0,.015,.29),(.041,.034,.34),dark)
for j in range(9):
    tf=pivot('TailFeather'+str(j),tail,(0,0,.3))
    feather('Tail vane',tf,((j-4)*.008,0,0),(0,0,1),.75,.025,pale)

for i,s in enumerate((-1,1)):
    arm=pivot('WingArm'+str(i),body,(s*.10,.06,-.15));hand=pivot('WingHand'+str(i),arm,(s*.30,0,0))
    blob('Upper wing',arm,(s*.13,0,.035),(.17,.038,.08),dark)
    for j in range(16):
        par=arm if j<8 else hand
        x=s*(j%8)*.039
        length=.29+(j/15)*.14
        feather('White remex',par,(x,0,0),(s*.3,0,1),length,.034,pale)

        feather('Wing covert',par,(x,.009,-.015),(s*.3,0,1),.15,.025,dark)
    leg=pivot('Leg'+str(i),body,(s*.09,0,.04));blob('Thigh',leg,(0,-.10,-.02),(.052,.13,.06),dark)
    shank=pivot('Shank'+str(i),leg,(0,-.20,-.04));blob('Shin',shank,(0,-.10,.015),(.022,.11,.025),horn)
    foot=pivot('Foot'+str(i),shank,(0,-.22,.03))
    for j in range(9):
        feather('Hindwing feather',shank,(s*.015,-j*.021,0),(s*.4,0,1),.24,.025,pale)

    for toe in (-1,0,1):
        blob('Toe',foot,(toe*.028,0,-.055),(.011,.010,.065),horn)
        blob('Talon',foot,(toe*.029,-.004,-.12),(.006,.009,.023),black)
# Merge mesh pieces per animated pivot; preserve the articulation hierarchy.
for parent in [o for o in scene.objects if o.type=='EMPTY']:
    if parent.type!='EMPTY':continue
    children=[o for o in parent.children if o.type=='MESH']
    if not children:continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in children:o.select_set(True)
    bpy.context.view_layer.objects.active=children[0]
    bpy.ops.object.convert(target='MESH');bpy.ops.object.join()
    bpy.context.object.name=parent.name+'Plumage'
scene.world=bpy.data.worlds.new('Anchiornis World');scene.world.color=(.15,.15,.15)
scene['description']='Reference-inspired Anchiornis: blue-grey plumage, long neck, golden swept crest and spotted cream flight feathers. Game Bird pivots animate the model.'
(ROOT/'assets/anchiornis').mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'assets/anchiornis/anchiornis.glb'),export_format='GLB',use_active_scene=True,export_animations=False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'art/anchiornis/anchiornis-review.blend'))
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.shading.type='MATERIAL'
        with bpy.context.temp_override(area=area,region=next(r for r in area.regions if r.type=='WINDOW')):
            bpy.ops.object.select_all(action='SELECT');bpy.ops.view3d.view_all(center=False)
result={'blend':bpy.data.filepath,'export':'assets/anchiornis/anchiornis.glb'}
