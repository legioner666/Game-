extends RefCounted
## Мінімальна локалізація UA/EN. Тексти беруться за ключем.

const Settings = preload("res://settings.gd")

const STR := {
	"title": {"uk": "CROWN RUSH", "en": "CROWN RUSH"},
	"tagline": {"uk": "Золото — у вежі. Фортеця — під захистом.", "en": "Turn gold into towers. Hold the keep."},
	"level_meadow": {"uk": "Галявина", "en": "Meadow"},
	"level_desert": {"uk": "Пустеля", "en": "Desert"},
	"level_snow": {"uk": "Крижана долина", "en": "Frozen Valley"},
	"level": {"uk": "Рівень %d", "en": "Level %d"},
	"next_level": {"uk": "Наступний рівень", "en": "Next level"},
	"choose_level": {"uk": "Обери рівень", "en": "Choose level"},
	"locked": {"uk": "Спершу пройди попередній", "en": "Beat the previous level first"},
	"all_done": {"uk": "Усі рівні пройдено!", "en": "All levels cleared!"},
	"play": {"uk": "Грати", "en": "Play"},
	"settings": {"uk": "Налаштування", "en": "Settings"},
	"copy_report": {"uk": "Скопіювати звіт", "en": "Copy report"},
	"copied": {"uk": "Скопійовано ✓", "en": "Copied ✓"},
	"back": {"uk": "Назад", "en": "Back"},
	"language": {"uk": "Мова", "en": "Language"},
	"lang_name": {"uk": "Українська", "en": "English"},
	"shadows": {"uk": "Тіні", "en": "Shadows"},
	"vibration": {"uk": "Вібрація", "en": "Vibration"},
	"sound": {"uk": "Звук", "en": "Sound"},
	"on": {"uk": "Увімк.", "en": "On"},
	"off": {"uk": "Вимк.", "en": "Off"},
	"stress": {"uk": "Тест навантаження", "en": "Stress test"},
	"wave": {"uk": "Хвиля %d/%d", "en": "Wave %d/%d"},
	"next_wave": {"uk": "Наступна хвиля через %d", "en": "Next wave in %d"},
	"last_wave": {"uk": "Остання хвиля!", "en": "Final wave!"},
	"wave_start": {"uk": "Хвиля %d", "en": "Wave %d"},
	"wave_clear": {"uk": "Хвилю відбито! +%d", "en": "Wave cleared! +%d"},
	"base": {"uk": "Замок", "en": "Castle"},
	"stop_hint": {"uk": "Зупинись, щоб будувати", "en": "Stop to build"},
	"hint_move": {"uk": "Тягни палець по екрану, щоб рухатись", "en": "Drag your finger to move"},
	"hint_build": {"uk": "Збирай монети й стань на плитку з ціною, щоб побудувати вежу", "en": "Collect coins and stand on a tile to build a tower"},
	"pause": {"uk": "Пауза", "en": "Paused"},
	"resume": {"uk": "Продовжити", "en": "Resume"},
	"restart": {"uk": "Спочатку", "en": "Restart"},
	"to_menu": {"uk": "В меню", "en": "Main menu"},
	"win": {"uk": "Перемога!", "en": "Victory!"},
	"lose": {"uk": "Замок впав", "en": "Castle fell"},
	"again": {"uk": "Ще раз", "en": "Play again"},
	"stats": {"uk": "Зібрано монет: %d\nЧас: %s", "en": "Coins collected: %d\nTime: %s"},
	"lose_stats": {"uk": "Дійшов до хвилі %d\nЗібрано монет: %d", "en": "Reached wave %d\nCoins collected: %d"},
	"crossbow": {"uk": "Арбалет", "en": "Crossbow"},
	"cannon": {"uk": "Гармата", "en": "Cannon"},
	"barracks": {"uk": "Казарма", "en": "Barracks"},
	"upgrade": {"uk": "Покращити", "en": "Upgrade"},
}


static func t(key: String) -> String:
	var d: Dictionary = STR.get(key, {})
	if d.is_empty():
		return key
	return str(d.get(Settings.lang, d.get("en", key)))
