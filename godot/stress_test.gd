extends Node3D
## Стрес-тест орди для Crown Rush (Godot 4.4, рендерер Mobile).
## Мета: побачити, скільки дрібних юнітів тримає телефон без просідання FPS.
## Керування: тягни палець по екрану, щоб рухати героя. Кнопки зверху міняють навантаження.
## Усе будується кодом, щоб сцену було легко читати й рецензувати в git.

const MAX_ENEMIES := 2000
const START_ENEMIES := 300
const STEP := 0.25  # крок таблиці точок стежки (одиниць)
const HERO_SPEED := 7.0
const WAYPOINTS: Array[Vector3] = [
	Vector3(7, 0, -17), Vector3(7, 0, -11), Vector3(-7, 0, -8), Vector3(-7, 0, -2),
	Vector3(7, 0, 1), Vector3(7, 0, 7), Vector3(-4, 0, 10), Vector3(-4, 0, 15),
]
const TOWER_POSITIONS: Array[Vector3] = [
	Vector3(0, 0.8, -9.5), Vector3(0, 0.8, -4.0), Vector3(0, 0.8, 2.5), Vector3(0, 0.8, 8.0),
]

var _samples := PackedVector3Array()
var _total := 0.0

var _count := START_ENEMIES
var _dist := PackedFloat32Array()
var _spd := PackedFloat32Array()
var _off_x := PackedFloat32Array()
var _off_z := PackedFloat32Array()
var _phase := PackedFloat32Array()
var _buf := PackedFloat32Array()
var _mm: MultiMesh

var _hero: MeshInstance3D
var _light: DirectionalLight3D
var _label: Label
var _shadow_button: Button

var _time := 0.0
var _frames := 0
var _acc := 0.0
var _worst := 0.0
var _fps := 0.0
var _worst_shown := 0.0
var _log_timer := 0.0

var _touch_id := -1
var _drag_origin := Vector2.ZERO
var _move_dir := Vector2.ZERO


func _ready() -> void:
	randomize()
	_build_path()
	_build_world()
	_build_enemies()
	_build_ui()
	_refresh_label()


# ---------- Стежка ----------

func _build_path() -> void:
	var pts := PackedVector3Array()
	for i in WAYPOINTS.size() - 1:
		var a := WAYPOINTS[i]
		var b := WAYPOINTS[i + 1]
		var seg_len := a.distance_to(b)
		var n := maxi(1, int(seg_len / STEP))
		for k in n:
			pts.append(a.lerp(b, float(k) / n))
	pts.append(WAYPOINTS[WAYPOINTS.size() - 1])
	_samples = pts
	_total = (pts.size() - 1) * STEP


# ---------- Світ ----------

func _make_material(color: Color) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = 1.0
	return m


func _add_box(size: Vector3, pos: Vector3, color: Color) -> MeshInstance3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	mesh.material = _make_material(color)
	var mi := MeshInstance3D.new()
	mi.mesh = mesh
	add_child(mi)
	mi.position = pos
	return mi


func _build_world() -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.62, 0.78, 0.92)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.75, 0.78, 0.85)
	env.ambient_light_energy = 0.6
	var we := WorldEnvironment.new()
	we.environment = env
	add_child(we)

	_light = DirectionalLight3D.new()
	_light.rotation_degrees = Vector3(-55, 30, 0)
	_light.shadow_enabled = true
	_light.directional_shadow_max_distance = 45.0
	add_child(_light)

	var cam := Camera3D.new()
	cam.keep_aspect = Camera3D.KEEP_WIDTH
	cam.fov = 55.0
	add_child(cam)
	cam.position = Vector3(0, 14, 11)
	cam.look_at(Vector3(0, 0, -1))

	# Земля й стежка
	_add_box(Vector3(60, 0.1, 80), Vector3(0, -0.1, 0), Color(0.36, 0.62, 0.30))
	for i in WAYPOINTS.size() - 1:
		var a := WAYPOINTS[i]
		var b := WAYPOINTS[i + 1]
		var seg := _add_box(Vector3(2.6, 0.06, a.distance_to(b)), Vector3.ZERO, Color(0.80, 0.68, 0.46))
		seg.look_at_from_position((a + b) * 0.5 + Vector3(0, 0.02, 0), b, Vector3.UP)
	for w in WAYPOINTS:
		_add_box(Vector3(2.6, 0.06, 2.6), w + Vector3(0, 0.02, 0), Color(0.80, 0.68, 0.46))

	# Вежі й база
	for p in TOWER_POSITIONS:
		_add_box(Vector3(0.9, 1.6, 0.9), p, Color(0.45, 0.32, 0.2))
		_add_box(Vector3(1.2, 0.3, 1.2), p + Vector3(0, 1.0, 0), Color(0.6, 0.45, 0.3))
	_add_box(Vector3(3.0, 1.8, 3.0), Vector3(-4, 0.9, 16.5), Color(0.2, 0.35, 0.8))

	# Герой
	var cap := CapsuleMesh.new()
	cap.radius = 0.4
	cap.height = 1.4
	cap.material = _make_material(Color(0.2, 0.4, 1.0))
	_hero = MeshInstance3D.new()
	_hero.mesh = cap
	add_child(_hero)
	_hero.position = Vector3(0, 0.7, 11)


# ---------- Орда ----------

