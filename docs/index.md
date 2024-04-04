---
layout: home
markdownStyles: false
title: Reuse repeating texture regions
titleTemplate: SpriteDicing • :title

hero:
  name: SpriteDicing
  text: Reuse repeating<br>texture regions
  tagline: Cross-engine tool for lossless compression<br>of sprite textures with identical areas.
  actions:
    - theme: brand
      text: Get Started
      link: /guide/
    - theme: alt
      text: View on GitHub
      link: https://github.com/elringus/sprite-dicing
  image:
    src: /favicon.svg
    alt: SpriteDicing
---

<div class="features">
    <div class="container">
        <div class="items" style="margin: -8px">
            <div class="items">
                <div class="grid-3 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">✨</div>
                                <h2 class="title">Discard Identical Areas</h2>
                            </div>
                            <p class="details">Keep only the unique textures subset in the final build. Ideal for <abbr title="Visual Novels">VNs</abbr>, where character sprites mostly differ in face expressions.</p></article>
                    </div>
                </div>
                <div class="grid-3 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">🧩️</div>
                                <h2 class="title">Engine-agnostic</h2>
                            </div>
                            <p class="details">SpriteDicing is compiled into native library with C ABI making it embeddable to any game engine, framework or programming language.</p></article>
                    </div>
                </div>
                <div class="grid-3 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">💻</div>
                                <h2 class="title">Standalone Tool</h2>
                            </div>
                            <p class="details">Alternatively, standalone CLI tool is available for Windows, macOS and Linux; it'll produce atlases and JSON with the diced sprites specs.</p></article>
                    </div>
                </div>
            </div>
            <div class="items">
                <div class="grid-4 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">🦀</div>
                                <h2 class="title">Written in Rust</h2>
                            </div>
                            <p class="details">SpriteDicing is authored in Rust programming language for best performance and reliability.</p></article>
                    </div>
                </div>
                <div class="grid-4 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">🌐️</div>
                                <h2 class="title">Versatile</h2>
                            </div>
                            <p class="details">CLI tool supports input textures in multiple formats: PNG, WEBP, JPEG, BMP, TIFF, TGS and DDS.</p></article>
                    </div>
                </div>
                <div class="grid-4 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">🛠️</div>
                                <h2 class="title">Customizable</h2>
                            </div>
                            <p class="details">Has plethora of options: atlas size limit, UV inset, padding, trimming, sprite pivot and more.</p></article>
                    </div>
                </div>
                <div class="grid-4 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">🕹️</div>
                                <h2 class="title">Unity Integration</h2>
                            </div>
                            <p class="details">First-party plugin for Unity game engine with a dedicated editor UI; outputs native sprite assets.</p></article>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<style>
