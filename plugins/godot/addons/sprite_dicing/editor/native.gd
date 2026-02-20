extends RefCounted

var _native: Object = null


func _init() -> void:
    if ClassDB.class_exists("SpriteDicing"):
        _native = ClassDB.instantiate("SpriteDicing")


func is_available() -> bool:
    return _native != null and _native.call("is_available")


func dice(sources: Array, prefs: Dictionary) -> Dictionary:
    if not _native:
        return {"error": "Native dicer not available. GDExtension not loaded."}
    
    return _native.call("dice", sources, prefs)
