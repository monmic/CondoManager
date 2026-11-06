import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  workers: 1,
  use: {
    actionTimeout: 10_000, baseURL: 'https://localhost:7262', browserName: 'chromium', channel: 'msedge',
    viewport: { width: 1440, height: 1000 }, ignoreHTTPSErrors: true,
    // Authentication traces could capture credentials or tokens; keep them off.
    trace: 'off',
  },
  webServer: {
    command: 'dotnet run --no-build --project ../../backend/CondoManager.Api --no-launch-profile --urls https://localhost:7262',
    url: 'https://localhost:7262/health', ignoreHTTPSErrors: true, reuseExistingServer: false, timeout: 30_000,
  },
})