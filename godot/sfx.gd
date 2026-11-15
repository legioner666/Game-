extends Node
## Crown Rush original procedural SFX pack.
## All samples are generated locally; no network, external assets or licenses.
## Existing sounds are prepared as before; new cues are prewarmed one per frame
## to avoid increasing the startup cost noticeably.

const Settings = preload("res://settings.gd")
const RATE := 22050
const POLYPHONY := 12

var _players: Array[AudioStreamPlayer] = []
var _streams: Dictionary = {}
var _next := 0

# New names are optional hooks for Claude to connect in game.gd.
# [initial Hz, final Hz, seconds, volume, noise mix, overtone mix]
const EXTRA: Dictionary = {
	"crossbow": [1250.0, 410.0, 0.075, 0.27, 0.32, 0.24],
	"cannon": [205.0, 38.0, 0.42, 0.62, 0.47, 0.10],
	"arrow_hit": [430.0, 90.0, 0.095, 0.24, 0.50, 0.0],
	"enemy_death": [490.0, 115.0, 0.16, 0.24, 0.26, 0.28],
	"upgrade": [310.0, 1200.0, 0.32, 0.41, 0.06, 0.26],
	"freeze": [1700.0, 240.0, 0.43, 0.31, 0.10, 0.44],
	"meteor": [680.0, 60.0, 0.66, 0.57, 0.47, 0.16],
	"ui_confirm": [590.0, 910.0, 0.095, 0.25, 0.0, 0.28],
	"ui_cancel": [850.0, 410.0, 0.105, 0.21, 0.0, 0.15],
	"castle_hit": [220.0, 65.0, 0.36, 0.55, 0.45, 0.12],
}

var _pending: Array[String] = []


func _ready() -> void:
	for i in POLYPHONY:
		var player := AudioStreamPlayer.new()
		add_child(player)
		_players.append(player)

	# Keep the original event keys compatible with all existing callers.
	_streams["coin"] = _tone(1050.0, 1700.0, 0.085, 0.31, 0.02, 0.30)
	_streams["tick"] = _tone(710.0, 840.0, 0.044, 0.21, 0.03)
	_streams["build"] = _tone(260.0, 970.0, 0.30, 0.41, 0.07, 0.17)
	_streams["shoot"] = _tone(980.0, 310.0, 0.10, 0.28, 0.24, 0.25)
	_streams["hero_shoot"] = _tone(860.0, 390.0, 0.075, 0.20, 0.13, 0.31)
	_streams["boom"] = _tone(185.0, 42.0, 0.38, 0.59, 0.54, 0.10)
	_streams["kill"] = _tone(320.0, 100.0, 0.08, 0.15, 0.54)
	_streams["hit"] = _tone(150.0, 65.0, 0.29, 0.50, 0.61)
	_streams["spawn"] = _tone(330.0, 480.0, 0.12, 0.25, 0.07, 0.17)
	_streams["win"] = _tone(380.0, 1270.0, 0.69, 0.43, 0.02, 0.32)
	_streams["lose"] = _tone(390.0, 68.0, 0.78, 0.45, 0.12, 0.19)
	_streams["click"] = _tone(630.0, 495.0, 0.050, 0.27, 0.02)
	for key in EXTRA:
		_pending.append(key)
	set_process(not _pending.is_empty())


func _process(_delta: float) -> void:
	# Build one optional clip on each rendered frame. Each waveform is
	# calculated once, not continuously during gameplay.
	if _pending.is_empty():
		set_process(false)
		return
	var sound_name: String = _pending.pop_front()
	if not _streams.has(sound_name):
		_streams[sound_name] = _optional_tone(sound_name)


func _optional_tone(sound_name: String) -> AudioStreamWAV:
	var spec: Array = EXTRA[sound_name]
	return _tone(float(spec[0]), float(spec[1]), float(spec[2]), float(spec[3]), float(spec[4]), float(spec[5]))


func _tone(f0: float, f1: float, dur: float, vol: float, noise: float = 0.0, overtone: float = 0.0) -> AudioStreamWAV:
	var sample_count: int = int(RATE * dur)
	var bytes := PackedByteArray()
	bytes.resize(sample_count * 2)
	var phase := 0.0
	var inverse_count: float = 1.0 / float(sample_count)
	for i in sample_count:
		var t: float = float(i) * inverse_count
		phase += TAU * lerpf(f0, f1, t) / float(RATE)
		var tonal: float = (sin(phase) + overtone * sin(2.01 * phase)) / (1.0 + overtone)
		var white_noise: float = randf() * 2.0 - 1.0
		var wave: float = tonal * (1.0 - noise) + white_noise * noise
		var envelope: float = (1.0 - t) * (1.0 - t) * minf(1.0, t * 55.0)
		bytes.encode_s16(i * 2, int(clampf(wave * envelope * vol, -1.0, 1.0) * 32767.0))
	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = RATE
	wav.stereo = false
	wav.data = bytes
	return wav


func play(sound_name: String, pitch: float = 1.0) -> void:
	if not Settings.sound:
		return
	if not _streams.has(sound_name):
		if not EXTRA.has(sound_name):
			return
		_streams[sound_name] = _optional_tone(sound_name)
		_pending.erase(sound_name)
	var stream: AudioStream = _streams[sound_name]
	var chosen: AudioStreamPlayer = null
	for offset in _players.size():
		var candidate: AudioStreamPlayer = _players[(_next + offset) % _players.size()]
		if not candidate.playing:
			chosen = candidate
			break
	if chosen == null:
		chosen = _players[_next]
	_next = (_next + 1) % _players.size()
	chosen.stream = stream
	chosen.pitch_scale = clampf(pitch, 0.65, 1.75)
	chosen.play()
