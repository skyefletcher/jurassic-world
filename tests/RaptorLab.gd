extends Node3D
## Standalone review scene for the playable Blue raptor.
const MODEL = preload("res://assets/raptor/raptor.glb")
const WALK_SPEED := 2.24516
const RUN_SPEED := 4.794
const CLIPS := ["Idle", "Walk", "Run", "Bite", "Eat", "Drink", "Roar"]

var delta_skin := "--delta" in OS.get_cmdline_user_args()
var echo_skin := "--echo" in OS.get_cmdline_user_args()
var actor: Node3D
var model: Node3D
var player: AnimationPlayer
var tree: AnimationTree
var camera: Camera3D
var status: Label
var mode_button: Button
var speed_slider: HSlider
var auto_mode := true
var treadmill := false
var paused := false
var playback := 1.0
var speed := 0.0
var auto_clock := 0.0
var yaw := 2.1
var pitch := 0.25
var distance := 5.2
var orbiting := false
var action_clip := ""
var action_left := 0.0

func _ready() -> void:
	_build_stage()
	actor = Node3D.new()
	add_child(actor)
	model = MODEL.instantiate()
	actor.add_child(model)
	model.position.y = 0.02
	if delta_skin or echo_skin:
		var skin_mesh := model.find_children("*", "MeshInstance3D", true, false)[0] as MeshInstance3D
		var material := skin_mesh.get_active_material(0).duplicate() as StandardMaterial3D
		material.albedo_texture = load("res://assets/raptor/echo-body.png" if echo_skin else "res://assets/raptor/delta-body.png")
		skin_mesh.set_surface_override_material(0, material)
	player = model.find_child("AnimationPlayer", true, false) as AnimationPlayer
	if player == null:
		_fail("Imported model has no AnimationPlayer")
		return
	for clip in CLIPS:
		if not player.has_animation(clip):
			_fail("Missing animation: " + clip + "; found: " + str(player.get_animation_list()))
			return
	for clip in ["Idle", "Walk", "Run"]:
		player.get_animation(clip).loop_mode = Animation.LOOP_LINEAR
	_build_animation_tree()
	_build_ui()
	_update_camera()
	if "--check" in OS.get_cmdline_user_args():
		_run_checks.call_deferred()
	elif "--capture" in OS.get_cmdline_user_args():
		_capture.call_deferred()

func _build_stage() -> void:
	var environment := Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color("#303b43")
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color("#d3dfe7")
	environment.ambient_light_energy = 0.65
	environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	var world := WorldEnvironment.new()
	world.environment = environment
	add_child(world)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-48, -32, 0)
	sun.light_energy = 1.6
	sun.shadow_enabled = true
	add_child(sun)
	var ground := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(400, 400)
	var material := ShaderMaterial.new()
	var shader := Shader.new()
	shader.code = """shader_type spatial;
render_mode cull_disabled;
varying vec3 world_position;
void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
void fragment() {
	vec2 cell = abs(fract(world_position.xz - 0.5) - 0.5) / max(fwidth(world_position.xz), vec2(0.0001));
	float line = 1.0 - clamp(min(cell.x, cell.y), 0.0, 1.0);
	line *= 1.0 - smoothstep(15.0, 60.0, length(world_position.xz - CAMERA_POSITION_WORLD.xz));
	ALBEDO = mix(vec3(0.21, 0.26, 0.23), vec3(0.32, 0.38, 0.33), line * 0.6);
	ROUGHNESS = 1.0;
}"""
	material.shader = shader
	plane.material = material
	ground.mesh = plane
	add_child(ground)
	camera = Camera3D.new()
	camera.fov = 48
	camera.current = true
	add_child(camera)

func _animation(clip: String) -> AnimationNodeAnimation:
	var node := AnimationNodeAnimation.new()
	node.animation = clip
	return node

