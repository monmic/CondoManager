using CondoManager.Domain;
namespace CondoManager.UnitTests;
public class DocumentDomainTests
{
 static MeetingMinutes Draft()=>new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1,"Synthetic minutes","{}",new string('A',64),Guid.NewGuid());
 [Fact] public void DraftUsesOptimisticRevision(){var d=Draft();d.Edit(1,"Updated","Note",null,null);Assert.Equal(2,d.Revision);Assert.Throws<InvalidOperationException>(()=>d.Edit(1,"Stale",null,null,null));}
 [Fact] public void FinalMinutesCannotBeEdited(){var d=Draft();d.Finalize(Guid.NewGuid(),Guid.NewGuid());Assert.Equal(MinutesStatus.Finalized,d.Status);Assert.Throws<InvalidOperationException>(()=>d.Edit(d.Revision,"Wrong",null,null,null));}
 [Fact] public void CorrectionRequiresReason()=>Assert.Throws<ArgumentException>(()=>new MeetingMinutes(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),2,"Synthetic","{}",new string('A',64),Guid.NewGuid(),Guid.NewGuid()));
}
