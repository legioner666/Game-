extends Node3D
## Crown Rush — ігровий рівень (вертикальний зріз).
## Герой збирає монети з убитих ворогів, носить їх стопкою над головою й будує вежі,
## ставши на плитку з ціною. Вежі та союзники відбивають хвилі орди до замку.
## Усі моделі процедурні (див. models.gd), без зовнішніх ассетів.

signal exit_to_menu
signal restart
signal next_level

const Models = preload("res://models.gd")
const I18n = preload("res://i18n.gd")
const Settings = preload("res://settings.gd")

var sfx: Node

# ---------- Налаштування світу ----------

const STEP := 0.25
const YAW_WINDOW := 7
const WAYPOINTS: Array[Vector3] = [
	Vector3(7, 0, -17), Vector3(7, 0, -11), Vector3(-7, 0, -8), Vector3(-7, 0, -2),
	Vector3(7, 0, 1), Vector3(7, 0, 7), Vector3(-4, 0, 10), Vector3(-4, 0, 15),
]
const BASE_POS := Vector3(-4.0, 0.0, 17.8)
const RALLY := Vector3(-4.0, 0.0, 12.3)
const HERO_START := Vector3(5.0, 0.0, 16.0)
const BOUNDS := Vector2(11.5, 20.0)
const CAM_OFFSET := Vector3(0, 23, 16)
const CAM_Z_MIN := -6.0
const CAM_Z_MAX := 12.5
const CAM_X_LIMIT := 4.0
const CAM_FOV := 30.0
const DEPOSIT_MAX_SPEED := 3.2
const JOY_RADIUS := 170.0
const JOY_DEADZONE := 0.1

const HERO_SPEED := 7.4
const HERO_ACCEL := 48.0
const HERO_DECEL := 64.0
const HERO_RANGE := 7.5
const HERO_DMG := 9.0
const HERO_CD := 0.65
const CARRY_CAP := 150
const START_COINS := 15
const MAGNET_RADIUS := 3.0
const PLATE_REACH := 2.3
const DEPOSIT_INTERVAL := 0.045
const COIN_LIFETIME := 14.0
const BASE_HP_MAX := 20
const MAX_ENEMIES := 600
const MAX_COINS := 450
const STACK_VISIBLE := 40
const ALLY_AGGRO := 7.0
const ALLY_SPEED := 4.6

# Типи ворогів: 0 — рядовий, 1 — бігун, 2 — здоровань
const E_HP := [60.0, 34.0, 500.0]
const E_SPD_MIN := [2.0, 3.6, 1.2]
const E_SPD_MAX := [2.6, 4.2, 1.5]
const E_DMG := [1, 1, 5]
const E_COINS := [2, 2, 14]
const E_SCALE := [1.2, 1.1, 1.0]
const E_CONTACT_DPS := [4.0, 3.0, 14.0]
const E_OFFSET := [0.9, 0.9, 0.35]

const KIND := {
	"crossbow": {"cost": [15, 25, 40], "range": [8.5, 9.0, 9.5], "dmg": [12.0, 18.0, 28.0], "cd": [0.7, 0.6, 0.5]},
	"cannon": {"cost": [40, 60, 90], "range": [8.5, 9.0, 9.5], "dmg": [40.0, 65.0, 100.0], "cd": [2.2, 2.0, 1.8], "aoe": [2.0, 2.3, 2.6]},
	"barracks": {"cost": [30, 45, 65], "cap": [3, 4, 6], "dmg": [8.0, 11.0, 15.0], "hp": [40.0, 60.0, 90.0]},
}
const PLATE_DEFS := [
	{"kind": "crossbow", "pos": Vector3(-3, 0, -6)},
	{"kind": "crossbow", "pos": Vector3(3, 0, -6)},
	{"kind": "cannon", "pos": Vector3(-3, 0, 4)},
	{"kind": "crossbow", "pos": Vector3(3, 0, 4)},
	{"kind": "barracks", "pos": Vector3(1.5, 0, 12.8)},
	{"kind": "crossbow", "pos": Vector3(3, 0, -13), "min_level": 1},
	{"kind": "cannon", "pos": Vector3(-6.5, 0, 6.5), "min_level": 1},
]
# Група: [тип, кількість, старт (с), інтервал (с)]
const LEVEL_MULT := [1.0, 3.0, 4.2]
const LEVEL_START_MULT := [1.0, 1.3, 1.35]
const LEVEL_HP := [1.0, 1.2, 1.3]
const LEVEL_SPD := [1.0, 1.04, 1.06]
const LEVEL_COIN := [1.0, 1.1, 1.25]
const LEVEL_START := [15, 20, 30]
const LEVEL_BRUTES := [0, 3, 6]
const WAVES := [
	[[0, 7, 0.0, 1.2]],
	[[0, 11, 0.0, 1.0], [1, 4, 4.0, 1.8]],
	[[0, 16, 0.0, 0.9], [1, 7, 3.0, 1.4], [2, 1, 12.0, 1.0]],
	[[0, 22, 0.0, 0.8], [1, 10, 2.0, 1.2], [2, 2, 10.0, 6.0]],
	[[0, 29, 0.0, 0.7], [1, 14, 2.0, 1.0], [2, 4, 8.0, 4.0]],
]


# ---------- Допоміжні класи ----------

class Batch:
	var mm: MultiMesh
	var node: MultiMeshInstance3D
	var buf := PackedFloat32Array()
	var n := 0
	var cap := 0

	func _init(parent: Node3D, mesh: Mesh, capacity: int, shadows: bool = true) -> void:
		cap = capacity
		mm = MultiMesh.new()
		mm.transform_format = MultiMesh.TRANSFORM_3D
		mm.instance_count = capacity
		mm.mesh = mesh
		mm.custom_aabb = AABB(Vector3(-40, -5, -40), Vector3(80, 30, 80))
		mm.visible_instance_count = 0
		buf.resize(capacity * 12)
		node = MultiMeshInstance3D.new()
		node.multimesh = mm
		if not shadows:
			node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		parent.add_child(node)

	func begin() -> void:
		n = 0

	func add(pos: Vector3, yaw: float, sx: float, sy: float, sz: float) -> void:
		if n >= cap:
			return
		var o := n * 12
		var c := cos(yaw)
		var s := sin(yaw)
		buf[o] = sx * c
		buf[o + 1] = 0.0
		buf[o + 2] = sz * s
		buf[o + 3] = pos.x
		buf[o + 4] = 0.0
		buf[o + 5] = sy
		buf[o + 6] = 0.0
		buf[o + 7] = pos.y
		buf[o + 8] = -sx * s
		buf[o + 9] = 0.0
		buf[o + 10] = sz * c
		buf[o + 11] = pos.z
		n += 1

	func add_basis(pos: Vector3, b: Basis) -> void:
		if n >= cap:
			return
		var o := n * 12
		buf[o] = b.x.x
		buf[o + 1] = b.y.x
		buf[o + 2] = b.z.x
		buf[o + 3] = pos.x
		buf[o + 4] = b.x.y
		buf[o + 5] = b.y.y
		buf[o + 6] = b.z.y
		buf[o + 7] = pos.y
		buf[o + 8] = b.x.z
		buf[o + 9] = b.y.z
		buf[o + 10] = b.z.z
		buf[o + 11] = pos.z
		n += 1

	func end() -> void:
		mm.visible_instance_count = n
		mm.buffer = buf


class Plate:
	var kind := ""
	var pos := Vector3.ZERO
	var level := 0
	var progress := 0
	var node: Node3D
	var fill: MeshInstance3D
	var tile: MeshInstance3D
	var label: Label3D
	var sub_label: Label3D
	var icon: MeshInstance3D
	var tower: Node3D
	var head: Node3D
	var cooldown := 0.0
	var recoil := 0.0
	var spawn_timer := 0.5


class Coin:
	var pos := Vector3.ZERO
	var vel := Vector3.ZERO
	var state := 3  # 0 лежить, 1 летить до героя, 2 летить на плитку, 3 падає
	var age := 0.0
	var speed := 0.0
	var target := Vector3.ZERO
	var spin := 0.0


class Arrow:
	var pos := Vector3.ZERO
	var target_id := -1
	var last := Vector3.ZERO
	var dmg := 0.0
	var speed := 26.0


class Ball:
	var start := Vector3.ZERO
	var dest := Vector3.ZERO
	var t := 0.0
	var dur := 0.7
	var dmg := 0.0
	var aoe := 2.0


class Ally:
	var pos := Vector3.ZERO
	var home := Vector3.ZERO
	var hp := 40.0
	var max_hp := 40.0
	var cd := 0.0
	var yaw := 0.0
	var swing := 0.0
	var hurt := 0.0
	var plate: Plate
	var walk := 0.0
	var phase := 0.0


class Fx:
	var pos := Vector3.ZERO
	var vel := Vector3.ZERO
	var life := 0.5
	var max_life := 0.5
	var kind := 0
	var size := 1.0


class JoyDraw extends Control:
	var active := false
	var origin := Vector2.ZERO
	var knob := Vector2.ZERO

	func _draw() -> void:
		if not active:
			return
		draw_circle(origin, 170.0, Color(1, 1, 1, 0.10))
		draw_arc(origin, 170.0, 0.0, TAU, 48, Color(1, 1, 1, 0.40), 4.0)
		draw_circle(origin + knob, 62.0, Color(1, 1, 1, 0.55))