func _build_animation_tree() -> void:
	tree = AnimationTree.new()
	model.add_child(tree)
	tree.anim_player = tree.get_path_to(player)
	tree.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	var graph := AnimationNodeBlendTree.new()
	graph.add_node("Idle", _animation("Idle"))
	graph.add_node("Walk", _animation("Walk"))
	graph.add_node("Run", _animation("Run"))
	graph.add_node("WalkRate", AnimationNodeTimeScale.new())
	graph.add_node("RunRate", AnimationNodeTimeScale.new())
	var idle_walk := AnimationNodeBlend2.new()
	idle_walk.sync = true
	graph.add_node("IdleWalk", idle_walk)
	var walk_run := AnimationNodeBlend2.new()
	walk_run.sync = true
	graph.add_node("WalkRun", walk_run)
	graph.add_node("Action", _animation("Bite"))
	graph.add_node("ActionSeek", AnimationNodeTimeSeek.new())
	graph.add_node("ActionBlend", AnimationNodeBlend2.new())
	graph.connect_node("WalkRate", 0, "Walk")
	graph.connect_node("RunRate", 0, "Run")
	graph.connect_node("IdleWalk", 0, "Idle")
	graph.connect_node("IdleWalk", 1, "WalkRate")
	graph.connect_node("WalkRun", 0, "IdleWalk")
	graph.connect_node("WalkRun", 1, "RunRate")
	graph.connect_node("ActionBlend", 0, "WalkRun")
	graph.connect_node("ActionSeek", 0, "Action")
	graph.connect_node("ActionBlend", 1, "ActionSeek")
	graph.connect_node("output", 0, "ActionBlend")
	tree.tree_root = graph
	tree.active = true
	tree.set("parameters/IdleWalk/blend_amount", 0.0)
	tree.set("parameters/WalkRun/blend_amount", 0.0)
	tree.set("parameters/ActionBlend/blend_amount", 0.0)
	tree.advance(0)

func _build_ui() -> void:
	var layer := CanvasLayer.new()
	add_child(layer)
	var panel := PanelContainer.new()
	panel.position = Vector2(20, 20)
	panel.custom_minimum_size.x = 390
	layer.add_child(panel)
	var margin := MarginContainer.new()
	for side in ["left", "top", "right", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 16)
	panel.add_child(margin)
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 9)
	margin.add_child(box)
	var title := Label.new()
	title.text = "ECHO / MOVEMENT STUDY" if echo_skin else ("DELTA / MOVEMENT STUDY" if delta_skin else "BLUE / MOVEMENT STUDY")
	title.add_theme_font_size_override("font_size", 23)
	box.add_child(title)
	var instructions := Label.new()
	instructions.text = "WASD move  |  Shift run  |  Space pause\nRight-drag orbit  |  Wheel zoom  |  R reset"
	box.add_child(instructions)
	status = Label.new()
	box.add_child(status)
	mode_button = Button.new()
	mode_button.focus_mode = Control.FOCUS_NONE
	mode_button.text = "Automatic walk / run  |  click for manual"
	mode_button.pressed.connect(_toggle_mode)
	box.add_child(mode_button)
	var still := CheckButton.new()
	still.text = "Stationary view (animate without travelling)"
	still.toggled.connect(func(value: bool): treadmill = value)
	box.add_child(still)
	var speed_label := Label.new()
	speed_label.text = "Playback speed"
	box.add_child(speed_label)
	speed_slider = HSlider.new()
	speed_slider.min_value = 0.1
	speed_slider.max_value = 1.5
	speed_slider.step = 0.05
	speed_slider.value = 1.0
	speed_slider.value_changed.connect(func(value: float): playback = value)
	box.add_child(speed_slider)
	var row := HBoxContainer.new()
	box.add_child(row)
	for clip in ["Bite", "Eat", "Drink", "Roar"]:
		var button := Button.new()
		button.focus_mode = Control.FOCUS_NONE
		button.text = clip
		button.pressed.connect(_play_action.bind(clip))
		row.add_child(button)
	var note := Label.new()
	note.text = "Brown-ochre skin | Ferocious Industries\nPreview of the playable Echo raptor." if echo_skin else ("Dark-green skin | Ferocious Industries\nPreview of the playable Delta raptor." if delta_skin else "Silver-blue skin | Ferocious Industries\nPreview of the playable Blue raptor.")
	note.add_theme_color_override("font_color", Color("#b2c3bc"))
	box.add_child(note)

func _toggle_mode() -> void:
	auto_mode = not auto_mode
	mode_button.text = "Automatic walk / run  |  click for manual" if auto_mode else "Manual WASD  |  click for automatic"

