extends RefCounted
## Налаштування гравця (мова, тіні, вібрація, звук). Зберігаються локально.

const PATH := "user://settings.cfg"

static var lang := "uk"
static var shadows := true
static var vibration := true
static var sound := true
static var stars: Array = [0, 0, 0]
# Persistent improvements bought with the best-earned (non-consumed) level stars.
static var upgrades: Dictionary = {"crossbow": 0, "cannon": 0, "barracks": 0}
const LEVEL_COUNT := 3


static func load_settings() -> void:
	var loc := OS.get_locale_language()
	lang = "en" if (loc != "uk" and loc != "ru" and loc != "") else "uk"
	var cf := ConfigFile.new()
	if cf.load(PATH) != OK:
		return
	lang = str(cf.get_value("main", "lang", lang))
	shadows = bool(cf.get_value("main", "shadows", shadows))
	vibration = bool(cf.get_value("main", "vibration", vibration))
	sound = bool(cf.get_value("main", "sound", sound))
	for i in LEVEL_COUNT:
		stars[i] = clampi(int(cf.get_value("progress", "stars_%d" % i, 0)), 0, 3)
	for kind in upgrades:
		upgrades[kind] = clampi(int(cf.get_value("progress", "tech_%s" % kind, 0)), 0, 3)


static func save_settings() -> void:
	var cf := ConfigFile.new()
	cf.set_value("main", "lang", lang)
	cf.set_value("main", "shadows", shadows)
	cf.set_value("main", "vibration", vibration)
	cf.set_value("main", "sound", sound)
	for i in LEVEL_COUNT:
		cf.set_value("progress", "stars_%d" % i, stars[i])
	for kind in upgrades:
		cf.set_value("progress", "tech_%s" % kind, upgrades[kind])
	cf.save(PATH)


static func unlocked(level: int) -> bool:
	return level == 0 or int(stars[level - 1]) > 0


static func record_stars(level: int, n: int) -> void:
	if n > int(stars[level]):
		stars[level] = n
		save_settings()
