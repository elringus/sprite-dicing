//! C/C++ application binary interface of the library.

use std::ffi::{c_char, CStr, CString};
use std::path::Path;

/// ABI wrapper over [sprite_dicing::dice_in_dir].
///
/// # Arguments
///
/// * `dir`:
/// * `out`:
///
/// returns: *const i8
///
/// # Safety
///
/// Inherent to C/C++ ABI.
///
/// # Examples
///
/// ```
///
/// ```
#[no_mangle]
pub unsafe extern "C" fn dice_in_dir(dir: *const c_char, out: *const c_char) -> *const c_char {
    let _ = Path::new(CStr::from_ptr(dir).to_str().unwrap());
    let _ = Path::new(CStr::from_ptr(out).to_str().unwrap());
    CString::new("").unwrap().into_raw()
}
