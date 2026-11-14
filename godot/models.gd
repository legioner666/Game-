extends RefCounted
## Процедурні low-poly моделі Crown Rush: усе будується з примітивів, без зовнішніх ассетів.
## Рої (вороги, союзники, монети) складаємо в один ArrayMesh з кольорами вершин,
## щоб малювати сотні штук одним викликом (MultiMesh).
## Герой, вежі й будівлі — це набір вузлів, щоб їх частини можна було анімувати.

const SKIN := Color(0.96, 0.72, 0.58)
const GOLD := Color(1.0, 0.78, 0.16)
const WOOD := Color(0.50, 0.34, 0.20)
const WOOD_L := Color(0.70, 0.52, 0.31)
const STONE := Color(0.58, 0.60, 0.66)
const STONE_D := Color(0.36, 0.38, 0.45)
const BLUE := Color(0.20, 0.38, 0.88)
const BLUE_D := Color(0.12, 0.24, 0.62)
const RED := Color(0.85, 0.16, 0.14)
const RED_D := Color(0.62, 0.10, 0.10)

static var _vc_mat: StandardMaterial3D
static var _mats := {}


# ---------- Матеріали ----------

static func vc_material() -> StandardMaterial3D:
	if _vc_mat == null:
		_vc_mat = StandardMaterial3D.new()
		_vc_mat.vertex_color_use_as_albedo = true
		_vc_mat.roughness = 0.9
	return _vc_mat


static func mat(c: Color) -> StandardMaterial3D:
	var key := c.to_html()
	if not _mats.has(key):
		var m := StandardMaterial3D.new()
		m.albedo_color = c
		m.roughness = 0.9
		_mats[key] = m
	return _mats[key]


# ---------- Примітиви ----------

static func box(size: Vector3) -> BoxMesh:
	var m := BoxMesh.new()
	m.size = size
	return m


static func capsule(r: float, h: float, seg: int = 8, rings: int = 3) -> CapsuleMesh:
	var m := CapsuleMesh.new()
	m.radius = r
	m.height = h
	m.radial_segments = seg
	m.rings = rings
	return m


static func sphere(r: float, seg: int = 8) -> SphereMesh:
	var m := SphereMesh.new()
	m.radius = r
	m.height = r * 2.0
	m.radial_segments = seg
	m.rings = maxi(2, seg / 2)
	return m


static func cyl(top: float, bottom: float, h: float, seg: int = 8) -> CylinderMesh:
	var m := CylinderMesh.new()
	m.top_radius = top
	m.bottom_radius = bottom
	m.height = h
	m.radial_segments = seg
	m.rings = 1
	return m


# ---------- Складання мешів ----------

static func part(mesh: Mesh, pos: Vector3, color: Color, rot_deg: Vector3 = Vector3.ZERO, scl: Vector3 = Vector3.ONE) -> Array:
	var b := Basis.from_euler(rot_deg * (PI / 180.0)) * Basis.from_scale(scl)
	return [mesh, Transform3D(b, pos), color]


static func compose(parts: Array) -> ArrayMesh:
	var verts := PackedVector3Array()
	var norms := PackedVector3Array()
	var cols := PackedColorArray()
	var idx := PackedInt32Array()
	for p in parts:
		var mesh: PrimitiveMesh = p[0]
		var xf: Transform3D = p[1]
		var col: Color = p[2]
		var arr: Array = mesh.get_mesh_arrays()
		var av: PackedVector3Array = arr[Mesh.ARRAY_VERTEX]
		var an: PackedVector3Array = arr[Mesh.ARRAY_NORMAL]
		var ai: PackedInt32Array = arr[Mesh.ARRAY_INDEX]
		var nb := xf.basis.inverse().transposed()
		var base := verts.size()
		for i in av.size():
			verts.append(xf * av[i])
			norms.append((nb * an[i]).normalized())
			cols.append(col)
		for k in ai:
			idx.append(base + k)
	var arrays := []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = verts
	arrays[Mesh.ARRAY_NORMAL] = norms
	arrays[Mesh.ARRAY_COLOR] = cols
	arrays[Mesh.ARRAY_INDEX] = idx
	var am := ArrayMesh.new()
	am.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	am.surface_set_material(0, vc_material())
	return am


