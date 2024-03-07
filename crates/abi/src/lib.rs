//! C/C++ ABI bindings wrapper over the APIs of the main lib.

#![allow(clippy::missing_safety_doc)]

use std::ffi::{c_char, CStr, CString};
use std::path::Path;

#[no_mangle]
pub unsafe extern "C" fn dice_in_dir(dir: *const c_char, out: *const c_char) -> *const c_char {
    let r_dir = Path::new(CStr::from_ptr(dir).to_str().unwrap());
    let r_out = Path::new(CStr::from_ptr(out).to_str().unwrap());
    let result = sprite_dicing::dice_in_dir(r_dir, r_out).unwrap();
    CString::new(result).unwrap().into_raw()
}
