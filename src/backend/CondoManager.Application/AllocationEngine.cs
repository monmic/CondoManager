using System.Numerics;
using CondoManager.Domain;
namespace CondoManager.Application;
public record AllocationRecipient(Guid TenantId,Guid CondominiumId,Guid PropertyUnitId,string UnitCode,Guid? PersonId,string? PersonName,decimal? Weight,bool IsEstimated=false);
public record ResolvedAllocationComponent(int Position,AllocationCriterion Criterion,decimal Percentage,Guid? TableId,Guid? GroupId,string? ConsumptionType,string? UnitOfMeasure,decimal? NominalDenominator,bool TableIsActive,bool NormalizeAdmitted,IReadOnlyList<AllocationRecipient> Recipients,string EvidenceJson);
public enum AllocationSourceKind { ExpenseDocument, BudgetLine }
public record ResolvedAllocationSource(int Position,Guid TenantId,Guid CondominiumId,Guid ExpenseDocumentId,string DocumentNumber,decimal Amount,decimal PreviouslyAllocated,Guid RuleId,string RuleName,int RuleVersion,IReadOnlyList<ResolvedAllocationComponent> Components,AllocationSourceKind SourceKind=AllocationSourceKind.ExpenseDocument);
public record AllocationCalculationInput(Guid TenantId,Guid CondominiumId,Guid AccountingPeriodId,DateOnly CompetenceFrom,DateOnly CompetenceTo,IReadOnlyList<ResolvedAllocationSource> Sources);
public record AllocationCalculationLine(int SourcePosition,int ComponentPosition,Guid PropertyUnitId,string UnitCode,Guid? PersonId,string? PersonName,decimal Weight,decimal EffectiveDenominator,decimal? NominalDenominator,decimal ComponentAmount,decimal ComponentUnroundedAmount,decimal ComponentRoundingAdjustment,decimal Amount,decimal BaseAmount,decimal UnroundedAmount,decimal RoundingAdjustment,bool ResidualCent);
public record AllocationCalculationResult(string EngineVersion,decimal OriginalAmount,decimal AllocatedAmount,decimal Difference,IReadOnlyList<AllocationCalculationLine> Lines,IReadOnlyList<string> Warnings,IReadOnlyList<string> Errors) { public bool IsValid=>Errors.Count==0 && Difference==0; }
public interface IAllocationEngine { AllocationCalculationResult Calculate(AllocationCalculationInput input); }
// Pure deterministic calculator. Decimal input, exact integer rational arithmetic for cents/remainders.
public sealed class AllocationEngine : IAllocationEngine
{
    public const string Version="largest-remainder-v1";
    public AllocationCalculationResult Calculate(AllocationCalculationInput input)
    {
        var lines=new List<AllocationCalculationLine>();var warnings=new List<string>();var errors=new List<string>();decimal total=0;
        try
        {
            Guard.Id(input.TenantId);Guard.Id(input.CondominiumId);Guard.Id(input.AccountingPeriodId);Guard.Dates(input.CompetenceFrom,input.CompetenceTo);
            if(input.Sources==null || input.Sources.Count is <1 or >100 || input.Sources.Select(s=>s.Position).Distinct().Count()!=input.Sources.Count) throw new ArgumentException("Da 1 a 100 quote di spesa con posizioni univoche.");
            total=Money.Validate(input.Sources.Sum(s=>Money.Positive(s.Amount)));
            foreach(var source in input.Sources.OrderBy(s=>s.Position))
            {
                if(source.TenantId!=input.TenantId || source.CondominiumId!=input.CondominiumId) throw new ArgumentException("Fonte di un altro tenant o condominio.");
                Money.Positive(source.Amount);
                if(source.Components.Count is <1 or >20 || source.Components.Select(c=>c.Position).Distinct().Count()!=source.Components.Count || source.Components.Sum(c=>c.Percentage)!=100) throw new ArgumentException("Percentuali non bilanciate: totale esattamente 100%.");
                var components=source.Components.OrderBy(c=>c.Position).ToArray();foreach(var c in components) Guard.Decimal(c.Percentage,6,0.000001m,100);
                var budgets=SplitCents(source.Amount,components.Select(c=>c.Percentage).ToArray(),components.Select(c=>c.Position.ToString("D4")).ToArray());
                for(var index=0;index<components.Length;index++)
                {
                    var c=components[index];Guard.EnumValue(c.Criterion);
                    if(c.Criterion==AllocationCriterion.Mixed) throw new ArgumentException("Un componente deve avere un criterio elementare.");
                    var recipients=c.Recipients.OrderBy(r=>StableKey(r),StringComparer.Ordinal).ToArray();
                    if(recipients.Length==0) throw new ArgumentException("Gruppo senza unita ammesse.");
                    if(recipients.Any(r=>r.TenantId!=input.TenantId || r.CondominiumId!=input.CondominiumId)) throw new ArgumentException("Destinatario di un altro tenant o condominio.");
                    if(recipients.Select(r=>(r.PropertyUnitId,r.PersonId)).Distinct().Count()!=recipients.Length) throw new ArgumentException("Destinatario duplicato.");
                    foreach(var r in recipients) { Guard.Id(r.PropertyUnitId);Guard.Text(r.UnitCode);if(r.Weight==null) throw new ArgumentException("Consumi o quote mancanti per l'unita "+r.UnitCode);if(c.Criterion==AllocationCriterion.DirectCharge) Money.Positive(r.Weight.Value); else Guard.Decimal(r.Weight.Value,6,0,999999999999.999999m); }
                    var effective=recipients.Sum(r=>r.Weight!.Value);
                    if(effective<=0) throw new ArgumentException("Totale quote o consumi ammessi nullo.");
                    if(c.Criterion==AllocationCriterion.Millesimal)
                    {
                        if(c.TableId==null) throw new ArgumentException("Tabella millesimale assente.");
                        if(!c.TableIsActive) throw new ArgumentException("Tabella millesimale non attiva.");
                        if(c.NominalDenominator==null || c.NominalDenominator<=0) throw new ArgumentException("Denominatore nominale non valido.");
                        if(effective!=c.NominalDenominator && !c.NormalizeAdmitted) throw new ArgumentException("Totale ammesso diverso dal nominale: richiedere normalizzazione esplicita.");
                        if(c.NormalizeAdmitted) warnings.Add(FormattableString.Invariant($"Fonte {source.Position+1}, componente {c.Position+1}: normalizzazione esplicita da nominale {c.NominalDenominator} a totale ammesso {effective}."));
                    }
                    if(c.Criterion==AllocationCriterion.EqualShares && recipients.Any(r=>r.Weight!=1 || r.PersonId!=null)) throw new ArgumentException("Parti uguali richiede peso 1 per unita.");
                    if(c.Criterion==AllocationCriterion.DirectCharge && (components.Length!=1 || effective!=source.Amount)) throw new ArgumentException("Gli addebiti diretti devono sommare esattamente la quota di spesa.");
                    if(c.Criterion!=AllocationCriterion.DirectCharge && recipients.Any(r=>r.PersonId!=null)) throw new ArgumentException("Soggetti espliciti ammessi solo per addebiti diretti.");
                    if(recipients.Any(r=>r.IsEstimated)) warnings.Add($"Fonte {source.Position+1}, componente {c.Position+1}: sono presenti consumi stimati esplicitamente.");
                    var portions=SplitCents(budgets[index].Amount,recipients.Select(r=>r.Weight!.Value).ToArray(),recipients.Select(StableKey).ToArray());
                    for(var n=0;n<recipients.Length;n++) { var r=recipients[n];var p=portions[n];lines.Add(new(source.Position,c.Position,r.PropertyUnitId,r.UnitCode,r.PersonId,r.PersonName,r.Weight!.Value,effective,c.NominalDenominator,budgets[index].Amount,budgets[index].Unrounded,budgets[index].Amount-budgets[index].Unrounded,p.Amount,p.Base,p.Unrounded,p.Amount-p.Unrounded,p.Amount!=p.Base)); }
                    if(lines.Count>20000) throw new ArgumentException("Massimo 20000 righe per calcolo: suddividere l'operazione.");
                }
            }
        }
        catch(Exception e) when(e is ArgumentException or InvalidOperationException or OverflowException) { errors.Add(e is OverflowException?"Valori troppo grandi per il calcolo.":e.Message);lines.Clear(); }
        var allocated=lines.Sum(l=>l.Amount);return new(Version,total,allocated,total-allocated,lines,warnings,errors);
    }
    private static string StableKey(AllocationRecipient r)=>r.UnitCode.ToUpperInvariant()+"|"+r.PropertyUnitId.ToString("D")+"|"+r.PersonId?.ToString("D");
    public sealed record Portion(decimal Amount,decimal Base,decimal Unrounded);
    public static Portion[] SplitCents(decimal amount,decimal[] weights,string[] keys)
    {
        Money.Validate(amount);if(weights.Length==0 || weights.Length!=keys.Length || keys.Distinct(StringComparer.Ordinal).Count()!=keys.Length) throw new ArgumentException("Pesi o chiavi non validi.");foreach(var weight in weights) Guard.Decimal(weight,6,0,999999999999.999999m);
        // All weights have <=6 decimal places. Multiplication remains exact in BigInteger.
        var values=weights.Select(v=>new BigInteger(v*1000000m)).ToArray();var denominator=values.Aggregate(BigInteger.Zero,(a,b)=>a+b);if(denominator<=0) throw new ArgumentException("Denominatore nullo.");
        var cents=new BigInteger(amount*100m);var floors=new BigInteger[values.Length];var remainders=new BigInteger[values.Length];
        for(var i=0;i<values.Length;i++) floors[i]=BigInteger.DivRem(cents*values[i],denominator,out remainders[i]);
        var remaining=(int)(cents-floors.Aggregate(BigInteger.Zero,(a,b)=>a+b));
        var winners=Enumerable.Range(0,values.Length).OrderByDescending(i=>remainders[i]).ThenBy(i=>keys[i],StringComparer.Ordinal).Take(remaining).ToHashSet();
        return Enumerable.Range(0,values.Length).Select(i=>new Portion((decimal)(floors[i]+(winners.Contains(i)?1:0))/100m,(decimal)floors[i]/100m,(decimal)(cents*values[i]*BigInteger.Pow(10,10)/denominator)/1000000000000m)).ToArray();
    }
}
public record ConsumptionSegment(string MeterCode,DateOnly From,DateOnly To,decimal Initial,decimal Final,decimal Consumption,bool IsEstimated,Guid InitialReadingId,Guid FinalReadingId);
public record ConsumptionResult(decimal? Consumption,IReadOnlyList<ConsumptionSegment> Segments,IReadOnlyList<string> Errors,bool IsEstimated);
public static class ConsumptionCalculator
{
    public static ConsumptionResult Calculate(IReadOnlyList<ConsumptionReading> readings,DateOnly from,DateOnly to)
    {
        var errors=new List<string>();var segments=new List<ConsumptionSegment>();
        if(readings.Select(r=>(r.TenantId,r.CondominiumId,r.PropertyUnitId,r.AccountingPeriodId,r.ConsumptionType,r.UnitOfMeasure)).Distinct().Count()>1) errors.Add("Letture appartenenti a contesti, unita o tipi differenti.");var replaced=readings.Where(r=>r.ReplacesReadingId!=null).Select(r=>r.ReplacesReadingId).ToHashSet();
        var current=readings.Where(r=>!replaced.Contains(r.Id)).ToArray();
        foreach(var group in current.GroupBy(r=>r.MeterCode,StringComparer.Ordinal))
        {
            var initials=group.Where(r=>r.Boundary==ReadingBoundary.Initial).ToArray();var finals=group.Where(r=>r.Boundary==ReadingBoundary.Final).ToArray();
            if(initials.Length!=1 || finals.Length!=1) { errors.Add("Letture iniziali/finali mancanti o duplicate per "+group.Key);continue; }
            var a=initials[0];var b=finals[0];
            if(a.Date>=b.Date || b.Value<a.Value) { errors.Add("Letture incoerenti per "+group.Key+": data o consumo negativo; sostituzioni richiedono un nuovo codice contatore.");continue; }
            segments.Add(new(group.Key,a.Date,b.Date,a.Value,b.Value,b.Value-a.Value,a.IsEstimated||b.IsEstimated,a.Id,b.Id));
        }
        segments=segments.OrderBy(s=>s.From).ThenBy(s=>s.MeterCode,StringComparer.Ordinal).ToList();
        if(segments.Count==0 || segments[0].From!=from || segments[^1].To!=to) errors.Add("Letture mancanti agli estremi del periodo di competenza.");
        for(var i=1;i<segments.Count;i++) if(segments[i-1].To!=segments[i].From) errors.Add("Intervalli dei contatori sovrapposti o con intervalli scoperti.");
        return new(errors.Count==0?segments.Sum(s=>s.Consumption):null,segments,errors,segments.Any(s=>s.IsEstimated));
    }
}
