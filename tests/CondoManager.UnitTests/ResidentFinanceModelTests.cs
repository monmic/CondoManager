using CondoManager.Application;
using CondoManager.Domain;
namespace CondoManager.UnitTests;
public class ResidentFinanceModelTests
{
    private static readonly Guid T=Guid.NewGuid(),C=Guid.NewGuid();
    [Fact] public void DraftPlanNeverHasCreditsAndIssuedPlanCannotBeSuperseded()
    {var p=new InstallmentPlan(T,C,Guid.NewGuid(),Guid.NewGuid(),1,null,Guid.NewGuid(),"hash","{}","{}",100);Assert.Null(p.JournalEntryId);p.Approve(Guid.NewGuid(),Guid.NewGuid());Assert.Equal(InstallmentPlanStatus.Approved,p.Status);Assert.Throws<InvalidOperationException>(p.SupersedeDraft);Assert.Throws<InvalidOperationException>(()=>p.Approve(Guid.NewGuid(),Guid.NewGuid()));}
    [Fact] public void DraftVersionCanBeSupersededWithoutPosting()
    {var p=new InstallmentPlan(T,C,Guid.NewGuid(),Guid.NewGuid(),1,null,Guid.NewGuid(),"hash","{}","{}",100);p.SupersedeDraft();Assert.Null(p.JournalEntryId);Assert.Equal(InstallmentPlanStatus.Superseded,p.Status);}
    [Fact] public void NegativeInstallmentsAndExcessPrecisionRejected()
    {Assert.Throws<ArgumentOutOfRangeException>(()=>new Installment(T,C,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),-1));Assert.Throws<ArgumentOutOfRangeException>(()=>new Installment(T,C,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),0.001m));}
    [Fact] public void ExactAmountsPreserveEveryDebtorAndEveryDeadlineEvenForTinyAmounts()
    {var quotas=Enumerable.Range(0,3).Select(_=>new IndividualQuota(Guid.NewGuid(),Guid.NewGuid(),0.01m)).ToArray();var schedule=Enumerable.Range(1,3).Select(i=>new InstallmentSchedule(new(2026,i,1),0.01m)).ToArray();var a=InstallmentCalculator.CalculateExactAmounts(quotas,schedule);Assert.Equal(0.03m,a.Sum(r=>r.Amount));Assert.All(a.GroupBy(r=>r.Position),g=>Assert.Equal(0.01m,g.Sum(r=>r.Amount)));Assert.All(a.GroupBy(r=>r.DebtorId),g=>Assert.Equal(0.01m,g.Sum(r=>r.Amount)));Assert.All(a,r=>Assert.True(r.Amount>=0));Assert.Equal(a,InstallmentCalculator.CalculateExactAmounts(quotas,schedule));}
    [Fact] public void ExactAmountsRejectDifferentTotalsAndNonMonetaryWeights()
    {var q=new IndividualQuota(Guid.NewGuid(),Guid.NewGuid(),100);Assert.Throws<ArgumentException>(()=>InstallmentCalculator.CalculateExactAmounts([q],[new(new(2026,1,1),99)]));Assert.Throws<ArgumentOutOfRangeException>(()=>InstallmentCalculator.CalculateExactAmounts([q],[new(new(2026,1,1),99.999m),new(new(2026,2,1),0.001m)]));}
    [Fact] public void InvalidUnorderedAndDuplicateDeadlinesRejected()
    {var q=new IndividualQuota(Guid.NewGuid(),Guid.NewGuid(),1);Assert.Throws<ArgumentException>(()=>InstallmentCalculator.Calculate([q],[new(new(2026,2,1),50),new(new(2026,1,1),50)]));Assert.Throws<ArgumentException>(()=>InstallmentCalculator.Calculate([q],[new(new(2026,1,1),50),new(new(2026,1,1),50)]));}
    [Fact] public void ReceiptPreservesPayerDistinctFromDebtorAndRequiresPositiveMoney()
    {var d=Guid.NewGuid();var p=Guid.NewGuid();var r=new ResidentReceipt(T,C,Guid.NewGuid(),Guid.NewGuid(),d,p,new(2026,1,1),100,null,Guid.NewGuid(),"hash");Assert.Equal(d,r.DebtorId);Assert.Equal(p,r.PayerId);Assert.Throws<ArgumentException>(()=>new ReceiptAllocation(T,C,r.Id,Guid.NewGuid(),Guid.NewGuid(),0));}
}
