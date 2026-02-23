@tool
class_name DicedSprite2D extends Node2D

## The diced sprite atlas resource from which to render the sprite.
@export var atlas: DicedSpriteAtlas:
    set(value):
        atlas = value
        _update_sprite()
        queue_redraw()

## The sprite identifier inside the specified atlas resource to render.
@export var sprite_id: String:
    set(value):
        sprite_id = value
        _update_sprite()
        queue_redraw()

static var _colors := PackedColorArray([Color.WHITE, Color.WHITE, Color.WHITE])
var _triangles: Array = []
var _uvs: Array = []
var _texture: Texture2D


func _draw() -> void:
    if _texture == null: return
    
    for i in range(_triangles.size()):
        draw_primitive(_triangles[i], _colors, _uvs[i], _texture)


func _update_sprite() -> void:
    _triangles = []
    _uvs = []
    _texture = null
    
    if atlas == null: return
    var sprite := atlas.get_sprite(sprite_id)
    if sprite == null: return
    if sprite.atlas_index < 0 or sprite.atlas_index >= atlas.atlas_textures.size(): return
    
    _triangles = sprite.get_triangles()
    _uvs = sprite.get_triangle_uvs()
    _texture = atlas.atlas_textures[sprite.atlas_index]
    
    queue_redraw()


func _get_configuration_warnings() -> PackedStringArray:
    var warnings := PackedStringArray()
    
    if atlas == null:
        warnings.append("An atlas resource is required to render a diced sprite.")
    elif sprite_id.is_empty():
        warnings.append("Sprite identifier is not set.")
    elif atlas.get_sprite(sprite_id) == null:
        warnings.append("Sprite \"%s\" not found in atlas." % sprite_id)
    
    return warnings
