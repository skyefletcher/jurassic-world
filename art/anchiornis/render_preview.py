import bpy
from mathutils import Vector
from pathlib import Path
root=Path('C:/Users/SkyeFletcher/code/jurassic-world')
scene=bpy.context.scene
target=Vector((0,-.15,.48))
for i,side in enumerate((-1,1)):
    bpy.data.objects['WingArm'+str(i)].rotation_euler=(.65,-side*.08,-side*.70)
    bpy.data.objects['WingHand'+str(i)].rotation_euler.z=side*.35
for i in range(9):
    bpy.data.objects['TailFeather'+str(i)].rotation_euler.z=-(i/8-.5)*.16
bpy.ops.object.camera_add(location=(2.2,1.8,1.2))
camera=bpy.context.object
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=2.1;scene.camera=camera
for loc,energy,size in [((2,2,4),350,3),((-2,-2,3),250,2)]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    light=bpy.context.object;light.data.energy=energy;light.data.shape='DISK';light.data.size=size
    light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1100;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.render.filepath=str(root/'art/anchiornis/anchiornis-preview.png')
bpy.ops.render.render(write_still=True)