static func mi(mesh: Mesh, pos: Vector3, color: Color, rot_deg: Vector3 = Vector3.ZERO, scl: Vector3 = Vector3.ONE) -> MeshInstance3D:
	var m := MeshInstance3D.new()
	m.mesh = mesh
	m.material_override = mat(color)
	m.position = pos
	m.rotation_degrees = rot_deg
	m.scale = scl
	return m


static func no_shadow(n: GeometryInstance3D) -> void:
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF


# ---------- Рої: вороги ----------

static func grunt_mesh() -> ArrayMesh:
	return compose([
		part(capsule(0.27, 0.86, 7, 2), Vector3(0, 0.43, 0), RED),
		part(sphere(0.2, 7), Vector3(0, 0.98, 0), SKIN),
		part(cyl(0.17, 0.22, 0.17, 7), Vector3(0, 1.12, 0), STONE_D),
		part(box(Vector3(0.09, 0.62, 0.09)), Vector3(0.34, 0.62, -0.1), WOOD, Vector3(-30, 0, 0)),
		part(sphere(0.12, 5), Vector3(0.34, 0.9, -0.27), STONE_D),
	])


static func runner_mesh() -> ArrayMesh:
	var orange := Color(0.96, 0.52, 0.12)
	return compose([
		part(capsule(0.22, 0.74, 7, 2), Vector3(0, 0.37, 0), orange),
		part(sphere(0.18, 7), Vector3(0, 0.84, 0), SKIN),
		part(cyl(0.185, 0.185, 0.07, 7), Vector3(0, 0.95, 0), RED_D),
		part(box(Vector3(0.05, 0.4, 0.05)), Vector3(0.28, 0.5, -0.15), Color(0.85, 0.87, 0.92), Vector3(-60, 0, 0)),
	])


static func brute_mesh() -> ArrayMesh:
	return compose([
		part(capsule(0.52, 1.7, 9, 3), Vector3(0, 0.85, 0), Color(0.78, 0.15, 0.12)),
		part(sphere(0.45, 8), Vector3(0, 0.6, -0.18), Color(0.95, 0.62, 0.52), Vector3.ZERO, Vector3(1.0, 0.8, 0.9)),
		part(sphere(0.31, 8), Vector3(0, 1.85, -0.05), SKIN),
		part(cyl(0.26, 0.34, 0.26, 8), Vector3(0, 2.07, -0.05), STONE_D),
		part(capsule(0.17, 0.8, 7, 2), Vector3(-0.66, 1.15, 0), SKIN, Vector3(0, 0, 12)),
		part(capsule(0.17, 0.8, 7, 2), Vector3(0.66, 1.15, -0.1), SKIN, Vector3(-30, 0, -12)),
		part(box(Vector3(0.2, 1.1, 0.2)), Vector3(0.78, 1.15, -0.45), WOOD, Vector3(-35, 0, 0)),
		part(box(Vector3(0.42, 0.42, 0.42)), Vector3(0.78, 1.62, -0.78), STONE_D, Vector3(-35, 0, 0)),
	])


static func ally_mesh() -> ArrayMesh:
	return compose([
		part(capsule(0.25, 0.82, 7, 2), Vector3(0, 0.41, 0), BLUE),
		part(sphere(0.19, 7), Vector3(0, 0.94, 0), SKIN),
		part(cyl(0.17, 0.21, 0.2, 7), Vector3(0, 1.08, 0), BLUE_D),
		part(box(Vector3(0.05, 0.16, 0.2)), Vector3(0, 1.22, 0), RED),
		part(box(Vector3(0.06, 0.62, 0.06)), Vector3(0.32, 0.6, -0.2), Color(0.86, 0.88, 0.94), Vector3(-55, 0, 0)),
		part(box(Vector3(0.05, 0.42, 0.34)), Vector3(-0.3, 0.55, -0.05), GOLD),
	])


# ---------- Рої: дрібні об'єкти ----------

static func coin_mesh() -> ArrayMesh:
	return compose([
		part(cyl(0.24, 0.24, 0.07, 10), Vector3(0, 0.26, 0), GOLD, Vector3(0, 0, 90)),
		part(cyl(0.15, 0.15, 0.085, 10), Vector3(0, 0.26, 0), Color(1.0, 0.9, 0.45), Vector3(0, 0, 90)),
	])


static func stack_disc_mesh() -> ArrayMesh:
	return compose([
		part(cyl(0.3, 0.3, 0.07, 10), Vector3.ZERO, GOLD),
		part(cyl(0.2, 0.2, 0.08, 10), Vector3.ZERO, Color(1.0, 0.9, 0.45)),
	])


