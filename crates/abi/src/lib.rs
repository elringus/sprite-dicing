//! C/C++ application binary interface of the library.

use sprite_dicing::{AtlasFormat, FsPrefs, Pivot, Prefs};
use std::ffi::{c_char, CStr};
use std::path::Path;

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CPrefs {
    pub out: *const c_char,
    pub recursive: bool,
    pub separator: *const c_char,
    pub size: u32,
    pub pad: u32,
    pub inset: f32,
    pub trim: bool,
    pub limit: u32,
    pub square: bool,
    pub pot: bool,
    pub ppu: f32,
    pub px: f32,
    pub py: f32,
}

/// ABI wrapper over [sprite_dicing::dice_in_dir].
///
/// # Safety
///
/// Inherent to C/C++ ABI.
#[no_mangle]
pub unsafe extern "C" fn dice_in_dir(dir: *const c_char, prefs: CPrefs) {
    let dir = Path::new(to_str(dir));
    let fs_prefs = to_fs_prefs(&prefs);
    let prefs = to_prefs(&prefs);
    sprite_dicing::dice_in_dir(dir, &fs_prefs, &prefs).unwrap();
}

unsafe fn to_fs_prefs(prefs: &CPrefs) -> FsPrefs {
    FsPrefs {
        out: Some(Path::new(to_str(prefs.out)).to_owned()),
        recursive: prefs.recursive,
        separator: to_str(prefs.separator).to_owned(),
        atlas_format: AtlasFormat::Png,
    }
}

fn to_prefs(prefs: &CPrefs) -> Prefs {
    Prefs {
        unit_size: prefs.size,
        padding: prefs.pad,
        uv_inset: prefs.inset,
        trim_transparent: prefs.trim,
        atlas_size_limit: prefs.limit,
        atlas_square: prefs.square,
        atlas_pot: prefs.pot,
        ppu: prefs.ppu,
        pivot: Pivot {
            x: prefs.px,
            y: prefs.py,
        },
    }
}

unsafe fn to_str<'a>(str: *const c_char) -> &'a str {
    CStr::from_ptr(str).to_str().unwrap()
}
