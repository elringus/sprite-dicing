//! Command line interface for the main library.

use std::error::Error;
use std::{env, process};

fn main() -> Result<(), Box<dyn Error>> {
    _ = env::args().nth(1).unwrap_or_else(|| {
        eprintln!("Missing directory path.");
        process::exit(1);
    });
    Ok(())
}