static func arrow_mesh() -> ArrayMesh:
	return compose([
		part(box(Vector3(0.05, 0.05, 0.8)), Vector3.ZERO, WOOD_L),
		part(box(Vector3(0.12, 0.12, 0.2)), Vector3(0, 0, -0.45), Color(0.85, 0.87, 0.92)),
		part(box(Vector3(0.16, 0.03, 0.18)), Vector3(0, 0, 0.35), Color(0.95, 0.95, 0.95)),
	])


static func ball_mesh() -> ArrayMesh:
	return compose([part(sphere(0.3, 8), Vector3.ZERO, Color(0.15, 0.15, 0.18))])


static func fx_mesh(color: Color) -> ArrayMesh:
	return compose([part(box(Vector3(0.18, 0.18, 0.18)), Vector3.ZERO, color)])


static func tree_mesh() -> ArrayMesh:
	return compose([
		part(cyl(0.12, 0.17, 0.8, 6), Vector3(0, 0.4, 0), WOOD),
		part(cyl(0.0, 0.85, 1.5, 7), Vector3(0, 1.45, 0), Color(0.20, 0.50, 0.25)),
		part(cyl(0.0, 0.62, 1.2, 7), Vector3(0, 2.2, 0), Color(0.27, 0.60, 0.30)),
	])


static func rock_mesh() -> ArrayMesh:
	return compose([
		part(sphere(0.6, 5), Vector3(0, 0.2, 0), Color(0.58, 0.60, 0.62), Vector3.ZERO, Vector3(1.0, 0.65, 1.15)),
		part(sphere(0.35, 5), Vector3(0.5, 0.1, 0.3), Color(0.48, 0.50, 0.53), Vector3.ZERO, Vector3(1.0, 0.7, 1.0)),
	])


# ---------- Герой ----------

static func hero() -> Node3D:
	var root := Node3D.new()
	var body := Node3D.new()
	body.name = "body"
	root.add_child(body)
	body.add_child(mi(capsule(0.34, 1.0, 10, 4), Vector3(0, 0.78, 0), BLUE))
	body.add_child(mi(box(Vector3(0.7, 0.14, 0.42)), Vector3(0, 0.62, 0), GOLD))          # пояс
	body.add_child(mi(sphere(0.27, 10), Vector3(0, 1.56, 0), SKIN))
	body.add_child(mi(box(Vector3(0.34, 0.14, 0.12)), Vector3(0, 1.4, -0.2), Color(0.45, 0.28, 0.14)))  # борода
	body.add_child(mi(cyl(0.25, 0.22, 0.16, 8), Vector3(0, 1.82, 0), GOLD))                # корона
	for k in 5:
		var a := TAU * k / 5.0
		body.add_child(mi(cyl(0.0, 0.07, 0.17, 5), Vector3(cos(a) * 0.2, 1.98, sin(a) * 0.2), GOLD))
	body.add_child(mi(box(Vector3(0.62, 0.95, 0.07)), Vector3(0, 1.0, 0.33), Color(0.80, 0.12, 0.16), Vector3(8, 0, 0)))  # плащ

	var leg_l := Node3D.new()
	leg_l.position = Vector3(-0.16, 0.45, 0)
	leg_l.add_child(mi(box(Vector3(0.18, 0.46, 0.2)), Vector3(0, -0.2, 0), Color(0.40, 0.26, 0.15)))
	root.add_child(leg_l)
	var leg_r := Node3D.new()
	leg_r.position = Vector3(0.16, 0.45, 0)
	leg_r.add_child(mi(box(Vector3(0.18, 0.46, 0.2)), Vector3(0, -0.2, 0), Color(0.40, 0.26, 0.15)))
	root.add_child(leg_r)

	var arm_l := Node3D.new()
	arm_l.position = Vector3(-0.4, 1.15, 0)
	arm_l.rotation.x = 1.3
	arm_l.add_child(mi(capsule(0.1, 0.62, 6, 2), Vector3(0, -0.25, 0), BLUE_D))
	body.add_child(arm_l)
	var arm_r := Node3D.new()
	arm_r.position = Vector3(0.4, 1.15, 0)
	arm_r.add_child(mi(capsule(0.1, 0.62, 6, 2), Vector3(0, -0.25, 0), BLUE_D))
	body.add_child(arm_r)

	var bow := Node3D.new()
	bow.position = Vector3(-0.4, 1.05, -0.55)
	bow.add_child(mi(box(Vector3(0.07, 0.24, 0.07)), Vector3.ZERO, WOOD))
	bow.add_child(mi(box(Vector3(0.06, 0.5, 0.06)), Vector3(0, 0.34, -0.05), WOOD_L, Vector3(-18, 0, 0)))
	bow.add_child(mi(box(Vector3(0.06, 0.5, 0.06)), Vector3(0, -0.34, -0.05), WOOD_L, Vector3(18, 0, 0)))
	bow.add_child(mi(box(Vector3(0.015, 1.1, 0.015)), Vector3(0, 0, 0.1), Color(0.95, 0.95, 0.9)))
	body.add_child(bow)

	root.set_meta("body", body)
	root.set_meta("leg_l", leg_l)
	root.set_meta("leg_r", leg_r)
	root.set_meta("arm_l", arm_l)
	root.set_meta("arm_r", arm_r)
	root.set_meta("bow", bow)
	return root


