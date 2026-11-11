import bpy
from mathutils import Vector
s=bpy.context.scene
mesh=bpy.data.objects["Retopo"]
points=[mesh.matrix_world@v.co for v in mesh.data.shape_keys.key_blocks["Basis"].data]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
center=(lo+hi)/2
bpy.ops.object.camera_add(location=center+Vector((7,-3,2.3)))
camera=bpy.context.object
camera.rotation_euler=(center-camera.location).to_track_quat("-Z","Y").to_euler()
camera.data.type="ORTHO";camera.data.ortho_scale=max(hi-lo)*1.25
s.camera=camera
for location,power,size in [((4,-4,7),1300,5),((-4,1,5),1000,4)]:
    bpy.ops.object.light_add(type="AREA", location=center+Vector(location))
    light=bpy.context.object;light.data.energy=power;light.data.shape="DISK";light.data.size=size
    light.rotation_euler=(center-light.location).to_track_quat("-Z","Y").to_euler()
s.world=s.world or bpy.data.worlds.new("PreviewWorld")
s.world.color=(.22,.22,.22)
s.render.engine="CYCLES";s.cycles.samples=16
s.render.resolution_x=1000;s.render.resolution_y=650;s.render.resolution_percentage=100
s.render.filepath="C:/Users/SkyeFletcher/code/jurassic-world/art/raptor/charlie-preview.png"
bpy.ops.render.render(write_still=True)
