@tool
extends EditorPlugin

var inspector_plugin: EditorInspectorPlugin


func _enter_tree() -> void:
    inspector_plugin = DicedSpriteAtlasInspectorPlugin.new()
    add_inspector_plugin(inspector_plugin)
    add_custom_type("DicedSprite2D", "Node2D", preload("res://addons/sprite_dicing/runtime/diced_sprite_2d_type.gd"), null)


func _exit_tree() -> void:
    if inspector_plugin:
        remove_inspector_plugin(inspector_plugin)
    remove_custom_type("DicedSprite2D")


class DicedSpriteAtlasInspectorPlugin extends EditorInspectorPlugin:
    func _can_handle(object: Object) -> bool:
        return object is DicedSpriteAtlas
    
    func _parse_begin(object: Object) -> void:
        var atlas: DicedSpriteAtlas = object
        
        var container := VBoxContainer.new()
        
        var build_button := Button.new()
        build_button.text = "Build Atlas" if atlas.sprites.is_empty() else "Rebuild Atlas"
        build_button.tooltip_text = "Build the diced sprite atlas from source textures"
        
        build_button.pressed.connect(_on_build_pressed.bind(atlas, build_button))
        
        container.add_child(build_button)
        add_custom_control(container)
    
    func _on_build_pressed(atlas: DicedSpriteAtlas, button: Button) -> void:
        if atlas.input_folder.is_empty():
            push_error("Input folder is not set")
            return
        
        button.disabled = true
        button.text = "Building..."
        
        var builder := AtlasBuilder.new(atlas)
        builder.build()
        
        button.disabled = false
        button.text = "Rebuild Atlas"