# ---------- Вежі й будівлі ----------

static func crossbow_tower(level: int) -> Node3D:
	var root := Node3D.new()
	var wood := WOOD if level == 1 else (Color(0.40, 0.42, 0.50) if level == 2 else Color(0.30, 0.32, 0.40))
	for sx in [-1.0, 1.0]:
		for sz in [-1.0, 1.0]:
			root.add_child(mi(box(Vector3(0.16, 1.5, 0.16)), Vector3(0.48 * sx, 0.75, 0.48 * sz), wood))
	root.add_child(mi(box(Vector3(1.3, 0.14, 1.3)), Vector3(0, 1.5, 0), WOOD_L if level == 1 else STONE))
	root.add_child(mi(box(Vector3(1.0, 0.1, 0.1)), Vector3(0, 0.55, 0.48), wood))
	var head := Node3D.new()
	head.position = Vector3(0, 1.75, 0)
	var accent := Color(0.18, 0.62, 0.88) if level >= 2 else Color(0.55, 0.38, 0.22)
	head.add_child(mi(cyl(0.2, 0.26, 0.28, 8), Vector3(0, -0.05, 0), wood))                 # тумба
	head.add_child(mi(box(Vector3(0.14, 0.14, 1.0)), Vector3(0, 0.2, -0.1), Color(0.45, 0.3, 0.18)))   # ложа
	head.add_child(mi(box(Vector3(0.62, 0.09, 0.12)), Vector3(-0.28, 0.2, -0.52), accent, Vector3(0, 28, 0)))
	head.add_child(mi(box(Vector3(0.62, 0.09, 0.12)), Vector3(0.28, 0.2, -0.52), accent, Vector3(0, -28, 0)))
	head.add_child(mi(box(Vector3(0.9, 0.02, 0.02)), Vector3(0, 0.2, -0.3), Color(0.95, 0.95, 0.9)))
	head.add_child(mi(box(Vector3(0.05, 0.05, 0.6)), Vector3(0, 0.3, -0.45), Color(0.85, 0.87, 0.92)))      # болт
	if level >= 3:
		head.add_child(mi(sphere(0.12, 6), Vector3(0, 0.42, 0.3), GOLD))
		root.add_child(mi(cyl(0.05, 0.05, 0.7, 5), Vector3(0.55, 1.95, 0.55), WOOD))
		root.add_child(mi(box(Vector3(0.4, 0.26, 0.04)), Vector3(0.75, 2.15, 0.55), BLUE))
	root.add_child(head)
	root.set_meta("head", head)
	root.set_meta("muzzle", Vector3(0, 0.3, -0.85))
	return root


