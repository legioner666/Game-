extends RefCounted
## Fortress mastery — permanent, local, offline improvements.
## Uses earned stars as a non-consumed budget so level unlocking never regresses.

const Settings = preload("res://settings.gd")

const KINDS := ["crossbow", "cannon", "barracks", "hero", "castle"]
const MAX_RANK := 3
const RANK_COST := [1, 1, 2]
## Для бота/калібрування: >= 0 замінює ранг усіх покращень.
static var forced_rank := -1
## Для калібрування: ранги по видах у порядку KINDS, напр. "33322".
static var forced_ranks := ""


static func rank(kind: String) -> int:
	if not KINDS.has(kind):
		return 0
	if forced_ranks.length() == KINDS.size():
		return clampi(int(forced_ranks[KINDS.find(kind)]), 0, MAX_RANK)
	if forced_rank >= 0:
		return mini(forced_rank, MAX_RANK)
	return clampi(int(Settings.upgrades.get(kind, 0)), 0, MAX_RANK)


static func next_price(kind: String) -> int:
	if rank(kind) >= MAX_RANK:
		return 0
	return RANK_COST[rank(kind)]


static func earned_points() -> int:
	var n := 0
	for stars in Settings.stars:
		n += clampi(int(stars), 0, 3)
	return n


static func _cost_of(r: int) -> int:
	var n := 0
	for i in r:
		n += RANK_COST[i]
	return n


static func spent_points() -> int:
	var n := 0
	for kind in KINDS:
		n += _cost_of(rank(kind))
	return n


static func total_cost() -> int:
	return _cost_of(MAX_RANK) * KINDS.size()


## Частка майстерності 0..100: скільки з усіх покращень уже куплено.
static func mastery_percent() -> int:
	return int(round(100.0 * float(spent_points()) / float(total_cost())))


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


## Відсоток бонусу за один ранг (для тексту в майстерні).
const STEP_PERCENT := {"crossbow": 20, "cannon": 18, "barracks": 20, "hero": 25, "castle": 0}
const CASTLE_HP_STEP := 4
## Рекомендована майстерність (%) для [складність][рівень] — за прогонами бота.
const RECOMMENDED := [[0, 0, 0], [15, 30, 40], [50, 65, 80]]


static func multiplier(kind: String) -> float:
	return 1.0 + 0.01 * float(STEP_PERCENT.get(kind, 0)) * float(rank(kind))


## Додаткова міцність замку.
static func castle_bonus() -> int:
	return CASTLE_HP_STEP * rank("castle")
