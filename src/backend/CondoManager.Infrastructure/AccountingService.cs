using System.Security.Cryptography;
using System.Text.Json;
using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;

public sealed partial class AccountingService(AppDbContext db, ICurrentTenant current, AllocationService? finalResolver=null) : IAccountingService
{
    private Guid TenantId => current.RequireRead();
    private async Task Condo(Guid id, CancellationToken ct)
    { _ = TenantId; if (!await db.Condominiums.AnyAsync(x => x.Id == id,ct)) throw new NotFoundException(); }
    private async Task<T> Write<T>(Guid? condo, Func<Task<T>> action, CancellationToken ct)
    {
        current.RequireWrite();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (condo.HasValue)
        {
            await Condo(condo.Value,ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Condominiums\" WHERE \"TenantId\"={TenantId} AND \"Id\"={condo.Value} FOR UPDATE",ct);
            if (!await db.Condominiums.AnyAsync(x => x.Id == condo && x.IsActive,ct)) throw new InvalidOperationException("Condominio inattivo.");
        }
        else // Serialize tenant-wide supplier edits with accounting operations' FK locks.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Tenants\" WHERE \"Id\"={TenantId} FOR UPDATE",ct);
        var result = await action(); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return result;
    }
    private void Audit(string name, Guid subject) => db.SecurityAudits.Add(new SecurityAudit { Id=Guid.NewGuid(),TenantId=TenantId,ActorUserId=current.UserId,SubjectId=subject,Event=name,Timestamp=DateTimeOffset.UtcNow });
    private async Task<AccountingPeriod> Period(Guid condo, Guid id, DateOnly date, CancellationToken ct)
    {
        var row = await db.AccountingPeriods.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condo,ct) ?? throw new NotFoundException();
        JournalEntry.EnsureOpen(row,date); return row;
    }
    private async Task<Account> LedgerAccount(Guid condo, Guid id, CancellationToken ct)
    { return await db.Accounts.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condo && x.IsActive,ct) ?? throw new NotFoundException(); }
    private async Task<Account> Control(Guid condo, AccountPurpose purpose, CancellationToken ct) => await db.Accounts.SingleOrDefaultAsync(x => x.CondominiumId == condo && x.Purpose == purpose && x.IsActive,ct) ?? throw new InvalidOperationException("Inizializzare il piano dei conti.");
    private async Task<ExpenseDocument> Expense(Guid condo, Guid id, CancellationToken ct) => await db.ExpenseDocuments.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condo,ct) ?? throw new NotFoundException();
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private async Task<JournalEntry?> Retry(Guid condo, Guid requestId, string hash, CancellationToken ct)
    {
        Guard.Id(requestId);
        var row = await db.JournalEntries.Include(x => x.Lines).SingleOrDefaultAsync(x => x.CondominiumId == condo && x.RequestId == requestId,ct);
        if (row != null && row.RequestHash != hash) throw new InvalidOperationException("Chiave di idempotenza gia usata con contenuto diverso."); return row;
    }
    private async Task<JournalDto> Persist(JournalEntry row, CancellationToken ct)
    { row.Post(); db.Add(row); Audit("Accounting."+row.Kind+"Posted",row.Id); await db.SaveChangesAsync(ct); return await JournalDtoFor(row,ct); }
    public async Task<IReadOnlyList<AccountDto>> Accounts(Guid condo, CancellationToken ct)
    { await Condo(condo,ct); return (await db.Accounts.Where(x => x.CondominiumId == condo).OrderBy(x => x.Code).ToListAsync(ct)).Select(Dto).ToArray(); }
    public Task<IReadOnlyList<AccountDto>> InitializeChart(Guid condo, CancellationToken ct) => Write(condo,async () => {
        var existing = await db.Accounts.Where(x => x.CondominiumId == condo).ToListAsync(ct);
        foreach (var row in Account.Defaults(TenantId,condo))
        {
            if (row.Purpose != AccountPurpose.General && existing.Any(x => x.Purpose == row.Purpose)) continue;
            if (existing.Any(x => x.Code == row.Code)) { if (row.Purpose != AccountPurpose.General) throw new InvalidOperationException("Codice del conto di controllo occupato: liberarlo prima di inizializzare."); continue; }
            db.Add(row); existing.Add(row);
        }
        Audit("Accounting.ChartInitialized",condo); return (IReadOnlyList<AccountDto>)existing.OrderBy(x => x.Code).Select(Dto).ToArray();
    },ct);
    public Task<AccountDto> SaveAccount(Guid condo, Guid? id, AccountInput input, CancellationToken ct) => Write(condo,async () => {
        var row = id.HasValue ? await db.Accounts.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condo,ct) ?? throw new NotFoundException() : new Account(TenantId,condo,input.Code,input.Name,input.Type);
        if (row.Type != input.Type) throw new InvalidOperationException("La tipologia di un conto esistente e immutabile.");
        var parent = input.ParentAccountId; var visited = new HashSet<Guid> { row.Id };
        while (parent != null)
        { if (!visited.Add(parent.Value)) throw new ArgumentException("Ciclo nella gerarchia contabile."); var ancestor=await LedgerAccount(condo,parent.Value,ct); if (ancestor.Type!=row.Type) throw new ArgumentException("Padre e figlio devono avere la stessa tipologia."); parent=ancestor.ParentAccountId; }
        if (!input.IsActive && (await db.Accounts.AnyAsync(x => x.ParentAccountId == row.Id && x.IsActive,ct) || await db.FinancialAccounts.AnyAsync(x => x.AccountId == row.Id && x.IsActive,ct))) throw new InvalidOperationException("Disattivare prima conti figli e conto finanziario.");
        row.Update(input.Code,input.Name,input.Description,input.ParentAccountId,input.IsActive); if (!id.HasValue) db.Add(row); Audit("Accounting.AccountSaved",row.Id); return Dto(row);
    },ct);
    public async Task<IReadOnlyList<SupplierDto>> Suppliers(string? search, CancellationToken ct)
    { _=TenantId; var query=db.Suppliers.AsQueryable(); if(!string.IsNullOrWhiteSpace(search)) query=query.Where(x=>x.Name.ToLower().Contains(search.Trim().ToLower())); return (await query.OrderBy(x=>x.Name).ToListAsync(ct)).Select(Dto).ToArray(); }
    public Task<SupplierDto> SaveSupplier(Guid? id, SupplierInput input, CancellationToken ct) => Write<SupplierDto>(null,async () => {
        var row=id.HasValue ? await db.Suppliers.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new NotFoundException() : new Supplier(TenantId);
        row.Update(input.Name,input.TaxCode,input.VatNumber,input.Email,input.Phone,input.Address,input.Iban,input.Notes,input.IsActive); if(!id.HasValue) db.Add(row); Audit("Accounting.SupplierSaved",row.Id); return Dto(row);
    },ct);
    public async Task DeactivateSupplier(Guid id, CancellationToken ct) => await Write<object?>(null,async()=> {
        var row=await db.Suppliers.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new NotFoundException();
        row.Update(row.Name,row.TaxCode,row.VatNumber,row.Email,row.Phone,row.Address,row.Iban,row.Notes,false); Audit("Accounting.SupplierDeactivated",id); return null;
    },ct);
    public async Task<IReadOnlyList<ExpenseDto>> Expenses(Guid condo, Guid? period, CancellationToken ct)
    { await Condo(condo,ct); var query=db.ExpenseDocuments.Where(x=>x.CondominiumId==condo); if(period.HasValue) query=query.Where(x=>x.AccountingPeriodId==period); var result=new List<ExpenseDto>(); foreach(var row in await query.OrderByDescending(x=>x.DocumentDate).ToListAsync(ct)) result.Add(await ExpenseDtoFor(row,ct)); return result; }
    public Task<ExpenseDto> SaveExpense(Guid condo, Guid? id, ExpenseInput input, CancellationToken ct) => Write(condo,async () => {
        await Period(condo,input.AccountingPeriodId,input.DocumentDate,ct);
        if(!await db.Suppliers.AnyAsync(x=>x.Id==input.SupplierId && x.IsActive,ct)) throw new NotFoundException();
        var account=await LedgerAccount(condo,input.AccountId,ct); if(account.Type!=AccountType.Expense) throw new ArgumentException("Selezionare un conto di spesa.");
        var row=id.HasValue ? await Expense(condo,id.Value,ct) : new ExpenseDocument(TenantId,condo);
        if(id.HasValue) await Period(condo,row.AccountingPeriodId,row.DocumentDate,ct);
        row.Update(input.AccountingPeriodId,input.SupplierId,input.AccountId,input.DocumentNumber,input.DocumentDate,input.DueDate,input.Description,input.NetAmount,input.VatAmount,input.GrossAmount,input.Notes);
        if(!id.HasValue) db.Add(row); Audit("Accounting.ExpenseDraftSaved",row.Id); await db.SaveChangesAsync(ct); return await ExpenseDtoFor(row,ct);
    },ct);
    public Task<JournalDto> RegisterExpense(Guid condo, Guid id, PostingInput input, CancellationToken ct) => Write(condo,async () => {
        var hash=Hash(new { Operation="Expense", Id=id }); var retry=await Retry(condo,input.RequestId,hash,ct); if(retry!=null) return await JournalDtoFor(retry,ct);
        var row=await Expense(condo,id,ct); row.EnsureDraft(); var period=await Period(condo,row.AccountingPeriodId,row.DocumentDate,ct);
        var journal=new JournalEntry(TenantId,condo,period,row.DocumentDate,row.Description,row.DocumentNumber,input.RequestId,hash,JournalKind.Expense,row.Id);
        journal.AddLine(await LedgerAccount(condo,row.AccountId,ct),row.GrossAmount,0); journal.AddLine(await Control(condo,AccountPurpose.Payables,ct),0,row.GrossAmount); row.Register(); return await Persist(journal,ct);
    },ct);
    public async Task CancelDraft(Guid condo, Guid id, CancellationToken ct) => await Write<object?>(condo,async () => { var row=await Expense(condo,id,ct); row.EnsureDraft(); await Period(condo,row.AccountingPeriodId,row.DocumentDate,ct); row.Cancel(); Audit("Accounting.ExpenseDraftCancelled",id); return null; },ct);
    public Task<JournalDto> Pay(Guid condo, Guid id, PaymentInput input, CancellationToken ct) => Write(condo,async () => {
        var hash=Hash(new {Operation="Payment",Id=id,Input=input}); var retry=await Retry(condo,input.RequestId,hash,ct); if(retry!=null) return await JournalDtoFor(retry,ct);
        var doc=await Expense(condo,id,ct); var detail=await ExpenseDtoFor(doc,ct); doc.ValidatePayment(input.Amount,detail.PaidAmount);
        var period=await Period(condo,input.AccountingPeriodId,input.Date,ct); var financial=await Financial(condo,input.FinancialAccountId,ct);
        if(input.Date<doc.DocumentDate) throw new ArgumentException("Pagamento precedente al documento.");
        var journal=new JournalEntry(TenantId,condo,period,input.Date,"Pagamento "+doc.DocumentNumber,input.Reference,input.RequestId,hash,JournalKind.Payment,id);
        journal.AddLine(await Control(condo,AccountPurpose.Payables,ct),input.Amount,0); journal.AddLine(await LedgerAccount(condo,financial.AccountId,ct),0,input.Amount);
        db.Add(new ExpensePayment(TenantId,condo,id,financial.Id,journal.Id,input.Amount)); return await Persist(journal,ct);
    },ct);
    private async Task<FinancialAccount> Financial(Guid condo, Guid id, CancellationToken ct) => await db.FinancialAccounts.SingleOrDefaultAsync(x=>x.CondominiumId==condo && x.Id==id && x.IsActive,ct) ?? throw new NotFoundException();
    public async Task<IReadOnlyList<FinancialAccountDto>> FinancialAccounts(Guid condo, CancellationToken ct)
    { await Condo(condo,ct); var rows=await db.FinancialAccounts.Where(x=>x.CondominiumId==condo).OrderBy(x=>x.Name).ToListAsync(ct); var result=new List<FinancialAccountDto>(); foreach(var row in rows) result.Add(await FinancialDtoFor(row,ct)); return result; }
    public Task<FinancialAccountDto> CreateFinancialAccount(Guid condo, FinancialAccountInput input, CancellationToken ct) => Write(condo,async () => {
        Guard.Id(input.RequestId); var hash=Hash(new {Operation="Opening",Input=input});
        var existing=await db.FinancialAccounts.SingleOrDefaultAsync(x=>x.CondominiumId==condo && x.CreationRequestId==input.RequestId,ct);
        if(existing!=null) { if(existing.CreationRequestHash!=hash) throw new InvalidOperationException("Chiave di idempotenza gia utilizzata."); return await FinancialDtoFor(existing,ct); }
        var period=await Period(condo,input.AccountingPeriodId,input.Date,ct);
        var account=new Account(TenantId,condo,input.Code,input.Name,AccountType.Asset); db.Add(account);
        var financial=new FinancialAccount(TenantId,condo,account.Id,input.Name,input.Type,input.Iban,input.OpeningBalance); financial.SetCreationRequest(input.RequestId,hash); db.Add(financial);
        // Zero openings have no artificial zero-valued journal: identity is the unique ledger code/name.
        if(input.OpeningBalance!=0)
        {
            var offset=await Control(condo,AccountPurpose.OpeningEquity,ct); var amount=Math.Abs(input.OpeningBalance);
            var journal=new JournalEntry(TenantId,condo,period,input.Date,"Saldo iniziale "+input.Name,null,input.RequestId,hash,JournalKind.Opening);
            journal.AddLine(account,input.OpeningBalance>0?amount:0,input.OpeningBalance<0?amount:0); journal.AddLine(offset,input.OpeningBalance<0?amount:0,input.OpeningBalance>0?amount:0); await Persist(journal,ct);
        }
        Audit("Accounting.FinancialAccountCreated",financial.Id); await db.SaveChangesAsync(ct); return await FinancialDtoFor(financial,ct);
    },ct);
    public Task<FinancialAccountDto> UpdateFinancialAccount(Guid condo, Guid id, FinancialAccountUpdate input, CancellationToken ct) => Write(condo,async()=>{
        var row=await db.FinancialAccounts.SingleOrDefaultAsync(x=>x.Id==id && x.CondominiumId==condo,ct) ?? throw new NotFoundException(); row.Set(input.Name,input.Iban,input.IsActive); Audit("Accounting.FinancialAccountSaved",id); return await FinancialDtoFor(row,ct);
    },ct);
    public Task<JournalDto> Receipt(Guid condo, ReceiptInput input, CancellationToken ct) => Write(condo,async()=>{
        var hash=Hash(new {Operation="Receipt",Input=input}); var retry=await Retry(condo,input.RequestId,hash,ct); if(retry!=null) return await JournalDtoFor(retry,ct);
        Money.Positive(input.Amount); var period=await Period(condo,input.AccountingPeriodId,input.Date,ct); var financial=await Financial(condo,input.FinancialAccountId,ct);
        var journal=new JournalEntry(TenantId,condo,period,input.Date,input.Description,input.Reference,input.RequestId,hash,JournalKind.Receipt);
        journal.AddLine(await LedgerAccount(condo,financial.AccountId,ct),input.Amount,0); journal.AddLine(await Control(condo,AccountPurpose.UnallocatedReceipts,ct),0,input.Amount); return await Persist(journal,ct);
    },ct);
    public async Task<IReadOnlyList<JournalDto>> Journals(Guid condo, Guid? period, Guid? account, DateOnly? from, DateOnly? to, string? search, CancellationToken ct)
    {
        await Condo(condo,ct); var query=db.JournalEntries.Include(x=>x.Lines).Where(x=>x.CondominiumId==condo);
        if(period.HasValue) query=query.Where(x=>x.AccountingPeriodId==period); if(account.HasValue) query=query.Where(x=>x.Lines.Any(l=>l.AccountId==account));
        if(from.HasValue) query=query.Where(x=>x.Date>=from); if(to.HasValue) query=query.Where(x=>x.Date<=to); if(!string.IsNullOrWhiteSpace(search)) query=query.Where(x=>x.Description.ToLower().Contains(search.ToLower()));
        var result=new List<JournalDto>(); foreach(var row in await query.OrderByDescending(x=>x.Date).ThenByDescending(x=>x.CreatedAt).ToListAsync(ct)) result.Add(await JournalDtoFor(row,ct)); return result;
    }
    public Task<JournalDto> Post(Guid condo, JournalInput input, CancellationToken ct) => Write(condo,async()=>{
        var hash=Hash(new {Operation="Manual",Input=input}); var retry=await Retry(condo,input.RequestId,hash,ct); if(retry!=null) return await JournalDtoFor(retry,ct);
        if(input.Lines==null || input.Lines.Count>200) throw new ArgumentException("Da 2 a 200 righe richieste.");
        var period=await Period(condo,input.AccountingPeriodId,input.Date,ct); var journal=new JournalEntry(TenantId,condo,period,input.Date,input.Description,input.Reference,input.RequestId,hash,JournalKind.Manual);
        foreach(var line in input.Lines) {
            var account=await LedgerAccount(condo,line.AccountId,ct);
            if(account.Purpose is AccountPurpose.Payables or AccountPurpose.UnallocatedReceipts or AccountPurpose.ResidentReceivables or AccountPurpose.BudgetContributions) throw new InvalidOperationException("Usare i flussi documenti, pagamenti o incassi per i conti di controllo.");
            if(account.Purpose == AccountPurpose.OpeningEquity) current.RequireAdmin();
            if(await db.FinancialAccounts.AnyAsync(x=>x.AccountId==account.Id && !x.IsActive,ct)) throw new InvalidOperationException("Conto finanziario inattivo.");
            journal.AddLine(account,line.Debit,line.Credit);
        }
        return await Persist(journal,ct);
    },ct);
    public Task<JournalDto> Reverse(Guid condo, Guid id, ReversalInput input, CancellationToken ct) => Write(condo,async()=>{
        // Reversals require StudioAdmin: operational posting is also available to Collaborators.
        current.RequireAdmin(); var hash=Hash(new {Operation="Reversal",Id=id,Input=input}); var retry=await Retry(condo,input.RequestId,hash,ct); if(retry!=null) return await JournalDtoFor(retry,ct);
        var original=await db.JournalEntries.Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==id && x.CondominiumId==condo,ct) ?? throw new NotFoundException();
        if(original.Kind is JournalKind.Assessment or JournalKind.ReceiptAttribution or JournalKind.FinalAdjustment or JournalKind.ResidentOpening || await db.ReceiptBankLinks.AnyAsync(l=>l.JournalEntryId==id,ct)) throw new InvalidOperationException("Usare le rettifiche del modulo rate/incassi; non stornare direttamente i movimenti collegati.");
        if(original.Kind is JournalKind.Reversal || await db.JournalEntries.AnyAsync(x=>x.ReversesEntryId==id,ct)) throw new InvalidOperationException("Scrittura non stornabile o gia stornata.");
        if(input.Date<original.Date) throw new ArgumentException("Storno precedente alla registrazione.");
        if(original.Kind==JournalKind.Expense && await db.AdministrativeClosures.AnyAsync(c=>c.CondominiumId==condo && c.AccountingPeriodId==original.AccountingPeriodId,ct)) throw new InvalidOperationException("Costo di un esercizio chiuso: rettifica amministrativa successiva non automatizzata.");
        if(original.Kind==JournalKind.Expense && original.ExpenseDocumentId.HasValue)
        {
            var doc=await Expense(condo,original.ExpenseDocumentId.Value,ct); var detail=await ExpenseDtoFor(doc,ct); if(detail.PaidAmount!=0) throw new InvalidOperationException("Stornare prima i pagamenti."); doc.Cancel();
        }
        var period=await Period(condo,input.AccountingPeriodId,input.Date,ct); var journal=new JournalEntry(TenantId,condo,period,input.Date,Guard.Text(input.Reason),original.Reference,input.RequestId,hash,JournalKind.Reversal,original.ExpenseDocumentId,id);
        foreach(var line in original.Lines) { var account=await db.Accounts.SingleAsync(x=>x.Id==line.AccountId && x.CondominiumId==condo,ct); if(!account.IsActive) throw new InvalidOperationException("Riattivare i conti originali prima dello storno."); journal.AddLine(account,line.Credit,line.Debit); }
        return await Persist(journal,ct);
    },ct);
    public async Task<LedgerSummary> Summary(Guid condo, Guid? period, CancellationToken ct)
    {
        await Condo(condo,ct); if(period.HasValue && !await db.AccountingPeriods.AnyAsync(x=>x.Id==period && x.CondominiumId==condo,ct)) throw new NotFoundException();
        var accounts=await db.Accounts.Where(x=>x.CondominiumId==condo).OrderBy(x=>x.Code).ToListAsync(ct);
        var lines=db.JournalEntryLines.Where(x=>x.CondominiumId==condo); if(period.HasValue) lines=lines.Where(l=>db.JournalEntries.Any(j=>j.Id==l.JournalEntryId && j.AccountingPeriodId==period));
        var amounts=await lines.GroupBy(x=>x.AccountId).Select(g=>new {Id=g.Key,Debit=g.Sum(x=>x.Debit),Credit=g.Sum(x=>x.Credit)}).ToDictionaryAsync(x=>x.Id,ct);
        var balances=accounts.Select(a=>{ amounts.TryGetValue(a.Id,out var v); var debit=v?.Debit??0;var credit=v?.Credit??0;return new BalanceDto(a.Id,a.Code,a.Name,a.Type,debit,credit,Money.Balance(a.Type,debit,credit)); }).ToArray();
        var journals=await Journals(condo,period,null,null,null,null,ct);
        var expense=journals.Where(x=>x.Kind==JournalKind.Expense).Sum(x=>x.Lines.Sum(l=>l.Debit));
        var payments=journals.Where(x=>x.Kind==JournalKind.Payment).Sum(x=>x.Lines.Sum(l=>l.Debit));
        foreach(var reversal in journals.Where(x=>x.Kind==JournalKind.Reversal)) {
            var original=await db.JournalEntries.SingleAsync(x=>x.Id==reversal.ReversesEntryId,ct);
            if(original.Kind==JournalKind.Expense) expense-=reversal.Lines.Sum(l=>l.Debit); if(original.Kind==JournalKind.Payment) payments-=reversal.Lines.Sum(l=>l.Debit);
        }
        var controlIds=accounts.Where(x=>x.Purpose==AccountPurpose.Payables).Select(x=>x.Id).ToHashSet();
        var financialIds=await db.FinancialAccounts.Where(x=>x.CondominiumId==condo).Select(x=>x.AccountId).ToListAsync(ct);
        // Period summary shows movements for that period; availability is explicitly all-time/current.
        var availability=await db.JournalEntryLines.Where(x=>x.CondominiumId==condo && financialIds.Contains(x.AccountId)).SumAsync(x=>x.Debit-x.Credit,ct);
        return new(expense,payments,balances.Where(x=>controlIds.Contains(x.AccountId)).Sum(x=>x.Balance),availability,balances,journals.Take(10).ToArray());
    }
    private async Task<ExpenseDto> ExpenseDtoFor(ExpenseDocument row, CancellationToken ct)
    {
        var raw=await (from p in db.ExpensePayments join j in db.JournalEntries on p.JournalEntryId equals j.Id where p.ExpenseDocumentId==row.Id select new {Payment=p,Journal=j,Reversed=db.JournalEntries.Any(r=>r.ReversesEntryId==j.Id)}).ToListAsync(ct);
        var payments=raw.Select(x=>new PaymentDto(x.Payment.Id,x.Payment.FinancialAccountId,x.Journal.Id,x.Journal.Date,x.Payment.Amount,x.Journal.Reference,x.Reversed)).ToArray(); var paid=payments.Where(x=>!x.IsReversed).Sum(x=>x.Amount);
        return new(row.Id,row.AccountingPeriodId,row.SupplierId,row.AccountId,row.DocumentNumber,row.DocumentDate,row.DueDate,row.Description,row.NetAmount,row.VatAmount,row.GrossAmount,paid,row.Residual(paid),row.Status(paid),row.Notes,payments);
    }
    private async Task<JournalDto> JournalDtoFor(JournalEntry row, CancellationToken ct)
    {
        var ids=row.Lines.Select(x=>x.AccountId).ToArray(); var accounts=await db.Accounts.Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,ct);
        var reversed=await db.JournalEntries.Where(x=>x.ReversesEntryId==row.Id).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
        return new(row.Id,row.AccountingPeriodId,row.Date,row.Description,row.Reference,row.Kind,row.ExpenseDocumentId,row.ReversesEntryId,reversed,row.RequestId,row.Lines.Select(l=>new JournalLineDto(l.AccountId,accounts[l.AccountId].Code,accounts[l.AccountId].Name,l.Debit,l.Credit)).ToArray());
    }
    private async Task<FinancialAccountDto> FinancialDtoFor(FinancialAccount row, CancellationToken ct) => new(row.Id,row.AccountId,row.Name,row.Type,row.Iban,row.OpeningBalance,await db.JournalEntryLines.Where(x=>x.AccountId==row.AccountId).SumAsync(x=>x.Debit-x.Credit,ct),row.IsActive);
    private static AccountDto Dto(Account x)=>new(x.Id,x.Code,x.Name,x.Type,x.Purpose,x.Description,x.ParentAccountId,x.IsActive);
    private static SupplierDto Dto(Supplier x)=>new(x.Id,x.Name,x.TaxCode,x.VatNumber,x.Email,x.Phone,x.Address,x.Iban,x.Notes,x.IsActive);
}