class CoinIcon extends Control:
	func _init() -> void:
		custom_minimum_size = Vector2(70, 70)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		var c := size * 0.5
		draw_circle(c, 31.0, Color(0.72, 0.5, 0.05))
		draw_circle(c, 27.0, Color(1.0, 0.78, 0.16))
		draw_circle(c, 17.0, Color(1.0, 0.9, 0.45))


# ---------- Стан ----------

var _samples := PackedVector3Array()
var _yaws := PackedFloat32Array()
var _total := 0.0

# вороги (масиви-структури для швидкості)
var e_id := PackedInt32Array()
var e_type := PackedInt32Array()
var e_dist := PackedFloat32Array()
var e_spd := PackedFloat32Array()
var e_hp := PackedFloat32Array()
var e_ox := PackedFloat32Array()
var e_oz := PackedFloat32Array()
var e_ph := PackedFloat32Array()
var e_slow := PackedFloat32Array()
var e_px := PackedFloat32Array()
var e_pz := PackedFloat32Array()
var _next_id := 1
var _idx_of := {}

var _plates: Array = []
var _coins: Array = []
var _arrows: Array = []
var _balls: Array = []
var _allies: Array = []
var _fx: Array = []
var _obstacles: Array = []

var _hero: Node3D
var _hero_vel := Vector3.ZERO
var _real_speed := 0.0
var _hero_yaw := 0.0
var _walk_phase := 0.0
var _hero_cd := 0.0
var _bow_kick := 0.0
var _carried := START_COINS
var _total_collected := 0
var _deposit_timer := 0.0
var _combo := 0
var _combo_t := 0.0
var _kill_snd_cd := 0.0

var _camera: Camera3D
var _light: DirectionalLight3D
var _castle: Node3D
var _shake := 0.0
var _cam_x := 0.0
var _cam_z := 0.0

var level := 0
var _waves: Array = []
var _stars_earned := 0
var _run_id := 0
var _wave := 0
var _phase := 0  # 0 — пауза між хвилями, 1 — хвиля йде
var _inter_t := 4.0
var _wave_clock := 0.0
var _spawn_queue: Array = []
var _base_hp := BASE_HP_MAX

var _time := 0.0
var _play_time := 0.0
var _paused := false
var _over := false
var _built_any := false
var _spent := 0
var _lost_coins := 0
var _wave_leaks := 0
var _leaks_total := 0
var _wave_start_t := 0.0
var _wave_hp0 := 0
var _first_build := true
var _pause_count := 0
var _fps_sum := 0.0
var _fps_n := 0
var _fps_min := 999.0
var _fps_t := 0.0
var _frame_max := 0.0
var _slow_frames := 0
var _frames := 0

# батчі
var _b_enemy: Array = []
var _b_ally: Batch
var _b_coin: Batch
var _b_stack: Batch
var _b_arrow: Batch
var _b_ball: Batch
var _b_fx: Array = []
var _b_hp_bg: Batch
var _b_hp_fg: Batch

# керування
var _joy_id := -1
var _joy_origin := Vector2.ZERO
var _joy_vec := Vector2.ZERO
var _joy_draw: JoyDraw

# інтерфейс
var _ui: CanvasLayer
var _coin_label: Label
var _wave_label: Label
var _sub_label: Label
var _hp_bar: ProgressBar
var _hint_label: Label
var _banner_label: Label
var _stop_label: Label
var _stop_t := 0.0
var _banner_tween: Tween
var _overlay: Control
var _stack_label: Label3D

# тестові режими (аргументи після --)
var _autoplay := false
var _fast_steps := 1
var _shot_prefix := ""
var _shot_times: Array = []
var _shot_idx := 0
var _show_kind := ""
var _bot_log_t := 0.0


func _ready() -> void:
	randomize()
	_parse_args()
	_waves = _make_waves(clampi(level, 0, LEVEL_MULT.size() - 1))
	_carried = LEVEL_START[clampi(level, 0, LEVEL_START.size() - 1)]
	_build_path()
	_build_world()
	_build_batches()
	_build_hero()
	_build_plates()
	_build_ui()
	_rebuild_obstacles()
	_run_id = Report.begin_run()
	Report.ev(0.0, "level", "n=%d" % (level + 1))
	if _show_kind != "":
		_paused = true
		_show_overlay(_show_kind)


func _parse_args() -> void:
	for a in OS.get_cmdline_user_args():
		if a == "--autoplay":
			_autoplay = true
		elif a.begins_with("--show="):
			_show_kind = a.substr(7)
		elif a.begins_with("--coins="):
			_carried = int(a.substr(8))
		elif a.begins_with("--fast="):
			_fast_steps = maxi(1, int(a.substr(7)))
		elif a.begins_with("--shots="):
			var spec := a.substr(8).split("@")
			_shot_prefix = spec[0]
			for tstr in spec[1].split(","):
				_shot_times.append(float(tstr))


# ---------- Стежка ----------

func _make_waves(lv: int) -> Array:
	var m: float = LEVEL_MULT[lv]
	var out: Array = []
	for wi in WAVES.size():
		# складність наростає: перша хвиля м'якша, остання — повна
		var mw: float = lerpf(LEVEL_START_MULT[lv], m, pow(float(wi) / (WAVES.size() - 1), 2.0))
		var groups: Array = []
		for g in WAVES[wi]:
			var t: int = g[0]
			var cnt: int = g[1]
			var n: int = cnt if t == 2 else int(ceil(cnt * mw))
			groups.append([t, n, g[2], float(g[3]) / sqrt(mw)])
		out.append(groups)
	if LEVEL_BRUTES[lv] > 0:
		for wi in [out.size() - 2, out.size() - 1]:
			out[wi].append([2, LEVEL_BRUTES[lv], 6.0, 5.0])
	return out


func _build_path() -> void:
	var pts := PackedVector3Array()
	var yaws := PackedFloat32Array()
	for i in WAYPOINTS.size() - 1:
		var a := WAYPOINTS[i]
		var b := WAYPOINTS[i + 1]
		var n := maxi(1, int(a.distance_to(b) / STEP))
		var dir := (b - a).normalized()
		var yaw := atan2(-dir.x, -dir.z)
		for k in n:
			pts.append(a.lerp(b, float(k) / n))
			yaws.append(yaw)
	pts.append(WAYPOINTS[WAYPOINTS.size() - 1])
	yaws.append(yaws[yaws.size() - 1])
	_samples = pts
	# згладжуємо кут повороту вздовж стежки, щоб на кутах вороги повертались плавно
	var sm := PackedFloat32Array()
	sm.resize(yaws.size())
	for i in yaws.size():
		var sx := 0.0
		var sz := 0.0
		for j in range(maxi(0, i - YAW_WINDOW), mini(yaws.size() - 1, i + YAW_WINDOW) + 1):
			sx += sin(yaws[j])
			sz += cos(yaws[j])
		sm[i] = atan2(sx, sz)
	_yaws = sm
	_total = (pts.size() - 1) * STEP


func _path_pos(d: float) -> Vector3:
	var f := maxf(d, 0.0) / STEP
	var i := mini(int(f), _samples.size() - 1)
	var j := mini(i + 1, _samples.size() - 1)
	return _samples[i].lerp(_samples[j], f - float(i))


func _path_yaw(d: float) -> float:
	var f := maxf(d, 0.0) / STEP
	var i := mini(int(f), _yaws.size() - 1)
	var j := mini(i + 1, _yaws.size() - 1)
	return lerp_angle(_yaws[i], _yaws[j], f - float(i))


func _dist_to_path(p: Vector3) -> float:
	var best := 1e9
	for i in WAYPOINTS.size() - 1:
		var a := WAYPOINTS[i]
		var ab := WAYPOINTS[i + 1] - a
		var t := clampf((p - a).dot(ab) / ab.length_squared(), 0.0, 1.0)
		best = minf(best, p.distance_to(a + ab * t))
	return best


# ---------- Світ ----------

func _build_world() -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.70, 0.82, 0.88)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.86, 0.83, 0.80)
	env.ambient_light_energy = 0.45
	var we := WorldEnvironment.new()
	we.environment = env
	add_child(we)

	_light = DirectionalLight3D.new()
	_light.rotation_degrees = Vector3(-58, 28, 0)
	_light.light_energy = 0.8
	_light.shadow_enabled = Settings.shadows
	_light.directional_shadow_max_distance = 60.0
	add_child(_light)

	_camera = Camera3D.new()
	_camera.keep_aspect = Camera3D.KEEP_WIDTH
	_camera.fov = CAM_FOV
	add_child(_camera)
	_cam_x = clampf(HERO_START.x, -CAM_X_LIMIT, CAM_X_LIMIT)
	_cam_z = clampf(HERO_START.z, CAM_Z_MIN, CAM_Z_MAX)
	_camera.position = Vector3(_cam_x, 0, _cam_z) + CAM_OFFSET
	_camera.look_at(Vector3(_cam_x, 0, _cam_z))

	# земля, стежка
	var ground := Models.mi(Models.box(Vector3(160, 0.2, 160)), Vector3(0, -0.1, 0), Color(0.47, 0.65, 0.47))
	Models.no_shadow(ground)
	add_child(ground)
	var rim_c := Color(0.66, 0.50, 0.35)
	var sand_c := Color(0.84, 0.71, 0.55)
	for i in WAYPOINTS.size() - 1:
		var a := WAYPOINTS[i]
		var b := WAYPOINTS[i + 1]
		var dir := (b - a).normalized()
		var yaw := atan2(-dir.x, -dir.z)
		var mid := (a + b) * 0.5
		var rim := Models.mi(Models.box(Vector3(3.2, 0.04, a.distance_to(b))), mid, rim_c)
		rim.rotation.y = yaw
		var sand := Models.mi(Models.box(Vector3(2.6, 0.06, a.distance_to(b))), mid + Vector3(0, 0.02, 0), sand_c)
		sand.rotation.y = yaw
		Models.no_shadow(rim)
		Models.no_shadow(sand)
		add_child(rim)
		add_child(sand)
	for w in WAYPOINTS:
		var rimp := Models.mi(Models.cyl(1.6, 1.6, 0.04, 14), w, rim_c)
		var sandp := Models.mi(Models.cyl(1.3, 1.3, 0.06, 14), w + Vector3(0, 0.02, 0), sand_c)
		Models.no_shadow(rimp)
		Models.no_shadow(sandp)
		add_child(rimp)
		add_child(sandp)

	# замок
	_castle = Models.castle()
	_castle.position = BASE_POS
	add_child(_castle)

	_build_decor()


