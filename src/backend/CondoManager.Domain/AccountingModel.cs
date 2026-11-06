namespace CondoManager.Domain;

public enum AccountType { Asset, Liability, Equity, Income, Expense }
public enum AccountPurpose { General, Payables, UnallocatedReceipts, OpeningEquity, ResidentReceivables, BudgetContributions }
public enum FinancialAccountType { BankAccount, Cash }
public enum ExpenseStatus { Draft, Registered, PartiallyPaid, Paid, Cancelled }
public enum JournalKind { Manual, Expense, Payment, Receipt, Opening, Reversal, Assessment, ReceiptAttribution, FinalAdjustment, ResidentOpening }
public static class Money
{
    public const decimal Maximum = 9999999999999999.99m;
    public static decimal Validate(decimal value, bool signed = false) => Guard.Decimal(value, 2, signed ? -Maximum : 0, Maximum);
    public static decimal Positive(decimal value) => Validate(value) > 0 ? value : throw new ArgumentException("Importo positivo obbligatorio.");
    public static decimal Balance(AccountType type, decimal debit, decimal credit) => type is AccountType.Asset or AccountType.Expense ? debit - credit : credit - debit;
}
public sealed class Account : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public AccountType Type { get; private set; }
    public AccountPurpose Purpose { get; private set; }
    public Guid? ParentAccountId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Account(Guid tenantId, Guid condominiumId, string code, string name, AccountType type, AccountPurpose purpose = AccountPurpose.General) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code).ToUpperInvariant(); Name = Guard.Text(name); Type = Guard.EnumValue(type); Purpose = Guard.EnumValue(purpose); }
    public void Update(string code, string name, string? description, Guid? parentAccountId, bool isActive)
    {
        if (parentAccountId == Id) throw new ArgumentException("Un conto non puo essere padre di se stesso.");
        if (!isActive && Purpose != AccountPurpose.General) throw new InvalidOperationException("I conti di controllo devono rimanere attivi.");
        Code = Guard.Text(code).ToUpperInvariant(); Name = Guard.Text(name); Description = Guard.Optional(description); ParentAccountId = parentAccountId; IsActive = isActive;
    }
    public static IReadOnlyList<Account> Defaults(Guid tenantId, Guid condoId)
    {
        var definitions = new (string Code, string Name, AccountType Type, AccountPurpose Purpose)[] {
            ("100", "Crediti verso condomini", AccountType.Asset, AccountPurpose.General),
            ("200", "Debiti verso fornitori", AccountType.Liability, AccountPurpose.Payables),
            ("210", "Incassi da attribuire", AccountType.Liability, AccountPurpose.UnallocatedReceipts),
            ("300", "Fondo di riserva", AccountType.Equity, AccountPurpose.General),
            ("310", "Saldi di apertura", AccountType.Equity, AccountPurpose.OpeningEquity),
            ("500", "Spese generali", AccountType.Expense, AccountPurpose.General),
            ("510", "Spese patrimoniali", AccountType.Expense, AccountPurpose.General),
            ("520", "Ascensore", AccountType.Expense, AccountPurpose.General),
            ("530", "Riscaldamento", AccountType.Expense, AccountPurpose.General),
            ("540", "Acqua", AccountType.Expense, AccountPurpose.General),
            ("550", "Manutenzioni ordinarie", AccountType.Expense, AccountPurpose.General),
            ("560", "Manutenzioni straordinarie", AccountType.Expense, AccountPurpose.General),
            ("570", "Spese bancarie", AccountType.Expense, AccountPurpose.General) };
        return definitions.Select(x => new Account(tenantId, condoId, x.Code, x.Name, x.Type, x.Purpose)).ToArray();
    }
}
public sealed class Supplier : TenantEntity
{
    public string Name { get; private set; } = "";
    public string? TaxCode { get; private set; }
    public string? VatNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? Iban { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Supplier(Guid tenantId) : base(tenantId) { }
    public void Update(string name, string? taxCode, string? vatNumber, string? email, string? phone, string? address, string? iban, string? notes, bool isActive)
    {
        Name = Guard.Text(name); TaxCode = Guard.Optional(taxCode,32)?.ToUpperInvariant(); VatNumber = Guard.Optional(vatNumber,32)?.ToUpperInvariant();
        Email = Guard.Optional(email,254); if (Email != null && !System.Net.Mail.MailAddress.TryCreate(Email, out _)) throw new ArgumentException("Email non valida.");
        Phone = Guard.Optional(phone,40); Address = Guard.Optional(address); Iban = Banking.Iban(iban); Notes = Guard.Optional(notes,4000); IsActive = isActive;
    }
}
public static class Banking
{
    public static string? Iban(string? value)
    {
        value = Guard.Optional(value,40)?.Replace(" ", "").ToUpperInvariant();
        if (value == null) return null;
        if (value.Length is < 15 or > 34 || !char.IsAsciiLetterUpper(value[0]) || !char.IsAsciiLetterUpper(value[1]) || !char.IsAsciiDigit(value[2]) || !char.IsAsciiDigit(value[3]) || value.Any(c => !char.IsAsciiLetterUpper(c) && !char.IsAsciiDigit(c))) throw new ArgumentException("IBAN non valido.");
        var remainder = 0;
        foreach (var c in value[4..] + value[..4]) remainder = char.IsAsciiDigit(c) ? (remainder * 10 + c - '0') % 97 : (remainder * 100 + c - 'A' + 10) % 97;
        return remainder == 1 ? value : throw new ArgumentException("Checksum IBAN non valido.");
    }
}
public sealed class FinancialAccount : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid AccountId { get; private set; }
    public string Name { get; private set; }
    public FinancialAccountType Type { get; private set; }
    public string? Iban { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public Guid CreationRequestId { get; private set; }
    public string CreationRequestHash { get; private set; } = "";
    public void SetCreationRequest(Guid requestId, string hash) { if (CreationRequestId != Guid.Empty) throw new InvalidOperationException("Creazione immutabile."); CreationRequestId=Guard.Id(requestId); CreationRequestHash=Guard.Text(hash,64); }
    public bool IsActive { get; private set; } = true;
    public FinancialAccount(Guid tenantId, Guid condominiumId, Guid accountId, string name, FinancialAccountType type, string? iban, decimal openingBalance) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); AccountId = Guard.Id(accountId); Name = Guard.Text(name); Type = Guard.EnumValue(type); OpeningBalance = Money.Validate(openingBalance,true); Set(name,iban,true); }
    public void Set(string name, string? iban, bool isActive)
    { Name = Guard.Text(name); Iban = Banking.Iban(iban); if (Type == FinancialAccountType.Cash && Iban != null) throw new ArgumentException("La cassa non ha IBAN."); IsActive = isActive; }
}
public sealed class ExpenseDocument : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid AccountingPeriodId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid AccountId { get; private set; }
    public string DocumentNumber { get; private set; } = "";
    public DateOnly DocumentDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string Description { get; private set; } = "";
    public decimal NetAmount { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal GrossAmount { get; private set; }
    public string? Notes { get; private set; }
    public bool IsRegistered { get; private set; }
    public bool IsCancelled { get; private set; }
    public ExpenseDocument(Guid tenantId, Guid condominiumId) : base(tenantId) { CondominiumId = Guard.Id(condominiumId); }
    public void Update(Guid periodId, Guid supplierId, Guid accountId, string number, DateOnly date, DateOnly? dueDate, string description, decimal net, decimal vat, decimal gross, string? notes)
    {
        EnsureDraft(); AccountingPeriodId = Guard.Id(periodId); SupplierId = Guard.Id(supplierId); AccountId = Guard.Id(accountId);
        Guard.Dates(date,dueDate); DocumentNumber = Guard.Text(number); DocumentDate = date; DueDate = dueDate; Description = Guard.Text(description);
        NetAmount = Money.Validate(net); VatAmount = Money.Validate(vat); GrossAmount = Money.Positive(gross);
        if (net + vat != gross) throw new ArgumentException("Imponibile piu IVA deve coincidere con il totale."); Notes = Guard.Optional(notes,4000);
    }
    public void EnsureDraft() { if (IsRegistered || IsCancelled) throw new InvalidOperationException("Documento contabilizzato o annullato immutabile."); }
    public void Register() { EnsureDraft(); IsRegistered = true; }
    public void Cancel() { if (IsCancelled) throw new InvalidOperationException("Documento gia annullato."); IsCancelled = true; }
    public decimal Residual(decimal paid) { if (!IsRegistered || IsCancelled) return 0; return GrossAmount - Money.Validate(paid); }
    public void ValidatePayment(decimal amount, decimal paid) { if (!IsRegistered || IsCancelled || Money.Positive(amount) > Residual(paid)) throw new InvalidOperationException("Pagamento non consentito o superiore al residuo."); }
    public ExpenseStatus Status(decimal paid) => IsCancelled ? ExpenseStatus.Cancelled : !IsRegistered ? ExpenseStatus.Draft : paid == 0 ? ExpenseStatus.Registered : paid == GrossAmount ? ExpenseStatus.Paid : ExpenseStatus.PartiallyPaid;
}
public sealed class JournalEntry : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid AccountingPeriodId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; }
    public string? Reference { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestHash { get; private set; }
    public JournalKind Kind { get; private set; }
    public Guid? ReversesEntryId { get; private set; }
    public Guid? ExpenseDocumentId { get; private set; }
    public bool IsPosted { get; private set; }
    private readonly List<JournalEntryLine> _lines = [];
    public IReadOnlyCollection<JournalEntryLine> Lines => _lines.AsReadOnly();
    private JournalEntry(Guid tenantId) : base(tenantId) { Description = ""; RequestHash = ""; }
    public JournalEntry(Guid tenantId, Guid condominiumId, AccountingPeriod period, DateOnly date, string description, string? reference, Guid requestId, string requestHash, JournalKind kind, Guid? expenseDocumentId = null, Guid? reversesEntryId = null) : base(tenantId)
    {
        if (period.TenantId != tenantId || period.CondominiumId != condominiumId) throw new ArgumentException("Esercizio estraneo al condominio.");
        EnsureOpen(period,date); CondominiumId = Guard.Id(condominiumId); AccountingPeriodId = period.Id; Date = date; Description = Guard.Text(description); Reference = Guard.Optional(reference);
        RequestId = Guard.Id(requestId); RequestHash = Guard.Text(requestHash,64); Kind = Guard.EnumValue(kind); ExpenseDocumentId = expenseDocumentId; ReversesEntryId = reversesEntryId;
    }
    public static void EnsureOpen(AccountingPeriod period, DateOnly date)
    { if (period.Status != AccountingPeriodStatus.Open || date < period.StartsOn || date > period.EndsOn) throw new InvalidOperationException("Registrare solo entro un esercizio aperto."); }
    public void AddLine(Account account, decimal debit, decimal credit)
    {
        if (IsPosted) throw new InvalidOperationException("Scrittura contabilizzata immutabile.");
        if (!account.IsActive || account.TenantId != TenantId || account.CondominiumId != CondominiumId) throw new ArgumentException("Conto inattivo o estraneo al condominio.");
        _lines.Add(new JournalEntryLine(TenantId,CondominiumId,Id,account.Id,debit,credit));
    }
    public void Post()
    {
        if (IsPosted) throw new InvalidOperationException("Scrittura gia contabilizzata.");
        if (_lines.Count < 2 || _lines.Sum(x => x.Debit) != _lines.Sum(x => x.Credit)) throw new InvalidOperationException("Scrittura sbilanciata: almeno due righe e Dare uguale Avere.");
        IsPosted = true;
    }
}
public sealed class JournalEntryLine : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public JournalEntryLine(Guid tenantId, Guid condominiumId, Guid journalEntryId, Guid accountId, decimal debit, decimal credit) : base(tenantId)
    {
        CondominiumId = Guard.Id(condominiumId); JournalEntryId = Guard.Id(journalEntryId); AccountId = Guard.Id(accountId); Debit = Money.Validate(debit); Credit = Money.Validate(credit);
        if ((debit > 0) == (credit > 0)) throw new ArgumentException("Ogni riga deve avere solo Dare o solo Avere positivo.");
    }
}
public sealed class ExpensePayment : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid ExpenseDocumentId { get; private set; }
    public Guid FinancialAccountId { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public decimal Amount { get; private set; }
    public ExpensePayment(Guid tenantId, Guid condominiumId, Guid expenseDocumentId, Guid financialAccountId, Guid journalEntryId, decimal amount) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); ExpenseDocumentId = Guard.Id(expenseDocumentId); FinancialAccountId = Guard.Id(financialAccountId); JournalEntryId = Guard.Id(journalEntryId); Amount = Money.Positive(amount); }
}
