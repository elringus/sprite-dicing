# SpriteDicing

[![openupm](https://img.shields.io/npm/v/com.elringus.spritedicing?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.elringus.spritedicing/)
[![CodeFactor](https://www.codefactor.io/repository/github/elringus/spritedicing/badge)](https://www.codefactor.io/repository/github/elringus/spritedicing)
[![codecov](https://codecov.io/gh/Elringus/SpriteDicing/branch/master/graph/badge.svg?token=DBUTGP0Q7C)](https://codecov.io/gh/Elringus/SpriteDicing)

<a href="https://naninovel.com">
  <p align="center">Sprite Dicing is used in <strong>Naninovel</strong> — visual novel engine. Check it out!</p>
  <p align="center"><img src="https://raw.githubusercontent.com/Elringus/CDN/main/naninovel-banner-wide.png"></p>
</a>

Sprite Dicing is an extension for [Unity game engine](https://unity3d.com) allowing to split a set of sprite textures into dices, discard identical ones, bake unique dices into atlas textures and then seamlessly reconstruct the original sprites at runtime.

The solution significantly reduces build size when multiple textures with identical areas are used. Consider a [visual novel](https://en.wikipedia.org/wiki/Visual_novel) type of game, where multiple textures per character are used, each portraying a different emotion; most of the texture space is occupied with identical data, while only a small area varies:

![](https://i.gyazo.com/af08d141e7a08b6a8e2ef60c07332bbf.png)

These original five textures have total size of **17.5MB**. After dicing, the resulting atlas texture will contain only the unique areas of the original textures and consume just **2.4MB**, effectively compressing the textures by **86.3%**.

## Installation

Use [UPM](https://docs.unity3d.com/Manual/upm-ui.html) to install the package via the following git URL: `https://github.com/Elringus/SpriteDicing.git#package` or download and import [SpriteDicing.unitypackage](https://github.com/Elringus/SpriteDicing/raw/master/SpriteDicing.unitypackage) manually.

![](https://i.gyazo.com/b54e9daa9a483d9bf7f74f0e94b2d38a.gif)

Minimum supported Unity version: 2019.3

## Usage

1. Create a `DicedSpriteAtlas` asset using `Assets -> Create -> Diced Sprite Atlas` menu command, select it;
2. Specify `Input Folder` — project directory, containing the source textures to process. You can drag-drop a folder from the project hierarchy window or select one with object picker;
3. Click `Build Atlas` button and wait for the procedure to finish;
4. Generated sprites will appear inside the atlas asset; drag and drop them on scene like regular sprites.

![](https://i.gyazo.com/faddf19580d8e6c9e0660d61976b2bef.gif)

## Atlas Generation Options

You can optionally configure atlas generation settings via the editor inspector window.

![](https://i.gyazo.com/252de40911101e488a7e8e65a61924cd.png)

| Option | Description
| --- | --- |
| Decouple Sprite Data | Whether to save sprite assets in a separate folder instead of adding them as childs of the atlas asset. |
| Trim Transparent | Improves compression ratio by discarding fully-transparent diced units, but may also change sprite dimensions. Disable to preserve original texture dimensions. |
| Default Pivot | Relative pivot point position in 0 to 1 range, counting from the bottom-left corner. Can be changed after build for each sprite individually. |
| Keep Original | Whether to preserve original sprites pivot (usable for animations). |
| Atlas Size Limit | Maximum size of a single generated atlas texture; will generate multiple textures when the limit is reached. |
| Square | The generated atlas textures will always be square. Less efficient, but required for PVRTC compression. |
| POT | The generated atlas textures will always have width and height of power of two. Extremely inefficient, but may be required by some older GPUs. |
| Pixels Per Unit | How many pixels in the sprite correspond to the unit in the world. |
| Dice Unit Size | The size of a single diced unit. |
| Padding | The size of a pixel border to add between adjacent diced units inside atlas. Increase to prevent texture bleeding artifacts (usually appear as thin gaps between diced units). Larger values will consume more texture space, but yield better anti-bleeding results. Minimum value of 2 is recommended in most cases. When 2 is not enough to prevent bleeding, consider adding a bit of `UV Inset` before increasing the padding. |
| UV Inset | Relative inset of the diced units UV coordinates. Can be used in addition to (or instead of) `Padding` to prevent texture bleeding artifacts. Won't consume any texture space, but higher values could visually distort the final result. |
| Input Folder | Asset folder with source sprite textures. |
| Include Subfolders | Whether to recursively search for textures inside the input folder. |
| Prepend Names | Whether to prepend sprite names with the subfolder name; eg: `SubfolderName.SpriteName`. |

All the above descriptions are available as tooltips when hovering corresponding configuration options in the editor.

## Compression Ratio

When inspecting atlas asset, notice `Compression Ratio` line; it shows the ratio between source textures size and generated data (atlas textures + sprite meshes).

![](https://i.gyazo.com/c104f864bb4ce2b33760616ced9a9276.png)

When close to 1 or lower, the value will be in yellow/red color indicating the generated data size is close to or even larger then the source and you should either change atlas generation configuration (eg, increase dice unit size) or not use the solution at all.

## UI

To use the diced sprites in UI (eg, `Image` component), enable `Use Sprite Mesh`. In case the option is not available, make sure to assign a source image.

![](https://i.gyazo.com/8f22fe0bded5ae72b5ef662e842bcacf.png)

## Animation

It's possible to use diced sprites for animation. Make sure to enable `Keep Original Pivot` when generating the atlas to preserve relative positions of the generated sprites. An example on animating diced sprites is available in "Animation" scene.

![](https://i.gyazo.com/9df7af39368a7b17f067a03a50c41509.gif)
