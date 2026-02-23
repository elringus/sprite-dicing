#include "sprite_dicing.h"

#include <godot_cpp/classes/project_settings.hpp>
#include <godot_cpp/variant/packed_vector2_array.hpp>
#include <godot_cpp/variant/packed_int32_array.hpp>
#include <godot_cpp/variant/packed_byte_array.hpp>
#include <gdextension_interface.h>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/godot.hpp>

#include <vector>
#include <cstring>

#ifdef _WIN32
    #define WIN32_LEAN_AND_MEAN
    #include <windows.h>
    #define LOAD_LIB(path) LoadLibraryA(path)
    #define GET_PROC(handle, name) GetProcAddress((HMODULE)handle, name)
    #define FREE_LIB(handle) FreeLibrary((HMODULE)handle)
#elif defined(__APPLE__)
    #include <dlfcn.h>
    #define LOAD_LIB(path) dlopen(path, RTLD_NOW)
    #define GET_PROC(handle, name) dlsym(handle, name)
    #define FREE_LIB(handle) dlclose(handle)
#else
    #include <dlfcn.h>
    #define LOAD_LIB(path) dlopen(path, RTLD_NOW)
    #define GET_PROC(handle, name) dlsym(handle, name)
    #define FREE_LIB(handle) dlclose(handle)
#endif

