extends Node3D
## Original, lightweight 3D meteor telegraph, falling fireball and impact shockwave.
## No external meshes/textures; driven by the game's paused-aware simulation tick.

signal impacted(world_position: Vector3)

const RADIUS := 3.8
const FALL_TIME := 0.60
const BLAST_TIME := 0.38
const FALL_HEIGHT := 12.0

var _outline_mesh: TorusMesh
var _rock_mesh: SphereMesh
var _glow_mesh: SphereMesh
var _target: Node3D
var _target_ring: MeshInstance3D
var _target_fill: MeshInstance3D
var _active: Array[Dictionary] = []

var _warning_material: StandardMaterial3D
var _fill_material: StandardMaterial3D
var _fire_material: StandardMaterial3D
var _core_material: StandardMaterial3D
var _trail_material: StandardMaterial3D


func _ready() -> void:
	_outline_mesh = TorusMesh.new()
	_outline_mesh.inner_radius = RADIUS - 0.13
	_outline_mesh.outer_radius = RADIUS
	_outline_mesh.ring_segments = 8
	_outline_mesh.rings = 48
	_rock_mesh = SphereMesh.new()
	_rock_mesh.radius = 0.82
	_rock_mesh.height = 1.64
	_rock_mesh.radial_segments = 12
	_rock_mesh.rings = 6
	_glow_mesh = SphereMesh.new()
	_glow_mesh.radius = 1.0
	_glow_mesh.height = 2.0
	_glow_mesh.radial_segments = 12
	_glow_mesh.rings = 6
	_warning_material = _mat(Color(1.0, 0.30, 0.12, 0.96))
	_fill_material = _mat(Color(1.0, 0.30, 0.12, 0.18))
	_fire_material = _mat(Color(1.0, 0.39, 0.07))
	_core_material = _mat(Color(1.0, 0.90, 0.28))
	_trail_material = _mat(Color(1.0, 0.53, 0.08, 0.72))
	_target = Node3D.new()
	add_child(_target)
	_target_ring = _mesh(_outline_mesh, _warning_material)
	_target.add_child(_target_ring)
	_target_fill = _circle(_fill_material)
	_target.add_child(_target_fill)
	_target_ring.position.y = 0.16
	_target_fill.position.y = 0.10
	_target.visible = false


func _mat(color: Color) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = color
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	if color.a < 1.0:
		material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	return material


func _mesh(mesh: Mesh, material: Material) -> MeshInstance3D:
	var node := MeshInstance3D.new()
	node.mesh = mesh
	node.material_override = material
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return node


func _circle(material: Material) -> MeshInstance3D:
	var disk_mesh := CylinderMesh.new()
	disk_mesh.top_radius = RADIUS
	disk_mesh.bottom_radius = RADIUS
	disk_mesh.height = 0.02
	disk_mesh.radial_segments = 48
	return _mesh(disk_mesh, material)


func show_target(world: Vector3) -> void:
	_target.visible = true
	_target.position = Vector3(world.x, 0.0, world.z)


func hide_target() -> void:
	_target.visible = false


func launch(world: Vector3) -> void:
	hide_target()
	var root := Node3D.new()
	root.position = Vector3(world.x, 0.0, world.z)
	add_child(root)

	var warning := _mesh(_outline_mesh, _warning_material)
	warning.position.y = 0.12
	root.add_child(warning)
	var meteor := _mesh(_rock_mesh, _fire_material)
	meteor.position.y = FALL_HEIGHT
	root.add_child(meteor)
	# Visible flame tail, made from three inexpensive, shared procedural spheres.
	for i in 3:
		var tail := _mesh(_glow_mesh, _core_material if i == 0 else _trail_material)
		var size: float = 0.56 - float(i) * 0.13
		tail.scale = Vector3(size, size * 1.65, size)
		tail.position = Vector3(0, 0.95 + float(i) * 0.82, 0)
		meteor.add_child(tail)
	_active.append({
		"root": root, "meteor": meteor, "warning": warning,
		"blast": null, "flash": null, "age": 0.0, "hit": false,
	})


func tick(delta: float) -> void:
	var i := _active.size() - 1
	while i >= 0:
		var effect: Dictionary = _active[i]
		var age: float = float(effect["age"]) + delta
		effect["age"] = age
		var root: Node3D = effect["root"]
		var meteor: MeshInstance3D = effect["meteor"]
		var warning: MeshInstance3D = effect["warning"]
		if age < FALL_TIME:
			var progress: float = clampf(age / FALL_TIME, 0.0, 1.0)
			meteor.position.y = FALL_HEIGHT * (1.0 - progress * progress)
			meteor.rotate_y(delta * 6.0)
			warning.scale = Vector3.ONE * (0.94 + 0.06 * sin(progress * TAU * 4.0))
		elif not bool(effect["hit"]):
			effect["hit"] = true
			meteor.visible = false
			warning.visible = false
			var shock := _mesh(_outline_mesh, _core_material)
			shock.position.y = 0.20
			shock.scale = Vector3(0.20, 1.0, 0.20)
			root.add_child(shock)
			var flash := _mesh(_glow_mesh, _trail_material)
			flash.position.y = 0.65
			flash.scale = Vector3(0.4, 0.2, 0.4)
			root.add_child(flash)
			effect["blast"] = shock
			effect["flash"] = flash
			impacted.emit(root.position)
		elif age <= FALL_TIME + BLAST_TIME:
			var t: float = clampf((age - FALL_TIME) / BLAST_TIME, 0.0, 1.0)
			var shock: MeshInstance3D = effect["blast"]
			var flash: MeshInstance3D = effect["flash"]
			shock.scale = Vector3(0.2 + 0.8 * t, 1.0, 0.2 + 0.8 * t)
			flash.scale = Vector3(0.4 + 2.5 * t, 0.22 * (1.0 - t), 0.4 + 2.5 * t)
		else:
			root.queue_free()
			_active.remove_at(i)
		i -= 1