func _play_action(clip: String) -> void:
	if not action_clip.is_empty():
		return
	speed = 0.0
	action_clip = clip
	action_left = player.get_animation(clip).length
	var animation := (tree.tree_root as AnimationNodeBlendTree).get_node("Action") as AnimationNodeAnimation
	animation.animation = clip
	tree.set("parameters/ActionSeek/seek_request", 0.0)

func _process(delta: float) -> void:
	if tree == null or not tree.active:
		return
	var dt := minf(delta, 0.1) * playback if not paused else 0.0
	var target := 0.0
	var direction := Vector3.FORWARD
	if auto_mode:
		auto_clock += dt
		var phase := fmod(auto_clock, 20.0)
		if phase < 3.0:
			target = 0.0
		elif phase < 9.0:
			target = WALK_SPEED
		elif phase < 15.0:
			target = RUN_SPEED
		else:
			target = 0.0
		if phase < 0.1 and speed < 0.1:
			actor.position = Vector3.ZERO
	else:
		var input := Vector2(
			float(Input.is_physical_key_pressed(KEY_D)) - float(Input.is_physical_key_pressed(KEY_A)),
			float(Input.is_physical_key_pressed(KEY_S)) - float(Input.is_physical_key_pressed(KEY_W)))
		if not input.is_zero_approx():
			var forward := -camera.global_basis.z
			forward.y = 0
			forward = forward.normalized()
			direction = (camera.global_basis.x * input.x - forward * input.y).normalized()
			target = RUN_SPEED if Input.is_physical_key_pressed(KEY_SHIFT) else WALK_SPEED
	if not action_clip.is_empty():
		target = 0.0
		action_left -= dt
		if action_left <= 0.0:
			action_clip = ""
	_step(dt, target, direction)
	status.text = "%s  |  %.2f m/s  |  %.2fx%s" % [
		action_clip if not action_clip.is_empty() else ("Run" if speed > WALK_SPEED + 0.2 else ("Walk" if speed > 0.1 else "Idle")),
		speed, playback, "  PAUSED" if paused else ""]
	_update_camera()

func _step(dt: float, target: float, direction: Vector3) -> void:
	speed = move_toward(speed, target, 3.5 * dt)
	if speed > 0.01:
		actor.rotation.y = lerp_angle(actor.rotation.y, atan2(-direction.x, -direction.z), 1.0 - exp(-8.0 * dt))
		if not treadmill:
			actor.position += -actor.basis.z * speed * dt
	# The source faces +Z after glTF import; a 180-degree turn makes -Z forward.
	model.rotation.y = PI
	tree.set("parameters/WalkRate/scale", maxf(speed / WALK_SPEED, 0.05))
	tree.set("parameters/RunRate/scale", maxf(speed / RUN_SPEED, 0.05))
	var walking := smoothstep(0.0, 0.5, speed)
	var running := smoothstep(WALK_SPEED, RUN_SPEED, speed)
	tree.set("parameters/IdleWalk/blend_amount", walking)
	tree.set("parameters/WalkRun/blend_amount", running)
	var blend := float(tree.get("parameters/ActionBlend/blend_amount"))
	blend = move_toward(blend, 0.0 if action_clip.is_empty() else 1.0, dt * 6.0)
	tree.set("parameters/ActionBlend/blend_amount", blend)
	tree.advance(dt)

func _update_camera() -> void:
	var center := actor.position + Vector3(0, 0.95, 0)
	camera.position = center + Vector3(sin(yaw) * cos(pitch), sin(pitch), cos(yaw) * cos(pitch)) * distance
	camera.look_at(center)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			orbiting = event.pressed
		elif event.pressed and event.button_index == MOUSE_BUTTON_WHEEL_UP:
			distance = maxf(3.0, distance - 0.5)
		elif event.pressed and event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			distance = minf(18.0, distance + 0.5)
	elif event is InputEventMouseMotion and orbiting:
		yaw -= event.relative.x * 0.006
		pitch = clampf(pitch + event.relative.y * 0.006, 0.05, 1.1)
	elif event is InputEventKey and event.pressed and not event.echo:
		if event.physical_keycode == KEY_SPACE:
			paused = not paused
		elif event.physical_keycode == KEY_R:
			actor.position = Vector3.ZERO
			actor.rotation = Vector3.ZERO
			speed = 0.0
			auto_clock = 0.0

func _fail(message: String) -> void:
	push_error("RAPTOR LAB: " + message)
	get_tree().quit(1)

