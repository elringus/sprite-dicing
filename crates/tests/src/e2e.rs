//! End-to-end tests of the solution crates.

mod common;

use crate::common::*;
use serde_json::Value;
use sprite_dicing::{
    AtlasFormat, DicedSprite, FsPrefs, Pivot, Prefs, RawArtifacts, Rect, Uv, Vertex,
};
use std::fs;
use std::path::PathBuf;
use std::str::FromStr;

#[test]
fn mono_reproduces() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Png).unwrap();
    assert_repro(&MONO, diced, &prefs);
}

#[test]
fn mono_atlas_not_square_when_not_forced() {
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
fn mono_atlas_square_when_forced() {
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
fn mono_atlas_pot_when_forced() {
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
fn mono_single_atlases_when_not_limited() {
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
fn mono_multiple_atlases_when_limited() {
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
fn mono_can_write_into_webp() {
    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let diced = sprite_dicing::dice_raw(&MONO, &prefs, &AtlasFormat::Webp).unwrap();
    assert_repro(&MONO, diced, &prefs);
}

#[test]
fn mono_reproduces_in_dir() {
    let crate_dir = env!("CARGO_MANIFEST_DIR");
    let mono_dir = PathBuf::from_str(&format!("{}/fixtures/mono", crate_dir)).unwrap();
    let tmp_dir = PathBuf::from_str(&format!("{}/tmp", crate_dir)).unwrap();
    fs::create_dir(&tmp_dir).unwrap();

    let prefs = Prefs {
        unit_size: 1,
        padding: 0,
        ..Prefs::default()
    };
    let fs_prefs = FsPrefs {
        out: Some(tmp_dir.to_owned()),
        ..FsPrefs::default()
    };

    sprite_dicing::dice_dir(&mono_dir, &fs_prefs, &prefs).unwrap();
    let atlas = fs::read(format!("{}/atlas_0.png", tmp_dir.to_str().unwrap())).unwrap();

    let mut sprites = vec![];
    let json = fs::read_to_string(format!("{}/sprites.json", tmp_dir.to_str().unwrap())).unwrap();
    let json = serde_json::from_str::<Value>(&json).unwrap();
    for sprite in json.as_array().unwrap().iter() {
        sprites.push(DicedSprite {
            id: sprite["id"].as_str().unwrap().to_owned(),
            atlas_index: sprite["atlas"].as_u64().unwrap() as usize,
            vertices: sprite["vertices"]
                .as_array()
                .unwrap()
                .iter()
                .map(|v| Vertex {
                    x: v["x"].as_f64().unwrap() as f32,
                    y: v["y"].as_f64().unwrap() as f32,
                })
                .collect::<Vec<_>>(),
            uvs: sprite["uvs"]
                .as_array()
                .unwrap()
                .iter()
                .map(|v| Uv {
                    u: v["u"].as_f64().unwrap() as f32,
                    v: v["v"].as_f64().unwrap() as f32,
                })
                .collect::<Vec<_>>(),
            indices: sprite["indices"]
                .as_array()
                .unwrap()
                .iter()
                .map(|v| v.as_u64().unwrap() as usize)
                .collect::<Vec<_>>(),
            rect: Rect {
                x: sprite["rect"]["x"].as_f64().unwrap() as f32,
                y: sprite["rect"]["y"].as_f64().unwrap() as f32,
                width: sprite["rect"]["width"].as_f64().unwrap() as f32,
                height: sprite["rect"]["height"].as_f64().unwrap() as f32,
            },
            pivot: Pivot::new(0.5, 0.5),
        })
    }

    let artifacts = RawArtifacts {
        atlases: vec![atlas],
        sprites,
    };
    assert_repro(&MONO, artifacts, &prefs);
    fs::remove_dir_all(tmp_dir).unwrap();
}
