using CondoManager.Application;
using CondoManager.Domain;
namespace CondoManager.UnitTests;
public class BudgetTests
{
    private static readonly Guid Tenant=Guid.NewGuid(),Condo=Guid.NewGuid();
    private static AccountingPeriod Period() {var p=new AccountingPeriod(Tenant,Condo,"E",new(2026,1,1),new(2026,12,31));p.Open();return p;}
    [Fact] public void BudgetApprovalPreservesVersionAndIsImmutable()
    {var b=new Budget(Tenant,Condo,Period(),"Synthetic budget");b.Propose();Assert.Throws<InvalidOperationException>(()=>b.Update("Changed",null));b.Approve(Guid.NewGuid(),"{}","Administrative reference");Assert.NotNull(b.ApprovedAt);Assert.Throws<InvalidOperationException>(()=>b.Approve(Guid.NewGuid(),"{}",null));b.Supersede();Assert.Equal(BudgetStatus.Superseded,b.Status);}
    [Fact] public void CrossTenantCondominiumAndClosedPeriodRejected()
    {var p=Period();Assert.Throws<ArgumentException>(()=>new Budget(Guid.NewGuid(),Condo,p,"Invalid"));Assert.Throws<ArgumentException>(()=>new Budget(Tenant,Guid.NewGuid(),p,"Invalid"));p.Close();Assert.Throws<InvalidOperationException>(()=>new Budget(Tenant,Condo,p,"Invalid"));}
    private static InstallmentSchedule[] Schedule(params decimal[] weights)=>weights.Select((w,i)=>new InstallmentSchedule(new(2026,i+1,10),w)).ToArray();
    [Fact] public void ReferenceAIndividualInstallmentsReconcile()
    {var q=new[]{6000m,3600m,2400m}.Select(a=>new IndividualQuota(Guid.NewGuid(),Guid.NewGuid(),a)).ToArray();var rows=InstallmentCalculator.Calculate(q,Schedule(25,25,25,25));Assert.Equal(12000,rows.Sum(r=>r.Amount));foreach(var quota in q)Assert.All(rows.Where(r=>r.PropertyUnitId==quota.PropertyUnitId),r=>Assert.Equal(quota.Amount/4,r.Amount));Assert.All(rows.GroupBy(r=>r.Position),g=>Assert.Equal(3000,g.Sum(r=>r.Amount)));}
    [Theory] [InlineData("100.00","33.34","33.33","33.33")] [InlineData("0.01","0.01","0.00","0.00")]
    public void CentsAreDeterministic(string amount,string first,string second,string third)
    {var q=new IndividualQuota(Guid.NewGuid(),Guid.NewGuid(),decimal.Parse(amount,System.Globalization.CultureInfo.InvariantCulture));var schedule=Schedule(1,1,1);var a=InstallmentCalculator.Calculate([q],schedule,false);Assert.Equal(new[]{first,second,third}.Select(x=>decimal.Parse(x,System.Globalization.CultureInfo.InvariantCulture)),a.Select(r=>r.Amount));Assert.Equal(q.Amount,a.Sum(r=>r.Amount));Assert.Equal(a,InstallmentCalculator.Calculate([q],schedule,false));}
    [Fact] public void PercentagesAndCustomWeights()
    {var q=new IndividualQuota(Guid.NewGuid(),Guid.NewGuid(),100);Assert.Equal(new decimal[]{10,30,60},InstallmentCalculator.Calculate([q],Schedule(10,30,60)).Select(r=>r.Amount));Assert.Throws<ArgumentException>(()=>InstallmentCalculator.Calculate([q],Schedule(30,30)));Assert.Throws<ArgumentOutOfRangeException>(()=>InstallmentCalculator.Calculate([q],Schedule(-1,101)));Assert.Throws<ArgumentException>(()=>InstallmentCalculator.Calculate([q,q],Schedule(100)));}
    [Theory] [InlineData(550,400,50,150)] [InlineData(450,500,-50,-50)] [InlineData(500,500,0,0)]
    public void EconomicAdjustmentIsDistinctFromFinancialBalance(int actual,int paid,int economic,int financial)
    {var s=new SettlementAmounts(500,actual,0,0,paid,0);s.Validate();Assert.Equal(economic,s.EconomicAdjustment);Assert.Equal(financial,s.FinancialBalance);}
    [Fact] public void PriorDebtAdjustmentsAndCreditsRemainDistinct()
    {var s=new SettlementAmounts(500,550,100,-20,400,30);s.Validate();Assert.Equal(50,s.EconomicAdjustment);Assert.Equal(200,s.FinancialBalance);}
}
