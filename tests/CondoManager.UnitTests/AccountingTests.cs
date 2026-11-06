using CondoManager.Domain;
namespace CondoManager.UnitTests;
public class AccountingTests
{
    private static readonly Guid Tenant = Guid.NewGuid(), Condo = Guid.NewGuid();
    private static AccountingPeriod OpenPeriod() { var p=new AccountingPeriod(Tenant,Condo,"E",new(2026,7,1),new(2027,6,30));p.Open();return p; }
    private static JournalEntry Journal(AccountingPeriod? period=null) => new(Tenant,Condo,period??OpenPeriod(),new(2026,8,1),"Synthetic posting",null,Guid.NewGuid(),"hash",JournalKind.Manual);
    [Fact] public void DefaultsHaveUniqueCodesAndExplicitControlAccounts()
    { var chart=Account.Defaults(Tenant,Condo);Assert.Equal(chart.Count,chart.Select(x=>x.Code).Distinct().Count());Assert.Equal(3,chart.Count(x=>x.Purpose!=AccountPurpose.General)); }
    [Theory] [InlineData("100","22","122")] [InlineData("122","0","122")]
    public void ExpenseAcceptsExactGrossAndZeroVat(string net,string vat,string gross)
    { var d=new ExpenseDocument(Tenant,Condo);d.Update(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"TEST",new(2026,8,1),null,"Synthetic",decimal.Parse(net),decimal.Parse(vat),decimal.Parse(gross),null);d.Register(); Assert.Equal(ExpenseStatus.Registered,d.Status(0));Assert.Throws<InvalidOperationException>(()=>d.EnsureDraft()); }
    [Fact] public void InvalidAmountsAndPrecisionRejected()
    { Assert.Throws<ArgumentOutOfRangeException>(()=>Money.Validate(1.001m));Assert.Throws<ArgumentOutOfRangeException>(()=>Money.Validate(-1));var d=new ExpenseDocument(Tenant,Condo);Assert.Throws<ArgumentException>(()=>d.Update(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"T",new(2026,8,1),null,"Synthetic",100,22,121,null)); }
    [Fact] public void BalancedJournalBecomesImmutable()
    { var j=Journal();j.AddLine(new(Tenant,Condo,"E","Expense",AccountType.Expense),122,0);j.AddLine(new(Tenant,Condo,"L","Liability",AccountType.Liability),0,122);j.Post();Assert.True(j.IsPosted);Assert.Throws<InvalidOperationException>(()=>j.AddLine(new(Tenant,Condo,"A","Asset",AccountType.Asset),1,0)); }
    [Fact] public void UnbalancedAndInvalidLinesRejected()
    { var j=Journal();j.AddLine(new(Tenant,Condo,"A","Asset",AccountType.Asset),1,0);Assert.Throws<InvalidOperationException>(j.Post);Assert.Throws<ArgumentException>(()=>j.AddLine(new(Tenant,Condo,"B","Asset",AccountType.Asset),1,1)); }
    [Fact] public void ClosedPeriodAndCrossCondominiumAccountRejected()
    { var p=OpenPeriod();p.Close();Assert.Throws<InvalidOperationException>(()=>Journal(p));var j=Journal();Assert.Throws<ArgumentException>(()=>j.AddLine(new(Tenant,Guid.NewGuid(),"A","Other",AccountType.Asset),1,0));Assert.Throws<ArgumentException>(()=>j.AddLine(new(Guid.NewGuid(),Condo,"A","Other",AccountType.Asset),1,0)); }
    [Fact] public void PartialFullAndExcessPayments()
    { var d=new ExpenseDocument(Tenant,Condo);d.Update(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"T",new(2026,8,1),null,"Synthetic",100,22,122,null);Assert.Throws<InvalidOperationException>(()=>d.ValidatePayment(1,0));d.Register();d.ValidatePayment(22,0);Assert.Equal(100,d.Residual(22));Assert.Equal(ExpenseStatus.PartiallyPaid,d.Status(22));Assert.Throws<InvalidOperationException>(()=>d.ValidatePayment(101,22));d.ValidatePayment(100,22);Assert.Equal(ExpenseStatus.Paid,d.Status(122)); }
    [Theory] [InlineData(AccountType.Asset,80)] [InlineData(AccountType.Expense,80)] [InlineData(AccountType.Liability,-80)] [InlineData(AccountType.Equity,-80)] [InlineData(AccountType.Income,-80)]
    public void NormalBalanceConvention(AccountType type,int expected) => Assert.Equal(expected,Money.Balance(type,100,20));
}
