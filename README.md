<p align="center">
  <a href="https://github.com/elringus/sprite-dicing" target="_blank" rel="noopener noreferrer">
    <img width="200" src="https://raw.githubusercontent.com/elringus/sprite-dicing/main/docs/public/favicon.svg" alt="SpriteDicing">
  </a>
</p>
<br/>
<p align="center">
  <a href="https://crates.io/crates/sprite_dicing"><img alt="crates.io" src="https://img.shields.io/crates/v/sprite_dicing"></a>
  <a href="https://www.codefactor.io/repository/github/elringus/sprite-dicing"><img src="https://www.codefactor.io/repository/github/elringus/sprite-dicing/badge" alt="CodeFactor"/></a>
  <a href="https://codecov.io/gh/elringus/sprite-dicing"><img src="https://codecov.io/gh/elringus/sprite-dicing/branch/main/graph/badge.svg?token=DBUTGP0Q7C" alt="CodeCov"></a>
</p>
<br/>

# Reuse repeating texture regions

Use SpriteDicing to split a set of sprite textures into units, discard identical ones, bake unique units into atlas textures to then seamlessly reconstruct the original sprites at runtime, without actually keeping original textures in the build.

The solution significantly reduces build size when multiple textures with identical areas are used. Consider a [visual novel](https://en.wikipedia.org/wiki/Visual_novel) type of game, where multiple textures per character are used, each portraying different emotion; most of the texture space is occupied with identical data, while only a small area varies:

<img alt="dicing illustration" src="https://raw.githubusercontent.com/elringus/sprite-dicing/main/docs/public/img/banner.png">

These original five textures have total size of **17.5MB**. After dicing, the resulting atlas texture will contain only the unique areas of the original textures and consume just **2.4MB**, effectively compressing the textures by **86.3%**.

<a href="https://naninovel.com">
  <p align="center">Sprite Dicing is used in <strong>Naninovel</strong> — visual novel engine. Check it out!</p>
  <p align="center"><img alt="naninovel banner" src="https://raw.githubusercontent.com/elringus/cdn/main/naninovel-banner-wide.png"></p>
</a>

## 🎬 Get Started

https://dicing.elringus.com/guide/getting-started
