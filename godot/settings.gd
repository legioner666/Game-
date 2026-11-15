extends RefCounted
## Налаштування гравця (мова, тіні, вібрація, звук). Зберігаються локально.

const PATH := "user://settings.cfg"

static var lang := "uk"
static var shadows := true
static var vibration := true
static var sound := true
static var music := true
static var stars: Array = [0, 0, 0, 0, 0, 0, 0, 0, 0]  # index = level + LEVEL_COUNT * difficulty
static var difficulty := 0
# Persistent improvements bought with the best-earned (non-consumed) level stars.
static var upgrades: Dictionary = {"crossbow": 0, "cannon": 0, "barracks": 0, "hero": 0, "castle": 0}
const LEVEL_COUNT := 3
const DIFF_COUNT := 3


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
	music = bool(cf.get_value("main", "music", music))
	for i in LEVEL_COUNT * DIFF_COUNT:
		stars[i] = clampi(int(cf.get_value("progress", "stars_%d" % i, 0)), 0, 3)
	difficulty = clampi(int(cf.get_value("progress", "difficulty", 0)), 0, DIFF_COUNT - 1)
	if not diff_unlocked(difficulty):
		difficulty = 0
	for kind in upgrades:
		upgrades[kind] = clampi(int(cf.get_value("progress", "tech_%s" % kind, 0)), 0, 3)


static func save_settings() -> void:
	var cf := ConfigFile.new()
	cf.set_value("main", "lang", lang)
	cf.set_value("main", "shadows", shadows)
	cf.set_value("main", "vibration", vibration)
	cf.set_value("main", "sound", sound)
	cf.set_value("main", "music", music)
	for i in LEVEL_COUNT * DIFF_COUNT:
		cf.set_value("progress", "stars_%d" % i, stars[i])
	cf.set_value("progress", "difficulty", difficulty)
	for kind in upgrades:
		cf.set_value("progress", "tech_%s" % kind, upgrades[kind])
	cf.save(PATH)


static func stars_of(level: int, diff: int) -> int:
	return int(stars[level + LEVEL_COUNT * diff])


## Складність відкривається, коли пройдено всі рівні попередньої.
static func diff_unlocked(diff: int) -> bool:
	if diff <= 0:
		return true
	for lv in LEVEL_COUNT:
		if stars_of(lv, diff - 1) <= 0:
			return false
	return true


static func unlocked(level: int, diff: int = 0) -> bool:
	if not diff_unlocked(diff):
		return false
	return level == 0 or stars_of(level - 1, diff) > 0


static func record_stars(level: int, diff: int, n: int) -> void:
	var i := level + LEVEL_COUNT * diff
	if n > int(stars[i]):
		stars[i] = n
		save_settings()