:root {
    --vp-home-hero-name-color: transparent;
    --vp-home-hero-name-background: -webkit-linear-gradient(120deg, #ff5d00 30%, #ffdd3a);
    --vp-home-hero-image-background-image: linear-gradient(140deg, #ff5d00 60%, #5f00bf 40%);
    --vp-home-hero-image-filter: blur(60px) opacity(0.66);
}

@media (min-width: 640px) {
    :root {
        --vp-home-hero-image-filter: blur(80px) opacity(0.66);
    }
}

@media (min-width: 960px) {
    :root {
        --vp-home-hero-image-filter: blur(120px) opacity(0.66);
    }

    .VPHome .name .clip {
        line-height: 64px;
        font-size: 60px;
    }

    .VPHome .main .text {
        line-height: 64px;
        font-size: 57px;
    }
}

.image-bg {
    border-radius: 13% !important;
}

.VPHome .tagline a {
    color: var(--vp-c-brand-1);
    text-decoration: underline;
    text-underline-offset: 5px;
    transition: color 0.25s;
}

.VPHome .tagline a:hover {
    color: var(--vp-c-brand-2);
}

.VPHome article .details a {
    color: var(--vp-c-brand-1);
    text-decoration: underline;
    text-underline-offset: 3px;
    transition: color 0.25s;
}

.VPHome article .details a:hover {
    color: var(--vp-c-brand-2);
}

.VPHome .VPButton.medium.brand {
    position: relative;
    display: flex;
    align-items: center;
    padding-top: 5px;
    padding-bottom: 5px;
    padding-right: 15px;
    background-color: #df5100;
}

.VPHome .VPButton.medium.brand:hover {
    background-color: #ef6a1d;
}

.VPHome .VPButton.medium.brand::after {
    content: "";
    mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='22' height='22' viewBox='0 0 24 24' fill='currentColor'%3E%3Cpath d='M17.92 11.62a1.001 1.001 0 0 0-.21-.33l-5-5a1.003 1.003 0 1 0-1.42 1.42l3.3 3.29H7a1 1 0 0 0 0 2h7.59l-3.3 3.29a1.002 1.002 0 0 0 .325 1.639 1 1 0 0 0 1.095-.219l5-5a1 1 0 0 0 .21-.33 1 1 0 0 0 0-.76Z'%3E%3C/path%3E%3C/svg%3E") no-repeat 50% 50%;
    /* Required to render correctly on mobile. */
    display: inline-block;
    width: 22px;
    height: 22px;
    padding-left: 30px;
    background-color: var(--vp-button-brand-text);
}

.VPHome .VPButton.medium.alt {
    position: relative;
    display: flex;
    align-items: center;
    padding-top: 5px;
    padding-bottom: 5px;
    padding-right: 15px;
}

.VPHome .VPButton.medium.alt::after {
    content: "";
    mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 24 24' fill='currentColor'%3E%3Cpath d='M19.33 10.18a1 1 0 0 1-.77 0 1 1 0 0 1-.62-.93l.01-1.83-8.2 8.2a1 1 0 0 1-1.41-1.42l8.2-8.2H14.7a1 1 0 0 1 0-2h4.25a1 1 0 0 1 1 1v4.25a1 1 0 0 1-.62.93Z'%3E%3C/path%3E%3Cpath d='M11 4a1 1 0 1 1 0 2H7a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1v-4a1 1 0 1 1 2 0v4a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3V7a3 3 0 0 1 3-3h4Z'%3E%3C/path%3E%3C/svg%3E") no-repeat 50% 50%;
    /* Required to render correctly on mobile. */
    display: inline-block;
    width: 20px;
    height: 20px;
    padding-left: 32px;
    background-color: var(--vp-button-alt-text);
}
</style>

<style scoped>
/* A hack copying home page specific styles, as they're applied with guid attr. */
.features { position: relative; padding: 0 24px; }
@media (min-width: 640px) { .features { padding: 0 48px; } }
@media (min-width: 960px) { .features { padding: 0 64px; } }
.container { margin: 0 auto; max-width: 1152px; }
.items { display: flex; flex-wrap: wrap; }
.item { padding: 8px; width: 100%; }
@media (min-width: 640px) { .item.grid-4 { width: 50%; } }
@media (min-width: 768px) { .item.grid-4 { width: 50%; } .item.grid-3 { width: calc(100% / 3); } }
@media (min-width: 960px) { .item.grid-4 {width: 25%} }
.VPFeature { display: block; border: 1px solid var(--vp-c-bg-soft); border-radius: 12px; height: 100%;background-color: var(--vp-c-bg-soft); transition: border-color .25s, background-color .25s; }
.box { display: flex; flex-direction: column; padding: 24px; height: 100%; }
.box-title { display: flex; align-items: baseline; column-gap: 15px; }
.icon {display: flex; justify-content: center; align-items: center; margin-bottom: 20px; border-radius: 6px;background-color: var(--vp-c-default-soft); width: 40px; height: 40px; font-size: 22px; transition: background-color .25s; }
.title { line-height: 24px; font-size: 18px; font-weight: 600; }
.details { flex-grow: 1; line-height: 24px; font-size: 14px; font-weight: 500; color: var(--vp-c-text-2); }
</style>
