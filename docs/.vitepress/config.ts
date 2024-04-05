import { defineConfig } from "vitepress";
import proc from "node:child_process";
import imgit from "imgit/vite";

export default defineConfig({
    title: "SpriteDicing",
    titleTemplate: ":title • SpriteDicing",
    description: "Cross-engine tool for lossless compression of sprite textures with identical areas.",
    appearance: "dark",
    cleanUrls: true,
    lastUpdated: true,
    vite: { plugins: [imgit({ width: 688 })] },
    head: [
        ["link", { rel: "icon", href: "/favicon.svg" }],
        ["link", { rel: "preload", href: "/fonts/inter.woff2", as: "font", type: "font/woff2", crossorigin: "" }],
        ["link", { rel: "preload", href: "/fonts/jb.woff2", as: "font", type: "font/woff2", crossorigin: "" }],
        ["meta", { name: "og:image", content: "/img/og.jpg" }],
        ["meta", { name: "twitter:card", content: "summary_large_image" }]
    ],
    themeConfig: {
        logo: { src: "/favicon.svg" },
        logoLink: "/",
        socialLinks: [{ icon: "github", link: "https://github.com/elringus/sprite-dicing" }],
        search: { provider: "local", options: { detailedView: true } },
        lastUpdated: { text: "Updated", formatOptions: { dateStyle: "medium" } },
        sidebarMenuLabel: "Menu",
        darkModeSwitchLabel: "Appearance",
        returnToTopLabel: "Return to top",
        outline: { label: "On this page", level: "deep" },
        docFooter: { prev: "Previous page", next: "Next page" },
        nav: [
            { text: "Guide", link: "/guide/", activeMatch: "/guide/" },
            {
                text: proc.execSync("git describe --abbrev=0 --tags").toString(), items: [
                    { text: "Changes", link: "https://github.com/elringus/sprite-dicing/releases/latest" },
                    { text: "Contribute", link: "https://github.com/elringus/sprite-dicing/labels/help%20wanted" }
                ]
            }
        ],
        editLink: {
            pattern: "https://github.com/elringus/sprite-dicing/edit/main/docs/:path",
            text: "Edit this page on GitHub"
        },
        sidebar: {
            "/guide/": [
                {
                    text: "Guide",
                    items: [
                        { text: "Introduction", link: "/guide/" },
                        { text: "Getting Started", link: "/guide/getting-started" },
                        { text: "API", link: "/guide/api" },
                        { text: "ABI", link: "/guide/abi" },
                        { text: "CLI", link: "/guide/cli" },
                        { text: "Unity", link: "/guide/unity" }
                    ]
                }
            ]
        }
    },
    sitemap: { hostname: "https://dicing.elringus.me" }
});
