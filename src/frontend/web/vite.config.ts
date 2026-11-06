import { readFileSync } from 'node:fs'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig(({ command }) => {
  const certificate = process.env.DEV_HTTPS_PFX_PATH
  if (command === 'serve' && !certificate)
    throw new Error('Configurare DEV_HTTPS_PFX_PATH e DEV_HTTPS_PFX_PASSWORD per Vite HTTPS, oppure usare la SPA compilata servita dall’API.')
  return {
    plugins: [react()],
    server: {
      host: 'localhost',
      https: certificate ? { pfx: readFileSync(certificate), passphrase: process.env.DEV_HTTPS_PFX_PASSWORD } : undefined,
      proxy: { '/api': { target: 'https://localhost:7262', secure: false } },
    },
  }
})