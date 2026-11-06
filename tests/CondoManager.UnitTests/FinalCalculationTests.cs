using CondoManager.Application;
using CondoManager.Domain;
namespace CondoManager.UnitTests;
public class FinalCalculationTests
{
 [Theory][InlineData(500,550,50)][InlineData(300,250,-50)][InlineData(200,200,0)]
 public void EconomicAdjustmentExcludesReceipts(int budget,int actual,int expected)=>Assert.Equal(expected,FinalCalculations.Economic(budget,actual));
 [Fact] public void CompetenceAndFinancialAreDistinctAndAlreadyIssuedIsNotChargedTwice(){Assert.Equal(50,FinalCalculations.Economic(500,550));Assert.Equal(50,FinalCalculations.DeltaToPost(550,500));Assert.Equal(150,FinalCalculations.Financial(0,500,50,400));Assert.Equal(-50,FinalCalculations.Financial(0,300,-50,300));}
 [Fact] public void PriorBalanceIsAddedOnce(){Assert.Equal(150,FinalCalculations.Financial(100,500,0,450));Assert.Equal(120,FinalCalculations.Financial(100,500,0,450,30));}
 [Theory][InlineData(100,110,10,10)][InlineData(100,90,-10,-10)]
 public void ComparisonsUseSignedDifferenceAndPercentage(int budget,int actual,int diff,int pct){var c=FinalCalculations.Compare(Guid.NewGuid(),"Synthetic",budget,actual);Assert.Equal(diff,c.Difference);Assert.Equal(pct,c.Percentage);}
 [Fact] public void MissingCategoryBudgetDoesNotDivideByZero(){var c=FinalCalculations.Compare(Guid.NewGuid(),"New category",0,100);Assert.Equal(100,c.Difference);Assert.Null(c.Percentage);var absent=FinalCalculations.Compare(Guid.NewGuid(),"Budget only",100,0);Assert.Equal(-100,absent.Difference);Assert.Equal(-100,absent.Percentage);}
 [Fact] public void StatementApprovedIsImmutableAndMustBeProposed(){var s=new FinalStatement(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),null,1,null,"Synthetic",null,Guid.NewGuid(),Guid.NewGuid(),"hash","{}","{}","hash");Assert.Throws<InvalidOperationException>(()=>s.Approve(Guid.NewGuid(),null));s.Propose();s.Approve(Guid.NewGuid(),null);Assert.Throws<InvalidOperationException>(s.Propose);s.Supersede();Assert.Equal(FinalStatementStatus.Superseded,s.Status);}
 [Fact] public void ZeroSettlementCanPostWithoutArtificialJournal(){var b=new StatementSettlement(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new(2026,1,1),Guid.NewGuid(),"hash","{}","hash",Guid.NewGuid());b.Post(null,Guid.NewGuid());Assert.NotNull(b.PostedAt);Assert.Null(b.JournalEntryId);Assert.Throws<InvalidOperationException>(()=>b.Post(null,Guid.NewGuid()));}
}