func _build_decor() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 11
	var trees := Batch.new(self, Models.tree_mesh(), 90)
	var rtrees := Batch.new(self, Models.round_tree_mesh(), 60)
	var rocks := Batch.new(self, Models.rock_mesh(), 40)
	var bushes := Batch.new(self, Models.bush_mesh(), 60)
	var flowers := Batch.new(self, Models.flower_mesh(), 90)
	var tufts := Batch.new(self, Models.tuft_mesh(), 200)
	var pebbles := Batch.new(self, Models.pebble_mesh(), 120)
	for b in [trees, rtrees, rocks, bushes, flowers, tufts, pebbles]:
		b.begin()
	var tries := 0
	var placed_t := 0
	var placed_r := 0
	var placed_s := 0
	var placed_f := 0
	var placed_u := 0
	while tries < 2200 and (placed_t < 70 or placed_r < 26 or placed_s < 36 or placed_f < 50 or placed_u < 140):
		tries += 1
		var p := Vector3(rng.randf_range(-17.0, 17.0), 0.0, rng.randf_range(-27.0, 27.0))
		var dpath := _dist_to_path(p)
		if dpath < 1.9:
			continue
		if p.distance_to(BASE_POS) < 5.0 or p.distance_to(HERO_START) < 2.5:
			continue
		var near_plate := false
		for d in PLATE_DEFS:
			var pp: Vector3 = d["pos"]
			if p.distance_to(pp) < 3.0:
				near_plate = true
				break
		if near_plate:
			continue
		var roll := rng.randf()
		if roll < 0.28 and placed_t < 70 and dpath > 3.2:
			var s := rng.randf_range(0.8, 1.35)
			(trees if rng.randf() < 0.6 else rtrees).add(p, rng.randf() * TAU, s, s * rng.randf_range(0.9, 1.2), s)
			placed_t += 1
		elif roll < 0.38 and placed_r < 26 and dpath > 3.2:
			var s2 := rng.randf_range(0.7, 1.4)
			rocks.add(p, rng.randf() * TAU, s2, s2, s2)
			placed_r += 1
		elif roll < 0.52 and placed_s < 36:
			var s3 := rng.randf_range(0.8, 1.3)
			bushes.add(p, rng.randf() * TAU, s3, s3, s3)
			placed_s += 1
		elif roll < 0.68 and placed_f < 50:
			var s4 := rng.randf_range(0.9, 1.4)
			flowers.add(p, rng.randf() * TAU, s4, s4, s4)
			placed_f += 1
		elif placed_u < 140:
			var s5 := rng.randf_range(0.8, 1.5)
			tufts.add(p, rng.randf() * TAU, s5, s5, s5)
			placed_u += 1
	# камінці вздовж країв стежки
	var k := 0
	while k < _samples.size() and k < 4000:
		if k % 5 == 0 and rng.randf() < 0.6:
			var sp := _samples[k]
			var side := -1.0 if rng.randf() < 0.5 else 1.0
			var ang := _yaws[mini(k, _yaws.size() - 1)]
			var nrm := Vector3(cos(ang), 0, -sin(ang)) * side
			var pp2 := sp + nrm * rng.randf_range(1.55, 1.95)
			if pp2.distance_to(BASE_POS) > 4.0:
				var s6 := rng.randf_range(0.7, 1.3)
				pebbles.add(pp2, rng.randf() * TAU, s6, s6, s6)
		k += 1
	for b in [trees, rtrees, rocks, bushes, flowers, tufts, pebbles]:
		b.end()


func _build_batches() -> void:
	_b_enemy = [
		Batch.new(self, Models.grunt_mesh(), MAX_ENEMIES),
		Batch.new(self, Models.runner_mesh(), MAX_ENEMIES),
		Batch.new(self, Models.brute_mesh(), 40),
	]
	_b_ally = Batch.new(self, Models.ally_mesh(), 30)
	_b_coin = Batch.new(self, Models.coin_mesh(), MAX_COINS, false)
	_b_stack = Batch.new(self, Models.stack_disc_mesh(), STACK_VISIBLE, false)
	_b_arrow = Batch.new(self, Models.arrow_mesh(), 120, false)
	_b_ball = Batch.new(self, Models.ball_mesh(), 30, false)
	_b_fx = [
		Batch.new(self, Models.fx_mesh(Color(0.9, 0.15, 0.12)), 300, false),
		Batch.new(self, Models.fx_mesh(Color(0.92, 0.9, 0.85)), 300, false),
		Batch.new(self, Models.fx_mesh(Color(1.0, 0.85, 0.2)), 300, false),
	]
	var q_bg := QuadMesh.new()
	q_bg.size = Vector2(1.5, 0.2)
	q_bg.center_offset = Vector3(0.75, 0, 0)
	q_bg.material = _bar_material(Color(0.1, 0.1, 0.12))
	var q_fg := QuadMesh.new()
	q_fg.size = Vector2(1.5, 0.2)
	q_fg.center_offset = Vector3(0.75, 0, 0)
	q_fg.material = _bar_material(Color(0.95, 0.2, 0.18))
	_b_hp_bg = Batch.new(self, q_bg, 40, false)
	_b_hp_fg = Batch.new(self, q_fg, 40, false)


func _bar_material(c: Color) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = c
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	m.billboard_keep_scale = true
	return m


func _build_hero() -> void:
	_hero = Models.hero()
	_hero.scale = Vector3.ONE * 1.2
	_hero.position = HERO_START
	add_child(_hero)
	_stack_label = Label3D.new()
	_stack_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_stack_label.pixel_size = 0.012
	_stack_label.font_size = 96
	_stack_label.outline_size = 24
	_stack_label.modulate = Color(1.0, 0.92, 0.45)
	_stack_label.no_depth_test = true
	_stack_label.shaded = false
	add_child(_stack_label)


func _build_plates() -> void:
	for def in PLATE_DEFS:
		if level < int(def.get("min_level", 0)):
			continue
		var p := Plate.new()
		p.kind = def["kind"]
		p.pos = def["pos"]
		p.node = Node3D.new()
		p.node.position = p.pos
		add_child(p.node)
		p.tile = Models.mi(Models.box(Vector3(2.6, 0.06, 2.6)), Vector3(0, 0.06, 0), Color(0.36, 0.34, 0.32))
		Models.no_shadow(p.tile)
		p.node.add_child(p.tile)
		p.fill = Models.mi(Models.box(Vector3(2.4, 0.05, 1.0)), Vector3(0, 0.1, 1.2), Color(0.91, 0.71, 0.28))
		Models.no_shadow(p.fill)
		p.fill.visible = false
		p.node.add_child(p.fill)
		for sx in [-1.0, 1.0]:
			for sz in [-1.0, 1.0]:
				var c1 := Models.mi(Models.box(Vector3(0.6, 0.05, 0.12)), Vector3(1.3 * sx, 0.11, 1.3 * sz - 0.0), Color(0.4, 0.9, 0.4))
				var c2 := Models.mi(Models.box(Vector3(0.12, 0.05, 0.6)), Vector3(1.3 * sx, 0.11, 1.3 * sz - 0.24 * sz), Color(0.4, 0.9, 0.4))
				Models.no_shadow(c1)
				Models.no_shadow(c2)
				p.node.add_child(c1)
				p.node.add_child(c2)
		p.label = Label3D.new()
		p.label.rotation_degrees = Vector3(-90, 0, 0)
		p.label.position = Vector3(0, 0.16, -0.2)
		p.label.pixel_size = 0.013
		p.label.font_size = 110
		p.label.outline_size = 22
		p.label.shaded = false
		p.label.modulate = Color(1, 1, 1)
		p.node.add_child(p.label)
		p.icon = Models.mi(Models.cyl(0.26, 0.26, 0.05, 14), Vector3(0, 0.12, 0.62), Models.GOLD)
		Models.no_shadow(p.icon)
		p.node.add_child(p.icon)
		p.sub_label = Label3D.new()
		p.sub_label.rotation_degrees = Vector3(-90, 0, 0)
		p.sub_label.position = Vector3(0, 0.16, -0.98)
		p.sub_label.pixel_size = 0.009
		p.sub_label.font_size = 80
		p.sub_label.outline_size = 16
		p.sub_label.shaded = false
		p.sub_label.modulate = Color(0.85, 0.95, 1.0)
		p.node.add_child(p.sub_label)
		_plates.append(p)
		_refresh_plate(p)


