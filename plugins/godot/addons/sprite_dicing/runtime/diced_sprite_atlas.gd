class_name DicedSpriteAtlas
extends Resource

@export var atlas_textures: Array[Texture2D] = []
@export var sprites: Array[DicedSprite] = []

@export_group("Input")
@export var input_folder: String
@export var include_subfolders: bool = false
@export var separator: String = "."

@export_group("Atlas")
@export_enum("1024", "2048", "4096", "8192") var atlas_size_limit: int = 2048
@export var force_square: bool = false
@export var force_pot: bool = false

@export_group("Dicing")
@export_enum("8", "16", "32", "64", "128", "256") var dice_unit_size: int = 64
@export_range(0, 128, 2) var padding: int = 2
@export_range(0, 0.5, 0.001) var uv_inset: float = 0.0
@export var trim_transparent: bool = true

@export_group("Sprite")
@export_range(0.001, 1000, 0.001) var pixels_per_unit: float = 100.0
@export var default_pivot: Vector2 = Vector2(0.5, 0.5)
@export var keep_original_pivot: bool = false

@export_group("Output")
@export var decouple_sprite_data: bool = false
@export var compression_ratio: String = "Unknown (build atlas to update)"


func get_sprite(p_name: String) -> DicedSprite:
    for sprite in sprites:
        if sprite and sprite.sprite_id == p_name:
            return sprite
    return null


func get_sprite_names() -> PackedStringArray:
    var names := PackedStringArray()
    for sprite in sprites:
        if sprite:
            names.push_back(sprite.sprite_id)
    return names
