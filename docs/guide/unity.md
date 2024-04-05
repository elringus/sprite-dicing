# Unity

SpriteDicing has a [Unity](https://unity.com/) integration with full-fledged atlas editor. It communicates with the Rust libraries via C ABI and outputs native Unity sprite assets.

The extension package bundles pre-built binaries for Windows x64, Mac ARM and Linux x64. All the code is editor-only, so this covers all the Unity editor-supported platforms.

Minimum supported Unity version: 2022.3. For previous Unity versions use legacy v1.x branch: https://github.com/elringus/sprite-dicing/tree/arch/v1.

## Installation

Use [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html) to install the package as a remote git package with the following URI:

```
https://github.com/elringus/sprite-dicing.git?path=/plugins/unity/Assets/SpriteDicing
```

![](/img/upm.mp4)

Alternatively, clone the repository and install [extension directory](https://github.com/elringus/sprite-dicing/tree/main/plugins/unity/Assets/SpriteDicing) as local package.

## Usage

1. Create a `DicedSpriteAtlas` asset using `Assets -> Create -> Diced Sprite Atlas` menu command, select it;
2. Specify `Input Folder` — project directory, containing the source textures to process. You can drag-drop a folder from the project hierarchy window or select one with object picker;
3. Click `Build Atlas` button and wait for the procedure to finish;
4. Generated sprites will appear inside the atlas asset; drag and drop them on scene like regular sprites.

![](https://i.gyazo.com/faddf19580d8e6c9e0660d61976b2bef.gif)

## Atlas Generation Options

You can optionally configure atlas generation settings via the editor inspector window.

![](https://i.gyazo.com/f1dc73948ae7d9611f080c152376de02.png)

| Option | Description
| --- | --- |
| Decouple Sprite Data | Whether to save sprite assets in a separate folder instead of adding them as childs of the atlas asset. |
| Trim Transparent | Whether to discard fully-transparent diced units on the generated mesh. Disable to preserve original texture dimensions (usable for animations). |
| Default Pivot | Relative pivot point position in 0 to 1 range, counting from the bottom-left corner. Can be changed after build for each sprite individually. |
| Keep Original | Whether to use pivot set on source textures (if any) instead of default. |
| Atlas Size Limit | Maximum size of a single generated atlas texture; will generate multiple textures when the limit is reached. |
| Square | The generated atlas textures will always be square. Less efficient, but required for PVRTC compression. |
| POT | The generated atlas textures will always have width and height of power of two. Extremely inefficient, but may be required by some older GPUs. |
| Pixels Per Unit | How many pixels in the sprite correspond to the unit in the world. |
| Dice Unit Size | The size of a single diced unit. |
| Padding | The size of a pixel border to add between adjacent diced units inside atlas. Increase to prevent texture bleeding artifacts (usually appear as thin gaps between diced units). Larger values will consume more texture space, but yield better anti-bleeding results. Minimum value of 2 is recommended in most cases. When 2 is not enough to prevent bleeding, consider adding a bit of `UV Inset` before increasing the padding. |
| UV Inset | Relative inset of the diced units UV coordinates. Can be used in addition to (or instead of) `Padding` to prevent texture bleeding artifacts. Won't consume any texture space, but higher values could visually distort the final result. |
| Input Folder | Asset folder with source sprite textures. |
| Include Subfolders | Whether to recursively search for textures inside the input folder. |
| Separator | When sprite is from a sub-folder(s), the separator will be used to join the folder name(s) and the sprite name. |

All the above descriptions are available as tooltips when hovering corresponding configuration options in the editor.

## Compression Ratio

When inspecting atlas asset, notice `Compression Ratio` line; it shows the ratio between source textures size and generated data (atlas textures + sprite meshes).

![](https://i.gyazo.com/c104f864bb4ce2b33760616ced9a9276.png)

When close to 1 or lower, the value will be in yellow/red color indicating the generated data size is close to or even larger than the source and you should either change atlas generation configuration (eg, increase dice unit size) or not use the solution at all.

## uGUI

To use the diced sprites in with Unity UI (aka uGUI), enable `Use Sprite Mesh`. In case the option is not available, make sure to assign a source image.

![](https://i.gyazo.com/8f22fe0bded5ae72b5ef662e842bcacf.png)

## UI Toolkit

Diced sprites are normal Unity sprite assets and can be assigned as source for various visual UI Toolkit elements, no additional setup is required. Check "UI" scene in the [samples directory](https://github.com/elringus/sprite-dicing/tree/main/plugins/unity/Assets/Samples) for an example.

## Animation

It's possible to use diced sprites for animation. Make sure to disable `Trim Transparent` when generating the atlas to preserve relative positions of the generated sprites. An example on animating diced sprites is available in "Animation" scene.

![](https://i.gyazo.com/9df7af39368a7b17f067a03a50c41509.gif)