func _rebuild_obstacles() -> void:
	_obstacles.clear()
	_obstacles.append([BASE_POS, 2.4])
	for p in _plates:
		if p.level > 0:
			_obstacles.append([p.pos, 1.0 if p.kind != "barracks" else 1.4])


# ---------- Інтерфейс ----------

func _style_box(c: Color, radius: int = 24) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = c
	sb.set_corner_radius_all(radius)
	sb.content_margin_left = 24
	sb.content_margin_right = 24
	sb.content_margin_top = 12
	sb.content_margin_bottom = 12
	return sb


func _make_button(text: String, size: Vector2, color: Color, font: int = 52) -> Button:
	var b := Button.new()
	b.text = text
	b.focus_mode = Control.FOCUS_NONE
	b.custom_minimum_size = size
	b.add_theme_font_size_override("font_size", font)
	b.add_theme_stylebox_override("normal", _style_box(color))
	b.add_theme_stylebox_override("hover", _style_box(color.lightened(0.1)))
	b.add_theme_stylebox_override("pressed", _style_box(color.darkened(0.15)))
	return b


func _make_label(text: String, font: int, color: Color = Color.WHITE) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", font)
	l.add_theme_color_override("font_color", color)
	l.add_theme_color_override("font_outline_color", Color(0.05, 0.05, 0.1))
	l.add_theme_constant_override("outline_size", 12)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l


func _build_ui() -> void:
	_ui = CanvasLayer.new()
	add_child(_ui)
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_ui.add_child(root)

	_joy_draw = JoyDraw.new()
	_joy_draw.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_joy_draw.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(_joy_draw)

	var bar := HBoxContainer.new()
	bar.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	bar.offset_left = 30
	bar.offset_right = -30
	bar.offset_top = 70
	bar.offset_bottom = 190
	bar.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar.add_theme_constant_override("separation", 20)
	root.add_child(bar)

	var pause := _make_button("II", Vector2(110, 110), Color(0.125, 0.21, 0.28, 0.92), 54)
	pause.pressed.connect(_toggle_pause)
	bar.add_child(pause)

	var mid := VBoxContainer.new()
	mid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	mid.mouse_filter = Control.MOUSE_FILTER_IGNORE
	mid.alignment = BoxContainer.ALIGNMENT_CENTER
	bar.add_child(mid)
	_wave_label = _make_label("", 56)
	_wave_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	mid.add_child(_wave_label)
	_sub_label = _make_label("", 38, Color(0.9, 0.95, 1.0))
	_sub_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	mid.add_child(_sub_label)

	var chip := PanelContainer.new()
	chip.mouse_filter = Control.MOUSE_FILTER_IGNORE
	chip.add_theme_stylebox_override("panel", _style_box(Color(0.125, 0.21, 0.28, 0.88), 40))
	bar.add_child(chip)
	var chip_row := HBoxContainer.new()
	chip_row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	chip_row.add_theme_constant_override("separation", 12)
	chip.add_child(chip_row)
	chip_row.add_child(CoinIcon.new())
	_coin_label = _make_label("0", 64, Color(1.0, 0.9, 0.45))
	_coin_label.custom_minimum_size = Vector2(110, 0)
	chip_row.add_child(_coin_label)

	var hp_row := HBoxContainer.new()
	hp_row.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	hp_row.offset_left = 60
	hp_row.offset_right = -60
	hp_row.offset_top = 215
	hp_row.offset_bottom = 265
	hp_row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hp_row.add_theme_constant_override("separation", 16)
	root.add_child(hp_row)
	hp_row.add_child(_make_label(I18n.t("base"), 38))
	_hp_bar = ProgressBar.new()
	_hp_bar.max_value = BASE_HP_MAX
	_hp_bar.value = BASE_HP_MAX
	_hp_bar.show_percentage = false
	_hp_bar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_hp_bar.custom_minimum_size = Vector2(0, 34)
	_hp_bar.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_hp_bar.add_theme_stylebox_override("background", _style_box(Color(0.08, 0.08, 0.12, 0.8), 17))
	_hp_bar.add_theme_stylebox_override("fill", _style_box(Color(0.85, 0.32, 0.27), 17))
	hp_row.add_child(_hp_bar)

	_hint_label = _make_label(I18n.t("hint_move"), 46, Color(1, 1, 1))
	_hint_label.set_anchors_and_offsets_preset(Control.PRESET_CENTER_TOP)
	_hint_label.offset_left = -440
	_hint_label.offset_right = 440
	_hint_label.offset_top = 300
	_hint_label.offset_bottom = 460
	_hint_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_hint_label.add_theme_stylebox_override("normal", _style_box(Color(0.05, 0.08, 0.16, 0.6), 30))
	_hint_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_hint_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(_hint_label)

	_banner_label = _make_label("", 72, Color(1.0, 0.9, 0.45))
	_banner_label.set_anchors_and_offsets_preset(Control.PRESET_CENTER_TOP)
	_banner_label.offset_left = -520
	_banner_label.offset_right = 520
	_banner_label.offset_top = 285
	_banner_label.offset_bottom = 395
	_banner_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_banner_label.pivot_offset = Vector2(520, 55)
	_banner_label.modulate.a = 0.0
	_banner_label.add_theme_constant_override("outline_size", 18)
	_stop_label = _make_label(I18n.t("stop_hint"), 52, Color(1, 1, 1))
	_stop_label.set_anchors_and_offsets_preset(Control.PRESET_CENTER_BOTTOM)
	_stop_label.offset_left = -400
	_stop_label.offset_right = 400
	_stop_label.offset_top = -520
	_stop_label.offset_bottom = -420
	_stop_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_stop_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_stop_label.add_theme_stylebox_override("normal", _style_box(Color(0.08, 0.14, 0.2, 0.75), 30))
	_stop_label.visible = false
	root.add_child(_stop_label)
	root.add_child(_banner_label)


func _banner(text: String, color: Color = Color(1.0, 0.9, 0.45)) -> void:
	if _banner_tween:
		_banner_tween.kill()
	_banner_label.text = text
	_banner_label.add_theme_color_override("font_color", color)
	_banner_label.modulate.a = 1.0
	_banner_label.scale = Vector2(0.6, 0.6)
	_banner_tween = create_tween()
	_banner_tween.tween_property(_banner_label, "scale", Vector2.ONE, 0.3).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	_banner_tween.tween_interval(1.4)
	_banner_tween.tween_property(_banner_label, "modulate:a", 0.0, 0.5)


func _show_overlay(kind: String) -> void:
	if kind != "pause":
		_hint_label.visible = false
	if _overlay:
		_overlay.queue_free()
	_overlay = Control.new()
	_overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_ui.add_child(_overlay)
	var dim := ColorRect.new()
	dim.color = Color(0.05, 0.1, 0.14, 0.78)
	dim.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_overlay.add_child(dim)
	var box := VBoxContainer.new()
	box.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
	box.offset_left = -400
	box.offset_right = 400
	box.offset_top = -620
	box.offset_bottom = 620
	box.alignment = BoxContainer.ALIGNMENT_CENTER
	box.add_theme_constant_override("separation", 28)
	_overlay.add_child(box)
	var title_key := "pause"
	var title_color := Color.WHITE
	if kind == "win":
		title_key = "win"
		title_color = Color(1.0, 0.85, 0.3)
	elif kind == "lose":
		title_key = "lose"
		title_color = Color(1.0, 0.4, 0.35)
	var title := _make_label(I18n.t(title_key), 120, title_color)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(title)
	if kind == "win" or kind == "lose":
		var txt := ""
		if kind == "win":
			var secs := int(_play_time)
			txt = I18n.t("stats") % [_total_collected, "%d:%02d" % [secs / 60, secs % 60]]
		else:
			txt = I18n.t("lose_stats") % [_wave + 1, _total_collected]
		if kind == "win":
			var srow := HBoxContainer.new()
			srow.alignment = BoxContainer.ALIGNMENT_CENTER
			srow.add_child(StarRow.new(_stars_earned, 90.0))
			box.add_child(srow)
		var st := _make_label(txt, 52, Color(0.9, 0.95, 1.0))
		st.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		box.add_child(st)
	if kind == "win":
		if level < Settings.LEVEL_COUNT - 1:
			var bn := _make_button(I18n.t("next_level"), Vector2(0, 150), Color(0.91, 0.71, 0.28), 60)
			for cn in ["font_color", "font_hover_color", "font_pressed_color"]:
				bn.add_theme_color_override(cn, Color("#2a2015"))
			bn.pressed.connect(func(): next_level.emit())
			box.add_child(bn)
		else:
			var done := _make_label(I18n.t("all_done"), 56, Color(0.97, 0.85, 0.45))
			done.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			box.add_child(done)
	if kind == "pause":
		var b1 := _make_button(I18n.t("resume"), Vector2(0, 140), Color(0.2, 0.62, 0.3), 58)
		b1.pressed.connect(_toggle_pause)
		box.add_child(b1)
	var b2 := _make_button(I18n.t("again") if kind != "pause" else I18n.t("restart"), Vector2(0, 140), Color(0.85, 0.6, 0.1), 58)
	b2.pressed.connect(func(): restart.emit())
	box.add_child(b2)
	var b3 := _make_button(I18n.t("to_menu"), Vector2(0, 140), Color(0.25, 0.3, 0.45), 58)
	b3.pressed.connect(func(): exit_to_menu.emit())
	box.add_child(b3)
	var b4 := _make_button(I18n.t("copy_report"), Vector2(0, 110), Color(0.2, 0.4, 0.55), 46)
	b4.pressed.connect(func():
		Report.copy_to_clipboard()
		b4.text = I18n.t("copied"))
	box.add_child(b4)


