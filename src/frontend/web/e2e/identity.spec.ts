import { test, expect } from '@playwright/test'
import { randomUUID } from 'node:crypto'

test('invito, ruolo Viewer, rifiuto scrittura server, revoca membership e logout', async ({ page, browser }) => {
  test.skip(!process.env.CONDOMANAGER_E2E, 'Usare scripts/Verify-Postgres.ps1 -Browser con database isolato.')
  page.on('dialog', dialog => dialog.accept())
  const email = 'viewer-' + randomUUID() + '@test.invalid'
  const password = process.env.CONDOMANAGER_E2E_PASSWORD!
  await page.goto('/')
  await page.getByLabel('Email', { exact: true }).fill(process.env.CONDOMANAGER_E2E_EMAIL!)
  await page.getByLabel('Password', { exact: true }).fill(password)
  await page.getByRole('button', { name: 'Accedi', exact: true }).click()
  await page.getByRole('button', { name: 'Utenti studio', exact: true }).click()
  await page.getByLabel('Email destinatario').fill(email)
  await page.getByLabel('Ruolo da assegnare').selectOption('Viewer')
  await page.getByRole('button', { name: 'Invia invito', exact: true }).click()
  const message = page.getByRole('row').filter({ has: page.getByRole('cell', { name: email, exact: true }) }).filter({ has: page.getByRole('link', { name: 'Apri link personale' }) })
  await expect(message).toBeVisible()
  const link = await message.getByRole('link').getAttribute('href')
  const context = await browser.newContext({ ignoreHTTPSErrors: true })
  const viewer = await context.newPage()
  await viewer.goto(link!)
  await expect(viewer).toHaveURL('https://localhost:7262/')
  await viewer.getByLabel('Nuova password').fill(password)
  await viewer.getByRole('button', { name: 'Crea account e accetta', exact: true }).click()
  await viewer.getByLabel('Email', { exact: true }).fill(email)
  await viewer.getByLabel('Password', { exact: true }).fill(password)
  await viewer.getByRole('button', { name: 'Accedi', exact: true }).click()
  await viewer.getByRole('button', { name: 'Condomini', exact: true }).click()
  await expect(viewer.getByText('Accesso in sola consultazione.')).toBeVisible()
  await expect(viewer.getByRole('button', { name: 'Nuovo condominio' })).toHaveCount(0)
  await expect(viewer.getByRole('button', { name: 'Utenti studio', exact: true })).toHaveCount(0)
  const csrf = await viewer.request.get('/api/auth/csrf')
  const token = (await csrf.json() as { token: string }).token
  const forbidden = await viewer.request.post('/api/condominiums', {
    headers: { 'X-CSRF-TOKEN': token }, data: { code: 'DENIED', name: 'Synthetic', address: 'X', postalCode: '0', city: 'X', province: 'X', country: 'X' },
  })
  expect(forbidden.status()).toBe(403)
  await page.getByRole('button', { name: 'Dashboard', exact: true }).click()
  await page.getByRole('button', { name: 'Utenti studio', exact: true }).click()
  await page.getByLabel('Accesso ' + email, { exact: true }).uncheck()
  await Promise.all([
    page.waitForResponse(response => response.url().includes('/api/studio/members/') && response.request().method() === 'PUT' && response.status() === 204),
    page.getByRole('row').filter({ has: page.getByLabel('Accesso ' + email, { exact: true }) }).getByRole('button', { name: 'Salva accesso' }).click(),
  ])
  await expect(page.getByLabel('Accesso ' + email, { exact: true })).not.toBeChecked()
  await viewer.reload()
  await expect(viewer.getByText('Non hai membership attive.', { exact: false })).toBeVisible()
  await viewer.getByRole('button', { name: 'Esci', exact: true }).click()
  await expect(viewer.getByRole('heading', { name: 'Accedi al tuo spazio', exact: true })).toBeVisible()
  expect((await viewer.request.get('/api/auth/me')).status()).toBe(401)
  await context.close()
})