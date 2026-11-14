extends Control
## Two cooldown abilities. This module owns buttons, timers and target selection.
## Damage application and reporting remain in the game integration hook.

signal spell_requested(kind: String)

const I18n = preload("res://i18n.gd")
const FREEZE_CD := 30.0
const METEOR_CD := 25.0
const FREEZE_SECONDS := 5.0
const METEOR_RADIUS := 3.8
const METEOR_DAMAGE := 150.0
const CELL := 4.0

class Glyph extends Control:
	var kind := ""

	func _init(k: String) -> void:
		kind = k
		custom_minimum_size = Vector2(66, 66)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		if kind == "freeze":
			for a in 3:
				var angle := float(a) * PI / 3.0
				var dir := Vector2(cos(angle), sin(angle)) * 26.0
				draw_line(Vector2(33, 33) - dir, Vector2(33, 33) + dir, Color(0.46, 0.90, 1.0), 6.0)
			draw_circle(Vector2(33, 33), 7.0, Color.WHITE)
		else:
			draw_colored_polygon(PackedVector2Array([
				Vector2(5, 7), Vector2(22, 18), Vector2(17, 26),
				Vector2(32, 32), Vector2(28, 42), Vector2(45, 38),
				Vector2(60, 58), Vector2(46, 46), Vector2(34, 54),
				Vector2(25, 40), Vector2(15, 42),
			]), Color(1.0, 0.45, 0.19))
			draw_circle(Vector2(42, 42), 12.0, Color(1.0, 0.79, 0.25))


var _level := 0
var _wave_active := false
var _has_targets := false
var _freeze_remaining := 0.0
var _cooldowns: Dictionary = {"freeze": 0.0, "meteor": 0.0}
var _buttons: Dictionary = {}
var _bars: Dictionary = {}
var _cd_labels: Dictionary = {}


func setup(lv: int) -> void:
	_level = lv
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	var row := HBoxContainer.new()
	row.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_RIGHT)
	row.offset_left = -390
	row.offset_right = -35
	row.offset_top = -375
	row.offset_bottom = -185
	row.alignment = BoxContainer.ALIGNMENT_END
	row.add_theme_constant_override("separation", 15)
	row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(row)
	_add_button(row, "freeze", Color(0.14, 0.41, 0.56))
	if _level >= 1:
		_add_button(row, "meteor", Color(0.61, 0.27, 0.18))
	_refresh()


func _add_button(row: HBoxContainer, kind: String, color: Color) -> void:
	var b := Button.new()
	b.text = ""
	b.focus_mode = Control.FOCUS_NONE
	b.custom_minimum_size = Vector2(158, 162)
	var normal := StyleBoxFlat.new()
	normal.bg_color = color
	normal.set_corner_radius_all(26)
	normal.shadow_color = Color(0, 0, 0, 0.5)
	normal.shadow_size = 6
	var pressed := normal.duplicate() as StyleBoxFlat
	pressed.bg_color = color.darkened(0.20)
	var disabled := normal.duplicate() as StyleBoxFlat
	disabled.bg_color = color.darkened(0.60)
	b.add_theme_stylebox_override("normal", normal)
	b.add_theme_stylebox_override("hover", normal)
	b.add_theme_stylebox_override("pressed", pressed)
	b.add_theme_stylebox_override("disabled", disabled)
	row.add_child(b)
	var icon := Glyph.new(kind)
	icon.position = Vector2(46, 6)
	b.add_child(icon)
	var label := Label.new()
	label.text = I18n.t("spell_" + kind)
	label.add_theme_font_size_override("font_size", 30)
	label.add_theme_color_override("font_color", Color.WHITE)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	label.offset_left = 4
	label.offset_right = -4
	label.offset_top = -66
	label.offset_bottom = -30
	b.add_child(label)
	var progress := ProgressBar.new()
	progress.max_value = FREEZE_CD if kind == "freeze" else METEOR_CD
	progress.show_percentage = false
	progress.custom_minimum_size = Vector2(0, 12)
	progress.mouse_filter = Control.MOUSE_FILTER_IGNORE
	progress.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	progress.offset_left = 14
	progress.offset_right = -14
	progress.offset_top = -24
	progress.offset_bottom = -11
	var bg := StyleBoxFlat.new()
	bg.bg_color = Color(0, 0, 0, 0.55)
	bg.set_corner_radius_all(5)
	var fill := StyleBoxFlat.new()
	fill.bg_color = Color(0.91, 0.85, 0.39)
	fill.set_corner_radius_all(5)
	progress.add_theme_stylebox_override("background", bg)
	progress.add_theme_stylebox_override("fill", fill)
	b.add_child(progress)
	var timer := Label.new()
	timer.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	timer.add_theme_font_size_override("font_size", 38)
	timer.add_theme_color_override("font_color", Color(1.0, 1.0, 0.92))
	timer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	timer.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
	timer.offset_left = -70
	timer.offset_right = 70
	timer.offset_top = -28
	timer.offset_bottom = 32
	b.add_child(timer)
	_buttons[kind] = b
	_bars[kind] = progress
	_cd_labels[kind] = timer
	b.pressed.connect(func() -> void: spell_requested.emit(kind))


