#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;

public class BattleActionAutomaticResultApplierTests
{
    [Test]
    public void RopeTrapTrigger_BuildsTrapAndStatusResultEvents()
    {
        TosterHexUnit owner = new TosterHexUnit { Name = "Owner" };
        TosterHexUnit target = new TosterHexUnit { Name = "Target" };
        HexClass hex = new HexClass();
        hex.AddTrap("Rope_Trap", 999, owner, false);

        BattleActionResult result = BattleActionAutomaticResultApplier.CreateTrapTriggeredResult(target, hex);

        Assert.That(result.ActionKind, Is.EqualTo(BattleActionKind.Trap));
        Assert.That(result.Events.Count, Is.EqualTo(2));
        Assert.That(result.Events[0].EventType, Is.EqualTo(BattleActionResultEventType.TrapTriggered));
        Assert.That(result.Events[0].TrapId, Is.EqualTo("Rope_Trap"));

        BattleActionResultEvent statusEvent = result.Events.First(e => e.EventType == BattleActionResultEventType.StatusApplied);
        Assert.That(statusEvent.StatusId, Is.EqualTo("Rope_Trap"));
        Assert.That(statusEvent.Duration, Is.EqualTo(1));
        Assert.That(statusEvent.SpecialResistanceModifier, Is.EqualTo(30));
        Assert.That(statusEvent.RemoveTrap, Is.True);
    }

    [Test]
    public void OwnerWalkingIntoOwnFireTrap_BuildsNoMutationEvents()
    {
        TosterHexUnit owner = new TosterHexUnit { Name = "Owner" };
        HexClass hex = new HexClass();
        hex.AddTrap("Fire_Trap", 2, owner, false);

        BattleActionResult result = BattleActionAutomaticResultApplier.CreateTrapTriggeredResult(owner, hex);

        Assert.That(result.ActionKind, Is.EqualTo(BattleActionKind.Trap));
        Assert.That(result.Events.Count, Is.EqualTo(0));
    }

    [Test]
    public void AutocastStatus_BuildsAutomaticStatusResultEvent()
    {
        TosterHexUnit actor = new TosterHexUnit { Name = "Actor" };

        BattleActionResult result = BattleActionAutomaticResultApplier.CreateAutocastStatusResult(actor, "Stone_Skin");

        Assert.That(result.ActionKind, Is.EqualTo(BattleActionKind.Automatic));
        Assert.That(result.Events.Count, Is.EqualTo(1));
        Assert.That(result.Events[0].EventType, Is.EqualTo(BattleActionResultEventType.StatusApplied));
        Assert.That(result.Events[0].StatusId, Is.EqualTo("Stone_Skin"));
        Assert.That(result.Events[0].Duration, Is.EqualTo(1));
    }
}
#endif
