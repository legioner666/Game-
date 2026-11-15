extends Node
## Original, offline procedural soundtrack.
## PCM is synthesized ONCE at load; the audio thread loops the cached sample.
## No external music files, licenses or network access required.

const Settings = preload("res://settings.gd")
const RATE := 22050
const LOOP_SECONDS := 8.0
const TARGET_DB := -17.0
const FADE_SPEED := 2.4

const MENU_LEAD := [64, 67, 71, 69, 67, 64, 62, 67, 64, 67, 72, 71, 69, 67, 64, 62]
const MENU_BASS := [40, 45, 36, 43]
const COMBAT_LEAD := [60, 67, 65, 63, 62, 67, 70, 65, 60, 63, 67, 70, 72, 67, 63, 62]
const COMBAT_BASS := [36, 41, 39, 43]

var _player: AudioStreamPlayer
var _tracks: Dictionary = {}
var _mode := ""


func _ready() -> void:
	_player = AudioStreamPlayer.new()
	_player.volume_db = -55.0
	add_child(_player)
	_tracks["menu"] = _compose(MENU_LEAD, MENU_BASS, false)
	_tracks["combat"] = _compose(COMBAT_LEAD, COMBAT_BASS, true)


func set_mode(mode: String) -> void:
	if mode == _mode:
		return
	if not _tracks.has(mode):
		return
	_mode = mode
	_player.stop()
	_player.stream = _tracks[mode]
	_player.volume_db = -55.0
	if Settings.sound:
		_player.play()


func _process(delta: float) -> void:
	if _mode == "":
		return
	if not Settings.sound:
		if _player.playing:
			_player.stop()
		return
	if not _player.playing:
		_player.stream = _tracks[_mode]
		_player.volume_db = -55.0
		_player.play()
	# Fade in to avoid abrupt sounds after menu transitions or toggling audio.
	_player.volume_db = lerpf(_player.volume_db, TARGET_DB, 1.0 - exp(-FADE_SPEED * delta))


func _hz(note: int) -> float:
	return 440.0 * pow(2.0, (float(note) - 69.0) / 12.0)


func _compose(lead_notes: Array, bass_notes: Array, intense: bool) -> AudioStreamWAV:
	var n := int(RATE * LOOP_SECONDS)
	var pcm := PackedByteArray()
	pcm.resize(n * 2)
	var lead_freq: Array[float] = []
	var bass_freq: Array[float] = []
	for note in lead_notes:
		lead_freq.append(_hz(int(note)))
	for note in bass_notes:
		bass_freq.append(_hz(int(note)))
	var melody_length := lead_freq.size()
	var low_length := bass_freq.size()
	for i in n:
		var t := float(i) / float(RATE)
		var step := mini(int(t * 2.0), melody_length - 1)
		var local_note: float = fposmod(t, 0.5) / 0.5
		var pulse_env := pow(1.0 - local_note, 1.6)
		var lead: float = (
			sin(TAU * lead_freq[step] * t) * 0.80
			+ sin(TAU * lead_freq[step] * 2.0 * t) * 0.20
		) * pulse_env
		var bass_idx := mini(int(t / 2.0), low_length - 1)
		var bass: float = (
			sin(TAU * bass_freq[bass_idx] * t) * 0.75
			+ sin(TAU * bass_freq[bass_idx] * 2.0 * t) * 0.25
		) * 0.48
		var percussion := 0.0
		if intense:
			# A small synthesized pulse replaces imported drum samples.
			var beat_phase: float = fposmod(t, 0.5)
			var kick_env := exp(-beat_phase * 28.0)
			percussion = sin(TAU * (72.0 - beat_phase * 50.0) * t) * kick_env * 0.10
		var music: float = lead * 0.33 + bass * 0.19 + percussion
		# Avoid clicks at the loop boundary.
		var fade: float = minf(1.0, minf(t, LOOP_SECONDS - t) * 35.0)
		pcm.encode_s16(i * 2, int(clampf(music * fade, -1.0, 1.0) * 32767.0))
	var track := AudioStreamWAV.new()
	track.format = AudioStreamWAV.FORMAT_16_BITS
	track.mix_rate = RATE
	track.stereo = false
	track.data = pcm
	track.loop_mode = AudioStreamWAV.LOOP_FORWARD
	track.loop_begin = 0
	track.loop_end = n
	return track
