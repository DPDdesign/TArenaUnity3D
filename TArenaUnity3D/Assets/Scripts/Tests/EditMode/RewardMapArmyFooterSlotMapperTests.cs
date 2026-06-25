#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class RewardMapArmyFooterSlotMapperTests
{
    [Test]
    public void ExistingStackAfterDeadFormationSlots_MapsToCompactFooterPosition()
    {
        RewardMapArmySnapshot army = Army(
            Stack("stack-rusher", "Rusher", 0),
            Stack("stack-thrower", "Thrower", 0),
            Stack("stack-specialist", "Specialist", 9));

        int visibleSlotIndex = RewardMapArmyFooterSlotMapper.ResolveVisibleSlotIndex(
            army,
            Stack("stack-specialist", "Specialist", 12),
            2);

        Assert.That(visibleSlotIndex, Is.EqualTo(0));
    }

    [Test]
    public void NewStackPreview_MapsToNextCompactFooterPosition()
    {
        RewardMapArmySnapshot army = Army(
            Stack("stack-rusher", "Rusher", 8),
            Stack("stack-specialist", "Specialist", 9));

        int visibleSlotIndex = RewardMapArmyFooterSlotMapper.ResolveVisibleSlotIndex(
            army,
            Stack("slot-4", "Wisp", 6),
            4);

        Assert.That(visibleSlotIndex, Is.EqualTo(2));
    }

    [Test]
    public void MissingChangedStack_KeepsFallbackSlot()
    {
        RewardMapArmySnapshot army = Army(Stack("stack-rusher", "Rusher", 8));

        int visibleSlotIndex = RewardMapArmyFooterSlotMapper.ResolveVisibleSlotIndex(
            army,
            null,
            3);

        Assert.That(visibleSlotIndex, Is.EqualTo(3));
    }

    private static RewardMapArmySnapshot Army(params RewardMapStackSnapshot[] stacks)
    {
        return new RewardMapArmySnapshot("snapshot", 0, new List<RewardMapStackSnapshot>(stacks));
    }

    private static RewardMapStackSnapshot Stack(string stackId, string unitId, int amount)
    {
        return new RewardMapStackSnapshot(
            stackId,
            unitId,
            unitId,
            "I",
            1,
            amount,
            0,
            amount * 10,
            new List<RewardMapSkillState>());
    }
}
#endif
