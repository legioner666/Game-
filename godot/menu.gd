extends CanvasLayer
## Головне меню та налаштування.

signal play_pressed(level: int)
signal stress_pressed

const I18n = preload("res://i18n.gd")
const Settings = preload("res://settings.gd")

var sfx: Node
var _root: Control
var _page := "main"


class CrownIcon extends Control:
	func _init() -> void:
		custom_minimum_size = Vector2(340, 230)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		var gold := Color(1.0, 0.78, 0.16)
		var dark := Color(0.72, 0.5, 0.05)
		var pts := PackedVector2Array([
			Vector2(10, 210), Vector2(10, 70), Vector2(90, 130), Vector2(170, 10),
			Vector2(250, 130), Vector2(330, 70), Vector2(330, 210),
		])
		draw_colored_polygon(pts, gold)
		draw_polyline(PackedVector2Array([pts[0], pts[1], pts[2], pts[3], pts[4], pts[5], pts[6], pts[0]]), dark, 8.0)
		draw_rect(Rect2(10, 170, 320, 40), dark)
		for c in [Vector2(10, 70), Vector2(170, 10), Vector2(330, 70)]:
			draw_circle(c, 22.0, Color(0.72, 0.3, 0.27))
			draw_arc(c, 22.0, 0.0, TAU, 24, dark, 5.0)
		for x in [70.0, 170.0, 270.0]:
			draw_circle(Vector2(x, 190), 10.0, Color(0.35, 0.65, 1.0))


class CastleScene extends Control:
	func _init() -> void:
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		var w := size.x
		var h := size.y
		var hill := Color("#527B5C")
		var hill2 := Color("#79A678")
		var stone := Color("#7f93a0")
		var stone_d := Color("#5d717e")
		var roof := Color("#B84C44")
		draw_colored_polygon(PackedVector2Array([Vector2(0, h * 0.84), Vector2(w * 0.3, h * 0.8), Vector2(w * 0.7, h * 0.83), Vector2(w, h * 0.79), Vector2(w, h), Vector2(0, h)]), hill)
		draw_colored_polygon(PackedVector2Array([Vector2(0, h * 0.9), Vector2(w * 0.4, h * 0.87), Vector2(w, h * 0.91), Vector2(w, h), Vector2(0, h)]), hill2)
		# дорога до замку
		draw_colored_polygon(PackedVector2Array([Vector2(w * 0.44, h * 0.82), Vector2(w * 0.56, h * 0.82), Vector2(w * 0.8, h), Vector2(w * 0.2, h)]), Color("#D7B68D"))
		# замок
		var cx := w * 0.5
		var base_y := h * 0.82
		draw_rect(Rect2(cx - 150, base_y - 150, 300, 150), stone)
		for i in 5:
			draw_rect(Rect2(cx - 150 + i * 64, base_y - 185, 44, 38), stone)
		for sx in [-1.0, 1.0]:
			var tx: float = cx + sx * 190.0
			draw_rect(Rect2(tx - 45, base_y - 230, 90, 230), stone_d)
			draw_colored_polygon(PackedVector2Array([Vector2(tx - 62, base_y - 230), Vector2(tx + 62, base_y - 230), Vector2(tx, base_y - 330)]), roof)
		draw_rect(Rect2(cx - 36, base_y - 90, 72, 90), Color("#3b2a1f"))
		draw_circle(Vector2(cx, base_y - 90), 36.0, Color("#3b2a1f"))
		# прапор
		draw_line(Vector2(cx, base_y - 185), Vector2(cx, base_y - 290), Color("#3b2a1f"), 6.0)
		draw_colored_polygon(PackedVector2Array([Vector2(cx, base_y - 290), Vector2(cx + 70, base_y - 265), Vector2(cx, base_y - 240)]), Color("#E8B547"))


func _ready() -> void:
	_build()


func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_GO_BACK_REQUEST:
		if _page != "main":
			_go("main")
		else:
			get_tree().quit()


func _style(c: Color, radius: int = 36) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = c
	sb.set_corner_radius_all(radius)
	sb.content_margin_left = 30
	sb.content_margin_right = 30
	sb.content_margin_top = 14
	sb.content_margin_bottom = 14
	sb.shadow_color = Color(0, 0, 0, 0.35)
	sb.shadow_size = 8
	return sb


func _button(text: String, color: Color, height: float = 150.0, font: int = 62) -> Button:
	var b := Button.new()
	b.text = text
	b.focus_mode = Control.FOCUS_NONE
	b.custom_minimum_size = Vector2(0, height)
	b.add_theme_font_size_override("font_size", font)
	var dark_text := color.get_luminance() > 0.5
	var tc := Color("#2a2015") if dark_text else Color("#F2E6CB")
	b.add_theme_color_override("font_color", tc)
	b.add_theme_color_override("font_hover_color", tc)
	b.add_theme_color_override("font_pressed_color", tc)
	b.add_theme_color_override("font_disabled_color", Color("#7d8a93"))
	b.add_theme_stylebox_override("normal", _style(color))
	b.add_theme_stylebox_override("hover", _style(color.lightened(0.1)))
	b.add_theme_stylebox_override("pressed", _style(color.darkened(0.15)))
	return b


