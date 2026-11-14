extends RefCounted
## Налаштування гравця (мова, тіні, вібрація, звук). Зберігаються локально.

const PATH := "user://settings.cfg"

static var lang := "uk"
static var shadows := true
static var vibration := true
static var sound := true


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


static func save_settings() -> void:
	var cf := ConfigFile.new()
	cf.set_value("main", "lang", lang)
	cf.set_value("main", "shadows", shadows)
	cf.set_value("main", "vibration", vibration)
	cf.set_value("main", "sound", sound)
	cf.save(PATH)