func _run_checks() -> void:
	set_process(false)
	var skeleton := model.find_child("*Skeleton*", true, false) as Skeleton3D
	if skeleton == null or skeleton.get_bone_count() < 20:
		_fail("Skeleton missing or incomplete")
		return
	var mesh := model.find_child("*Retopo*", true, false) as MeshInstance3D
	if mesh == null or mesh.mesh == null or mesh.mesh.get_surface_count() < 2:
		_fail("Expected body and detail material surfaces")
		return
	for surface in range(mesh.mesh.get_surface_count()):
		var material := mesh.mesh.surface_get_material(surface) as BaseMaterial3D
		if material == null or material.albedo_texture == null:
			_fail("Missing imported albedo texture on surface " + str(surface))
			return
	for clip in CLIPS:
		if player.get_animation(clip).get_track_count() == 0:
			_fail("Empty clip: " + clip)
			return
	var bounds: AABB = mesh.global_transform * mesh.get_aabb()
	if bounds.size.y < 1.5 or bounds.size.y > 2.5:
		_fail("Imported scale differs from approved Blender model: " + str(bounds.size))
		return
	var foot := skeleton.find_bone("Foot.L")
	if foot < 0:
		_fail("Left foot bone missing")
		return
	_step(0.0, 0.0, Vector3.FORWARD)
	var initial := skeleton.get_bone_global_pose(foot).origin
	var motion := 0.0
	for frame in range(420):
		_step(1.0 / 60.0, WALK_SPEED if frame < 150 else (RUN_SPEED if frame < 300 else 0.0), Vector3.FORWARD)
		motion = maxf(motion, initial.distance_to(skeleton.get_bone_global_pose(foot).origin))
		if not actor.position.is_finite() or not skeleton.get_bone_global_pose(foot).origin.is_finite():
			_fail("Invalid transform during locomotion blend")
			return
	if motion < 0.001:
		_fail("Imported skeleton did not animate")
		return
	if actor.position.z > -5.0 or speed > 0.01:
		_fail("Movement or deceleration failed")
		return
	for clip in ["Bite", "Eat", "Drink", "Roar", "Bite"]:
		_play_action(clip)
		for sample in range(12):
			_step(1.0 / 24.0, 0.0, Vector3.FORWARD)
			if not skeleton.get_bone_global_pose(foot).origin.is_finite():
				_fail("Invalid action pose: " + clip)
				return
		action_clip = ""
		_step(0.25, 0.0, Vector3.FORWARD)
	print("RAPTOR LAB PASSED: 7 textured clips, ", skeleton.get_bone_count(), " bones; walk/run/stop blend and moving foot verified.")
	get_tree().quit()

func _capture() -> void:
	set_process(false)
	for frame in range(100):
		_step(1.0 / 60.0, WALK_SPEED, Vector3.FORWARD)
	_update_camera()
	status.text = "Walk | %.2f m/s | 1.00x" % WALK_SPEED
	var detail := "--detail" in OS.get_cmdline_user_args()
	if detail:
		var focus := actor.position + Vector3(0, 1.25, -0.7)
		camera.position = focus + Vector3(sin(yaw) * cos(0.1), sin(0.1), cos(yaw) * cos(0.1)) * 2.65
		camera.look_at(focus)
	if "--face" in OS.get_cmdline_user_args():
		var focus := actor.position + Vector3(0, 1.55, -1.23)
		camera.position = focus + Vector3(sin(yaw) * cos(0.1), sin(0.1), cos(yaw) * cos(0.1)) * 1.4
		camera.look_at(focus)
	await RenderingServer.frame_post_draw
	DirAccess.make_dir_recursive_absolute("res://.runtime")
	var capture_path := "res://.runtime/raptor-eye-markings.png" if "--face" in OS.get_cmdline_user_args() else ("res://.runtime/raptor-skin-detail.png" if detail else "res://.runtime/raptor-lab.png")
	if delta_skin:
		capture_path = "res://.runtime/delta-skin-detail.png" if detail else "res://.runtime/delta-lab.png"
	if echo_skin:
		capture_path = "res://.runtime/echo-skin-detail.png" if detail else "res://.runtime/echo-lab.png"
	var error := get_viewport().get_texture().get_image().save_png(capture_path)
	print("Saved ", capture_path, ": ", error)
	get_tree().quit()
