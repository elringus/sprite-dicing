@tool
@icon("res://addons/sprite_dicing/icon.svg")

## Stores diced sprite data.
class_name DicedSprite extends Resource

@export var sprite_id: String
@export var atlas_index: int = 0
@export var vertices: PackedVector2Array
@export var uvs: PackedVector2Array
@export var indices: PackedInt32Array
@export var sprite_rect: Rect2
@export var sprite_pivot: Vector2

var _triangles: Array
var _triangle_uvs: Array
var _prepared: bool = false


func get_triangles() -> Array:
    if not _prepared: _prepare_draw_data()
    return _triangles


func get_triangle_uvs() -> Array:
    if not _prepared: _prepare_draw_data()
    return _triangle_uvs


func _prepare_draw_data() -> void:
    _prepared = true
    _triangles.clear()
    _triangle_uvs.clear()
    
    for i in range(indices.size() / 3):
        var idx := i * 3
        var tri := PackedVector2Array()
        var uv := PackedVector2Array()
        tri.resize(3)
        uv.resize(3)
        tri[0] = vertices[indices[idx]]
        tri[1] = vertices[indices[idx + 1]]
        tri[2] = vertices[indices[idx + 2]]
        uv[0] = uvs[indices[idx]]
        uv[1] = uvs[indices[idx + 1]]
        uv[2] = uvs[indices[idx + 2]]
        _triangles.append(tri)
        _triangle_uvs.append(uv)
