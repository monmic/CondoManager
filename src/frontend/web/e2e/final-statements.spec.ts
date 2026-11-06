import { test, expect } from '@playwright/test'

test('ciclo reale completo fino al consuntivo, conguagli e chiusura con insoluti', async ({ page }) => {
 test.skip(!process.env.CONDOMANAGER_E2E, 'Usare Verify-Postgres.ps1 -Browser su PostgreSQL isolato.')
 page.on('dialog', dialog => dialog.accept())
 await page.goto('/')
 await page.getByLabel('Email', { exact: true }).fill(process.env.CONDOMANAGER_E2E_EMAIL!)
 await page.getByLabel('Password', { exact: true }).fill(process.env.CONDOMANAGER_E2E_PASSWORD!)
 await page.getByRole('button', { name: 'Accedi', exact: true }).click()
 await expect(page.getByRole('button', { name: 'Consuntivi', exact: true })).toBeVisible()
 // Prior stages use real authenticated APIs. Their UI paths have separate browser tests.
 const fixture = await page.evaluate(async () => {
  const { token } = await (await fetch('/api/auth/csrf')).json() as { token: string }
  async function post<T>(path: string, body: unknown, method = 'POST'): Promise<T> { const r = await fetch('/api' + path, { method, headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token }, body: JSON.stringify(body) }); if (!r.ok) throw new Error(await r.text()); return r.json() as Promise<T> }
  const condo = await post<{ id: string }>('/condominiums', { code: 'C-FINAL', name: 'Condominio consuntivo sintetico', address: 'Sintetico', postalCode: '00000', city: 'Test', province: 'XX', country: 'Test' })
  const units: string[] = [], people: string[] = []
  for (const code of ['A', 'B', 'C']) {
   const u = await post<{ id: string }>('/condominiums/' + condo.id + '/units', { code, kind: 'Apartment' }); units.push(u.id)
   const p = await post<{ id: string }>('/people', { code: 'FINAL-' + code, kind: 'NaturalPerson', firstName: 'Synthetic', lastNameOrCompany: 'Final ' + code, taxCode: 'SYNTHETIC-FINAL-' + code }); people.push(p.id)
   await post('/condominiums/' + condo.id + '/units/' + u.id + '/associations', { personId: p.id, role: 'Owner', validFrom: '2026-01-01', fraction: '1' })
  }
  const p = await post<{ id: string }>('/condominiums/' + condo.id + '/accounting-periods', { code: 'E-FINAL', name: 'Esercizio consuntivo sintetico', startsOn: '2026-01-01', endsOn: '2026-12-31' }); await post('/condominiums/' + condo.id + '/accounting-periods/' + p.id + '/open', {})
  const t = await post<{ id: string }>('/condominiums/' + condo.id + '/allocation-tables', { code: 'T-FINAL', name: 'Millesimi finali sintetici', denominator: '1000', validFrom: '2026-01-01' })
  for (let i = 0; i < 3; i++) await post('/condominiums/' + condo.id + '/allocation-tables/' + t.id + '/entries/' + units[i], { value: ['500', '300', '200'][i] }, 'PUT')
  await post('/condominiums/' + condo.id + '/allocation-tables/' + t.id + '/activate', {})
  const rule = await post<{ id: string }>('/allocations/condominiums/' + condo.id + '/rules', { name: 'Regola finale sintetica', type: 'Millesimal', components: [{ criterion: 'Millesimal', percentage: '100', tableId: t.id }] }); await post('/allocations/condominiums/' + condo.id + '/rules/' + rule.id + '/activate', {})
  const accounts = await (await fetch('/api/accounting/condominiums/' + condo.id + '/accounts')).json() as { id: string; code: string }[]; const account = accounts.find(a => a.code === '550')!.id
  const budget = await post<{ id: string }>('/budgets/condominiums/' + condo.id, { accountingPeriodId: p.id, description: 'Preventivo finale sintetico', lines: [{ accountId: account, description: 'Manutenzione preventiva', amount: '1000', ruleId: rule.id }] }); const bp = '/budgets/condominiums/' + condo.id + '/' + budget.id
  await post(bp + '/propose', {}); const bpreview = await post<{ hash: string }>(bp + '/preview', {}); await post(bp + '/approve', { previewHash: bpreview.hash })
  const finance = '/resident-finance/condominiums/' + condo.id
  const plan = await post<{ id: string; snapshot: { hash: string } }>(finance + '/plans', { budgetId: budget.id, debtorDate: '2026-01-01', mode: 'Equal', schedule: [{ dueDate: '2026-08-01', weight: '1' }], debtors: units.map((u, i) => ({ propertyUnitId: u, personId: people[i], share: '1' })), requestId: crypto.randomUUID() }); await post(finance + '/plans/' + plan.id + '/approve', { requestId: crypto.randomUUID(), date: '2026-01-01', previewHash: plan.snapshot.hash })
  const bank = await post<{ id: string }>('/accounting/condominiums/' + condo.id + '/financial-accounts', { code: 'FINAL-BANK', name: 'Banca finale sintetica', type: 'BankAccount', openingBalance: '0', accountingPeriodId: p.id, date: '2026-01-01', requestId: crypto.randomUUID() })
  const receipt = await post<{ id: string }>(finance + '/receipts', { requestId: crypto.randomUUID(), accountingPeriodId: p.id, financialAccountId: bank.id, paymentDate: '2026-08-01', amount: '400', debtorId: people[0], payerId: people[1] })
  const credits = await (await fetch('/api' + finance + '/credits')).json() as { id: string; propertyUnitId: string }[]; await post(finance + '/receipts/' + receipt.id + '/allocate', { requestId: crypto.randomUUID(), accountingPeriodId: p.id, date: '2026-08-01', allocations: [{ installmentId: credits.find(c => c.propertyUnitId === units[0])!.id, amount: '400' }] })
  const supplier = await post<{ id: string }>('/accounting/suppliers', { name: 'Fornitore finale sintetico' }); const e = await post<{ id: string }>('/accounting/condominiums/' + condo.id + '/expenses', { accountingPeriodId: p.id, supplierId: supplier.id, accountId: account, documentNumber: 'SYN-FINAL-1100', documentDate: '2026-08-01', description: 'Costo effettivo sintetico', netAmount: '1100', vatAmount: '0', grossAmount: '1100' }); await post('/accounting/condominiums/' + condo.id + '/expenses/' + e.id + '/register', { requestId: crypto.randomUUID() })
  const input = { accountingPeriodId: p.id, competenceFrom: '2026-01-01', competenceTo: '2026-12-31', sources: [{ expenseDocumentId: e.id, amount: '1100', ruleId: rule.id }] }; const allocation = await post<{ previewHash: string }>('/allocations/condominiums/' + condo.id + '/preview', input); await post('/allocations/condominiums/' + condo.id + '/confirm', { requestId: crypto.randomUUID(), previewHash: allocation.previewHash, input })
  return { condo: condo.id, period: p.id }
 })
 await page.reload()
 await page.getByRole('button', { name: 'Consuntivi', exact: true }).click()
 await page.getByRole('combobox', { name: 'Condominio', exact: true }).selectOption(fixture.condo)
 await page.getByRole('combobox', { name: 'Esercizio consuntivo', exact: true }).selectOption(fixture.period)
 await page.getByRole('button', { name: 'Saldi iniziali', exact: true }).click()
 await page.getByRole('button', { name: 'Verifica saldo iniziale', exact: true }).click()
 await page.getByLabel('Modalità saldo iniziale').selectOption('VerifiedZero')
 await page.getByLabel('Motivo verifica saldo iniziale').fill('Primo esercizio, zero verificato su dati sintetici')
 await page.getByRole('button', { name: 'Salva', exact: true }).click()
 await expect(page.getByText(/Modalità VerifiedZero/)).toBeVisible()
 await page.getByRole('button', { name: 'Consuntivi', exact: true }).last().click()
 await page.getByRole('button', { name: 'Nuovo consuntivo', exact: true }).click()
 await page.getByLabel('Descrizione consuntivo').fill('Consuntivo sintetico completo')
 await page.getByRole('button', { name: 'Salva', exact: true }).click()
 await expect(page.getByText(/Costi 1.100,00 € · quote 1.100,00 € · differenza 0,00 €/)).toBeVisible()
 await page.getByRole('button', { name: 'Conserva bozza consuntivo', exact: true }).click()
 await page.getByRole('button', { name: 'Proponi consuntivo', exact: true }).click()
 await page.getByRole('button', { name: 'Approva consuntivo', exact: true }).click()
 await expect(page.getByRole('heading', { name: 'Consuntivo v1 · Approved', exact: true })).toBeVisible()
 await page.getByRole('button', { name: 'Confronto bilanci', exact: true }).click()
 await expect(page.getByRole('cell', { name: '10%', exact: true })).toBeVisible()
 await page.getByRole('button', { name: 'Conguagli', exact: true }).click()
 await expect(page.getByRole('cell', { name: '150,00 €', exact: true })).toBeVisible()
 await page.getByRole('button', { name: 'Anteprima contabilizzazione', exact: true }).click()
 await page.getByRole('button', { name: 'Conferma snapshot conguagli', exact: true }).click()
 await page.getByRole('button', { name: 'Contabilizza conguagli', exact: true }).click()
 await expect(page.getByRole('cell', { name: 'Contabilizzato', exact: true })).toBeVisible()
 await page.getByRole('button', { name: 'Chiusura esercizio', exact: true }).click()
 await expect(page.getByText('Controllo contabile 700,00 € · posizioni individuali 700,00 €', { exact: true })).toBeVisible()
 await page.getByRole('button', { name: 'Chiudi esercizio verificato', exact: true }).click()
 await expect(page.getByRole('combobox', { name: 'Esercizio consuntivo', exact: true })).toContainText('Closed')
 await page.screenshot({ path: '../../../.artifacts/final-desktop.png', fullPage: true })
 await page.setViewportSize({ width: 390, height: 844 })
 await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
 await page.screenshot({ path: '../../../.artifacts/final-mobile.png', fullPage: true })
})
