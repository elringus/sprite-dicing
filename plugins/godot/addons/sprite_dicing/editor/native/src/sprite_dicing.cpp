#include "sprite_dicing.h"
#include "abi/sprite_dicing.h"

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

static decltype(&sd_dice) dice_fn = nullptr;
static decltype(&sd_free) free_fn = nullptr;

struct ResultGuard {
    sd_result result;
    explicit ResultGuard(sd_result r) : result(r) {}
    ~ResultGuard() { free_fn(result); }
    ResultGuard(const ResultGuard&) = delete;
    ResultGuard& operator=(const ResultGuard&) = delete;
};

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

    dice_fn = (decltype(dice_fn))GET_PROC(lib_handle, "sd_dice");
    free_fn = (decltype(free_fn))GET_PROC(lib_handle, "sd_free");
    if (!dice_fn || !free_fn) {
        FREE_LIB(lib_handle);
        lib_handle = nullptr;
        dice_fn = nullptr;
        free_fn = nullptr;
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

    std::vector<sd_source_sprite> c_sprites;
    std::vector<PackedByteArray> pixel_buffers;
    std::vector<CharString> id_buffers;

    c_sprites.reserve(sources.size());
    pixel_buffers.reserve(sources.size());
    id_buffers.reserve(sources.size());

    for (int i = 0; i < sources.size(); i++) {
        Dictionary src = sources[i];
        uint32_t width = (uint32_t)(int)src["width"];
        uint32_t height = (uint32_t)(int)src["height"];

        pixel_buffers.push_back(src["pixels"]);
        const PackedByteArray& pixels = pixel_buffers.back();
        if ((uint64_t)pixels.size() != (uint64_t)width * height * sizeof(sd_pixel)) {
            result["error"] = "Pixel buffer size doesn't match the texture dimensions";
            return result;
        }

        id_buffers.push_back(String(src["id"]).utf8());

        sd_source_sprite c_sprite;
        c_sprite.id = id_buffers.back().get_data();
        c_sprite.texture.width = width;
        c_sprite.texture.height = height;
        c_sprite.texture.pixels = reinterpret_cast<const sd_pixel*>(pixels.ptr());
        c_sprite.has_pivot = src["has_pivot"];
        Vector2 pivot = src["pivot"];
        c_sprite.pivot.x = pivot.x;
        c_sprite.pivot.y = pivot.y;

        c_sprites.push_back(c_sprite);
    }

    sd_prefs c_prefs;
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
    c_prefs.on_progress = nullptr;
    c_prefs.user_data = nullptr;

    ResultGuard guard(dice_fn(c_sprites.data(), c_sprites.size(), &c_prefs));
    const sd_result& c_result = guard.result;

    if (c_result.error) {
        result["error"] = String::utf8(c_result.error);
        return result;
    }

    Array atlases;
    for (size_t i = 0; i < c_result.atlas_count; i++) {
        const sd_texture& c_atlas = c_result.atlases[i];
        Dictionary atlas;
        atlas["width"] = (int)c_atlas.width;
        atlas["height"] = (int)c_atlas.height;

        PackedByteArray pixels;
        pixels.resize((int64_t)c_atlas.width * c_atlas.height * sizeof(sd_pixel));
        if (c_atlas.pixels) memcpy(pixels.ptrw(), c_atlas.pixels, pixels.size());
        atlas["pixels"] = pixels;

        atlases.push_back(atlas);
    }
    result["atlases"] = atlases;

    Array sprites;
    for (size_t i = 0; i < c_result.sprite_count; i++) {
        const sd_diced_sprite& c_sprite = c_result.sprites[i];
        Dictionary sprite;
        sprite["id"] = String::utf8(c_sprite.id);
        sprite["atlas_index"] = (int)c_sprite.atlas_index;

        PackedVector2Array vertices;
        PackedVector2Array uvs;
        vertices.resize(c_sprite.vertex_count);
        uvs.resize(c_sprite.vertex_count);
        for (size_t j = 0; j < c_sprite.vertex_count; j++) {
            vertices.set(j, Vector2(c_sprite.vertices[j].x, c_sprite.vertices[j].y));
            uvs.set(j, Vector2(c_sprite.uvs[j].u, c_sprite.uvs[j].v));
        }
        sprite["vertices"] = vertices;
        sprite["uvs"] = uvs;

        PackedInt32Array indices;
        indices.resize(c_sprite.index_count);
        for (size_t j = 0; j < c_sprite.index_count; j++) {
            indices.set(j, (int32_t)c_sprite.indices[j]);
        }
        sprite["indices"] = indices;

        sprite["rect"] = Rect2(c_sprite.rect.x, c_sprite.rect.y, c_sprite.rect.width, c_sprite.rect.height);
        sprite["pivot"] = Vector2(c_sprite.pivot.x, c_sprite.pivot.y);

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