func _label(text: String, font: int, color: Color = Color("#F2E6CB")) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", font)
	l.add_theme_color_override("font_color", color)
	l.add_theme_color_override("font_outline_color", Color(0.03, 0.05, 0.12))
	l.add_theme_constant_override("outline_size", 14)
	l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	return l


func _click() -> void:
	if sfx:
		sfx.play("click")


func _go(page: String) -> void:
	_click()
	_page = page
	_build()


func _build() -> void:
	if _root:
		_root.queue_free()
	_root = Control.new()
	_root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(_root)

	var grad := Gradient.new()
	grad.set_color(0, Color("#3a5d73"))
	grad.set_color(1, Color("#142634"))
	var gt := GradientTexture2D.new()
	gt.gradient = grad
	gt.fill_from = Vector2(0.5, 0.0)
	gt.fill_to = Vector2(0.5, 1.0)
	gt.width = 8
	gt.height = 256
	var bg := TextureRect.new()
	bg.texture = gt
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	bg.stretch_mode = TextureRect.STRETCH_SCALE
	_root.add_child(bg)

	var scene := CastleScene.new()
	scene.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_root.add_child(scene)

	var box := VBoxContainer.new()
	box.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	box.offset_left = 90
	box.offset_right = -90
	box.offset_top = 330
	box.offset_bottom = -200
	box.add_theme_constant_override("separation", 34)
	box.alignment = BoxContainer.ALIGNMENT_BEGIN
	_root.add_child(box)

	if _page == "main":
		_build_main(box)
	elif _page == "levels":
		_build_levels(box)
	else:
		_build_settings(box)


func _build_main(box: VBoxContainer) -> void:
	var crown_row := HBoxContainer.new()
	crown_row.alignment = BoxContainer.ALIGNMENT_CENTER
	crown_row.add_child(CrownIcon.new())
	box.add_child(crown_row)
	box.add_child(_label(I18n.t("title"), 130, Color("#F8D874")))
	box.add_child(_label(I18n.t("tagline"), 50, Color("#F2E6CB")))
	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 120)
	box.add_child(spacer)
	var play := _button(I18n.t("play"), Color("#E8B547"), 190, 82)
	play.pressed.connect(func(): _go("levels"))
	box.add_child(play)
	var st := _button(I18n.t("settings"), Color("#2f4b5f"), 150, 58)
	st.pressed.connect(func(): _go("settings"))
	box.add_child(st)


func _build_levels(box: VBoxContainer) -> void:
	box.add_child(_label(I18n.t("choose_level"), 90, Color("#F8D874")))
	var sp := Control.new()
	sp.custom_minimum_size = Vector2(0, 40)
	box.add_child(sp)
	for lv in Settings.LEVEL_COUNT:
		var open := Settings.unlocked(lv)
		var row := Control.new()
		row.custom_minimum_size = Vector2(0, 190)
		box.add_child(row)
		var b := _button((I18n.t("level") % (lv + 1)) + " · " + I18n.t(Levels.data(lv)["name"]), Color("#E8B547") if open else Color("#3a4650"), 190, 72)
		b.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
		b.disabled = not open
		var l := lv
		b.pressed.connect(func():
			_click()
			play_pressed.emit(l))
		row.add_child(b)
		if open:
			var st := StarRow.new(int(Settings.stars[lv]), 44.0)
			st.position = Vector2(60, 70)
			st.mouse_filter = Control.MOUSE_FILTER_IGNORE
			row.add_child(st)
	var back := _button(I18n.t("back"), Color("#2f4b5f"), 140, 54)
	back.pressed.connect(func(): _go("main"))
	box.add_child(back)


func _toggle_row(box: VBoxContainer, key: String, value: bool, setter: Callable) -> void:
	var text := "%s: %s" % [I18n.t(key), I18n.t("on") if value else I18n.t("off")]
	var b := _button(text, Color("#4f8a5a") if value else Color("#7a4a44"), 140, 54)
	b.pressed.connect(func():
		setter.call(not value)
		Settings.save_settings()
		_go("settings"))
	box.add_child(b)


func _build_settings(box: VBoxContainer) -> void:
	box.add_child(_label(I18n.t("settings"), 100, Color(1.0, 0.85, 0.3)))
	var lang := _button("%s: %s" % [I18n.t("language"), I18n.t("lang_name")], Color("#2f4b5f"), 140, 54)
	lang.pressed.connect(func():
		Settings.lang = "en" if Settings.lang == "uk" else "uk"
		Settings.save_settings()
		_go("settings"))
	box.add_child(lang)
	_toggle_row(box, "shadows", Settings.shadows, func(v): Settings.shadows = v)
	_toggle_row(box, "vibration", Settings.vibration, func(v): Settings.vibration = v)
	_toggle_row(box, "sound", Settings.sound, func(v): Settings.sound = v)
	var stress := _button(I18n.t("stress"), Color(0.25, 0.25, 0.3), 120, 46)
	stress.pressed.connect(func():
		_click()
		stress_pressed.emit())
	box.add_child(stress)
	var rep := _button(I18n.t("copy_report"), Color(0.2, 0.4, 0.55), 120, 46)
	rep.pressed.connect(func():
		_click()
		Report.copy_to_clipboard()
		rep.text = I18n.t("copied"))
	box.add_child(rep)
	var back := _button(I18n.t("back"), Color("#E8B547"), 150, 58)
	back.pressed.connect(func(): _go("main"))
	box.add_child(back)
