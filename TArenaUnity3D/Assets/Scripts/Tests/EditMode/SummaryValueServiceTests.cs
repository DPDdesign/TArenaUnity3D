using System.Collections.Generic;
using NUnit.Framework;

public class SummaryValueServiceTests
{
    [Test]
    public void BuildSummary_WonFinalCreatesCandidateFromPreFinalSnapshot()
    {
        SummaryValueService service = new SummaryValueService(new InMemorySummaryValueRosterStore());

        SummaryValueScreenViewData screen = service.BuildSummary(new SummaryValueBuildRequest(
            "run-1",
            SummaryValueFinalResult.Won,
            Army("start", 300),
            Army("pre-final", 900),
            Army("post-final", 700),
            Timeline(),
            2,
            "slot-01"));

        Assert.That(screen.SavedArmyCandidate, Is.Not.Null);
        Assert.That(screen.SavedArmyCandidate.PreFinalSnapshotId, Is.EqualTo("pre-final"));
        Assert.That(screen.SavedArmyCandidate.ImmutableArmySnapshot.TotalArmyValue, Is.EqualTo(900));
        Assert.That(screen.PostFinalArmySnapshot.TotalArmyValue, Is.EqualTo(700));
        Assert.That(screen.ActionMode, Is.EqualTo(SummaryValueSaveActionMode.Save));
        Assert.That(screen.SaveSlots.Count, Is.EqualTo(8));
        Assert.That(screen.SaveSlots[0].State, Is.EqualTo(SummaryValueSlotState.Empty));
        Assert.That(screen.SaveSlots[1].State, Is.EqualTo(SummaryValueSlotState.Empty));
        Assert.That(screen.SaveSlots[2].State, Is.EqualTo(SummaryValueSlotState.Locked));
    }

    [Test]
    public void BuildSummary_LostFinalDoesNotCreateCandidate()
    {
        SummaryValueService service = new SummaryValueService(new InMemorySummaryValueRosterStore());

        SummaryValueScreenViewData screen = service.BuildSummary(new SummaryValueBuildRequest(
            "run-1",
            SummaryValueFinalResult.Lost,
            Army("start", 300),
            Army("pre-final", 900),
            Army("post-final", 0),
            Timeline(),
            2,
            "slot-01"));

        Assert.That(screen.SavedArmyCandidate, Is.Null);
        Assert.That(screen.CanSave, Is.False);
        Assert.That(screen.Message, Does.Contain("Final victory"));
    }

    [Test]
    public void Save_RejectsLockedSlotAndRequiresOverwriteConfirmation()
    {
        InMemorySummaryValueRosterStore store = new InMemorySummaryValueRosterStore();
        SummaryValueService service = new SummaryValueService(store);
        SummaryValueScreenViewData screen = service.BuildSummary(new SummaryValueBuildRequest(
            "run-1",
            SummaryValueFinalResult.Won,
            Army("start", 300),
            Army("pre-final", 900),
            Army("post-final", 800),
            Timeline(),
            2,
            "slot-01"));

        SummaryValueSaveResult locked = service.Save(new SummaryValueSaveCommand("run-1", screen.SavedArmyCandidate.CandidateId, "slot-03", false));
        Assert.That(locked.Success, Is.False);
        Assert.That(locked.Error, Is.EqualTo(SummaryValueError.LockedSlot));

        SummaryValueSaveResult saved = service.Save(new SummaryValueSaveCommand("run-1", screen.SavedArmyCandidate.CandidateId, "slot-01", false));
        Assert.That(saved.Success, Is.True);
        Assert.That(saved.ActionMode, Is.EqualTo(SummaryValueSaveActionMode.Save));

        SummaryValueScreenViewData overwriteScreen = service.BuildSummary(new SummaryValueBuildRequest(
            "run-2",
            SummaryValueFinalResult.Won,
            Army("start-2", 320),
            Army("pre-final-2", 950),
            Army("post-final-2", 820),
            Timeline(),
            2,
            "slot-01"));

        SummaryValueSaveResult missingConfirm = service.Save(new SummaryValueSaveCommand("run-2", overwriteScreen.SavedArmyCandidate.CandidateId, "slot-01", false));
        Assert.That(missingConfirm.Success, Is.False);
        Assert.That(missingConfirm.Error, Is.EqualTo(SummaryValueError.MissingConfirmation));
        Assert.That(missingConfirm.ActionMode, Is.EqualTo(SummaryValueSaveActionMode.Overwrite));
    }

    private static SummaryValueArmySnapshot Army(string id, int value)
    {
        return new SummaryValueArmySnapshot(id, value, new List<SummaryValueStackSnapshot>
        {
            new SummaryValueStackSnapshot("stack-rusher", "Rusher", "Rusher", "I", 1, value / 31, value, new List<SummaryValueSkillState> { new SummaryValueSkillState("Chope", true) })
        });
    }

    private static List<SummaryValueTimelineEntry> Timeline()
    {
        return new List<SummaryValueTimelineEntry>
        {
            new SummaryValueTimelineEntry("stage-1", 1, "Battle", "+12 Rusher", 520, 25),
            new SummaryValueTimelineEntry("stage-2", 2, "Shop", "Teach Rush", 760, 0),
            new SummaryValueTimelineEntry("stage-final", 3, "Final", "Victory proof", 900, 0)
        };
    }
}
