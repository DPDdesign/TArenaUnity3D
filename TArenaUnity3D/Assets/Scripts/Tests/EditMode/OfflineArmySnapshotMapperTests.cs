#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class OfflineArmySnapshotMapperTests
{
    [Test]
    public void FromRewardMap_DropsLostAndKeepsOnlyUnlockedSkills()
    {
        RewardMapArmySnapshot source = new RewardMapArmySnapshot(
            "reward-1",
            0,
            new List<RewardMapStackSnapshot>
            {
                new RewardMapStackSnapshot(
                    "slot-3",
                    "Swordsman",
                    "Swordsman",
                    "II",
                    4,
                    18,
                    6,
                    50,
                    new List<RewardMapSkillState>
                    {
                        new RewardMapSkillState("Parry", true),
                        new RewardMapSkillState("ShieldWall", false)
                    })
            });

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotMapper.FromRewardMap(source, 7, 11, 13);

        Assert.That(shared.AccountId, Is.EqualTo(7));
        Assert.That(shared.RunId, Is.EqualTo(11));
        Assert.That(shared.NodeId, Is.EqualTo(13));
        Assert.That(shared.Stacks.Count, Is.EqualTo(1));
        Assert.That(shared.Stacks[0].FormationSlot, Is.EqualTo(3));
        Assert.That(shared.Stacks[0].Amount, Is.EqualTo(18));
        Assert.That(shared.Stacks[0].Skills.Count, Is.EqualTo(1));
        Assert.That(shared.Stacks[0].Skills[0].SkillId, Is.EqualTo("Parry"));
    }

    [Test]
    public void ToRunBattle_RebuildsViewDataFromCatalogAndFormationSlot()
    {
        OfflineArmySnapshotRecord shared = OfflineArmySnapshotFactory.Create(
            1,
            2,
            0,
            4,
            new List<OfflineArmySnapshotStackRecord>
            {
                new OfflineArmySnapshotStackRecord(
                    0,
                    "Archer",
                    12,
                    5,
                    true,
                    new List<OfflineArmySnapshotStackSkillRecord>
                    {
                        new OfflineArmySnapshotStackSkillRecord(0, "Focus", 0, true)
                    })
            });

        RunBattleArmySnapshot mapped = OfflineArmySnapshotMapper.ToRunBattle(shared, new TestCatalogResolver());

        Assert.That(mapped.SnapshotId, Is.EqualTo("snapshot-unsaved"));
        Assert.That(mapped.TotalArmyValue, Is.EqualTo(144));
        Assert.That(mapped.Stacks.Count, Is.EqualTo(1));
        Assert.That(mapped.Stacks[0].StackId, Is.EqualTo("stack-archer"));
        Assert.That(mapped.Stacks[0].DisplayName, Is.EqualTo("Archer"));
        Assert.That(mapped.Stacks[0].Tier, Is.EqualTo("II"));
        Assert.That(mapped.Stacks[0].Level, Is.EqualTo(1));
        Assert.That(mapped.Stacks[0].Lost, Is.EqualTo(0));
        Assert.That(mapped.Stacks[0].Skills.Count, Is.EqualTo(2));
        Assert.That(mapped.Stacks[0].Skills[0].SkillId, Is.EqualTo("Focus"));
        Assert.That(mapped.Stacks[0].Skills[0].Unlocked, Is.True);
        Assert.That(mapped.Stacks[0].Skills[1].SkillId, Is.EqualTo("Volley"));
        Assert.That(mapped.Stacks[0].Skills[1].Unlocked, Is.False);
    }

    [Test]
    public void BuildLosses_ComparesByFormationSlot()
    {
        OfflineArmySnapshotRecord before = OfflineArmySnapshotFactory.Create(
            1,
            1,
            0,
            0,
            new List<OfflineArmySnapshotStackRecord>
            {
                new OfflineArmySnapshotStackRecord(0, "Knight", 10, 0, true, new List<OfflineArmySnapshotStackSkillRecord>()),
                new OfflineArmySnapshotStackRecord(0, "Archer", 7, 1, true, new List<OfflineArmySnapshotStackSkillRecord>())
            });

        OfflineArmySnapshotRecord after = OfflineArmySnapshotFactory.Create(
            1,
            1,
            0,
            0,
            new List<OfflineArmySnapshotStackRecord>
            {
                new OfflineArmySnapshotStackRecord(0, "Knight", 6, 0, true, new List<OfflineArmySnapshotStackSkillRecord>()),
                new OfflineArmySnapshotStackRecord(0, "Archer", 7, 1, true, new List<OfflineArmySnapshotStackSkillRecord>())
            });

        OfflineArmySnapshotLossDiff diff = OfflineArmySnapshotDiff.BuildLosses(before, after);

        Assert.That(diff.TotalLostAmount, Is.EqualTo(4));
        Assert.That(diff.Losses.Count, Is.EqualTo(1));
        Assert.That(diff.Losses[0].FormationSlot, Is.EqualTo(0));
        Assert.That(diff.Losses[0].UnitId, Is.EqualTo("Knight"));
        Assert.That(diff.Losses[0].AmountBefore, Is.EqualTo(10));
        Assert.That(diff.Losses[0].AmountAfter, Is.EqualTo(6));
        Assert.That(diff.Losses[0].LostAmount, Is.EqualTo(4));
    }

    private class TestCatalogResolver : IOfflineArmySnapshotCatalogResolver
    {
        public OfflineArmySnapshotUnitCatalogEntry FindUnit(string unitId)
        {
            if (unitId == "Archer")
            {
                return new OfflineArmySnapshotUnitCatalogEntry(
                    "Archer",
                    "Archer",
                    "II",
                    12,
                    new List<string> { "Focus", "Volley" });
            }

            return new OfflineArmySnapshotUnitCatalogEntry(
                unitId,
                unitId,
                "I",
                10,
                new List<string>());
        }
    }
}
#endif
