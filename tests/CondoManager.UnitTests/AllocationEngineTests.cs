using CondoManager.Application;
using CondoManager.Domain;
namespace CondoManager.UnitTests;
public class AllocationEngineTests
{
    private static readonly Guid T=Guid.NewGuid(),C=Guid.NewGuid(),P=Guid.NewGuid(),Table=Guid.NewGuid();
    private static readonly Guid[] Units=[Guid.Parse("00000000-0000-0000-0000-000000000001"),Guid.Parse("00000000-0000-0000-0000-000000000002"),Guid.Parse("00000000-0000-0000-0000-000000000003")];
    private static AllocationRecipient[] Recipients(params decimal[] weights)=>weights.Select((w,i)=>new AllocationRecipient(T,C,Units[i],((char)('A'+i)).ToString(),null,null,w)).ToArray();
    private static ResolvedAllocationComponent Component(AllocationCriterion type,decimal percent,decimal[] weights,int position=0,bool normalize=false)=>new(position,type,percent,type==AllocationCriterion.Millesimal?Table:null,null,type==AllocationCriterion.Consumption?"WATER":null,type==AllocationCriterion.Consumption?"M3":null,type==AllocationCriterion.Millesimal?1000:null,true,normalize,Recipients(weights),"{}");
    private static AllocationCalculationInput Input(decimal amount,params ResolvedAllocationComponent[] components)=>new(T,C,P,new(2026,1,1),new(2026,12,31),[new(0,T,C,Guid.NewGuid(),"SYN",amount,0,Guid.NewGuid(),"Synthetic",1,components)]);
    private static AllocationCalculationResult Calculate(AllocationCalculationInput input)=>new AllocationEngine().Calculate(input);
    private static void Balanced(AllocationCalculationResult result,decimal amount) { Assert.True(result.IsValid,string.Join("; ",result.Errors));Assert.Equal(amount,result.OriginalAmount);Assert.Equal(amount,result.Lines.Sum(l=>l.Amount));Assert.Equal(0,result.Difference);Assert.All(result.Lines,l=>Assert.True(l.Amount>=0)); }
    [Fact] public void ReferenceA_MillesimalStandard() { var r=Calculate(Input(1000,Component(AllocationCriterion.Millesimal,100,[500,300,200])));Balanced(r,1000);Assert.Equal(new decimal[]{500,300,200},r.Lines.Select(l=>l.Amount)); }
    [Fact] public void ReferenceB_EqualSharesStableResidual() { var input=Input(100,Component(AllocationCriterion.EqualShares,100,[1,1,1]));var r=Calculate(input);Balanced(r,100);Assert.Equal(new decimal[]{33.34m,33.33m,33.33m},r.Lines.Select(l=>l.Amount));Assert.True(r.Lines[0].ResidualCent);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(r),System.Text.Json.JsonSerializer.Serialize(Calculate(input))); }
    [Fact] public void ReferenceC_Mixed3070() { var r=Calculate(Input(1000,Component(AllocationCriterion.Millesimal,30,[500,300,200]),Component(AllocationCriterion.Consumption,70,[10,20,40],1)));Balanced(r,1000);Assert.Equal(new decimal[]{250,290,460},r.Lines.GroupBy(l=>l.UnitCode).Select(g=>g.Sum(l=>l.Amount))); }
    [Fact] public void ReferenceD_OneCentThreeUnits() { var r=Calculate(Input(.01m,Component(AllocationCriterion.EqualShares,100,[1,1,1])));Balanced(r,.01m);Assert.Equal(new decimal[]{.01m,0,0},r.Lines.Select(l=>l.Amount)); }
    [Fact] public void ExplicitGroupAndExclusionNormalization() { var c=Component(AllocationCriterion.Millesimal,100,[500,300],normalize:true);var r=Calculate(Input(100,c));Balanced(r,100);Assert.Equal(new decimal[]{62.5m,37.5m},r.Lines.Select(l=>l.Amount));Assert.Single(r.Warnings);Assert.False(Calculate(Input(100,c with{NormalizeAdmitted=false})).IsValid); }
    [Theory] [InlineData(99)] [InlineData(101)] [InlineData(0)] public void UnbalancedPercentagesRejected(int value) => Assert.False(Calculate(Input(100,Component(AllocationCriterion.EqualShares,value,[1,1,1]))).IsValid);
    [Fact] public void EmptyGroupsMissingConsumptionAndInactiveTablesRejected()
    { Assert.False(Calculate(Input(10,Component(AllocationCriterion.EqualShares,100,[]))).IsValid);var c=Component(AllocationCriterion.Consumption,100,[1,2,3]);Assert.False(Calculate(Input(10,c with{Recipients=[c.Recipients[0] with{Weight=null}]})).IsValid);Assert.False(Calculate(Input(10,Component(AllocationCriterion.Millesimal,100,[500,300,200]) with{TableIsActive=false})).IsValid);Assert.False(Calculate(Input(10,Component(AllocationCriterion.Millesimal,100,[500,300,200]) with{TableId=null})).IsValid); }
    [Fact] public void DecimalSharesAndComponentResidualsBalance()
    { var r=Calculate(Input(.07m,Component(AllocationCriterion.Millesimal,33.333333m,[333.333333m,333.333333m,333.333334m]),Component(AllocationCriterion.EqualShares,66.666667m,[1,1,1],1)));Balanced(r,.07m);Assert.Equal(.02m,r.Lines.Where(l=>l.ComponentPosition==0).Sum(l=>l.Amount));Assert.Equal(.05m,r.Lines.Where(l=>l.ComponentPosition==1).Sum(l=>l.Amount)); }
    [Fact] public void DirectUnitAndPersonalCharges()
    { var person=Guid.NewGuid();var c=Component(AllocationCriterion.DirectCharge,100,[60,40]);c=c with{Recipients=[c.Recipients[0],c.Recipients[1] with{PersonId=person,PersonName="Synthetic former owner"}]};var r=Calculate(Input(100,c));Balanced(r,100);Assert.Equal(person,r.Lines[1].PersonId);Assert.False(Calculate(Input(101,c)).IsValid); }
    [Fact] public void CrossTenantAndCrossCondominiumRejected()
    { var c=Component(AllocationCriterion.EqualShares,100,[1]);Assert.False(Calculate(Input(1,c with{Recipients=[c.Recipients[0] with{TenantId=Guid.NewGuid()}]})).IsValid);Assert.False(Calculate(Input(1,c with{Recipients=[c.Recipients[0] with{CondominiumId=Guid.NewGuid()}]})).IsValid); }
    [Fact] public void VeryLargeAmountsUseExactRemainders()
    { var r=Calculate(Input(Money.Maximum,Component(AllocationCriterion.Millesimal,100,[333.333333m,333.333333m,333.333334m])));Balanced(r,Money.Maximum); }
    [Fact] public void InputOrderDoesNotChangeTieBreak()
    { var c=Component(AllocationCriterion.EqualShares,100,[1,1,1]);var r=Calculate(Input(100,c with{Recipients=c.Recipients.Reverse().ToArray()}));Assert.Equal(Units[0],r.Lines.Single(l=>l.Amount==33.34m).PropertyUnitId);        var culture=System.Globalization.CultureInfo.CurrentCulture;
        try {
            var input=Input(100,Component(AllocationCriterion.Millesimal,100,[500.123456m,300.1m],normalize:true));
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("fr-FR");var french=Calculate(input);
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("en-US");var english=Calculate(input);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(french),System.Text.Json.JsonSerializer.Serialize(english));
        } finally { System.Globalization.CultureInfo.CurrentCulture=culture; } }
    private static ConsumptionReading Reading(string meter,ReadingBoundary boundary,DateOnly date,decimal value,Guid? previous=null,bool estimated=false)=>new(T,C,Units[0],P,"WATER",meter,"M3",date,boundary,value,estimated,estimated||previous.HasValue?"Synthetic correction":null,previous);
    [Fact] public void ReadingsCalculateConsumptionAndReplacementMeter()
    { var r=ConsumptionCalculator.Calculate([Reading("OLD",ReadingBoundary.Initial,new(2026,1,1),100),Reading("OLD",ReadingBoundary.Final,new(2026,6,30),130),Reading("NEW",ReadingBoundary.Initial,new(2026,6,30),0),Reading("NEW",ReadingBoundary.Final,new(2026,12,31),40)],new(2026,1,1),new(2026,12,31));Assert.Empty(r.Errors);Assert.Equal(70,r.Consumption);Assert.Equal(2,r.Segments.Count); }
    [Fact] public void MissingInconsistentAndUncoveredReadingsRejected()
    { var a=Reading("M",ReadingBoundary.Initial,new(2026,1,1),100);Assert.Null(ConsumptionCalculator.Calculate([a],new(2026,1,1),new(2026,12,31)).Consumption);Assert.Null(ConsumptionCalculator.Calculate([a,Reading("M",ReadingBoundary.Final,new(2026,12,31),99)],new(2026,1,1),new(2026,12,31)).Consumption); }
    [Fact] public void ReadingCorrectionsPreserveHistoryAndMarkEstimates()
    { var old=Reading("M",ReadingBoundary.Final,new(2026,12,31),30);var corrected=Reading("M",ReadingBoundary.Final,new(2026,12,31),40,old.Id,true);var r=ConsumptionCalculator.Calculate([Reading("M",ReadingBoundary.Initial,new(2026,1,1),10),old,corrected],new(2026,1,1),new(2026,12,31));Assert.Equal(30,r.Consumption);Assert.True(r.IsEstimated);Assert.Equal(corrected.Id,r.Segments.Single().FinalReadingId); }
    [Fact] public void InvalidCalculationRetainsTheFullOriginalBudget()
    {
        var input=Input(1,Component(AllocationCriterion.EqualShares,99,[1]));input=input with{Sources=[input.Sources[0],input.Sources[0] with{Position=1,Amount=2}]};var result=Calculate(input);Assert.False(result.IsValid);Assert.Equal(3,result.OriginalAmount);Assert.Equal(3,result.Difference);Assert.Empty(result.Lines);
    }
    [Fact] public void ConsumptionCannotCombineReadingsOfDifferentUnits()
    {
        var readings=new[]{Reading("OLD",ReadingBoundary.Initial,new(2026,1,1),0),Reading("OLD",ReadingBoundary.Final,new(2026,6,30),10),new ConsumptionReading(T,C,Units[1],P,"WATER","NEW","M3",new(2026,6,30),ReadingBoundary.Initial,0,false,null,null),new ConsumptionReading(T,C,Units[1],P,"WATER","NEW","M3",new(2026,12,31),ReadingBoundary.Final,10,false,null,null)};
        Assert.Null(ConsumptionCalculator.Calculate(readings,new(2026,1,1),new(2026,12,31)).Consumption);
    }}
