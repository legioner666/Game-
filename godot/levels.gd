class_name Levels
extends RefCounted
## Дані рівнів: шлях, майданчики під вежі, замок і тема локації.
## Усі карти мають однаковий розмір поля; відрізняються формою шляху та виглядом.

const STANDARD_PLATES := [
	{"kind": "crossbow", "pos": Vector3(-3, 0, -6)},
	{"kind": "crossbow", "pos": Vector3(3, 0, -6)},
	{"kind": "cannon", "pos": Vector3(-3, 0, 4)},
	{"kind": "crossbow", "pos": Vector3(3, 0, 4)},
	{"kind": "barracks", "pos": Vector3(1.5, 0, 12.8)},
]


static func data(level: int) -> Dictionary:
	match level:
		1:
			return {
				"name": "level_desert",
				"waypoints": [
					Vector3(-6, 0, -17), Vector3(-6, 0, -10), Vector3(6, 0, -10), Vector3(6, 0, -2),
					Vector3(-6, 0, -2), Vector3(-6, 0, 6), Vector3(3, 0, 6), Vector3(-2, 0, 11),
					Vector3(-2, 0, 15),
				],
				"base_pos": Vector3(-2.0, 0.0, 17.8),
				"hero_start": Vector3(4.0, 0.0, 16.5),
				"plates": [
					{"kind": "crossbow", "pos": Vector3(0.5, 0, 2.5)},
					{"kind": "crossbow", "pos": Vector3(-1.0, 0, -6.5)},
					{"kind": "cannon", "pos": Vector3(-5.0, 0, 9.5)},
					{"kind": "crossbow", "pos": Vector3(1.0, 0, -13.5)},
					{"kind": "barracks", "pos": Vector3(1.5, 0, 12.5)},
					{"kind": "crossbow", "pos": Vector3(2.5, 0, -6.5)},
					{"kind": "cannon", "pos": Vector3(-4.5, 0, -6.5)},
				],
				"theme": {
					"ground": Color(0.82, 0.72, 0.52), "sand": Color(0.72, 0.50, 0.34), "rim": Color(0.58, 0.40, 0.28),
					"bg": Color(0.95, 0.82, 0.62), "ambient": Color(0.95, 0.86, 0.74),
					"tree_a": "cactus", "tree_b": "cactus", "tuft": "dry_tuft",
					"trees": 36, "rocks": 40, "bushes": 0, "flowers": 0, "tufts": 90,
				},
			}
		2:
			return {
				"name": "level_snow",
				"waypoints": [
					Vector3(0, 0, -17), Vector3(0, 0, -12), Vector3(-6, 0, -9), Vector3(-6, 0, -3),
					Vector3(6, 0, -3), Vector3(6, 0, 3), Vector3(-6, 0, 6), Vector3(-6, 0, 11),
					Vector3(-1, 0, 11), Vector3(-1, 0, 15),
				],
				"base_pos": Vector3(-1.0, 0.0, 17.8),
				"hero_start": Vector3(5.0, 0.0, 16.0),
				"plates": [
					{"kind": "crossbow", "pos": Vector3(-1.5, 0, 0.5)},
					{"kind": "crossbow", "pos": Vector3(0.0, 0, -8.0)},
					{"kind": "cannon", "pos": Vector3(1.0, 0, 8.0)},
					{"kind": "crossbow", "pos": Vector3(2.0, 0, 0.5)},
					{"kind": "barracks", "pos": Vector3(2.5, 0, 12.0)},
					{"kind": "crossbow", "pos": Vector3(-6.5, 0, -12.5)},
					{"kind": "cannon", "pos": Vector3(-5.0, 0, 1.5)},
				],
				"theme": {
					"ground": Color(0.90, 0.94, 0.97), "sand": Color(0.60, 0.72, 0.86), "rim": Color(0.42, 0.54, 0.70),
					"bg": Color(0.72, 0.82, 0.92), "ambient": Color(0.80, 0.85, 0.95),
					"tree_a": "snow_pine", "tree_b": "snow_pine", "tuft": "tuft",
					"trees": 60, "rocks": 30, "bushes": 0, "flowers": 0, "tufts": 0,
				},
			}
		_:
			return {
				"name": "level_meadow",
				"waypoints": [
					Vector3(7, 0, -17), Vector3(7, 0, -11), Vector3(-7, 0, -8), Vector3(-7, 0, -2),
					Vector3(7, 0, 1), Vector3(7, 0, 7), Vector3(-4, 0, 10), Vector3(-4, 0, 15),
				],
				"base_pos": Vector3(-4.0, 0.0, 17.8),
				"hero_start": Vector3(5.0, 0.0, 16.0),
				"plates": STANDARD_PLATES,
				"theme": {
					"ground": Color(0.47, 0.65, 0.47), "sand": Color(0.84, 0.71, 0.55), "rim": Color(0.66, 0.50, 0.35),
					"bg": Color(0.70, 0.82, 0.88), "ambient": Color(0.86, 0.83, 0.80),
					"tree_a": "pine", "tree_b": "round", "tuft": "tuft",
					"trees": 70, "rocks": 26, "bushes": 36, "flowers": 50, "tufts": 140,
				},
			}
