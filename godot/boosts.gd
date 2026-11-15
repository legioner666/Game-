extends Node3D
## Timed pickup boosts. All gameplay state lives here; game.gd only calls public hooks.
## No imported assets. The four simple glyphs are drawn directly in Control.

signal picked(kind: String)

const I18n = preload("res://i18n.gd")
const Models = preload("res://models.gd")
const TYPES := ["haste", "frenzy", "gold", "barrier"]
const DURATION := 11.0
const DROP_CHANCE := 0.04
const PICKUP_LIFE := 14.0
const PICKUP_RADIUS := 2.3
const MAX_PICKUPS := 12

const SHADES := {
	"haste": Color(0.25, 0.73, 1.0),
	"frenzy": Color(1.0, 0.33, 0.24),
	"gold": Color(1.0, 0.79, 0.24),
	"barrier": Color(0.55, 0.45, 0.95),
}

class Glyph extends Control:
	var kind := ""
	var shade := Color.WHITE

	func _init(k: String, c: Color) -> void:
		kind = k
		shade = c
		custom_minimum_size = Vector2(44, 44)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		match kind:
			"haste":
				draw_colored_polygon(PackedVector2Array([
					Vector2(25, 2), Vector2(12, 23), Vector2(22, 23),
					Vector2(17, 42), Vector2(35, 17), Vector2(25, 17),
				]), shade)
			"frenzy":
				draw_line(Vector2(10, 8), Vector2(34, 36), shade, 6.0)
				draw_line(Vector2(34, 8), Vector2(10, 36), shade, 6.0)
				draw_circle(Vector2(22, 22), 5.0, shade)
			"gold":
				draw_circle(Vector2(22, 22), 18.0, shade)
				draw_arc(Vector2(22, 22), 11.0, 0.0, TAU, 24, Color(0.5, 0.32, 0.08), 3.0)
			"barrier":
				draw_colored_polygon(PackedVector2Array([
					Vector2(22, 3), Vector2(39, 12), Vector2(34, 31),
					Vector2(22, 42), Vector2(10, 31), Vector2(5, 12),
				]), shade)
				draw_line(Vector2(22, 10), Vector2(22, 33), Color.WHITE, 3.0)

var _time: Dictionary = {
	"haste": 0.0, "frenzy": 0.0, "gold": 0.0, "barrier": 0.0,
}
var _barrier_hits := 0
var _pickups: Array[Dictionary] = []
var _pickup_mesh: SphereMesh
var _materials: Dictionary = {}
var _hud: HBoxContainer
var _panels: Dictionary = {}
var _labels: Dictionary = {}


func _ready() -> void:
	_pickup_mesh = SphereMesh.new()
	_pickup_mesh.radius = 0.36
	_pickup_mesh.height = 0.72
	_pickup_mesh.radial_segments = 10
	_pickup_mesh.rings = 5
	for kind in TYPES:
		var material := StandardMaterial3D.new()
		material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		material.albedo_color = SHADES[kind]
		material.emission_enabled = true
		material.emission = SHADES[kind]
		_materials[kind] = material


func setup_hud(layer: CanvasLayer) -> void:
	_hud = HBoxContainer.new()
	_hud.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	_hud.offset_left = 60
	_hud.offset_right = -60
	_hud.offset_top = 465
	_hud.offset_bottom = 557
	_hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_hud.add_theme_constant_override("separation", 10)
	_hud.alignment = BoxContainer.ALIGNMENT_CENTER
	layer.add_child(_hud)
	for kind in TYPES:
		var panel := PanelContainer.new()
		panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
		var bg := StyleBoxFlat.new()
		bg.bg_color = Color(0.08, 0.15, 0.21, 0.86)
		bg.set_corner_radius_all(18)
		bg.content_margin_left = 10
		bg.content_margin_right = 10
		bg.content_margin_top = 8
		bg.content_margin_bottom = 8
		panel.add_theme_stylebox_override("panel", bg)
		var row := HBoxContainer.new()
		row.mouse_filter = Control.MOUSE_FILTER_IGNORE
		row.add_child(Glyph.new(kind, SHADES[kind]))
		var label := Label.new()
		label.add_theme_font_size_override("font_size", 21)
		label.clip_text = true
		label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		label.add_theme_color_override("font_color", Color(1.0, 0.97, 0.89))
		label.mouse_filter = Control.MOUSE_FILTER_IGNORE
		row.add_child(label)
		panel.custom_minimum_size = Vector2(250, 0)
		panel.add_child(row)
		_hud.add_child(panel)
		_panels[kind] = panel
		_labels[kind] = label
		panel.visible = false


func _glow(c: Color, a: float) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	m.albedo_color = Color(c.r, c.g, c.b, a)
	m.cull_mode = BaseMaterial3D.CULL_DISABLED
	return m


