import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// O build vai direto para a pasta wwwroot da API: um único "dotnet run" sobe o sistema inteiro.
export default defineConfig({
  plugins: [react()],
  build: { outDir: '../src/AgendaPet.Api/wwwroot', emptyOutDir: true },
  server: { proxy: { '/api': 'http://localhost:5000' } }
})
