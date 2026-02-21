class_name AtlasBuilder
extends RefCounted

const _Native = preload("res://addons/sprite_dicing/editor/native.gd")

var _atlas: DicedSpriteAtlas


func _init(atlas: DicedSpriteAtlas) -> void:
    _atlas = atlas


func build() -> void:
    if not _atlas:
        push_error("Atlas is null")
        return
    
    var input_folder: String = _atlas.input_folder
    if input_folder.is_empty():
        push_error("Input folder is not set")
        return
    
    print("Building diced sprite atlas...")
    
    var source_files := _find_source_files(input_folder, _atlas.include_subfolders)
    if source_files.is_empty():
        push_error("No source images found in ", input_folder)
        return
    
    print("Found ", source_files.size(), " source files")
    
    var sources: Array = []
    for path in source_files:
        var image: Image = _load_image(path)
        if not image:
            push_warning("Failed to load image: ", path)
            continue
        
        var sprite_id := _build_sprite_id(path, input_folder)
        sources.append({
            "id": sprite_id,
            "width": image.get_width(),
            "height": image.get_height(),
            "pixels": image.get_data(),
            "has_pivot": _atlas.keep_original_pivot,
            "pivot": Vector2(0.5, 0.5) if _atlas.keep_original_pivot else Vector2.ZERO
        })
    
    if sources.is_empty():
        push_error("No valid source images loaded")
        return
    
    var prefs := {
        "unit_size": _atlas.dice_unit_size,
        "padding": _atlas.padding,
        "uv_inset": _atlas.uv_inset,
        "trim_transparent": _atlas.trim_transparent,
        "atlas_size_limit": _atlas.atlas_size_limit,
        "atlas_square": _atlas.force_square,
        "atlas_pot": _atlas.force_pot,
        "ppu": _atlas.pixels_per_unit,
        "pivot": _atlas.default_pivot
    }
    
    var native := _Native.new()
    var result: Dictionary = native.dice(sources, prefs)
    
    if result.has("error") and not result.error.is_empty():
        push_error("Dicing failed: ", result.error)
        return
    
    var atlases: Array = result.get("atlases", [])
    var sprites: Array = result.get("sprites", [])
    
    print("Dicing complete. Generated ", atlases.size(), " atlas textures and ", sprites.size(), " sprites")
    
    _save_atlas_textures(atlases)
    _create_diced_sprites(sprites)
    
    print("Atlas build complete!")


func _find_source_files(folder: String, recursive: bool) -> PackedStringArray:
    var files := PackedStringArray()
    var dir := DirAccess.open(folder)
    if not dir:
        return files
    
    dir.list_dir_begin()
    var file := dir.get_next()
    
    while not file.is_empty():
        if file == "." or file == "..":
            file = dir.get_next()
            continue
        
        var full_path := folder.path_join(file)
        
        if dir.current_is_dir():
            if recursive:
                var sub_files := _find_source_files(full_path, true)
                for sub_file in sub_files:
                    files.push_back(sub_file)
        else:
            var ext := file.get_extension().to_lower()
            if ext in ["png", "jpg", "jpeg", "bmp", "webp"]:
                files.push_back(full_path)
        
        file = dir.get_next()
    
    dir.list_dir_end()
    return files


func _load_image(path: String) -> Image:
    var image := Image.load_from_file(path)
    if not image:
        return null
    
    if image.get_format() != Image.FORMAT_RGBA8:
        image.convert(Image.FORMAT_RGBA8)
    
    return image


func _build_sprite_id(path: String, root: String) -> String:
    var local := path.trim_prefix(root + "/")
    var name := local.get_basename()
    name = name.replace("/", _atlas.separator)
    return name


func _save_atlas_textures(atlas_data: Array) -> void:
    var textures: Array[Texture2D] = []
    
    var atlas_path := _atlas.resource_path
    var base_path := atlas_path.get_base_dir()
    var base_name := atlas_path.get_file().get_basename()
    
    for i in range(atlas_data.size()):
        var data: Dictionary = atlas_data[i]
        var width: int = data.width
        var height: int = data.height
        var pixels: PackedByteArray = data.pixels
        
        var image := Image.create_from_data(width, height, false, Image.FORMAT_RGBA8, pixels)
        
        var texture_path := base_path.path_join(base_name + "_{0:03d}.png".format([i + 1]))
        image.save_png(texture_path)
        
        var texture := ImageTexture.create_from_image(image)
        textures.append(texture)
    
    _atlas.atlas_textures = textures


func _create_diced_sprites(sprite_data: Array) -> void:
    var sprites: Array[DicedSprite] = []
    
    for data in sprite_data:
        var sprite := DicedSprite.new()
        sprite.sprite_id = data.id
        sprite.atlas_index = data.atlas_index
        sprite.vertices = data.vertices
        sprite.uvs = data.uvs
        sprite.indices = data.indices
        sprite.sprite_rect = data.rect
        sprite.sprite_pivot = data.pivot
        sprites.append(sprite)
    
    _atlas.diced_sprites = sprites