namespace godot {

void* SpriteDicing::lib_handle = nullptr;
bool SpriteDicing::lib_loaded = false;

struct CSlice { const void* ptr; uint64_t len; };
struct CPivot { float x, y; };
struct CRect { float x, y, width, height; };
struct CVertex { float x, y; };
struct CUv { float u, v; };
struct CTexture { uint32_t width; uint32_t height; CSlice pixels; };
struct CSourceSprite { const char* id; CTexture texture; bool has_pivot; CPivot pivot; };
struct CPrefs { uint32_t unit_size; uint32_t padding; float uv_inset; bool trim_transparent; uint32_t atlas_size_limit; bool atlas_square; bool atlas_pot; float ppu; CPivot pivot; bool has_progress_callback; void* progress_callback; };
struct CDicedSprite { const char* id; uint64_t atlas; CSlice vertices; CSlice uvs; CSlice indices; CRect rect; CPivot pivot; };
struct CArtifacts { CSlice atlases; CSlice sprites; };
struct CResult { const char* error; CArtifacts ok; };

typedef CResult (*DiceFunc)(CSlice sprites, CPrefs prefs);

void SpriteDicing::_bind_methods() {
    ClassDB::bind_method(D_METHOD("is_available"), &SpriteDicing::is_available);
    ClassDB::bind_method(D_METHOD("dice", "sources", "prefs"), &SpriteDicing::dice);
}

SpriteDicing::SpriteDicing() {}
SpriteDicing::~SpriteDicing() {}

bool SpriteDicing::is_available() const { return lib_loaded; }

bool SpriteDicing::load_library() {
    if (lib_loaded) return true;

    String res_path = "res://addons/sprite_dicing/editor/native/bin/sprite_dicing";
    String base_path = ProjectSettings::get_singleton()->globalize_path(res_path);

    String lib_path;
#ifdef _WIN32
    lib_path = base_path + ".dll";
#elif defined(__APPLE__)
    lib_path = base_path + ".dylib";
#else
    lib_path = base_path + ".so";
#endif

    CharString path_utf8 = lib_path.utf8();
    lib_handle = LOAD_LIB(path_utf8.get_data());
    if (!lib_handle) return false;

    DiceFunc dice_func = (DiceFunc)GET_PROC(lib_handle, "dice");
    if (!dice_func) {
        FREE_LIB(lib_handle);
        lib_handle = nullptr;
        return false;
    }

    lib_loaded = true;
    return true;
}

Dictionary SpriteDicing::dice(const Array& sources, const Dictionary& prefs) {
    Dictionary result;

    if (!lib_loaded && !load_library()) {
        result["error"] = "Native library not available";
        return result;
    }

    DiceFunc dice_func = (DiceFunc)GET_PROC(lib_handle, "dice");
    if (!dice_func) {
        result["error"] = "Failed to get dice function";
        return result;
    }

    std::vector<CSourceSprite> c_sprites;
    std::vector<std::vector<uint8_t>> pixel_buffers;
    std::vector<CharString> id_buffers;

    c_sprites.reserve(sources.size());
    pixel_buffers.reserve(sources.size());
    id_buffers.reserve(sources.size());

    for (int i = 0; i < sources.size(); i++) {
        Dictionary src = sources[i];

        id_buffers.push_back(String(src["id"]).utf8());
        pixel_buffers.push_back(std::vector<uint8_t>());

        PackedByteArray pixels = src["pixels"];
        auto& pixel_buf = pixel_buffers.back();
        pixel_buf.resize(pixels.size());
        memcpy(pixel_buf.data(), pixels.ptr(), pixels.size());

        CSourceSprite c_sprite;
        c_sprite.id = id_buffers.back().get_data();
        c_sprite.texture.width = (uint32_t)(int)src["width"];
        c_sprite.texture.height = (uint32_t)(int)src["height"];
        c_sprite.texture.pixels.ptr = pixel_buf.data();
        c_sprite.texture.pixels.len = pixel_buf.size() / 4;
        c_sprite.has_pivot = src["has_pivot"];
        Vector2 pivot = src["pivot"];
        c_sprite.pivot.x = pivot.x;
        c_sprite.pivot.y = pivot.y;

        c_sprites.push_back(c_sprite);
    }

    CPrefs c_prefs;
    c_prefs.unit_size = (uint32_t)(int)prefs["unit_size"];
    c_prefs.padding = (uint32_t)(int)prefs["padding"];
    c_prefs.uv_inset = prefs["uv_inset"];
    c_prefs.trim_transparent = prefs["trim_transparent"];
    c_prefs.atlas_size_limit = (uint32_t)(int)prefs["atlas_size_limit"];
    c_prefs.atlas_square = prefs["atlas_square"];
    c_prefs.atlas_pot = prefs["atlas_pot"];
    c_prefs.ppu = prefs["ppu"];
    Vector2 pivot = prefs["pivot"];
    c_prefs.pivot.x = pivot.x;
    c_prefs.pivot.y = pivot.y;
    c_prefs.has_progress_callback = false;
    c_prefs.progress_callback = nullptr;

    CSlice sprites_slice;
    sprites_slice.ptr = c_sprites.data();
    sprites_slice.len = c_sprites.size();

    CResult c_result = dice_func(sprites_slice, c_prefs);

    if (c_result.error && c_result.error[0] != '\0') {
        result["error"] = String(c_result.error);
        return result;
    }

    Array atlases;
    const CTexture* atlas_textures = static_cast<const CTexture*>(c_result.ok.atlases.ptr);
    for (uint64_t i = 0; i < c_result.ok.atlases.len; i++) {
        Dictionary atlas;
        atlas["width"] = (int)atlas_textures[i].width;
        atlas["height"] = (int)atlas_textures[i].height;

        uint64_t pixel_count = atlas_textures[i].pixels.len;
        uint64_t byte_count = pixel_count * 4;

        PackedByteArray pixels;
        pixels.resize(byte_count);
        memcpy(pixels.ptrw(), atlas_textures[i].pixels.ptr, byte_count);
        atlas["pixels"] = pixels;

        atlases.push_back(atlas);
    }
    result["atlases"] = atlases;

    Array sprites;
    const CDicedSprite* diced_sprites = static_cast<const CDicedSprite*>(c_result.ok.sprites.ptr);
    for (uint64_t i = 0; i < c_result.ok.sprites.len; i++) {
        Dictionary sprite;
        sprite["id"] = String(diced_sprites[i].id);
        sprite["atlas_index"] = (int)diced_sprites[i].atlas;

        PackedVector2Array vertices;
        const CVertex* verts = static_cast<const CVertex*>(diced_sprites[i].vertices.ptr);
        vertices.resize(diced_sprites[i].vertices.len);
        for (uint64_t j = 0; j < diced_sprites[i].vertices.len; j++) {
            vertices.set(j, Vector2(verts[j].x, verts[j].y));
        }
        sprite["vertices"] = vertices;

        PackedVector2Array uvs;
        const CUv* uv_data = static_cast<const CUv*>(diced_sprites[i].uvs.ptr);
        uvs.resize(diced_sprites[i].uvs.len);
        for (uint64_t j = 0; j < diced_sprites[i].uvs.len; j++) {
            uvs.set(j, Vector2(uv_data[j].u, uv_data[j].v));
        }
        sprite["uvs"] = uvs;

        PackedInt32Array indices;
        const uint64_t* idx_data = static_cast<const uint64_t*>(diced_sprites[i].indices.ptr);
        indices.resize(diced_sprites[i].indices.len);
        for (uint64_t j = 0; j < diced_sprites[i].indices.len; j++) {
            indices.set(j, (int32_t)idx_data[j]);
        }
        sprite["indices"] = indices;

        sprite["rect"] = Rect2(
            diced_sprites[i].rect.x,
            diced_sprites[i].rect.y,
            diced_sprites[i].rect.width,
            diced_sprites[i].rect.height
        );
        sprite["pivot"] = Vector2(diced_sprites[i].pivot.x, diced_sprites[i].pivot.y);

        sprites.push_back(sprite);
    }
    result["sprites"] = sprites;

    return result;
}

static void initialize_module(ModuleInitializationLevel p_level) {
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) return;
    ClassDB::register_class<SpriteDicing>();
}

static void uninitialize_module(ModuleInitializationLevel p_level) {
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) return;
}

extern "C" {
GDExtensionBool GDE_EXPORT sprite_dicing_library_init(
    GDExtensionInterfaceGetProcAddress p_get_proc_address,
    GDExtensionClassLibraryPtr p_library,
    GDExtensionInitialization *r_initialization
) {
    GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);
    init_obj.register_initializer(initialize_module);
    init_obj.register_terminator(uninitialize_module);
    init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);
    return init_obj.init();
}
}

}