func _icon(kind: String) -> Node3D:
	## Distinct readable silhouette per boost, large and rotating.
	var n := Node3D.new()
	var c: Color = SHADES[kind]
	var dark := c.darkened(0.45)
	match kind:
		"haste":  # lightning bolt
			n.add_child(Models.mi(Models.box(Vector3(0.34, 0.95, 0.3)), Vector3(0.12, 0.38, 0), c, Vector3(0, 0, 22)))
			n.add_child(Models.mi(Models.box(Vector3(0.34, 0.95, 0.3)), Vector3(-0.12, -0.38, 0), c, Vector3(0, 0, 22)))
			n.add_child(Models.mi(Models.box(Vector3(0.78, 0.3, 0.3)), Vector3(0, 0.02, 0), dark))
		"frenzy":  # crossed swords
			for sgn in [-1.0, 1.0]:
				n.add_child(Models.mi(Models.box(Vector3(0.2, 1.7, 0.12)), Vector3.ZERO, Color(0.92, 0.94, 1.0), Vector3(0, 0, 45.0 * sgn)))
				n.add_child(Models.mi(Models.box(Vector3(0.55, 0.14, 0.2)), Vector3(-0.42 * sgn, -0.42, 0), c, Vector3(0, 0, 45.0 * sgn)))
			n.add_child(Models.mi(Models.sphere(0.2), Vector3.ZERO, c))
		"gold":  # coin purse with big coin
			n.add_child(Models.mi(Models.sphere(0.55, 10), Vector3(0, -0.1, 0), Color(0.92, 0.62, 0.12)))
			n.add_child(Models.mi(Models.cyl(0.18, 0.3, 0.3), Vector3(0, 0.5, 0), dark))
			n.add_child(Models.mi(Models.cyl(0.4, 0.4, 0.08, 12), Vector3(0, -0.1, 0.5), Color(1.0, 0.9, 0.3), Vector3(90, 0, 0)))
		"barrier":  # shield
			n.add_child(Models.mi(Models.box(Vector3(0.95, 0.8, 0.2)), Vector3(0, 0.2, 0), c))
			n.add_child(Models.mi(Models.cyl(0.0, 0.48, 0.6, 4), Vector3(0, -0.4, 0), c, Vector3(90, 45, 0), Vector3(1, 1, 0.42)))
			n.add_child(Models.mi(Models.box(Vector3(0.18, 1.0, 0.24)), Vector3(0, 0.0, 0.02), Color.WHITE))
	return n


func maybe_drop(pos: Vector3) -> void:
	if _pickups.size() >= MAX_PICKUPS or randf() >= DROP_CHANCE:
		return
	_spawn_pickup(TYPES[randi_range(0, TYPES.size() - 1)], pos)


func _spawn_pickup(kind: String, pos: Vector3) -> void:
	var c: Color = SHADES[kind]
	var root := Node3D.new()
	add_child(root)
	root.position = pos
	# ground ring + tall light beam: visible from afar and above the coin pile
	var ring := Models.mi(Models.cyl(0.8, 0.8, 0.04, 20), Vector3(0, 0.06, 0), c)
	ring.material_override = _glow(c, 0.55)
	root.add_child(ring)
	var icon := _icon(kind)
	icon.scale = Vector3.ONE * 0.95
	root.add_child(icon)
	var tag := Label3D.new()
	tag.text = I18n.t("boost_" + kind)
	tag.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	tag.no_depth_test = true
	tag.font_size = 72
	tag.pixel_size = 0.0038
	tag.outline_size = 16
	tag.modulate = c.lightened(0.35)
	tag.outline_modulate = Color(0.05, 0.08, 0.12)
	tag.position = Vector3(0, 1.9, 0)
	root.add_child(tag)
	for node in [ring, icon]:
		if node is GeometryInstance3D:
			node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_pickups.append({"kind": kind, "pos": pos, "node": root, "icon": icon, "life": 0.0})


func tick(delta: float, hero: Vector3) -> void:
	for kind in TYPES:
		var remaining: float = float(_time[kind])
		if remaining > 0.0:
			_time[kind] = maxf(0.0, remaining - delta)
		if kind == "barrier" and float(_time[kind]) <= 0.0:
			_barrier_hits = 0

	var i: int = _pickups.size() - 1
	while i >= 0:
		var p: Dictionary = _pickups[i]
		var age: float = float(p["life"]) + delta
		p["life"] = age
		var model: Node3D = p["node"]
		var icon: Node3D = p["icon"]
		var pos: Vector3 = p["pos"]
		icon.position = Vector3(0, 0.95 + 0.14 * sin(age * 4.0), 0)
		icon.rotation = Vector3(-0.95, 0.45 * sin(age * 2.0), 0.0)
		if age > PICKUP_LIFE - 3.0:  # blink before vanishing
			model.visible = int(age * 6.0) % 2 == 0
		var dx: float = hero.x - pos.x
		var dz: float = hero.z - pos.z
		if dx * dx + dz * dz <= PICKUP_RADIUS * PICKUP_RADIUS:
			var kind: String = p["kind"]
			if float(_time[kind]) <= 0.0 and kind == "barrier":
				_barrier_hits = 3
			_time[kind] = DURATION  # refresh only; never stack multipliers or shield charges
			picked.emit(kind)
			model.queue_free()
			_pickups.remove_at(i)
		elif age >= PICKUP_LIFE:
			model.queue_free()
			_pickups.remove_at(i)
		i -= 1
	_update_hud()


func _update_hud() -> void:
	if _hud == null:
		return
	for kind in TYPES:
		var remaining: float = float(_time[kind])
		var panel: PanelContainer = _panels[kind]
		panel.visible = remaining > 0.0
		if remaining > 0.0:
			var label: Label = _labels[kind]
			var suffix := " %d" % _barrier_hits if kind == "barrier" else ""
			label.text = "%s%s\n%ds" % [I18n.t("boost_" + kind), suffix, int(ceil(remaining))]


func tower_rate_multiplier() -> float:
	return 2.0 if float(_time["haste"]) > 0.0 else 1.0


func hero_rate_multiplier() -> float:
	return 2.0 if float(_time["frenzy"]) > 0.0 else 1.0


func gold_multiplier() -> float:
	return 2.0 if float(_time["gold"]) > 0.0 else 1.0


func consume_barrier() -> bool:
	if float(_time["barrier"]) <= 0.0 or _barrier_hits <= 0:
		return false
	_barrier_hits -= 1
	if _barrier_hits == 0:
		_time["barrier"] = 0.0
	_update_hud()
	return true
