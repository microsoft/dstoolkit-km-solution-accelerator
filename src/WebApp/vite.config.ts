import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import basicSsl from "@vitejs/plugin-basic-ssl";
import postcss from "./postcss.config.js";

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [react(), basicSsl()],
    css: {
        postcss,
    },
});