func _build_enemies() -> void:
	_dist.resize(MAX_ENEMIES)
	_spd.resize(MAX_ENEMIES)
	_off_x.resize(MAX_ENEMIES)
	_off_z.resize(MAX_ENEMIES)
	_phase.resize(MAX_ENEMIES)
	_buf.resize(MAX_ENEMIES * 12)
	for i in MAX_ENEMIES:
		_dist[i] = randf() * _total
		_spd[i] = randf_range(2.6, 3.4)
		_off_x[i] = randf_range(-0.9, 0.9)
		_off_z[i] = randf_range(-0.9, 0.9)
		_phase[i] = randf() * TAU
		var o := i * 12
		_buf[o + 0] = 1.0
		_buf[o + 5] = 1.0
		_buf[o + 10] = 1.0

	var body := CapsuleMesh.new()
	body.radius = 0.28
	body.height = 0.9
	body.radial_segments = 6
	body.rings = 2
	body.material = _make_material(Color(0.85, 0.1, 0.1))

	_mm = MultiMesh.new()
	_mm.transform_format = MultiMesh.TRANSFORM_3D
	_mm.instance_count = MAX_ENEMIES
	_mm.mesh = body
	_mm.custom_aabb = AABB(Vector3(-30, -2, -30), Vector3(60, 8, 60))
	_mm.visible_instance_count = _count

	var mmi := MultiMeshInstance3D.new()
	mmi.multimesh = _mm
	add_child(mmi)


func _update_enemies(delta: float) -> void:
	for i in _count:
		var d: float = _dist[i] + _spd[i] * delta
		if d >= _total:
			d -= _total
		_dist[i] = d
		var p: Vector3 = _samples[int(d / STEP)]
		var o := i * 12
		_buf[o + 3] = p.x + _off_x[i]
		_buf[o + 7] = 0.45 + absf(sin(_time * 8.0 + _phase[i])) * 0.15
		_buf[o + 11] = p.z + _off_z[i]
	_mm.buffer = _buf


# ---------- Герой й ввід ----------

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		if event.pressed:
			_touch_id = event.index
			_drag_origin = event.position
			_move_dir = Vector2.ZERO
		elif event.index == _touch_id:
			_touch_id = -1
			_move_dir = Vector2.ZERO
	elif event is InputEventScreenDrag and event.index == _touch_id:
		var d: Vector2 = event.position - _drag_origin
		_move_dir = (d / 120.0).limit_length(1.0)


func _update_hero(delta: float) -> void:
	var p := _hero.position + Vector3(_move_dir.x, 0, _move_dir.y) * HERO_SPEED * delta
	p.x = clampf(p.x, -12.0, 12.0)
	p.z = clampf(p.z, -17.0, 17.0)
	_hero.position = p


# ---------- Інтерфейс ----------

func _make_button(text: String, cb: Callable) -> Button:
	var b := Button.new()
	b.text = text
	b.focus_mode = Control.FOCUS_NONE
	b.custom_minimum_size = Vector2(170, 130)
	b.add_theme_font_size_override("font_size", 44)
	b.pressed.connect(cb)
	return b


func _build_ui() -> void:
	var layer := CanvasLayer.new()
	add_child(layer)
	var box := VBoxContainer.new()
	box.position = Vector2(30, 90)
	box.add_theme_constant_override("separation", 14)
	layer.add_child(box)

	_label = Label.new()
	_label.add_theme_font_size_override("font_size", 38)
	_label.add_theme_color_override("font_color", Color.WHITE)
	_label.add_theme_color_override("font_outline_color", Color.BLACK)
	_label.add_theme_constant_override("outline_size", 10)
	box.add_child(_label)

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 14)
	box.add_child(row)
	row.add_child(_make_button("-100", _change_count.bind(-100)))
	row.add_child(_make_button("+100", _change_count.bind(100)))
	row.add_child(_make_button("+500", _change_count.bind(500)))
	_shadow_button = _make_button("", _toggle_shadows)
	row.add_child(_shadow_button)
	row.add_child(_make_button("Меню", func(): get_tree().change_scene_to_file("res://main.tscn")))
	_update_shadow_button()


func _change_count(delta_count: int) -> void:
	_count = clampi(_count + delta_count, 0, MAX_ENEMIES)
	_mm.visible_instance_count = _count
	_worst = 0.0
	_worst_shown = 0.0
	_refresh_label()


func _toggle_shadows() -> void:
	_light.shadow_enabled = not _light.shadow_enabled
	_update_shadow_button()
	_refresh_label()


func _update_shadow_button() -> void:
	_shadow_button.text = "Тіні:%s" % ("ON" if _light.shadow_enabled else "OFF")


func _refresh_label() -> void:
	_label.text = "FPS %d | найгірший кадр %.1f мс\nВорогів: %d | тіні: %s\n%s" % [
		roundi(_fps), _worst_shown * 1000.0, _count,
		"так" if _light.shadow_enabled else "ні",
		RenderingServer.get_video_adapter_name(),
	]


func _process(delta: float) -> void:
	_time += delta
	_update_enemies(delta)
	_update_hero(delta)

	_frames += 1
	_acc += delta
	_worst = maxf(_worst, delta)
	_log_timer += delta
	if _acc >= 1.0:
		_fps = _frames / _acc
		_worst_shown = _worst
		_frames = 0
		_acc = 0.0
		_worst = 0.0
		_refresh_label()
	if _log_timer >= 5.0:
		_log_timer = 0.0
		print("[stress] fps=%.1f worst=%.1fms enemies=%d shadows=%s" % [
			_fps, _worst_shown * 1000.0, _count, _light.shadow_enabled])