func _report_end(result: String) -> void:
	if not Report.is_current(_run_id):
		return
	var fps_avg := _fps_sum / maxf(1.0, float(_fps_n))
	Report.ev(_play_time, "summary", "result=%s wave=%d base=%d collected=%d spent=%d lost_on_ground=%d leaked=%d pauses=%d fps_avg=%.0f fps_min=%.0f worst_frame_ms=%.0f frames_over_25ms=%d/%d" % [result, _wave + 1, _base_hp, _total_collected, _spent, _lost_coins, _leaks_total, _pause_count, fps_avg, minf(_fps_min, fps_avg), _frame_max * 1000.0, _slow_frames, _frames])
	Report.end_run(result)


func _exit_tree() -> void:
	if not _over:
		_report_end("quit")


func _toggle_pause() -> void:
	if _over:
		return
	_paused = not _paused
	if _paused:
		_pause_count += 1
		Report.ev(_play_time, "pause", "")
		_show_overlay("pause")
	elif _overlay:
		_overlay.queue_free()
		_overlay = null
	_joy_id = -1
	_joy_vec = Vector2.ZERO
	_joy_draw.active = false
	_joy_draw.queue_redraw()


func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_GO_BACK_REQUEST:
		_toggle_pause()


# ---------- Ввід ----------

func _unhandled_input(event: InputEvent) -> void:
	if _paused or _over:
		return
	if event is InputEventScreenTouch:
		if event.pressed and _joy_id == -1:
			_joy_id = event.index
			_joy_origin = event.position
			_joy_vec = Vector2.ZERO
			_joy_draw.active = true
			_joy_draw.origin = _joy_origin
			_joy_draw.knob = Vector2.ZERO
			_joy_draw.queue_redraw()
		elif not event.pressed and event.index == _joy_id:
			_joy_id = -1
			_joy_vec = Vector2.ZERO
			_joy_draw.active = false
			_joy_draw.queue_redraw()
	elif event is InputEventScreenDrag and event.index == _joy_id:
		var d: Vector2 = event.position - _joy_origin
		var v := d.limit_length(JOY_RADIUS)
		_joy_vec = v / JOY_RADIUS
		if _joy_vec.length() < JOY_DEADZONE:
			_joy_vec = Vector2.ZERO
		_joy_draw.knob = v
		_joy_draw.queue_redraw()


# ---------- Службові ----------

func _snd(sound_name: String, pitch: float = 1.0) -> void:
	if sfx:
		sfx.play(sound_name, pitch)


func _vibrate(ms: int) -> void:
	if Settings.vibration:
		Input.vibrate_handheld(ms)


func _plate_cost(p: Plate) -> int:
	var costs: Array = KIND[p.kind]["cost"]
	return costs[mini(p.level, 2)]


func _refresh_plate(p: Plate) -> void:
	var maxed := p.level >= 3
	p.tile.visible = not maxed
	p.label.visible = not maxed
	p.icon.visible = not maxed
	p.sub_label.visible = not maxed
	if maxed:
		p.fill.visible = false
		return
	var cost := _plate_cost(p)
	p.label.text = str(cost - p.progress)
	var kind_name := I18n.t(p.kind)
	p.sub_label.text = kind_name if p.level == 0 else "%s  %d" % [I18n.t("upgrade"), p.level + 1]
	var ratio := float(p.progress) / float(cost)
	p.fill.visible = p.progress > 0
	if p.fill.visible:
		p.fill.scale = Vector3(1.0, 1.0, maxf(0.01, ratio * 2.4))
		p.fill.position = Vector3(0, 0.1, 1.2 - ratio * 1.2)
	if p.level > 0:
		p.node.position = p.pos
		p.label.position = Vector3(0, 0.16, 1.7) if p.kind != "barracks" else Vector3(0, 0.16, 1.9)
		p.icon.position = Vector3(0, 0.12, 2.3) if p.kind != "barracks" else Vector3(0, 0.12, 2.5)
		p.sub_label.position = Vector3(0, 0.16, 2.9) if p.kind != "barracks" else Vector3(0, 0.16, 3.1)
	else:
		p.label.position = Vector3(0, 0.16, -0.2)
		p.icon.position = Vector3(0, 0.12, 0.62)
		p.sub_label.position = Vector3(0, 0.16, -0.98)


# ---------- Основний цикл ----------

func _process(delta: float) -> void:
	if _paused or _over:
		_render_all(delta)
		_check_shots()
		return
	_frames += 1
	if _play_time > 2.0:
		_frame_max = maxf(_frame_max, delta)
		if delta > 0.025:
			_slow_frames += 1
	_fps_t += delta
	if _fps_t >= 1.0:
		_fps_t = 0.0
		var fps := Engine.get_frames_per_second()
		_fps_sum += fps
		_fps_n += 1
		_fps_min = minf(_fps_min, fps)
	var steps := _fast_steps
	var dt := delta if steps == 1 else 1.0 / 30.0
	dt = minf(dt, 0.05)
	for k in steps:
		_step(dt)
		if _over:
			break
	_render_all(delta)
	_update_hud()
	_check_shots()


func _step(delta: float) -> void:
	_time += delta
	_play_time += delta
	_kill_snd_cd = maxf(0.0, _kill_snd_cd - delta)
	_combo_t -= delta
	if _combo_t <= 0.0:
		_combo = 0
	if _autoplay:
		_bot_input()
	_update_waves(delta)
	_update_hero(delta)
	_update_enemies(delta)
	_update_allies(delta)
	_update_towers(delta)
	_update_hero_weapon(delta)
	_update_projectiles(delta)
	_cleanup_enemies()
	_update_coins(delta)
	_update_plates(delta)
	_update_fx(delta)


# ---------- Хвилі ----------

func _update_waves(delta: float) -> void:
	if _phase == 0:
		_inter_t -= delta
		if _inter_t <= 0.0:
			_start_wave(_wave)
		return
	_wave_clock += delta
	while _spawn_queue.size() > 0 and float(_spawn_queue[0][0]) <= _wave_clock:
		var ev: Array = _spawn_queue.pop_front()
		_spawn_enemy(int(ev[1]))
	if _spawn_queue.is_empty() and e_id.is_empty():
		_wave_cleared()


func _start_wave(idx: int) -> void:
	_spawn_queue.clear()
	var groups: Array = _waves[idx]
	for g in groups:
		var t: int = g[0]
		var cnt: int = g[1]
		var st: float = g[2]
		var iv: float = g[3]
		for k in cnt:
			_spawn_queue.append([st + k * iv, t])
	_spawn_queue.sort_custom(func(a, b): return a[0] < b[0])
	_wave_clock = 0.0
	_phase = 1
	if idx == _waves.size() - 1:
		_banner(I18n.t("last_wave"), Color(1.0, 0.45, 0.35))
	else:
		_banner(I18n.t("wave_start") % (idx + 1))
	_snd("spawn", 0.8)
	_wave_leaks = 0
	_wave_start_t = _play_time
	_wave_hp0 = _base_hp
	Report.ev(_play_time, "wave_start", "wave=%d base=%d carried=%d" % [idx + 1, _base_hp, _carried])
	if _autoplay:
		print("[bot] wave %d start t=%.0f base=%d carried=%d" % [idx + 1, _play_time, _base_hp, _carried])


func _wave_cleared() -> void:
	Report.ev(_play_time, "wave_clear", "wave=%d duration=%.1f base=%d->%d leaked=%d lost_coins=%d" % [_wave + 1, _play_time - _wave_start_t, _wave_hp0, _base_hp, _wave_leaks, _lost_coins])
	if _wave >= _waves.size() - 1:
		_finish(true)
		return
	var bonus := 6 + _wave * 4
	_carried = mini(CARRY_CAP, _carried + bonus)
	_total_collected += bonus
	_banner(I18n.t("wave_clear") % bonus, Color(0.5, 1.0, 0.6))
	_snd("build", 1.4)
	_wave += 1
	_phase = 0
	_inter_t = 7.0


func _finish(won: bool) -> void:
	if _over:
		return
	_over = true
	_stars_earned = 0
	if won:
		_stars_earned = 3 if _base_hp >= 18 else (2 if _base_hp >= 10 else 1)
		if not _autoplay:
			Settings.record_stars(level, _stars_earned)
	_report_end("win" if won else "lose")
	_snd("win" if won else "lose")
	_joy_id = -1
	_joy_vec = Vector2.ZERO
	_joy_draw.active = false
	_joy_draw.queue_redraw()
	if _autoplay:
		print("[bot] RESULT won=%s wave=%d base=%d coins=%d time=%.0f" % [won, _wave + 1, _base_hp, _total_collected, _play_time])
		if _shot_prefix == "":
			get_tree().quit()
		return
	_show_overlay("win" if won else "lose")


# ---------- Вороги ----------

func _spawn_enemy(t: int) -> void:
	if e_id.size() >= MAX_ENEMIES:
		return
	var id := _next_id
	_next_id += 1
	var off: float = E_OFFSET[t]
	var d := randf() * 0.4
	var sp := _samples[0]
	_idx_of[id] = e_id.size()
	e_id.append(id)
	e_type.append(t)
	e_dist.append(d)
	e_spd.append(randf_range(E_SPD_MIN[t], E_SPD_MAX[t]) * LEVEL_SPD[level])
	e_hp.append(E_HP[t] * LEVEL_HP[level])
	e_ox.append(randf_range(-off, off))
	e_oz.append(randf_range(-off, off))
	e_ph.append(randf() * TAU)
	e_slow.append(0.0)
	e_px.append(sp.x)
	e_pz.append(sp.z)


