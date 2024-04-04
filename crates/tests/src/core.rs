//! End-to-end tests of the core library.

use crate::common::*;
use sprite_dicing::{Pivot, Prefs};

#[test]
fn mono_1x_reproduced() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice(&SRC[MONO], &prefs).unwrap();
    assert_repro(MONO, diced, &prefs);
}

#[test]
fn mono_2x_reproduced() {
    let prefs = Prefs {
        unit_size: 2,
        padding: 0,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice(&SRC[MONO], &prefs).unwrap();
    assert_repro(MONO, diced, &prefs);
}

#[test]
fn mono_with_padding_reproduced() {
    let prefs = Prefs {
        unit_size: 2,
        padding: 2,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice(&SRC[MONO], &prefs).unwrap();
    assert_repro(MONO, diced, &prefs);
}

#[test]
fn sized_reproduced() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice(&SRC[SIZED], &prefs).unwrap();
    assert_repro(SIZED, diced, &prefs);
}

// #[test]
// fn icons_with_trimming_reproduced() {
//     let prefs = Prefs {
//         trim_transparent: true,
//         ..Prefs::default()
//     };
//     let diced = sprite_dicing::dice(&SRC[ICONS], &prefs).unwrap();
//     assert_repro(ICONS, diced, &prefs);
// }
//
// #[test]
// fn icons_without_trimming_reproduced() {
//     let prefs = Prefs {
//         trim_transparent: false,
//         ..Prefs::default()
//     };
//     let diced = sprite_dicing::dice(&SRC[ICONS], &prefs).unwrap();
//     assert_repro(ICONS, diced, &prefs);
// }
//
// #[test]
// fn icons_with_min_pivot_reproduced() {
//     let prefs = Prefs {
//         pivot: Pivot::new(0.0, 0.0),
//         ..Prefs::default()
//     };
//     let diced = sprite_dicing::dice(&SRC[ICONS], &prefs).unwrap();
//     assert_repro(ICONS, diced, &prefs);
// }
//
// #[test]
// fn icons_with_max_pivot_reproduced() {
//     let prefs = Prefs {
//         pivot: Pivot::new(1.0, 1.0),
//         ..Prefs::default()
//     };
//     let diced = sprite_dicing::dice(&SRC[ICONS], &prefs).unwrap();
//     assert_repro(ICONS, diced, &prefs);
// }
//
// #[test]
// fn icons_with_mixed_pivot_reproduced() {
//     let prefs = Prefs {
//         pivot: Pivot::new(0.25, 0.75),
//         ..Prefs::default()
//     };
//     let diced = sprite_dicing::dice(&SRC[ICONS], &prefs).unwrap();
//     assert_repro(ICONS, diced, &prefs);
// }

#[test]
fn atlas_not_square_when_not_forced() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        atlas_square: false,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice(&SRC[MONO], &prefs).unwrap();
    let atlas = from_texture(&diced.atlases[0]);
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
    let diced = sprite_dicing::dice(&SRC[MONO], &prefs).unwrap();
    let atlas = from_texture(&diced.atlases[0]);
    assert_eq!(atlas.width(), atlas.height());
    assert_repro(MONO, diced, &prefs);
}

#[test]
fn atlas_pot_when_forced() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        atlas_pot: true,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice(&SRC[MONO], &prefs).unwrap();
    let atlas = from_texture(&diced.atlases[0]);
    assert_eq!(atlas.width(), 4);
    assert_eq!(atlas.width(), atlas.height());
    assert_repro(MONO, diced, &prefs);
}

#[test]
fn single_atlases_when_not_limited() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        atlas_size_limit: 1024,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice(&SRC[MONO], &prefs).unwrap();
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
    let diced = sprite_dicing::dice(&SRC[MONO], &prefs).unwrap();
    assert_eq!(diced.atlases.len(), 2);
    assert_repro(MONO, diced, &prefs);
}
