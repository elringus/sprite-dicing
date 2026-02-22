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

var _cached_sprite: DicedSprite = null
var _cached_texture: Texture2D = null


func _ready() -> void:
    _update_sprite()


func _draw() -> void:
    if _cached_sprite == null or _cached_texture == null:
        return
    
    var triangles := _cached_sprite.get_expanded_triangles()
    var uvs := _cached_sprite.get_expanded_uvs()
    
    if triangles.is_empty():
        return
    
    var colors := PackedColorArray()
    colors.resize(triangles.size())
    colors.fill(modulate_color)
    
    draw_primitive(triangles, colors, uvs, _cached_texture)


func _update_sprite() -> void:
    _cached_sprite = null
    _cached_texture = null
    
    if atlas == null or sprite_name.is_empty():
        return
    
    _cached_sprite = atlas.get_sprite(sprite_name)
    if _cached_sprite == null:
        return
    
    if atlas.atlas_textures.is_empty():
        return
    
    var atlas_idx: int = _cached_sprite.atlas_index
    if atlas_idx < 0 or atlas_idx >= atlas.atlas_textures.size():
        return
    
    _cached_texture = atlas.atlas_textures[atlas_idx]


func _get_configuration_warnings() -> PackedStringArray:
    var warnings := PackedStringArray()
    
    if atlas == null:
        warnings.append("An atlas resource is required to render a diced sprite.")
    elif sprite_name.is_empty():
        warnings.append("Sprite name is not set.")
    elif atlas.get_sprite(sprite_name) == null:
        warnings.append("Sprite \"%s\" not found in atlas." % sprite_name)
    
    return warnings


func get_rect() -> Rect2:
    if _cached_sprite != null:
        return _cached_sprite.sprite_rect
    return Rect2()
