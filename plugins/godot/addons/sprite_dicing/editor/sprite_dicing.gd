@tool
extends EditorPlugin

var inspector_plugin: EditorInspectorPlugin


func _enter_tree() -> void:
    inspector_plugin = DicedSpriteAtlasInspectorPlugin.new()
    add_inspector_plugin(inspector_plugin)
    add_custom_type("DicedSprite2D", "Node2D",
        preload("res://addons/sprite_dicing/runtime/diced_sprite_2d.gd"),
        preload("res://addons/sprite_dicing/icon.svg"))


func _exit_tree() -> void:
    if inspector_plugin:
        remove_inspector_plugin(inspector_plugin)
    remove_custom_type("DicedSprite2D")


class DicedSpriteAtlasInspectorPlugin extends EditorInspectorPlugin:
    func _can_handle(object: Object) -> bool:
        return object is DicedSpriteAtlas
    
    func _parse_property(object: Object, _type: int, name: String, _hint_type: int, 
        _hint_string: String, _usage_flags: int, _wide: bool) -> bool:
        if name != "build_button_placeholder": return false
        
        var atlas: DicedSpriteAtlas = object
        var container := MarginContainer.new()
        container.add_theme_constant_override("margin_top", 5)
        container.add_theme_constant_override("margin_bottom", 5)
 
        var button := Button.new()
        button.icon = EditorInterface.get_base_control().get_theme_icon("TransitionSyncAutoBig", "EditorIcons")
        button.text = "Build Atlas" if atlas.diced_sprites.is_empty() else "Rebuild Atlas"
        button.tooltip_text = "Build the diced sprite atlas from the specified input"
        button.pressed.connect(_on_build_pressed.bind(atlas, button))
        
        container.add_child(button)
        add_custom_control(container)
        return true
    
    func _on_build_pressed(atlas: DicedSpriteAtlas, button: Button) -> void:
        if atlas.input_folder.is_empty():
            push_error("Input folder is not set")
            return
        
        button.disabled = true
        button.text = "Building..."
        
        var builder := AtlasBuilder.new(atlas)
        await builder.build()
        
        button.disabled = false
        button.text = "Rebuild Atlas"
