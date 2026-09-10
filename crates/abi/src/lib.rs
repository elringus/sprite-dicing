//! C application binary interface of the library.

#![allow(unsafe_op_in_unsafe_fn)]

mod models;

use models::*;
use sprite_dicing::{
    Artifacts, DicedSprite, Pivot, Pixel, Prefs, Progress, ProgressCallback, SourceSprite, Texture,
};
use std::ffi::{CStr, CString, c_char};
use std::{ptr, slice};

/// C ABI wrapper over [sprite_dicing::dice].
///
/// # Safety
///
/// Returned result is expected to be released with [sd_free].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn sd_dice(
    sprites: *const sd_source_sprite,
    count: usize,
    prefs: *const sd_prefs,
) -> sd_result {
    match dice(sprites, count, prefs) {
        Ok(artifacts) => to_c_artifacts(artifacts),
        Err(error) => to_c_error(&error),
    }
}

/// Releases a result returned from [sd_dice].
///
/// # Safety
///
/// The result has to be released only once.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn sd_free(result: sd_result) {
    free_c_str(result.error);
    for atlas in to_slice(result.atlases, result.atlas_count).unwrap_or_default() {
        free_array(atlas.pixels, pixel_count(atlas.width, atlas.height));
    }
    free_array(result.atlases, result.atlas_count);
    for sprite in to_slice(result.sprites, result.sprite_count).unwrap_or_default() {
        free_c_str(sprite.id);
        free_array(sprite.vertices, sprite.vertex_count);
        free_array(sprite.uvs, sprite.vertex_count);
        free_array(sprite.indices, sprite.index_count);
    }
    free_array(result.sprites, result.sprite_count);
}

unsafe fn dice(
    sprites: *const sd_source_sprite,
    count: usize,
    prefs: *const sd_prefs,
) -> Result<Artifacts, String> {
    let prefs = prefs.as_ref().ok_or("Prefs can't be null.")?;
    let sprites = to_slice(sprites, count).ok_or("Sprites can't be null.")?;
    let sprites = sprites
        .iter()
        .enumerate()
        .map(|(idx, c)| to_sprite(c).map_err(|e| format!("Sprite #{idx}: {e}")))
        .collect::<Result<Vec<_>, _>>()?;
    sprite_dicing::dice(&sprites, &to_prefs(prefs)).map_err(|e| e.to_string())
}

fn to_prefs(c: &sd_prefs) -> Prefs {
    Prefs {
        unit_size: c.unit_size,
        padding: c.padding,
        uv_inset: c.uv_inset,
        trim_transparent: c.trim_transparent,
        atlas_size_limit: c.atlas_size_limit,
        atlas_square: c.atlas_square,
        atlas_pot: c.atlas_pot,
        ppu: c.ppu,
        pivot: to_pivot(&c.pivot),
        on_progress: c.on_progress.map(|callback| -> ProgressCallback {
            let user_data = c.user_data;
            Box::new(move |progress: Progress| {
                let activity = to_c_string(&progress.activity);
                let progress = sd_progress {
                    ratio: progress.ratio,
                    activity: activity.as_ptr(),
                };
                unsafe { callback(progress, user_data) }
            })
        }),
    }
}

unsafe fn to_sprite(c: &sd_source_sprite) -> Result<SourceSprite, String> {
    let id = to_str(c.id).ok_or("ID has to be a non-null, valid UTF-8 string.")?;
    let texture = to_texture(&c.texture).ok_or("Texture pixels can't be null.")?;
    Ok(SourceSprite {
        id: id.to_owned(),
        texture,
        pivot: c.has_pivot.then(|| to_pivot(&c.pivot)),
    })
}

unsafe fn to_texture(c: &sd_texture) -> Option<Texture> {
    let pixels = to_slice(c.pixels, pixel_count(c.width, c.height))?;
    Some(Texture {
        width: c.width,
        height: c.height,
        pixels: pixels.iter().map(to_pixel).collect(),
    })
}