func set_wave_active(active: bool, has_targets: bool) -> void:
	if _wave_active != active or _has_targets != has_targets:
		_wave_active = active
		_has_targets = has_targets
		_refresh()


func tick(delta: float) -> void:
	for kind in _cooldowns:
		_cooldowns[kind] = maxf(0.0, float(_cooldowns[kind]) - delta)
	_freeze_remaining = maxf(0.0, _freeze_remaining - delta)
	_refresh()


func enemy_speed_multiplier() -> float:
	return 0.4 if _freeze_remaining > 0.0 else 1.0


func _refresh() -> void:
	for kind in _buttons:
		var b: Button = _buttons[kind]
		var remaining: float = float(_cooldowns[kind])
		b.disabled = remaining > 0.0 or not _wave_active or not _has_targets
		var cd: ProgressBar = _bars[kind]
		cd.value = cd.max_value - remaining
		var timer: Label = _cd_labels[kind]
		timer.text = "%ds" % int(ceil(remaining)) if remaining > 0.0 else ""


func activate(kind: String, px: PackedFloat32Array, pz: PackedFloat32Array) -> Dictionary:
	if not _buttons.has(kind) or not _wave_active or not _has_targets or float(_cooldowns[kind]) > 0.0:
		return {}
	if kind == "freeze":
		_freeze_remaining = FREEZE_SECONDS
		_cooldowns["freeze"] = FREEZE_CD
		_refresh()
		return {"kind": "freeze", "hits": px.size()}
	if kind != "meteor" or px.is_empty():
		return {}
	var center_idx := _densest_target(px, pz)
	var center := Vector2(px[center_idx], pz[center_idx])
	var hit_indices: Array[int] = []
	for i in px.size():
		var dx: float = px[i] - center.x
		var dz: float = pz[i] - center.y
		if dx * dx + dz * dz <= METEOR_RADIUS * METEOR_RADIUS:
			hit_indices.append(i)
	_cooldowns["meteor"] = METEOR_CD
	_refresh()
	return {
		"kind": "meteor", "center": center, "indices": hit_indices,
		"hits": hit_indices.size(), "damage": METEOR_DAMAGE,
	}


func _densest_target(px: PackedFloat32Array, pz: PackedFloat32Array) -> int:
	# Lightweight spatial grid; O(n * 9) neighborhood score, not an O(n^2) scan.
	var cells: Dictionary = {}
	for i in px.size():
		var cell := Vector2i(floori(px[i] / CELL), floori(pz[i] / CELL))
		cells[cell] = int(cells.get(cell, 0)) + 1
	var best := 0
	var best_score := -1
	for i in px.size():
		var cell := Vector2i(floori(px[i] / CELL), floori(pz[i] / CELL))
		var score := 0
		for x in range(-1, 2):
			for y in range(-1, 2):
				score += int(cells.get(cell + Vector2i(x, y), 0))
		if score > best_score:
			best_score = score
			best = i
	return best
