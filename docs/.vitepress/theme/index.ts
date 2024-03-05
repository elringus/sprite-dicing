import DefaultTheme from "vitepress/theme-without-fonts";
import "./style.css";

import "imgit/styles";
import "imgit/client";

// https://vitepress.dev/guide/extending-default-theme
export default { extends: { Layout: DefaultTheme.Layout } };