func _remove_enemy(i: int) -> void:
	var last := e_id.size() - 1
	_idx_of.erase(e_id[i])
	if i != last:
		e_id[i] = e_id[last]
		e_type[i] = e_type[last]
		e_dist[i] = e_dist[last]
		e_spd[i] = e_spd[last]
		e_hp[i] = e_hp[last]
		e_ox[i] = e_ox[last]
		e_oz[i] = e_oz[last]
		e_ph[i] = e_ph[last]
		e_slow[i] = e_slow[last]
		e_px[i] = e_px[last]
		e_pz[i] = e_pz[last]
		_idx_of[e_id[i]] = i
	e_id.resize(last)
	e_type.resize(last)
	e_dist.resize(last)
	e_spd.resize(last)
	e_hp.resize(last)
	e_ox.resize(last)
	e_oz.resize(last)
	e_ph.resize(last)
	e_slow.resize(last)
	e_px.resize(last)
	e_pz.resize(last)


func _update_enemies(delta: float) -> void:
	var n := e_id.size()
	for i in n:
		var spd := e_spd[i]
		var slow := e_slow[i]
		if slow > 0.0:
			spd *= 0.3
			e_slow[i] = slow - delta
		var d := e_dist[i] + spd * delta
		e_dist[i] = d
		var sp := _path_pos(d)
		var px := sp.x + e_ox[i]
		var pz := sp.z + e_oz[i]
		e_px[i] = px
		e_pz[i] = pz
		for a in _allies:
			var dx: float = a.pos.x - px
			var dz: float = a.pos.z - pz
			if dx * dx + dz * dz < 1.3:
				e_slow[i] = 0.2
				var dps: float = E_CONTACT_DPS[e_type[i]]
				a.hp -= dps * delta
				a.hurt = 0.15


func _damage_enemy(i: int, dmg: float) -> void:
	e_hp[i] -= dmg


func _cleanup_enemies() -> void:
	var i := e_id.size() - 1
	while i >= 0:
		var killed := e_hp[i] <= 0.0
		var leaked := e_dist[i] >= _total
		if killed or leaked:
			var t := e_type[i]
			if killed:
				var wp := Vector3(e_px[i], 0.0, e_pz[i])
				var cn: float = E_COINS[t] * LEVEL_COIN[level]
				_spawn_coins(wp, int(cn) + (1 if randf() < cn - floorf(cn) else 0))
				_burst(wp + Vector3(0, 0.5, 0), 0, 5 if t != 2 else 14, 3.5, 4.0)
				if _kill_snd_cd <= 0.0:
					_snd("kill", randf_range(0.9, 1.2))
					_kill_snd_cd = 0.05
			else:
				_hit_base(E_DMG[t])
			_remove_enemy(i)
		i -= 1


func _nearest_enemy(from: Vector3, max_r: float) -> int:
	var best := -1
	var best_d := max_r * max_r
	for i in e_id.size():
		var dx := e_px[i] - from.x
		var dz := e_pz[i] - from.z
		var d := dx * dx + dz * dz
		if d < best_d:
			best_d = d
			best = i
	return best


func _furthest_in_range(from: Vector3, r: float) -> int:
	var best := -1
	var best_dist := -1.0
	var r2 := r * r
	for i in e_id.size():
		var dx := e_px[i] - from.x
		var dz := e_pz[i] - from.z
		if dx * dx + dz * dz <= r2 and e_dist[i] > best_dist:
			best_dist = e_dist[i]
			best = i
	return best


func _hit_base(dmg: int) -> void:
	_wave_leaks += 1
	_leaks_total += 1
	_base_hp = maxi(0, _base_hp - dmg)
	_shake = 0.6
	_snd("hit")
	_vibrate(40)
	_burst(BASE_POS + Vector3(0, 1.0, 2.0), 1, 8, 3.0, 3.0)
	_castle.scale = Vector3(1.06, 0.94, 1.06)
	var tw := create_tween()
	tw.tween_property(_castle, "scale", Vector3.ONE, 0.3).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	if _base_hp <= 0:
		_finish(false)


# ---------- Герой ----------

func _update_hero(delta: float) -> void:
	var input := _joy_vec
	if _joy_id == -1:
		input = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	var target_vel := Vector3(input.x, 0.0, input.y) * HERO_SPEED
	var rate := HERO_ACCEL if target_vel.length() > 0.01 else HERO_DECEL
	_hero_vel = _hero_vel.move_toward(target_vel, rate * delta)
	var p := _hero.position + _hero_vel * delta
	for ob in _obstacles:
		var c: Vector3 = ob[0]
		var r: float = ob[1] + 0.4
		var off := Vector3(p.x - c.x, 0.0, p.z - c.z)
		var dist := off.length()
		if dist < r and dist > 0.001:
			p = c + off / dist * r
	p.x = clampf(p.x, -BOUNDS.x, BOUNDS.x)
	p.z = clampf(p.z, -BOUNDS.y, BOUNDS.y)
	p.y = 0.0
	var moved := p.distance_to(_hero.position)
	_real_speed = lerpf(_real_speed, moved / maxf(delta, 0.0001), 1.0 - exp(-18.0 * delta))
	_hero.position = p

	var speed := _real_speed
	var walk := clampf(speed / HERO_SPEED, 0.0, 1.0)
	var desired := _hero_yaw
	var tgt := _nearest_enemy(p, HERO_RANGE)
	if tgt >= 0:
		# цілимося у ворога навіть на бігу
		desired = atan2(-(e_px[tgt] - p.x), -(e_pz[tgt] - p.z))
	elif speed > 1.2:
		desired = atan2(-_hero_vel.x, -_hero_vel.z)
	_hero_yaw = lerp_angle(_hero_yaw, desired, 1.0 - exp(-14.0 * delta))
	_hero.rotation.y = _hero_yaw

	_walk_phase += speed * 1.7 * delta
	var sw := sin(_walk_phase) * 0.75 * walk
	(_hero.get_meta("leg_l") as Node3D).rotation.x = sw
	(_hero.get_meta("leg_r") as Node3D).rotation.x = -sw
	(_hero.get_meta("arm_r") as Node3D).rotation.x = -sw * 0.9
	var body := _hero.get_meta("body") as Node3D
	body.position.y = absf(sin(_walk_phase)) * 0.09 * walk
	body.scale.y = 1.0 + sin(_time * 3.0) * 0.012
	_bow_kick = maxf(0.0, _bow_kick - delta * 6.0)
	(_hero.get_meta("bow") as Node3D).position.z = -0.55 + _bow_kick * 0.14
	(_hero.get_meta("arm_l") as Node3D).rotation.x = 1.3 + _bow_kick * 0.12


func _update_hero_weapon(delta: float) -> void:
	_hero_cd -= delta
	if _hero_cd > 0.0:
		return
	var tgt := _nearest_enemy(_hero.position, HERO_RANGE)
	if tgt < 0:
		return
	_hero_cd = HERO_CD
	var fwd := Vector3(-sin(_hero_yaw), 0.0, -cos(_hero_yaw))
	_spawn_arrow(_hero.position + fwd * 0.7 + Vector3(0, 1.1, 0), tgt, HERO_DMG)
	_bow_kick = 1.0
	_snd("hero_shoot", randf_range(0.95, 1.1))


# ---------- Снаряди ----------

func _spawn_arrow(from: Vector3, tgt: int, dmg: float) -> void:
	if _arrows.size() >= 120:
		return
	var a := Arrow.new()
	a.pos = from
	a.target_id = e_id[tgt]
	a.last = Vector3(e_px[tgt], 0.6, e_pz[tgt])
	a.dmg = dmg
	_arrows.append(a)


func _spawn_ball(from: Vector3, tgt: int, dmg: float, aoe: float) -> void:
	if _balls.size() >= 30:
		return
	var dest := Vector3(e_px[tgt], 0.0, e_pz[tgt])
	var dur := clampf(from.distance_to(dest) / 13.0, 0.45, 1.0)
	var ahead := e_dist[tgt] + e_spd[tgt] * dur
	if ahead < _total:
		var sp := _path_pos(ahead)
		dest = Vector3(sp.x + e_ox[tgt], 0.0, sp.z + e_oz[tgt])
	var b := Ball.new()
	b.start = from
	b.dest = dest
	b.dur = dur
	b.dmg = dmg
	b.aoe = aoe
	_balls.append(b)


func _update_projectiles(delta: float) -> void:
	var i := _arrows.size() - 1
	while i >= 0:
		var a: Arrow = _arrows[i]
		var alive := _idx_of.has(a.target_id)
		if alive:
			var ti: int = _idx_of[a.target_id]
			a.last = Vector3(e_px[ti], 0.6, e_pz[ti])
		var to := a.last - a.pos
		var dist := to.length()
		var step := a.speed * delta
		if dist <= step + 0.25:
			if alive:
				_damage_enemy(_idx_of[a.target_id], a.dmg)
			_arrows.remove_at(i)
		else:
			a.pos += to / dist * step
		i -= 1
	i = _balls.size() - 1
	while i >= 0:
		var b: Ball = _balls[i]
		b.t += delta / b.dur
		if b.t >= 1.0:
			_explode(b)
			_balls.remove_at(i)
		i -= 1


