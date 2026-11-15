extends Node
## Точка входу: перемикає меню та гру.

const Menu = preload("res://menu.gd")
const Game = preload("res://game.gd")
const Settings = preload("res://settings.gd")
const Sfx = preload("res://sfx.gd")
const Soundscape = preload("res://soundscape.gd")

var _sfx: Node
var _soundscape: Node
var _menu: Node
var _game: Node
var _level := 0


func _ready() -> void:
	Settings.load_settings()
	_sfx = Sfx.new()
	add_child(_sfx)
	_soundscape = Soundscape.new()
	add_child(_soundscape)
	var direct := false
	for a in OS.get_cmdline_user_args():
		if a == "--autoplay" or a == "--direct":
			direct = true
		elif a.begins_with("--level="):
			_level = clampi(int(a.substr(8)), 0, Settings.LEVEL_COUNT - 1)
	if direct:
		_start_game(_level)
	else:
		_show_menu()


func _clear() -> void:
	if _menu:
		_menu.queue_free()
		_menu = null
	if _game:
		_game.queue_free()
		_game = null


func _show_menu() -> void:
	_clear()
	_soundscape.set_mode("menu")
	_menu = Menu.new()
	_menu.sfx = _sfx
	add_child(_menu)
	_menu.play_pressed.connect(_start_game)
	_menu.stress_pressed.connect(func(): get_tree().change_scene_to_file("res://stress_test.tscn"))


func _start_game(level: int) -> void:
	_level = level
	_clear()
	_soundscape.set_mode("combat")
	_game = Game.new()
	_game.sfx = _sfx
	_game.level = level
	add_child(_game)
	_game.exit_to_menu.connect(_show_menu)
	_game.restart.connect(func(): _start_game(_level))
	_game.next_level.connect(func(): _start_game(mini(_level + 1, Settings.LEVEL_COUNT - 1)))