fn to_c_artifacts(artifacts: Artifacts) -> sd_result {
    let (atlases, atlas_count) = to_array(artifacts.atlases.iter().map(to_c_texture).collect());
    let (sprites, sprite_count) = to_array(artifacts.sprites.iter().map(to_c_sprite).collect());
    sd_result {
        error: ptr::null(),
        atlases,
        atlas_count,
        sprites,
        sprite_count,
    }
}

fn to_c_error(error: &str) -> sd_result {
    sd_result {
        error: to_c_str(error),
        atlases: ptr::null(),
        atlas_count: 0,
        sprites: ptr::null(),
        sprite_count: 0,
    }
}

fn to_c_texture(texture: &Texture) -> sd_texture {
    let mut pixels: Vec<_> = texture.pixels.iter().map(to_c_pixel).collect();
    pixels.resize(
        pixel_count(texture.width, texture.height),
        sd_pixel::default(),
    );
    sd_texture {
        width: texture.width,
        height: texture.height,
        pixels: to_array(pixels).0,
    }
}

fn to_c_sprite(sprite: &DicedSprite) -> sd_diced_sprite {
    let (vertices, vertex_count) = to_array(sprite.vertices.iter().map(to_c_vertex).collect());
    let mut uvs: Vec<_> = sprite.uvs.iter().map(to_c_uv).collect();
    uvs.resize(vertex_count, sd_uv::default());
    let (indices, index_count) = to_array(sprite.indices.iter().map(|i| *i as u32).collect());
    sd_diced_sprite {
        id: to_c_str(&sprite.id),
        atlas_index: sprite.atlas_index,
        vertices,
        uvs: to_array(uvs).0,
        vertex_count,
        indices,
        index_count,
        rect: to_c_rect(&sprite.rect),
        pivot: to_c_pivot(&sprite.pivot),
    }
}

fn to_pixel(c: &sd_pixel) -> Pixel {
    Pixel::new(c.r, c.g, c.b, c.a)
}

fn to_c_pixel(pixel: &Pixel) -> sd_pixel {
    sd_pixel {
        r: pixel.r(),
        g: pixel.g(),
        b: pixel.b(),
        a: pixel.a(),
    }
}

fn to_pivot(c: &sd_pivot) -> Pivot {
    Pivot::new(c.x, c.y)
}

fn to_c_pivot(pivot: &Pivot) -> sd_pivot {
    sd_pivot {
        x: pivot.x,
        y: pivot.y,
    }
}

fn to_c_vertex(vertex: &sprite_dicing::Vertex) -> sd_vertex {
    sd_vertex {
        x: vertex.x,
        y: vertex.y,
    }
}

fn to_c_uv(uv: &sprite_dicing::Uv) -> sd_uv {
    sd_uv { u: uv.u, v: uv.v }
}

fn to_c_rect(rect: &sprite_dicing::Rect) -> sd_rect {
    sd_rect {
        x: rect.x,
        y: rect.y,
        width: rect.width,
        height: rect.height,
    }
}

fn pixel_count(width: u32, height: u32) -> usize {
    width as usize * height as usize
}

unsafe fn to_slice<'a, T>(ptr: *const T, len: usize) -> Option<&'a [T]> {
    match (ptr.is_null(), len) {
        (_, 0) => Some(&[]),
        (true, _) => None,
        (false, _) => Some(slice::from_raw_parts(ptr, len)),
    }
}

fn to_array<T>(vec: Vec<T>) -> (*const T, usize) {
    match vec.len() {
        0 => (ptr::null(), 0),
        len => (Box::into_raw(vec.into_boxed_slice()) as *const T, len),
    }
}

unsafe fn free_array<T>(ptr: *const T, len: usize) {
    if !ptr.is_null() {
        drop(Box::from_raw(ptr::slice_from_raw_parts_mut(
            ptr as *mut T,
            len,
        )));
    }
}

unsafe fn to_str<'a>(ptr: *const c_char) -> Option<&'a str> {
    match ptr.is_null() {
        true => None,
        false => CStr::from_ptr(ptr).to_str().ok(),
    }
}

fn to_c_string(str: &str) -> CString {
    CString::new(str).unwrap_or_default()
}

fn to_c_str(str: &str) -> *const c_char {
    to_c_string(str).into_raw()
}

