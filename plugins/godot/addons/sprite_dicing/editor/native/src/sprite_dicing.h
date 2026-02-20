#ifndef SPRITE_DICING_H
#define SPRITE_DICING_H

#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/variant/dictionary.hpp>
#include <godot_cpp/variant/array.hpp>

namespace godot {

class SpriteDicing : public RefCounted {
    GDCLASS(SpriteDicing, RefCounted)

private:
    static void* lib_handle;
    static bool lib_loaded;
    
    static bool load_library();

public:
    static void _bind_methods();
    
    SpriteDicing();
    ~SpriteDicing();
    
    bool is_available() const;
    Dictionary dice(const Array& sources, const Dictionary& prefs);
};

}

#endif
