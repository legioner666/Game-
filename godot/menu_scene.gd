class_name MenuScene
extends SubViewportContainer
## Жива 3D-діорама за меню: замок, герой із луком, орда, що марширує дорогою, дерева.
## Використовує ті самі процедурні моделі, що й гра, тому стиль цілісний.

const Models = preload("res://models.gd")

var _vp: SubViewport
var _cam: Camera3D
var _hero: Node3D
var _t := 0.0
var _marchers: Array = []
var _flags: Array = []

const ROAD_START := Vector3(2.6, 0.0, 14.0)
const ROAD_END := Vector3(0.9, 0.0, 3.0)


func _init() -> void:
	stretch = true
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)


func _ready() -> void:
	_vp = SubViewport.new()
	_vp.own_world_3d = true
	_vp.msaa_3d = Viewport.MSAA_2X
	_vp.handle_input_locally = false
	add_child(_vp)

	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.46, 0.68, 0.90)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.80, 0.86, 1.0)
	env.ambient_light_energy = 0.55
	env.adjustment_enabled = true
	env.adjustment_saturation = 1.15
	env.adjustment_contrast = 1.05
	env.fog_enabled = true
	env.fog_light_color = Color(0.70, 0.84, 0.95)
	env.fog_density = 0.012
	var we := WorldEnvironment.new()
	we.environment = env
	_vp.add_child(we)

	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-38, 35, 0)
	sun.light_energy = 1.0
	sun.light_color = Color(1.0, 0.93, 0.80)
	sun.shadow_enabled = true
	sun.shadow_blur = 1.5
	sun.directional_shadow_max_distance = 40.0
	_vp.add_child(sun)

	_cam = Camera3D.new()
	_cam.keep_aspect = Camera3D.KEEP_WIDTH
	_cam.fov = 52.0
	_vp.add_child(_cam)

	_build_ground()
	_build_castle()
	_build_scenery()
	_build_hero()
	_build_marchers()


func _build_ground() -> void:
	var g := Models.mi(Models.cyl(30.0, 30.0, 0.6, 24), Vector3(0, -0.3, 6), Color(0.38, 0.70, 0.36))
	_vp.add_child(g)
	# дорога: смуга від горизонту до воріт
	var dir := (ROAD_END - ROAD_START)
	var mid := (ROAD_END + ROAD_START) * 0.5
	var ang := atan2(dir.x, dir.z)
	var road := Models.mi(Models.box(Vector3(2.6, 0.06, dir.length() + 4.0)), Vector3(mid.x, 0.02, mid.z), Color(0.84, 0.72, 0.52), Vector3(0, rad_to_deg(ang), 0))
	_vp.add_child(road)
	for s in [-1.0, 1.0]:
		var edge := Models.mi(Models.box(Vector3(0.22, 0.09, dir.length() + 4.0)), Vector3(mid.x + s * 1.4 * cos(ang), 0.04, mid.z - s * 1.4 * sin(ang)), Color(0.96, 0.58, 0.30), Vector3(0, rad_to_deg(ang), 0))
		_vp.add_child(edge)
	# пагорб під замком
	_vp.add_child(Models.mi(Models.cyl(4.6, 6.2, 0.9, 16), Vector3(0, 0.45, -1.2), Color(0.34, 0.64, 0.33)))


func _build_castle() -> void:
	var c := Models.castle()
	c.position = Vector3(0, 0.9, -1.2)
	c.scale = Vector3.ONE * 1.35
	_vp.add_child(c)


func _build_scenery() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 24
	var tree := Models.tree_mesh()
	var round_tree := Models.round_tree_mesh()
	var bush := Models.bush_mesh()
	var rock := Models.rock_mesh()
	for i in 46:
		var x := rng.randf_range(-14.0, 14.0)
		var z := rng.randf_range(-8.0, 16.0)
		if absf(x - 1.6 + (z - 8.0) * 0.1) < 2.4 and z > -1.0:
			continue  # не на дорозі
		if absf(x) < 4.8 and z < 0.5:
			continue  # не на пагорбі замку
		var mesh: ArrayMesh = tree if rng.randf() < 0.55 else (round_tree if rng.randf() < 0.6 else (bush if rng.randf() < 0.7 else rock))
		var m := Models.mi(mesh, Vector3(x, 0.0, z), Color.WHITE, Vector3(0, rng.randf() * 360.0, 0), Vector3.ONE * rng.randf_range(0.9, 1.6))
		m.material_override = Models.vc_material()
		_vp.add_child(m)


func _build_hero() -> void:
	_hero = Models.hero()
	_hero.position = Vector3(-2.4, 0.0, 5.2)
	_hero.rotation_degrees = Vector3(0, -150, 0)
	_hero.scale = Vector3.ONE * 1.5
	_vp.add_child(_hero)


func _build_marchers() -> void:
	var specs := [
		[Models.grunt_mesh(), Models.walk_material(0.42, 10.0, 0.2, 0.08, false), 1.5, 0.0, 1.0],
		[Models.grunt_mesh(), Models.walk_material(0.42, 10.0, 0.2, 0.08, false), 1.5, 0.35, 1.0],
		[Models.runner_mesh(), Models.walk_material(0.42, 15.0, 0.22, 0.1, true), 2.0, 0.6, 1.0],
		[Models.brute_mesh(), Models.walk_material(0.85, 5.5, 0.3, 0.12, false), 1.1, 0.85, 1.1],
	]
	for s in specs:
		var m := MeshInstance3D.new()
		m.mesh = s[0]
		m.material_override = s[1]
		m.scale = Vector3.ONE * float(s[4])
		_vp.add_child(m)
		_marchers.append({"node": m, "speed": s[2], "phase": s[3]})


func _process(delta: float) -> void:
	_t += delta
	# повільний рух камери: легкий «подих» діорами
	var sway := sin(_t * 0.35) * 1.1
	_cam.position = Vector3(sway, 5.6 + sin(_t * 0.5) * 0.15, 15.0)
	_cam.look_at(Vector3(sway * 0.3, 3.4, 0.0))
	# орда марширує до воріт і циклічно з'являється знову
	for m in _marchers:
		var p := fposmod(_t * float(m["speed"]) * 0.05 + float(m["phase"]), 1.0)
		var node: MeshInstance3D = m["node"]
		node.position = ROAD_START.lerp(ROAD_END, p) + Vector3(sin(float(m["phase"]) * 9.0) * 0.5, 0.0, 0.0)
		node.rotation.y = atan2(-(ROAD_END.x - ROAD_START.x), -(ROAD_END.z - ROAD_START.z))
		node.visible = p < 0.93
	# герой: цикл натягу тятиви й пострілу
	if _hero:
		var cyc := fposmod(_t, 2.4) / 2.4
		var draw := clampf(cyc / 0.8, 0.0, 1.0)
		var kick := clampf(1.0 - (cyc - 0.8) / 0.08, 0.0, 1.0) if cyc >= 0.8 else 0.0
		var bow := _hero.get_meta("bow") as Node3D
		Models.bow_pull(bow, draw if cyc < 0.8 else 0.0)
		var arm_r := _hero.get_meta("arm_r") as Node3D
		arm_r.rotation.z = -1.25 * maxf(smoothstep(0.0, 0.4, draw), 0.55)
		arm_r.rotation.x = lerpf(0.0, 0.35, smoothstep(0.0, 0.5, draw))
		(_hero.get_meta("body") as Node3D).rotation.x = kick * 0.1
		_hero.position.y = sin(_t * 2.0) * 0.015
