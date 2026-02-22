@tool
class_name DicedSprite2D
extends Node2D

@export var atlas: DicedSpriteAtlas:
    set(value):
        atlas = value
        _update_sprite()
        queue_redraw()

@export var sprite_name: String:
    set(value):
        sprite_name = value
        _update_sprite()
        queue_redraw()

@export var modulate_color: Color = Color.WHITE:
    set(value):
        modulate_color = value
        queue_redraw()

var _triangles: Array
var _uvs: Array
var _colors: PackedColorArray
var _texture: Texture2D


func _init() -> void:
    _colors = PackedColorArray()
    _colors.resize(3)


func _draw() -> void:
    if _texture == null or _triangles.is_empty(): return
    
    _colors.fill(modulate_color)
    
    for i in range(_triangles.size()):
        draw_primitive(_triangles[i], _colors, _uvs[i], _texture)


func _update_sprite() -> void:
    _triangles = []
    _uvs = []
    _texture = null
    
    if atlas == null or sprite_name.is_empty():
        return
    
    var sprite := atlas.get_sprite(sprite_name)
    if sprite == null:
        return
    
    if atlas.atlas_textures.is_empty():
        return
    
    var atlas_idx: int = sprite.atlas_index
    if atlas_idx < 0 or atlas_idx >= atlas.atlas_textures.size():
        return
    
    _triangles = sprite.get_triangles()
    _uvs = sprite.get_triangle_uvs()
    _texture = atlas.atlas_textures[atlas_idx]
    
    queue_redraw()


func _get_configuration_warnings() -> PackedStringArray:
    var warnings := PackedStringArray()
    
    if atlas == null:
        warnings.append("An atlas resource is required to render a diced sprite.")
    elif sprite_name.is_empty():
        warnings.append("Sprite name is not set.")
    elif atlas.get_sprite(sprite_name) == null:
        warnings.append("Sprite \"%s\" not found in atlas." % sprite_name)
    
    return warnings
