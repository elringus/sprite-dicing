use crate::common::*;
use sprite_dicing::{AtlasFormat, Prefs};

#[test]
fn reproduces() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Png).unwrap();
    assert_repro(&MONO, diced, &prefs);
}

#[test]
fn errs_on_invalid_spec() {
    let prefs = Prefs {
        unit_size: 0,
        ..Prefs::default()
    };
    assert!(sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Png)
        .is_err_and(|e| e.to_string() == "Unit size can't be zero."));
}

#[test]
fn errs_on_invalid_source() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let mut mono = MONO.to_owned();
    let byte = vec![0u8];
    mono[0].bytes = &byte;
    assert!(sprite_dicing::dice_raw(&mono, &prefs, &AtlasFormat::Png).is_err());
}

#[test]
fn atlas_not_square_when_not_forced() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        atlas_square: false,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Png).unwrap();
    let atlas = bytes_to_img(&diced.atlases[0]);
    assert_ne!(atlas.width(), atlas.height());
}

#[test]
fn atlas_square_when_forced() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        atlas_square: true,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Png).unwrap();
    let atlas = bytes_to_img(&diced.atlases[0]);
    assert_eq!(atlas.width(), atlas.height());
    assert_repro(&MONO, diced, &prefs);
}

#[test]
fn atlas_pot_when_forced() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        atlas_pot: true,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Png).unwrap();
    let atlas = bytes_to_img(&diced.atlases[0]);
    assert_eq!(atlas.width(), 4);
    assert_eq!(atlas.width(), atlas.height());
    assert_repro(&MONO, diced, &prefs);
}

#[test]
fn single_atlases_when_not_limited() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        atlas_size_limit: 1024,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Png).unwrap();
    assert_eq!(diced.atlases.len(), 1);
}

#[test]
fn multiple_atlases_when_limited() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        atlas_size_limit: 2,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Png).unwrap();
    assert_eq!(diced.atlases.len(), 2);
    assert_repro(&MONO, diced, &prefs);
}

#[test]
fn can_write_into_webp() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Webp).unwrap();
    assert_repro(&MONO, diced, &prefs);
}
