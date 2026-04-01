import path from "path"
import react from "@vitejs/plugin-react"
import tailwindcss from "@tailwindcss/vite"
import { defineConfig } from "vite"

export default defineConfig({
  plugins: [react(), tailwindcss()],
  preview: {
    allowedHosts: [
      "rendatop.sistemaonline.shop",
      "rendatop.com.br",
      "www.rendatop.com.br",
      "app.rendatop.com.br",
    ]
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
})
