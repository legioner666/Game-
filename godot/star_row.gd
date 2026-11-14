class_name StarRow
extends Control
## Ряд із трьох зірок (малюється кодом, без шрифтових символів).

var count := 0
var star_size := 40.0


func _init(c: int = 0, s: float = 40.0) -> void:
	count = c
	star_size = s
	custom_minimum_size = Vector2(star_size * 3.4, star_size * 1.1)
	mouse_filter = Control.MOUSE_FILTER_IGNORE


func _draw() -> void:
	for i in 3:
		var c := Vector2(star_size * 0.55 + i * star_size * 1.15, star_size * 0.58)
		var pts := PackedVector2Array()
		for k in 10:
			var r := star_size * (0.52 if k % 2 == 0 else 0.22)
			var a := -PI / 2.0 + k * PI / 5.0
			pts.append(c + Vector2(cos(a), sin(a)) * r)
		draw_colored_polygon(pts, Color("#FFF8E1") if i < count else Color(0.16, 0.22, 0.26, 0.75))
