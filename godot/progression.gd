extends RefCounted
## Fortress mastery — permanent, local, offline tower improvements.
## Uses earned stars as a non-consumed budget so level unlocking never regresses.

const Settings = preload("res://settings.gd")

const KINDS := ["crossbow", "cannon", "barracks"]
const MAX_RANK := 3


static func rank(kind: String) -> int:
	if not KINDS.has(kind):
		return 0
	return clampi(int(Settings.upgrades.get(kind, 0)), 0, MAX_RANK)


static func next_price(kind: String) -> int:
	if rank(kind) >= MAX_RANK:
		return 0
	return rank(kind) + 1


static func earned_points() -> int:
	var n := 0
	for stars in Settings.stars:
		n += clampi(int(stars), 0, 3)
	return n


static func spent_points() -> int:
	var n := 0
	for kind in KINDS:
		var r := rank(kind)
		n += r * (r + 1) / 2
	return n


static func available_points() -> int:
	return maxi(0, earned_points() - spent_points())


static func can_buy(kind: String) -> bool:
	if not KINDS.has(kind) or rank(kind) >= MAX_RANK:
		return false
	return available_points() >= next_price(kind)


static func buy(kind: String) -> bool:
	if not can_buy(kind):
		return false
	Settings.upgrades[kind] = rank(kind) + 1
	Settings.save_settings()
	return true


static func reset() -> void:
	for kind in KINDS:
		Settings.upgrades[kind] = 0
	Settings.save_settings()


static func multiplier(kind: String) -> float:
	var r := rank(kind)
	match kind:
		"crossbow":
			return 1.0 + 0.06 * float(r) # Up to +18% damage
		"cannon":
			return 1.0 + 0.05 * float(r) # Up to +15% blast radius
		"barracks":
			return 1.0 + 0.08 * float(r) # Up to +24% ally max HP
	return 1.0