static func cannon_tower(level: int) -> Node3D:
	var root := Node3D.new()
	var stone := STONE if level == 1 else (Color(0.66, 0.68, 0.74) if level == 2 else Color(0.74, 0.76, 0.82))
	root.add_child(mi(cyl(0.7, 0.95, 1.5, 9), Vector3(0, 0.75, 0), stone))
	root.add_child(mi(cyl(0.95, 0.95, 0.14, 9), Vector3(0, 1.55, 0), STONE_D))
	for k in 6:
		var a := TAU * k / 6.0
		root.add_child(mi(box(Vector3(0.28, 0.2, 0.28)), Vector3(cos(a) * 0.85, 1.72, sin(a) * 0.85), stone))
	var head := Node3D.new()
	head.position = Vector3(0, 1.75, 0)
	var barrel_c := Color(0.2, 0.2, 0.24)
	var r := 0.2 + 0.03 * level
	head.add_child(mi(cyl(r, r + 0.07, 1.2, 9), Vector3(0, 0.12, -0.5), barrel_c, Vector3(-80, 0, 0)))
	head.add_child(mi(cyl(r + 0.05, r + 0.05, 0.14, 9), Vector3(0, 0.12, -0.15), GOLD if level >= 2 else STONE_D, Vector3(-80, 0, 0)))
	if level >= 3:
		head.add_child(mi(cyl(r + 0.05, r + 0.05, 0.14, 9), Vector3(0, 0.15, -0.75), GOLD, Vector3(-80, 0, 0)))
	head.add_child(mi(sphere(0.3, 8), Vector3(0, 0.05, 0.1), Color(0.28, 0.28, 0.32)))
	root.add_child(head)
	root.set_meta("head", head)
	root.set_meta("muzzle", Vector3(0, 0.3, -1.1))
	return root


static func barracks(level: int) -> Node3D:
	var root := Node3D.new()
	var wall := Color(0.88, 0.80, 0.62) if level == 1 else Color(0.80, 0.84, 0.92)
	root.add_child(mi(box(Vector3(2.2, 1.2, 1.7)), Vector3(0, 0.6, 0), wall))
	root.add_child(mi(box(Vector3(2.5, 0.1, 1.15)), Vector3(0, 1.62, 0.55), RED_D, Vector3(32, 0, 0)))
	root.add_child(mi(box(Vector3(2.5, 0.1, 1.15)), Vector3(0, 1.62, -0.55), RED_D, Vector3(-32, 0, 0)))
	root.add_child(mi(box(Vector3(0.5, 0.78, 0.08)), Vector3(0, 0.39, 0.86), Color(0.3, 0.2, 0.12)))
	root.add_child(mi(box(Vector3(0.4, 0.4, 0.08)), Vector3(0.7, 0.75, 0.86), Color(0.45, 0.68, 0.9)))
	root.add_child(mi(box(Vector3(0.4, 0.4, 0.08)), Vector3(-0.7, 0.75, 0.86), Color(0.45, 0.68, 0.9)))
	root.add_child(mi(cyl(0.04, 0.04, 1.1, 5), Vector3(0.9, 2.2, 0), WOOD))
	root.add_child(mi(box(Vector3(0.6, 0.38, 0.04)), Vector3(1.22, 2.45, 0), BLUE if level < 3 else GOLD))
	if level >= 2:
		root.add_child(mi(box(Vector3(0.5, 0.9, 0.5)), Vector3(-1.2, 0.45, -0.4), STONE))
		root.add_child(mi(cyl(0.0, 0.42, 0.5, 4), Vector3(-1.2, 1.15, -0.4), BLUE, Vector3(0, 45, 0)))
	root.set_meta("door", Vector3(0, 0, 1.4))
	return root


static func castle() -> Node3D:
	var root := Node3D.new()
	root.add_child(mi(box(Vector3(3.6, 1.7, 3.0)), Vector3(0, 0.85, 0), STONE))
	root.add_child(mi(box(Vector3(0.95, 1.15, 0.1)), Vector3(0, 0.58, 1.52), Color(0.25, 0.17, 0.12)))
	for sx in [-1.0, 1.0]:
		for sz in [-1.0, 1.0]:
			root.add_child(mi(cyl(0.5, 0.58, 2.6, 8), Vector3(1.8 * sx, 1.3, 1.5 * sz), STONE))
			root.add_child(mi(cyl(0.0, 0.78, 0.95, 8), Vector3(1.8 * sx, 3.05, 1.5 * sz), BLUE))
	root.add_child(mi(box(Vector3(1.8, 1.4, 1.8)), Vector3(0, 2.4, 0), Color(0.64, 0.66, 0.72)))
	root.add_child(mi(cyl(0.0, 1.45, 1.2, 4), Vector3(0, 3.7, 0), BLUE_D, Vector3(0, 45, 0)))
	root.add_child(mi(cyl(0.04, 0.04, 1.3, 5), Vector3(0, 4.9, 0), WOOD))
	root.add_child(mi(box(Vector3(0.8, 0.5, 0.05)), Vector3(0.42, 5.2, 0), BLUE))
	root.add_child(mi(box(Vector3(0.12, 0.3, 0.12)), Vector3(0.0, 5.2, 0.05), GOLD))
	return root
