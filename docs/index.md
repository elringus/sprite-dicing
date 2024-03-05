---
layout: home
markdownStyles: false
title: Convert media links to optimized HTML
titleTemplate: imgit • :title

hero:
  name: imgit
  text: Convert media links to optimized HTML
  tagline: Images, video and YouTube&#58; fetch, encode, scale, lazyload – for best UX and <a href="https://web.dev/vitals" target="_blank">Web Vitals</a>.
  actions:
    - theme: brand
      text: Get Started
      link: /guide/
    - theme: alt
      text: View on GitHub
      link: https://github.com/elringus/imgit
  image:
    src: /img/hero.webp
    alt: imgit
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
                                <h2 class="title">Transformative</h2>
                            </div>
                            <p class="details">Builds optimized HTML for arbitrary image, video and YouTube syntax, such as URLs, markdown or JSX tags.</p></article>
                    </div>
                </div>
                <div class="grid-3 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">⚡</div>
                                <h2 class="title">Accelerating</h2>
                            </div>
                            <p class="details">Encodes to the modern AV1/AVIF format compressing by up to 90% without noticeable quality loss. Supports GPU acceleration.</p></article>
                    </div>
                </div>
                <div class="grid-3 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">♻️</div>
                                <h2 class="title">Polyglot</h2>
                            </div>
                            <p class="details">Works with most known media formats: JPEG, PNG, APNG, SVG, GIF, WEBP, WEBM, MP4, AVI, MOV, MKV, BMP, TIFF, TGA and even PSD.</p></article>
                    </div>
                </div>
            </div>
            <div class="items">
                <div class="grid-4 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">🌊</div>
                                <h2 class="title">Smooth</h2>
                            </div>
                            <p class="details">Generates tiny blurred covers from the source content to be beautifully crossfaded into HD originals once lazy-loaded.</p></article>
                    </div>
                </div>
                <div class="grid-4 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">📐</div>
                                <h2 class="title">Adaptive</h2>
                            </div>
                            <p class="details">Optionally scales down the content to specified threshold while preserving high-resolution variants for high-DPI displays.</p></article>
                    </div>
                </div>
                <div class="grid-4 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">🌐</div>
                                <h2 class="title">Outgoing</h2>
                            </div>
                            <p class="details">Fetches from remote sources, such as image hostings. Uploads optimized content to designated endpoint, such as CDN.</p></article>
                    </div>
                </div>
                <div class="grid-4 item">
                    <div class="VPLink no-icon VPFeature">
                        <article class="box">
                            <div class="box-title">
                                <div class="icon">🗺️</div>
                                <h2 class="title">Omnipresent</h2>
                            </div>
                            <p class="details">Built-in plugins for Astro, SvelteKit, SolidStart, VitePress, Nuxt and Remix. Adapters for Node, Deno and Bun runtimes.</p></article>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<style>
:root {
  --vp-home-hero-name-color: transparent;
  --vp-home-hero-name-background: -webkit-linear-gradient(120deg, #ee3248 30%, #ffba3c);
  --vp-home-hero-image-background-image: linear-gradient(-45deg, #6719b7 50%, #ee3248 50%);
  --vp-home-hero-image-filter: blur(44px);
}

@media (min-width: 640px) {
  :root {
    --vp-home-hero-image-filter: blur(56px);
  }
}

@media (min-width: 960px) {
  :root {
    --vp-home-hero-image-filter: blur(68px);
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
    background-color: var(--vp-c-brand-1);
}

.VPHome .VPButton.medium.brand:hover {
    background-color: var(--vp-c-brand-2);
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
