//! End-to-end tests of the solution crates.

mod abi;
mod cli;
mod common;

use crate::common::*;
use sprite_dicing::RawArtifacts;

#[test]
fn foo() {
    assert!(reproducible(
        &MONO,
        RawArtifacts {
            atlases: vec![],
            sprites: vec![]
        }
    ));
}
