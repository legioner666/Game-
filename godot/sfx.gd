extends Node
## Прості звуки, згенеровані кодом (без аудіофайлів). Короткі тони зі зсувом частоти.

const RATE := 22050

var _players: Array[AudioStreamPlayer] = []
var _streams := {}
var _next := 0


func _ready() -> void:
	for i in 12:
		var p := AudioStreamPlayer.new()
		add_child(p)
		_players.append(p)
	_streams["coin"] = _tone(1000.0, 1700.0, 0.09, 0.35)
	_streams["tick"] = _tone(700.0, 800.0, 0.04, 0.22)
	_streams["build"] = _tone(260.0, 900.0, 0.30, 0.45, 0.1)
	_streams["shoot"] = _tone(900.0, 300.0, 0.10, 0.28, 0.25)
	_streams["hero_shoot"] = _tone(700.0, 420.0, 0.07, 0.18, 0.15)
	_streams["boom"] = _tone(170.0, 40.0, 0.38, 0.6, 0.55)
	_streams["kill"] = _tone(320.0, 110.0, 0.06, 0.16, 0.6)
	_streams["hit"] = _tone(140.0, 60.0, 0.30, 0.6, 0.6)
	_streams["spawn"] = _tone(300.0, 450.0, 0.12, 0.3)
	_streams["win"] = _tone(380.0, 1300.0, 0.7, 0.45)
	_streams["lose"] = _tone(420.0, 70.0, 0.8, 0.5, 0.1)
	_streams["click"] = _tone(600.0, 500.0, 0.05, 0.3)


func _tone(f0: float, f1: float, dur: float, vol: float, noise: float = 0.0) -> AudioStreamWAV:
	var n := int(RATE * dur)
	var data := PackedByteArray()
	data.resize(n * 2)
	var phase := 0.0
	for i in n:
		var t := float(i) / n
		phase += TAU * lerpf(f0, f1, t) / RATE
		var s := sin(phase) * (1.0 - noise) + (randf() * 2.0 - 1.0) * noise
		var env := pow(1.0 - t, 2.0) * minf(1.0, t * 40.0)
		data.encode_s16(i * 2, int(clampf(s * env * vol, -1.0, 1.0) * 32767.0))
	var w := AudioStreamWAV.new()
	w.format = AudioStreamWAV.FORMAT_16_BITS
	w.mix_rate = RATE
	w.stereo = false
	w.data = data
	return w


func play(sound_name: String, pitch: float = 1.0) -> void:
	if not preload("res://settings.gd").sound:
		return
	var s: AudioStream = _streams.get(sound_name)
	if s == null:
		return
	var p: AudioStreamPlayer = null
	for k in _players.size():
		var cand := _players[(_next + k) % _players.size()]
		if not cand.playing:
			p = cand
			break
	if p == null:
		p = _players[_next]
	_next = (_next + 1) % _players.size()
	p.stream = s
	p.pitch_scale = pitch
	p.play()
