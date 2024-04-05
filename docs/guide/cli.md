# CLI

The simplest way to use SpriteDicing is via standalone CLI utility. Download latest version for your operating system:

- [Windows x64](https://github.com/elringus/sprite-dicing/releases/latest/download/dice-windows-x64.exe)
- [macOS ARM](https://github.com/elringus/sprite-dicing/releases/latest/download/dice-mac-arm)
- [Linux x64](https://github.com/elringus/sprite-dicing/releases/latest/download/dice-linux-x64)

— and run the executable with the directory where source sprites/textures are located:

```ps1
./dice-windows-x64.exe dir
```

Tool can read textures in the following formats:

- PNG
- WEBP
- BMP
- JPEG
- TIFF
- DDS
- TGA

When completed, you'll get atlases and generated sprites data in JSON, which you can then use to build actual sprites for your engine/framework of choice.

To find the available CLI options, use `--help` flag:

```
./dice-windows-x64.exe --help
Usage: dice-windows-x64.exe [OPTIONS] <DIR>

Arguments:
  <DIR>  Input directory to look for textures to pack

Options:
  -o, --out <OUT>              Directory path to write generated data
  -r, --recursive              Recursively search for textures inside input directory
      --separator <SEPARATOR>  When recursive, the separator to join ID of nested sprites [default: /]
  -f, --format <FORMAT>        Format of the generated atlas textures [default: png] [possible values: png, webp, tga]
  -s, --size <SIZE>            The size of a single diced unit, in pixels [default: 64]
  -p, --pad <PAD>              The size of border between adjacent diced units, in pixels [default: 2]
  -i, --inset <INSET>          Relative inset (in 0.0-1.0 range) of the diced units UV coordinates [default: 0]
  -t, --trim                   Trim transparent areas on the built meshes
  -l, --limit <LIMIT>          Maximum size of a single generated atlas texture [default: 2048]
      --square                 Force atlas size to always be square
      --pot                    Force atlas size to always be power of two
      --ppu <PPU>              Pixel per unit ratio of the diced sprite mesh vertices [default: 100]
      --pivot <PIVOT> <PIVOT>  Origin of the diced sprite mesh, in relative offsets from top-left corner [default: 0.5 0.5]
  -h, --help                   Print help
```
