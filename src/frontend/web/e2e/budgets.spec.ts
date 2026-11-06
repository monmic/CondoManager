import { test, expect } from '@playwright/test'

test('preventivo reale: bozza, proposta, riparto, approvazione e nuova versione', async ({ page }) => {
  test.skip(!process.env.CONDOMANAGER_E2E, 'Usare Verify-Postgres.ps1 -Browser con database isolato.')
  page.on('dialog', dialog => dialog.accept())
  await page.goto('/')
  await page.getByLabel('Email', { exact: true }).fill(process.env.CONDOMANAGER_E2E_EMAIL!)
  await page.getByLabel('Password', { exact: true }).fill(process.env.CONDOMANAGER_E2E_PASSWORD!)
  await page.getByRole('button', { name: 'Accedi', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Preventivi', exact: true })).toBeVisible()
  // Registry fixtures are created through the real authenticated APIs, never mocks.
  const condoId = await page.evaluate(async () => {
    const { token } = await (await fetch('/api/auth/csrf')).json() as { token: string }
    async function post<T>(path: string, body: unknown): Promise<T> {
      const response = await fetch('/api' + path, { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token }, body: JSON.stringify(body) })
      if (!response.ok) throw new Error(await response.text())
      return response.json() as Promise<T>
    }
    const condo = await post<{ id: string }>('/condominiums', { code: 'C-BUDGET', name: 'Condominio preventivi sintetico', address: 'Sintetico', postalCode: '00000', city: 'Test', province: 'XX', country: 'Test' })
    for (const code of ['A', 'B', 'C']) await post('/condominiums/' + condo.id + '/units', { code, kind: 'Apartment' })
    const period = await post<{ id: string }>('/condominiums/' + condo.id + '/accounting-periods', { code: 'E-BUDGET', name: 'Esercizio preventivi sintetico', startsOn: '2026-01-01', endsOn: '2026-12-31' })
    await post('/condominiums/' + condo.id + '/accounting-periods/' + period.id + '/open', {})
    const rule = await post<{ id: string }>('/allocations/condominiums/' + condo.id + '/rules', { name: 'Preventivo parti uguali', type: 'EqualShares', components: [{ criterion: 'EqualShares', percentage: '100' }] })
    await post('/allocations/condominiums/' + condo.id + '/rules/' + rule.id + '/activate', {})
    return condo.id
  })
  // Reload registry resources after setup; all milestone interactions below use the UI.
  await page.reload()
  await page.getByRole('button', { name: 'Preventivi', exact: true }).click()
  await page.getByRole('combobox', { name: 'Condominio', exact: true }).selectOption(condoId)
  await page.getByRole('button', { name: 'Nuovo preventivo', exact: true }).click()
  await page.getByRole('combobox', { name: 'Esercizio', exact: true }).selectOption({ label: 'Esercizio preventivi sintetico' })
  await page.getByLabel('Descrizione preventivo', { exact: true }).fill('Preventivo sintetico')
  await page.getByRole('combobox', { name: 'Categoria', exact: true }).selectOption({ label: '550 · Manutenzioni ordinarie' })
  await page.getByLabel('Descrizione voce', { exact: true }).fill('Manutenzione preventiva sintetica')
  await page.getByLabel('Importo previsto', { exact: true }).fill('100')
  await page.getByRole('combobox', { name: 'Regola di riparto', exact: true }).selectOption({ label: 'Preventivo parti uguali · v1' })
  await page.getByRole('button', { name: 'Salva preventivo', exact: true }).click()
  await page.getByRole('button', { name: 'Proponi preventivo', exact: true }).click()
  await page.getByRole('button', { name: 'Simula riparto', exact: true }).click()
  await expect(page.getByRole('cell', { name: '33,34 €', exact: true })).toBeVisible()
  await page.getByLabel('Riferimento delibera').fill('Riferimento amministrativo sintetico')
  await page.getByRole('button', { name: 'Approva preventivo', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Riparto approvato', exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Crea nuova versione', exact: true }).click()
  await page.getByLabel('Importo previsto', { exact: true }).fill('110')
  await page.getByRole('button', { name: 'Salva preventivo', exact: true }).click()
  await expect(page.getByRole('cell', { name: '10,00 €', exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: 'Approved', exact: true })).toBeVisible()
  await page.screenshot({ path: '../../../.artifacts/budget-desktop.png', fullPage: true })
  await page.setViewportSize({ width: 390, height: 844 })
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await page.screenshot({ path: '../../../.artifacts/budget-mobile.png', fullPage: true })
})
