//! End-to-end tests of the solution crates.

mod abi;
mod cli;
mod common;

use crate::common::*;
use sprite_dicing::{AtlasFormat, Prefs};

#[test]
fn raw_artifacts_reproducible() {
    let diced = sprite_dicing::dice_raw(&MONO, &Prefs::default(), &AtlasFormat::Png).unwrap();
    assert_repro(&MONO, diced, &Prefs::default());
}
