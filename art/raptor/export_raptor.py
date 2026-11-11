"""Run in the approved Blender review file to export the Godot review asset.
Original FBX, mesh, skeleton and actions are preserved. Temporary NLA tracks
merge skeletal animation and facial shape keys into one named clip each.
"""
import bpy
import json
import struct
from pathlib import Path

ROOT = Path(bpy.data.filepath).resolve().parents[2]
CLIPS = {
    "Idle": "Raptor_Idl_7157f69",
    "Walk": "Raptor_Wal_63c0d2e",
    "Run": "Raptor_Run_9fcf3e1",
    "Bite": "Raptor_Bit_a12e3d9",
    "Eat": "Raptor_Eat_31d2c49",
    "Drink": "Raptor_Dri_8ec25a0",
    "Roar": "Raptor_Roa_7ac4aad",
    "BiteHeavy": "Raptor_Bit_b252254",
    "Scratch": "Raptor_Scr_0df2e27",
    "GoToSleep": "Raptor_GoT_62dbb30",
    "Sleep": "Raptor_Sle_2d2791d",
    "Leap": "Raptor_Lea_547539a",
    "Death": "Raptor_Dea_a16445c",
}
rig = bpy.data.objects["RaptorArmature"]
mesh = bpy.data.objects["Retopo"]
scene = bpy.context.scene
keys = mesh.data.shape_keys
assert "Neck_Back_Transition" not in keys.key_blocks, "Use the approved original neck"
owners = [rig, keys]
assert all(not owner.animation_data.nla_tracks for owner in owners), "Inspect existing NLA tracks first"
old_actions = [(owner, owner.animation_data.action, owner.animation_data.action_slot) for owner in owners]
old_frame = scene.frame_current
old_selection = list(bpy.context.selected_objects)
old_active = bpy.context.view_layer.objects.active
tracks = []
rest_points = [mesh.matrix_world @ point.co for point in keys.key_blocks["Basis"].data]
source_height = max(point.z for point in rest_points) - min(point.z for point in rest_points)
output = ROOT / "assets/raptor/raptor.glb"
output.parent.mkdir(parents=True, exist_ok=True)
try:
    for owner in owners:
        owner.animation_data.action = None
    for name, suffix in CLIPS.items():
        for owner, morph in [(rig, False), (keys, True)]:
            action = next(a for a in bpy.data.actions
                          if a.name.startswith("Key|") == morph and a.name.endswith(suffix))
            track = owner.animation_data.nla_tracks.new()
            track.name = name
            strip = track.strips.new(name, int(action.frame_range[0]), action)
            strip.action_slot = action.slots[0]
            strip.extrapolation = "NOTHING"
            tracks.append((owner, track))
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    rig.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(
        filepath=str(output), export_format="GLB", use_selection=True,
        use_active_scene=True, export_cameras=False, export_lights=False,
        export_animations=True, export_animation_mode="NLA_TRACKS",
        export_merge_animation="NLA_TRACK", export_frame_range=False,
        export_force_sampling=True, export_skins=True, export_influence_nb=8, export_morph=True,
        export_morph_animation=True, export_yup=True,
        export_copyright="PBR Velociraptor (Animated) by Ferocious Industries; CC Attribution; see art/raptor/SOURCE.md",
    )
    # Blender's NLA export can omit the armature object's static scale.
    # Normalise the exported asset to the approved Blender rest-mesh height.
    # An extra parent scales skin, bones and root motion together, preserving poses.
    raw = output.read_bytes()
    chunks = []
    cursor = 12
    while cursor < len(raw):
        length, kind = struct.unpack_from("<II", raw, cursor)
        chunks.append((kind, raw[cursor + 8:cursor + 8 + length]))
        cursor += 8 + length
    gltf = json.loads(chunks[0][1])
    bounds = [gltf["accessors"][primitive["attributes"]["POSITION"]]
              for primitive in gltf["meshes"][0]["primitives"]]
    exported_height = max(a["max"][1] for a in bounds) - min(a["min"][1] for a in bounds)
    scale = source_height / exported_height
    for export_scene in gltf["scenes"]:
        for root in export_scene["nodes"]:
            node = gltf["nodes"][root]
            assert "matrix" not in node and "scale" not in node, "Review non-identity export root before normalising"
        wrapper = len(gltf["nodes"])
        gltf["nodes"].append({"name": "RaptorAsset", "scale": [scale] * 3,
                              "children": export_scene["nodes"]})
        export_scene["nodes"] = [wrapper]
    document = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    document += b" " * ((-len(document)) % 4)
    chunks[0] = (chunks[0][0], document)
    payload = b"".join(struct.pack("<II", len(content), kind) + content for kind, content in chunks)
    output.write_bytes(struct.pack("<III", 0x46546C67, 2, 12 + len(payload)) + payload)
    metadata = {
        "source_rest_height": source_height,
        "export_scale_correction": scale,
        "skin_weights_per_vertex": 8,
        "source": Path(bpy.data.filepath).relative_to(ROOT).as_posix(),
        "appearance": mesh.get("appearance_note", "Original green skin"),
        "author": "Ferocious Industries",
        "clips": CLIPS,
        "fps": scene.render.fps,
        "walk_reference_speed": 2.9 / (31 / 24),
        "run_reference_speed": 4.794,
        "reference_note": "Metres/second derived from corresponding source root-motion clips at original scale; approximate, not foot-lock IK.",
        "neck": "Original approved shape, no neck correction",
    }
    (output.parent / "review.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
finally:
    for owner, track in tracks:
        owner.animation_data.nla_tracks.remove(track)
    for owner, action, slot in old_actions:
        owner.animation_data.action = action
        if action is not None:
            owner.animation_data.action_slot = slot
    scene.frame_set(old_frame)
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    for obj in old_selection:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = old_active
result = {"exported": str(output), "clips": list(CLIPS), "bytes": output.stat().st_size}
