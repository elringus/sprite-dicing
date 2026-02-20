class_name DicedSprite
extends Resource

@export var sprite_id: String
@export var atlas_index: int = 0
@export var vertices: PackedVector2Array
@export var uvs: PackedVector2Array
@export var indices: PackedInt32Array
@export var sprite_rect: Rect2
@export var sprite_pivot: Vector2

var _expanded_triangles: PackedVector2Array
var _expanded_uvs: PackedVector2Array
var _is_expanded: bool = false


func get_expanded_triangles() -> PackedVector2Array:
    if not _is_expanded:
        _expand_mesh()
    return _expanded_triangles


func get_expanded_uvs() -> PackedVector2Array:
    if not _is_expanded:
        _expand_mesh()
    return _expanded_uvs


func _expand_mesh() -> void:
    _is_expanded = true
    
    if vertices.is_empty() or uvs.is_empty() or indices.size() < 3:
        return
    
    var tri_count := indices.size() / 3
    _expanded_triangles = PackedVector2Array()
    _expanded_triangles.resize(tri_count * 3)
    _expanded_uvs = PackedVector2Array()
    _expanded_uvs.resize(tri_count * 3)
    
    for i in range(tri_count):
        for j in range(3):
            var idx := indices[i * 3 + j]
            var dst_idx := i * 3 + j
            _expanded_triangles[dst_idx] = vertices[idx]
            _expanded_uvs[dst_idx] = uvs[idx]
