class_name Report
extends RefCounted
## Простий звіт про ігрові сесії для тестування балансу.
## Нічого не надсилається в мережу: звіт копіюється в буфер обміну кнопкою.
## Файл user://report.log зберігає підсумки останніх забігів.

const PATH := "user://report.log"
const ID_PATH := "user://report_id.txt"
const MAX_BYTES := 40000
const VERSION := "0.3"

static var _lines: Array = []
static var _active := false
static var _t0 := 0
static var _id := ""
static var _run_id := 0


static func anon_id() -> String:
	if _id != "":
		return _id
	if FileAccess.file_exists(ID_PATH):
		var f := FileAccess.open(ID_PATH, FileAccess.READ)
		if f:
			_id = f.get_as_text().strip_edges()
	if _id == "":
		_id = "%08x" % (randi() & 0x7fffffff)
		var w := FileAccess.open(ID_PATH, FileAccess.WRITE)
		if w:
			w.store_string(_id)
	return _id


static func begin_run() -> int:
	if _active:
		end_run("abandoned")
	_run_id += 1
	_active = true
	_lines.clear()
	_t0 = Time.get_ticks_msec()
	var d := Time.get_datetime_string_from_system(false, true)
	ev(0.0, "start", "date=%s device=%s os=%s" % [d, OS.get_model_name(), OS.get_version()])
	return _run_id


static func is_current(id: int) -> bool:
	return _active and id == _run_id


## t — ігровий час у секундах, name — тип події, data — "ключ=значення" через пробіл.
static func ev(t: float, name: String, data: String = "") -> void:
	if not _active:
		return
	_lines.append("%6.1f %s %s" % [t, name, data])


static func end_run(result: String) -> void:
	if not _active:
		return
	var real := (Time.get_ticks_msec() - _t0) / 1000.0
	ev(-1.0, "end", "result=%s real_time=%.0fs" % [result, real])
	_active = false
	var old := ""
	if FileAccess.file_exists(PATH):
		var r := FileAccess.open(PATH, FileAccess.READ)
		if r:
			old = r.get_as_text()
	var all := old + "--- run ---\n" + "\n".join(_lines) + "\n"
	if all.length() > MAX_BYTES:
		all = all.substr(all.length() - MAX_BYTES)
		var cut := all.find("--- run ---")
		if cut > 0:
			all = all.substr(cut)
	var w := FileAccess.open(PATH, FileAccess.WRITE)
	if w:
		w.store_string(all)
	_lines.clear()


static func text() -> String:
	var out := "Crown Rush report v%s id=%s\n" % [VERSION, anon_id()]
	if FileAccess.file_exists(PATH):
		var r := FileAccess.open(PATH, FileAccess.READ)
		if r:
			# лише 3 останні забіги: інакше звіт не влазить у буфер обміну
			var runs := r.get_as_text().split("--- run ---\n", false)
			var from := maxi(0, runs.size() - 3)
			for i in range(from, runs.size()):
				out += "--- run ---\n" + runs[i]
	if _active and _lines.size() > 0:
		out += "--- run (in progress) ---\n" + "\n".join(_lines) + "\n"
	return out


static func copy_to_clipboard() -> void:
	DisplayServer.clipboard_set(text())
