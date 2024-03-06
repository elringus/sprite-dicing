//! Command line interface for the main library.

use std::{env, process};
use std::error::Error;
use std::path::Path;
use sprite_dicing;

fn main() -> Result<(), Box<dyn Error>> {
    let arg1 = env::args().nth(1).unwrap_or_else(|| {
        eprintln!("Missing directory path.");
        process::exit(1);
    });
    print!("{}", sprite_dicing::dice_in_dir(Path::new(&arg1), Path::new("./"))?);
    return Ok(());
}