unsafe fn free_c_str(ptr: *const c_char) {
    if !ptr.is_null() {
        drop(CString::from_raw(ptr as *mut c_char));
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::ffi::c_void;

    const RED: sd_pixel = sd_pixel {
        r: 255,
        g: 0,
        b: 0,
        a: 255,
    };
    const BLUE: sd_pixel = sd_pixel {
        r: 0,
        g: 0,
        b: 255,
        a: 255,
    };
    const CLEAR: sd_pixel = sd_pixel {
        r: 0,
        g: 0,
        b: 0,
        a: 0,
    };

    fn prefs() -> sd_prefs {
        sd_prefs {
            unit_size: 1,
            padding: 0,
            uv_inset: 0.0,
            trim_transparent: true,
            atlas_size_limit: 8,
            atlas_square: false,
            atlas_pot: false,
            ppu: 1.0,
            pivot: sd_pivot { x: 0.5, y: 0.5 },
            on_progress: None,
            user_data: ptr::null_mut(),
        }
    }

    fn sprite(id: &CStr, pixels: &[sd_pixel], width: u32, height: u32) -> sd_source_sprite {
        sd_source_sprite {
            id: id.as_ptr(),
            texture: sd_texture {
                width,
                height,
                pixels: pixels.as_ptr(),
            },
            has_pivot: false,
            pivot: sd_pivot { x: 0.0, y: 0.0 },
        }
    }

    fn dice(sprites: &[sd_source_sprite], prefs: &sd_prefs) -> sd_result {
        unsafe { sd_dice(sprites.as_ptr(), sprites.len(), prefs) }
    }

    fn error(result: &sd_result) -> Option<String> {
        unsafe { to_str(result.error) }.map(str::to_owned)
    }

    fn free(result: sd_result) {
        unsafe { sd_free(result) }
    }

    #[test]
    fn dices_sprites_into_atlases_and_meshes() {
        let pixels = [RED, BLUE, BLUE, RED];
        let sprites = [sprite(c"sprite", &pixels, 2, 2)];
        let result = dice(&sprites, &prefs());
        assert_eq!(error(&result), None);
        assert_eq!(result.atlas_count, 1);
        assert_eq!(result.sprite_count, 1);

        let atlas = unsafe { &*result.atlases };
        let count = pixel_count(atlas.width, atlas.height);
        let atlas_pixels = unsafe { to_slice(atlas.pixels, count) }.unwrap();
        assert!(atlas_pixels.contains(&RED));
        assert!(atlas_pixels.contains(&BLUE));

        let sprite = unsafe { &*result.sprites };
        assert_eq!(unsafe { to_str(sprite.id) }, Some("sprite"));
        assert_eq!(sprite.atlas_index, 0);
        assert!(sprite.vertex_count > 0 && sprite.vertex_count % 4 == 0);
        assert_eq!(sprite.index_count, sprite.vertex_count / 4 * 6);
        let indices = unsafe { to_slice(sprite.indices, sprite.index_count) }.unwrap();
        assert!(indices.iter().all(|i| (*i as usize) < sprite.vertex_count));
        let uvs = unsafe { to_slice(sprite.uvs, sprite.vertex_count) }.unwrap();
        assert!(uvs.iter().all(|uv| (0.0..=1.0).contains(&uv.u)));
        assert_eq!(sprite.rect.width, 2.0);
        assert_eq!(sprite.rect.height, 2.0);
        assert_eq!(sprite.pivot, sd_pivot { x: 0.5, y: 0.5 });
        free(result);
    }

    #[test]
    fn uses_sprite_pivot_when_specified() {
        let pixels = [RED];
        let mut sprites = [sprite(c"sprite", &pixels, 1, 1)];
        sprites[0].has_pivot = true;
        sprites[0].pivot = sd_pivot { x: 1.0, y: 0.0 };
        let result = dice(&sprites, &prefs());
        assert_eq!(error(&result), None);
        assert_eq!(
            unsafe { &*result.sprites }.pivot,
            sd_pivot { x: 1.0, y: 0.0 }
        );
        free(result);
    }

    #[test]
    fn returns_empty_result_without_sprites() {
        let result = unsafe { sd_dice(ptr::null(), 0, &prefs()) };
        assert_eq!(error(&result), None);
        assert!(result.atlases.is_null() && result.atlas_count == 0);
        assert!(result.sprites.is_null() && result.sprite_count == 0);
        free(result);
    }

    #[test]
    fn returns_empty_result_for_transparent_sprites() {
        let pixels = [CLEAR, CLEAR, CLEAR, CLEAR];
        let sprites = [sprite(c"sprite", &pixels, 2, 2)];
        let result = dice(&sprites, &prefs());
        assert_eq!(error(&result), None);
        assert!(result.atlases.is_null() && result.atlas_count == 0);
        assert!(result.sprites.is_null() && result.sprite_count == 0);
        free(result);
    }

    #[test]
    fn returns_error_when_prefs_are_null() {
        let result = unsafe { sd_dice(ptr::null(), 0, ptr::null()) };
        assert_eq!(error(&result).unwrap(), "Prefs can't be null.");
        assert!(result.atlases.is_null() && result.sprites.is_null());
        free(result);
    }

    #[test]
    fn returns_error_when_sprites_are_null() {
        let result = unsafe { sd_dice(ptr::null(), 1, &prefs()) };
        assert_eq!(error(&result).unwrap(), "Sprites can't be null.");
        free(result);
    }

    #[test]
    fn returns_error_when_sprite_id_is_null() {
        let pixels = [RED];
        let mut sprites = [sprite(c"", &pixels, 1, 1)];
        sprites[0].id = ptr::null();
        let result = dice(&sprites, &prefs());
        assert!(error(&result).unwrap().starts_with("Sprite #0: ID"));
        free(result);
    }

    #[test]
    fn returns_error_when_sprite_id_is_not_utf8() {
        let pixels = [RED];
        let id = [0xFFu8 as c_char, 0];
        let mut sprites = [sprite(c"", &pixels, 1, 1)];
        sprites[0].id = id.as_ptr();
        let result = dice(&sprites, &prefs());
        assert!(error(&result).unwrap().starts_with("Sprite #0: ID"));
        free(result);
    }

    #[test]
    fn returns_error_when_pixels_are_null() {
        let mut sprites = [sprite(c"1", &[RED], 1, 1), sprite(c"2", &[], 1, 1)];
        sprites[1].texture.pixels = ptr::null();
        let result = dice(&sprites, &prefs());
        assert_eq!(
            error(&result).unwrap(),
            "Sprite #1: Texture pixels can't be null."
        );
        free(result);
    }

    #[test]
    fn propagates_core_errors() {
        let pixels = [RED];
        let sprites = [sprite(c"sprite", &pixels, 1, 1)];
        let mut prefs = prefs();
        prefs.unit_size = 0;
        let result = dice(&sprites, &prefs);
        assert_eq!(error(&result).unwrap(), "Unit size can't be zero.");
        free(result);
    }

    #[test]
    fn reports_progress_with_user_data() {
        unsafe extern "C" fn on_progress(progress: sd_progress, user_data: *mut c_void) {
            let reports = &mut *user_data.cast::<Vec<(f32, String)>>();
            let activity = to_str(progress.activity).unwrap().to_owned();
            reports.push((progress.ratio, activity));
        }

        let mut reports: Vec<(f32, String)> = vec![];
        let mut prefs = prefs();
        prefs.on_progress = Some(on_progress);
        prefs.user_data = ptr::from_mut(&mut reports).cast();
        let pixels = [RED, BLUE, BLUE, RED];
        let sprites = [sprite(c"sprite", &pixels, 2, 2)];
        let result = dice(&sprites, &prefs);
        assert_eq!(error(&result), None);
        free(result);

        assert!(!reports.is_empty());
        assert!(reports.iter().all(|(ratio, _)| (0.0..=1.0).contains(ratio)));
        assert!(reports.iter().all(|(_, activity)| !activity.is_empty()));
    }

    #[test]
    fn frees_zero_initialized_result() {
        free(sd_result {
            error: ptr::null(),
            atlases: ptr::null(),
            atlas_count: 0,
            sprites: ptr::null(),
            sprite_count: 0,
        });
    }
}
