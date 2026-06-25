#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class BattleResultServiceTests
{
    [Test]
    public void Record_OffenceWinAgainstStrongerOpponentGivesPositiveRankAndXp()
    {
        InMemoryBattleResultStore store = new InMemoryBattleResultStore();
        BattleResultService service = new BattleResultService(store);

        BattleResultViewData result = service.Record(new BattleResultRecordRequest(
            "async-1",
            BattleResultKind.OffenceWin,
            Army("saved-attacker", 900),
            Army("saved-defender", 1200),
            new BattleResultOpponentMetadata("opponent-1", "Local Rival", 1050, 1200, true),
            1000,
            200));

        Assert.That(result.Success, Is.True);
        Assert.That(result.RankDelta, Is.GreaterThan(24));
        Assert.That(result.RankAfter, Is.EqualTo(1000 + result.RankDelta));
        Assert.That(result.AccountXpGained, Is.GreaterThan(80));
        Assert.That(result.PreservationRecord.AttackerPreserved, Is.True);
        Assert.That(result.PreservationRecord.DefenderPreserved, Is.True);
        Assert.That(store.Find("async-1"), Is.Not.Null);
    }

    [Test]
    public void Record_OffenceWinAgainstWeakOpponentRewardsLessRankThanEqualOpponent()
    {
        BattleResultService service = new BattleResultService(new InMemoryBattleResultStore());

        BattleResultViewData weak = service.Record(new BattleResultRecordRequest(
            "async-weak",
            BattleResultKind.OffenceWin,
            Army("saved-attacker", 1000),
            Army("saved-defender-weak", 700),
            new BattleResultOpponentMetadata("weak", "Weak Rival", 700, 700, true),
            1000,
            0));

        BattleResultViewData equal = service.Record(new BattleResultRecordRequest(
            "async-equal",
            BattleResultKind.OffenceWin,
            Army("saved-attacker", 1000),
            Army("saved-defender-equal", 1000),
            new BattleResultOpponentMetadata("equal", "Equal Rival", 1000, 1000, true),
            1000,
            0));

        Assert.That(weak.RankDelta, Is.LessThan(equal.RankDelta));
    }

    [Test]
    public void Record_RejectsMissingSavedArmies()
    {
        BattleResultService service = new BattleResultService(new InMemoryBattleResultStore());

        BattleResultViewData result = service.Record(new BattleResultRecordRequest(
            "async-1",
            BattleResultKind.OffenceWin,
            null,
            Army("saved-defender", 900),
            new BattleResultOpponentMetadata("opponent-1", "Local Rival", 1000, 900, true),
            1000,
            0));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(BattleResultError.MissingAttacker));
    }

    private static BattleResultSavedArmySnapshot Army(string id, int value)
    {
        return new BattleResultSavedArmySnapshot(id, "snapshot-" + id, id, value, new List<BattleResultStackSnapshot>
        {
            new BattleResultStackSnapshot("stack-rusher", "Rusher", "Rusher", value / 31, value, new List<BattleResultSkillState> { new BattleResultSkillState("Chope", true) })
        });
    }
}
#endif
