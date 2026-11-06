export type LedgerAccount = { id: string; code: string; name: string; type: string; purpose: string; description: string | null; parentAccountId: string | null; isActive: boolean }
export type Supplier = { id: string; name: string; taxCode: string | null; vatNumber: string | null; email: string | null; phone: string | null; address: string | null; iban: string | null; notes: string | null; isActive: boolean }
export type FinancialAccount = { id: string; accountId: string; name: string; type: string; iban: string | null; openingBalance: string; balance: string; isActive: boolean }
export type Payment = { id: string; financialAccountId: string; journalEntryId: string; date: string; amount: string; reference: string | null; isReversed: boolean }
export type Expense = { id: string; accountingPeriodId: string; supplierId: string; accountId: string; documentNumber: string; documentDate: string; dueDate: string | null; description: string; netAmount: string; vatAmount: string; grossAmount: string; paidAmount: string; residual: string; status: string; notes: string | null; payments: Payment[] }
export type Journal = { id: string; accountingPeriodId: string; date: string; description: string; reference: string | null; kind: string; expenseDocumentId: string | null; reversesEntryId: string | null; reversedByEntryId: string | null; requestId: string; lines: { accountId: string; code: string; name: string; debit: string; credit: string }[] }
export type LedgerSummary = { expenses: string; payments: string; payables: string; availability: string; balances: { accountId: string; code: string; name: string; type: string; debit: string; credit: string; balance: string }[]; recentEntries: Journal[] }
// Decimal strings remain exact. Formatting uses strings, never binary floating-point arithmetic.
export function euro(value: string) {
  const [integer, fraction = ''] = value.split('.')
  return integer.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + fraction.padEnd(2, '0') + ' €'
}