func _explode(b: Ball) -> void:
	var r2 := b.aoe * b.aoe
	for k in e_id.size():
		var dx := e_px[k] - b.dest.x
		var dz := e_pz[k] - b.dest.z
		if dx * dx + dz * dz <= r2:
			_damage_enemy(k, b.dmg)
	_burst(b.dest + Vector3(0, 0.4, 0), 1, 10, 4.5, 3.0)
	_burst(b.dest + Vector3(0, 0.4, 0), 2, 6, 3.5, 5.0)
	_snd("boom", randf_range(0.9, 1.1))
	_shake = maxf(_shake, 0.12)


# ---------- Вежі й союзники ----------

func _update_towers(delta: float) -> void:
	for p in _plates:
		if p.level == 0:
			continue
		if p.kind == "barracks":
			_update_barracks(p, delta)
			continue
		var st: Dictionary = KIND[p.kind]
		var li: int = p.level - 1
		var rng: float = st["range"][li]
		p.cooldown -= delta
		p.recoil = maxf(0.0, p.recoil - delta * 7.0)
		var tgt := _furthest_in_range(p.pos, rng)
		if tgt >= 0 and p.head:
			var yaw := atan2(-(e_px[tgt] - p.pos.x), -(e_pz[tgt] - p.pos.z))
			p.head.rotation.y = lerp_angle(p.head.rotation.y, yaw, 1.0 - exp(-12.0 * delta))
			if p.cooldown <= 0.0 and absf(angle_difference(p.head.rotation.y, yaw)) < 0.3:
				var cd: float = st["cd"][li]
				var dmg: float = st["dmg"][li]
				p.cooldown = cd
				p.recoil = 1.0
				var muzzle: Vector3 = p.tower.get_meta("muzzle")
				var from: Vector3 = p.head.to_global(muzzle)
				if p.kind == "crossbow":
					_spawn_arrow(from, tgt, dmg)
					_snd("shoot", randf_range(0.95, 1.1))
				else:
					var aoe: float = st["aoe"][li]
					_spawn_ball(from, tgt, dmg, aoe)
					_snd("boom", 1.3)
					_burst(from, 2, 4, 2.0, 2.0)
		if p.head:
			p.head.position.z = p.recoil * 0.12


func _update_barracks(p: Plate, delta: float) -> void:
	var li: int = p.level - 1
	var cap: int = KIND["barracks"]["cap"][li]
	var count := 0
	for a in _allies:
		if a.plate == p:
			count += 1
	if count < cap:
		p.spawn_timer -= delta
		if p.spawn_timer <= 0.0:
			p.spawn_timer = 5.0
			var a := Ally.new()
			a.plate = p
			a.pos = p.pos + Vector3(0, 0, 1.4)
			a.max_hp = KIND["barracks"]["hp"][li]
			a.hp = a.max_hp
			a.home = RALLY + Vector3(randf_range(-1.3, 1.3), 0.0, randf_range(-1.0, 1.0))
			a.phase = randf() * TAU
			_allies.append(a)
			_burst(a.pos + Vector3(0, 0.5, 0), 1, 5, 2.0, 2.0)


func _update_allies(delta: float) -> void:
	var i := _allies.size() - 1
	while i >= 0:
		var a: Ally = _allies[i]
		a.cd -= delta
		a.swing = maxf(0.0, a.swing - delta * 5.0)
		a.hurt = maxf(0.0, a.hurt - delta)
		if a.hp <= 0.0:
			_burst(a.pos + Vector3(0, 0.6, 0), 1, 8, 3.0, 3.0)
			a.plate.spawn_timer = maxf(a.plate.spawn_timer, 5.0)
			_allies.remove_at(i)
			i -= 1
			continue
		var tgt := _nearest_enemy(a.home, ALLY_AGGRO)
		var goal := a.home
		var engaged := false
		if tgt >= 0:
			goal = Vector3(e_px[tgt], 0.0, e_pz[tgt])
			engaged = true
		var to := goal - a.pos
		to.y = 0.0
		var dist := to.length()
		var stop := 0.8 if engaged else 0.2
		var moving := dist > stop
		a.walk = move_toward(a.walk, 1.0 if moving else 0.0, delta * 6.0)
		if moving:
			a.pos += to / dist * minf(ALLY_SPEED * delta, dist - stop * 0.5)
			a.yaw = lerp_angle(a.yaw, atan2(-to.x, -to.z), 1.0 - exp(-12.0 * delta))
		if engaged and dist < 1.25 and a.cd <= 0.0:
			a.cd = 0.55
			a.swing = 1.0
			var dmg: float = KIND["barracks"]["dmg"][a.plate.level - 1]
			for k in e_id.size():
				var dx := e_px[k] - a.pos.x
				var dz := e_pz[k] - a.pos.z
				if dx * dx + dz * dz < 1.6:
					_damage_enemy(k, dmg)
		i -= 1


# ---------- Монети ----------

func _spawn_coins(pos: Vector3, n: int) -> void:
	for k in n:
		if _coins.size() >= MAX_COINS:
			_coins.remove_at(0)
		var c := Coin.new()
		c.pos = pos + Vector3(0, 0.6, 0)
		var ang := randf() * TAU
		var sp := randf_range(1.5, 4.0) if n > 1 else randf_range(0.5, 1.5)
		c.vel = Vector3(cos(ang) * sp, randf_range(5.0, 8.0), sin(ang) * sp)
		c.spin = randf() * TAU
		_coins.append(c)


func _update_coins(delta: float) -> void:
	var hp := _hero.position
	var i := _coins.size() - 1
	while i >= 0:
		var c: Coin = _coins[i]
		c.age += delta
		c.spin += delta * 6.0
		var remove := false
		match c.state:
			3:
				c.vel.y -= 24.0 * delta
				c.pos += c.vel * delta
				if c.pos.y <= 0.0:
					c.pos.y = 0.0
					if c.vel.y < -3.0:
						c.vel.y = -c.vel.y * 0.35
						c.vel.x *= 0.6
						c.vel.z *= 0.6
					else:
						c.state = 0
						c.age = 0.0
			0:
				if c.age > COIN_LIFETIME:
					remove = true
					_lost_coins += 1
				else:
					var dx := c.pos.x - hp.x
					var dz := c.pos.z - hp.z
					if dx * dx + dz * dz < MAGNET_RADIUS * MAGNET_RADIUS and _carried < CARRY_CAP:
						c.state = 1
						c.speed = 7.0
			1:
				c.speed += 45.0 * delta
				var goal := hp + Vector3(0, 1.1, 0)
				var to := goal - c.pos
				var dist := to.length()
				if dist < 0.5 or dist <= c.speed * delta:
					if _carried < CARRY_CAP:
						_carried += 1
						_total_collected += 1
						_combo = mini(_combo + 1, 12)
						_combo_t = 0.4
						_snd("coin", 1.0 + _combo * 0.05)
					remove = true
				else:
					c.pos += to / dist * c.speed * delta
			2:
				c.speed += 30.0 * delta
				var to2 := c.target - c.pos
				var dist2 := to2.length()
				if dist2 < 0.4 or dist2 <= c.speed * delta:
					remove = true
				else:
					c.pos += to2 / dist2 * c.speed * delta
		if remove:
			_coins.remove_at(i)
		i -= 1


# ---------- Будівництво ----------

func _update_plates(delta: float) -> void:
	var hp := _hero.position
	_stop_t = maxf(0.0, _stop_t - delta)
	var active: Plate = null
	for p in _plates:
		if p.level >= 3:
			continue
		var dx: float = p.pos.x - hp.x
		var dz: float = p.pos.z - hp.z
		if dx * dx + dz * dz <= PLATE_REACH * PLATE_REACH:
			active = p
			break
	if active != null and _carried > 0 and _real_speed > DEPOSIT_MAX_SPEED:
		_stop_t = 0.5
	if active == null or _carried <= 0 or _real_speed > DEPOSIT_MAX_SPEED:
		_deposit_timer = 0.0
		return
	_deposit_timer -= delta
	var cost := _plate_cost(active)
	var changed := false
	while _deposit_timer <= 0.0 and _carried > 0 and active.progress < cost:
		_deposit_timer += DEPOSIT_INTERVAL
		_carried -= 1
		_spent += 1
		active.progress += 1
		changed = true
		var c := Coin.new()
		c.state = 2
		c.pos = hp + Vector3(0, 1.6, 0)
		c.target = active.pos + Vector3(0, 0.3, 0)
		c.speed = 6.0
		_coins.append(c)
		_snd("tick", 0.9 + float(active.progress) / cost * 0.6)
	if changed:
		_refresh_plate(active)
	if active.progress >= cost:
		_complete_plate(active)


func _complete_plate(p: Plate) -> void:
	p.level += 1
	Report.ev(_play_time, "build" if p.level == 1 else "upgrade", "%s level=%d first=%s" % [p.kind, p.level, str(_first_build)])
	_first_build = false
	p.progress = 0
	p.cooldown = 0.3
	_built_any = true
	if p.tower:
		p.tower.queue_free()
	match p.kind:
		"crossbow":
			p.tower = Models.crossbow_tower(p.level)
		"cannon":
			p.tower = Models.cannon_tower(p.level)
		_:
			p.tower = Models.barracks(p.level)
	p.tower.position = p.pos
	add_child(p.tower)
	p.head = p.tower.get_meta("head") if p.tower.has_meta("head") else null
	p.tower.scale = Vector3.ZERO
	var tw := create_tween()
	tw.tween_property(p.tower, "scale", Vector3.ONE, 0.4).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	_burst(p.pos + Vector3(0, 0.3, 0), 1, 14, 4.0, 3.0)
	_burst(p.pos + Vector3(0, 0.5, 0), 2, 10, 4.0, 5.0)
	_snd("build", 1.0 + 0.2 * (p.level - 1))
	_vibrate(35)
	p.spawn_timer = 0.3
	if p.kind == "barracks":
		for a in _allies:
			if a.plate == p:
				a.max_hp = KIND["barracks"]["hp"][p.level - 1]
				a.hp = a.max_hp
	_refresh_plate(p)
	_rebuild_obstacles()


