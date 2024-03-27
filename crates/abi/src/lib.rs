//! C/C++ application binary interface of the library.

use sprite_dicing::{AtlasFormat, FsPrefs, Pivot, Prefs};
use std::ffi::{c_char, CStr};
use std::path::Path;

/// ABI wrapper over [sprite_dicing::dice_in_dir].
///
/// # Safety
///
/// Inherent to C/C++ ABI.
#[no_mangle]
pub unsafe extern "C" fn dice_in_dir(
    dir: *const c_char,
    out: *const c_char,
    recursive: bool,
    separator: *const c_char,
    unit_size: u32,
    padding: u32,
    uv_inset: f32,
    trim_transparent: bool,
    atlas_size_limit: u32,
    atlas_square: bool,
    atlas_pot: bool,
    ppu: f32,
    pivot_x: f32,
    pivot_y: f32,
) {
    let dir = Path::new(to_str(dir));
    let fs_prefs = FsPrefs {
        out: Some(Path::new(to_str(out)).to_owned()),
        recursive,
        separator: to_str(separator).to_owned(),
        atlas_format: AtlasFormat::Png,
    };
    let prefs = Prefs {
        unit_size,
        padding,
        uv_inset,
        trim_transparent,
        atlas_size_limit,
        atlas_square,
        atlas_pot,
        ppu,
        pivot: Pivot {
            x: pivot_x,
            y: pivot_y,
        },
    };
    sprite_dicing::dice_in_dir(dir, &fs_prefs, &prefs).unwrap();
}

unsafe fn to_str<'a>(str: *const c_char) -> &'a str {
    CStr::from_ptr(str).to_str().unwrap()
}