# ---------- Ефекти ----------

func _burst(pos: Vector3, kind: int, n: int, speed: float, up: float) -> void:
	for k in n:
		if _fx.size() >= 700:
			return
		var f := Fx.new()
		f.pos = pos
		var ang := randf() * TAU
		var sp := randf_range(0.3, 1.0) * speed
		f.vel = Vector3(cos(ang) * sp, randf_range(0.3, 1.0) * up, sin(ang) * sp)
		f.kind = kind
		f.max_life = randf_range(0.3, 0.6)
		f.life = f.max_life
		f.size = randf_range(0.6, 1.2)
		_fx.append(f)


func _update_fx(delta: float) -> void:
	var i := _fx.size() - 1
	while i >= 0:
		var f: Fx = _fx[i]
		f.life -= delta
		if f.life <= 0.0:
			_fx.remove_at(i)
		else:
			f.vel.y -= 14.0 * delta
			f.pos += f.vel * delta
			if f.pos.y < 0.05:
				f.pos.y = 0.05
				f.vel.y = 0.0
				f.vel.x *= 0.8
				f.vel.z *= 0.8
		i -= 1


# ---------- Малювання ----------

func _render_all(delta: float) -> void:
	_render_enemies()
	_render_allies()
	_render_coins()
	_render_projectiles()
	_render_fx()
	_render_stack()
	_shake = maxf(0.0, _shake - delta * 2.5)
	var tx := clampf(_hero.position.x, -CAM_X_LIMIT, CAM_X_LIMIT)
	var tz := clampf(_hero.position.z, CAM_Z_MIN, CAM_Z_MAX)
	var k := 1.0 - exp(-4.0 * delta)
	_cam_x = lerpf(_cam_x, tx, k)
	_cam_z = lerpf(_cam_z, tz, k)
	var target := Vector3(_cam_x, 0.0, _cam_z)
	var off := Vector3(randf_range(-1, 1), 0, randf_range(-1, 1)) * _shake * 0.25
	_camera.position = target + CAM_OFFSET + off
	_camera.look_at(target)


func _render_enemies() -> void:
	for b in _b_enemy:
		b.begin()
	_b_hp_bg.begin()
	_b_hp_fg.begin()
	var toward_cam := Vector3(0, 0.82, 0.57)
	for i in e_id.size():
		var t := e_type[i]
		var bob := absf(sin(_time * 9.0 + e_ph[i]))
		var sc: float = E_SCALE[t]
		var pos := Vector3(e_px[i], bob * 0.1, e_pz[i])
		_b_enemy[t].add(pos, _path_yaw(e_dist[i]), sc, sc * (1.0 + bob * 0.07), sc)
		if t == 2:
			var ratio := clampf(e_hp[i] / (float(E_HP[2]) * LEVEL_HP[level]), 0.0, 1.0)
			var bp := Vector3(e_px[i] - 0.75, 2.9, e_pz[i]) + toward_cam * 0.5
			_b_hp_bg.add(bp, 0.0, 1.0, 1.0, 1.0)
			_b_hp_fg.add(bp + toward_cam * 0.04, 0.0, maxf(0.01, ratio), 1.0, 1.0)
	for b in _b_enemy:
		b.end()
	_b_hp_bg.end()
	_b_hp_fg.end()


func _render_allies() -> void:
	_b_ally.begin()
	for a in _allies:
		var bob: float = absf(sin(_time * 10.0 + a.phase)) * 0.08 * a.walk
		var sy: float = 1.0 + a.swing * 0.12
		_b_ally.add(a.pos + Vector3(0, bob, 0), a.yaw, 1.0 + a.hurt * 0.4, sy, 1.0 + a.swing * 0.15)
	_b_ally.end()


func _render_coins() -> void:
	_b_coin.begin()
	for c in _coins:
		if c.state == 2:
			continue
		if c.state == 0 and c.age > COIN_LIFETIME - 3.0 and fmod(c.age, 0.3) > 0.18:
			continue
		var bob := 0.0 if c.state != 0 else sin(_time * 4.0 + c.spin) * 0.05
		_b_coin.add(c.pos + Vector3(0, bob, 0), c.spin, 1.4, 1.4, 1.4)
	for c in _coins:
		if c.state == 2:
			_b_coin.add(c.pos, c.spin, 1.1, 1.1, 1.1)
	_b_coin.end()


func _render_projectiles() -> void:
	_b_arrow.begin()
	for a in _arrows:
		var dir: Vector3 = a.last - a.pos
		if dir.length() > 0.01:
			_b_arrow.add_basis(a.pos, Basis.looking_at(dir.normalized(), Vector3.UP))
	_b_arrow.end()
	_b_ball.begin()
	for b in _balls:
		var k: float = b.t
		var pos: Vector3 = b.start.lerp(b.dest, k)
		pos.y += 4.0 * 3.2 * k * (1.0 - k)
		_b_ball.add(pos, 0.0, 1.0, 1.0, 1.0)
	_b_ball.end()


func _render_fx() -> void:
	for b in _b_fx:
		b.begin()
	for f in _fx:
		var s: float = f.size * clampf(f.life / f.max_life, 0.0, 1.0)
		_b_fx[f.kind].add(f.pos, 0.0, s, s, s)
	for b in _b_fx:
		b.end()


func _render_stack() -> void:
	_b_stack.begin()
	var n := mini(_carried, STACK_VISIBLE)
	var base := _hero.position + Vector3(0, 2.15, 0)
	for k in n:
		var lag := float(k) / STACK_VISIBLE
		var sway := Vector3(-_hero_vel.x, 0.0, -_hero_vel.z) * 0.022 * (lag * lag * 8.0)
		var wob := Vector3(sin(_time * 3.0 + k * 0.3) * 0.015 * lag * 4.0, 0.0, 0.0)
		_b_stack.add(base + Vector3(0, k * 0.1, 0) + sway + wob, 0.0, 1.0, 1.0, 1.0)
	_b_stack.end()
	_stack_label.visible = _carried > 0
	_stack_label.text = "x%d" % _carried
	_stack_label.position = base + Vector3(0, n * 0.1 + 0.8, 0)


func _update_hud() -> void:
	_stop_label.visible = _stop_t > 0.0
	_coin_label.text = str(_carried)
	_hp_bar.value = _base_hp
	_wave_label.text = I18n.t("wave") % [mini(_wave + 1, _waves.size()), _waves.size()]
	if _phase == 0:
		_sub_label.text = I18n.t("next_wave") % maxi(1, int(ceil(_inter_t)))
	else:
		_sub_label.text = ""
	if _built_any and _hint_label.visible:
		_hint_label.visible = false
	elif _play_time > 6.0 and _hint_label.text != I18n.t("hint_build"):
		_hint_label.text = I18n.t("hint_build")


# ---------- Тестовий бот і скріншоти ----------

func _bot_input() -> void:
	var hp := _hero.position
	var goal := Vector3.ZERO
	var has_goal := false
	var stop_dist := 0.5
	# 1) плитка, на яку вистачає монет
	var best: Plate = null
	var best_rem := 1 << 30
	for p in _plates:
		if p.level >= 3:
			continue
		var rem: int = _plate_cost(p) - p.progress
		if rem < best_rem and (rem <= _carried or p.progress > 0):
			best_rem = rem
			best = p
	if best != null and _carried > 0:
		goal = best.pos
		has_goal = true
		stop_dist = 1.6
	else:
		var bd := 1e9
		for c in _coins:
			if c.state != 0:
				continue
			var d := hp.distance_to(c.pos)
			if d < bd:
				bd = d
				goal = c.pos
				has_goal = true
	if not has_goal:
		goal = Vector3(0, 0, 6)
	var to := goal - hp
	to.y = 0.0
	var dist := to.length()
	var v := Vector2.ZERO
	if dist > stop_dist:
		v = Vector2(to.x, to.z) / dist
	_joy_vec = v
	_joy_id = 9999
	_bot_log_t += 1.0 / 30.0
	if _bot_log_t >= 2.0 and OS.get_cmdline_user_args().has("--botlog"):
		_bot_log_t = 0.0
		var lv := []
		for p in _plates:
			lv.append("%d/%d" % [p.level, p.progress])
		print("[botlog] t=%.0f hero=%s goal=%s carried=%d vel=%.1f plates=%s" % [_play_time, hero_str(hp), goal, _carried, _hero_vel.length(), lv])


func hero_str(v: Vector3) -> String:
	return "(%.1f,%.1f)" % [v.x, v.z]


func _check_shots() -> void:
	if _shot_prefix == "" or _shot_idx >= _shot_times.size():
		return
	if _play_time >= float(_shot_times[_shot_idx]):
		var img := get_viewport().get_texture().get_image()
		img.save_png("%s_%d.png" % [_shot_prefix, int(_shot_times[_shot_idx])])
		_shot_idx += 1
		if _shot_idx >= _shot_times.size():
			get_tree().quit()
