using System.Collections.Generic;
using NUnit.Framework;

public class PRD44TacticalRewardStackIdentityTests
{
    [Test]
    public void BuildPlayerArmyAfterBattle_UsesBattleInputOrderForDuplicateUnitStacks()
    {
        RunBattleArmySnapshot beforeBattle = Army(
            Stack("stack-rusher", "Rusher", 30, 30 * 31),
            Stack("slot-1", "Rusher", 12, 12 * 31));
        List<RunBattleTacticalStackState> tacticalStacks = new List<RunBattleTacticalStackState>
        {
            new RunBattleTacticalStackState(string.Empty, "Rusher", 0, 9, false),
            new RunBattleTacticalStackState(string.Empty, "Rusher", 1, 25, false)
        };

        RunBattleArmySnapshot afterBattle = RunBattleTacticalStackReconciler.BuildPlayerArmyAfterBattle(
            beforeBattle,
            tacticalStacks,
            true);

        Assert.That(FindStack(afterBattle, "stack-rusher").Amount, Is.EqualTo(25));
        Assert.That(FindStack(afterBattle, "stack-rusher").Lost, Is.EqualTo(5));
        Assert.That(FindStack(afterBattle, "slot-1").Amount, Is.EqualTo(9));
        Assert.That(FindStack(afterBattle, "slot-1").Lost, Is.EqualTo(3));
    }

    [Test]
    public void BuildPlayerArmyAfterBattle_PrefersExplicitStackIdOverBattleInputOrder()
    {
        RunBattleArmySnapshot beforeBattle = Army(
            Stack("stack-rusher", "Rusher", 30, 30 * 31),
            Stack("slot-1", "Rusher", 12, 12 * 31));
        List<RunBattleTacticalStackState> tacticalStacks = new List<RunBattleTacticalStackState>
        {
            new RunBattleTacticalStackState("stack-rusher", "Rusher", 0, 27, false),
            new RunBattleTacticalStackState("slot-1", "Rusher", 1, 8, false)
        };

        RunBattleArmySnapshot afterBattle = RunBattleTacticalStackReconciler.BuildPlayerArmyAfterBattle(
            beforeBattle,
            tacticalStacks,
            true);

        Assert.That(FindStack(afterBattle, "stack-rusher").Amount, Is.EqualTo(27));
        Assert.That(FindStack(afterBattle, "stack-rusher").Lost, Is.EqualTo(3));
        Assert.That(FindStack(afterBattle, "slot-1").Amount, Is.EqualTo(8));
        Assert.That(FindStack(afterBattle, "slot-1").Lost, Is.EqualTo(4));
    }

    [Test]
    public void BuildPlayerArmyAfterBattle_FallsBackToUnitIdForSingleStackLegacyPayload()
    {
        RunBattleArmySnapshot beforeBattle = Army(Stack("stack-rusher", "Rusher", 28, 28 * 31));
        List<RunBattleTacticalStackState> tacticalStacks = new List<RunBattleTacticalStackState>
        {
            new RunBattleTacticalStackState(string.Empty, "Rusher", -1, 18, false)
        };

        RunBattleArmySnapshot afterBattle = RunBattleTacticalStackReconciler.BuildPlayerArmyAfterBattle(
            beforeBattle,
            tacticalStacks,
            true);

        Assert.That(FindStack(afterBattle, "stack-rusher").Amount, Is.EqualTo(18));
        Assert.That(FindStack(afterBattle, "stack-rusher").Lost, Is.EqualTo(10));
    }

    [Test]
    public void BuildPlayerArmyAfterBattle_MissingTacticalStackTreatsPreparedStackAsLost()
    {
        RunBattleArmySnapshot beforeBattle = Army(
            Stack("stack-rusher", "Rusher", 28, 28 * 31),
            Stack("stack-thrower", "Thrower", 10, 10 * 60));
        List<RunBattleTacticalStackState> tacticalStacks = new List<RunBattleTacticalStackState>
        {
            new RunBattleTacticalStackState(string.Empty, "Rusher", 0, 22, false)
        };

        RunBattleArmySnapshot afterBattle = RunBattleTacticalStackReconciler.BuildPlayerArmyAfterBattle(
            beforeBattle,
            tacticalStacks,
            true);

        Assert.That(FindStack(afterBattle, "stack-rusher").Amount, Is.EqualTo(22));
        Assert.That(FindStack(afterBattle, "stack-rusher").Lost, Is.EqualTo(6));
        Assert.That(FindStack(afterBattle, "stack-thrower").Amount, Is.EqualTo(0));
        Assert.That(FindStack(afterBattle, "stack-thrower").Lost, Is.EqualTo(10));
    }

    [Test]
    public void BuildPlayerArmyAfterBattle_LossOutcomeZerosPreparedStacks()
    {
        RunBattleArmySnapshot beforeBattle = Army(
            Stack("stack-rusher", "Rusher", 28, 28 * 31),
            Stack("stack-thrower", "Thrower", 10, 10 * 60));
        List<RunBattleTacticalStackState> tacticalStacks = new List<RunBattleTacticalStackState>
        {
            new RunBattleTacticalStackState(string.Empty, "Rusher", 0, 22, false),
            new RunBattleTacticalStackState(string.Empty, "Thrower", 1, 7, false)
        };

        RunBattleArmySnapshot afterBattle = RunBattleTacticalStackReconciler.BuildPlayerArmyAfterBattle(
            beforeBattle,
            tacticalStacks,
            false);

        Assert.That(FindStack(afterBattle, "stack-rusher").Amount, Is.EqualTo(0));
        Assert.That(FindStack(afterBattle, "stack-rusher").Lost, Is.EqualTo(28));
        Assert.That(FindStack(afterBattle, "stack-thrower").Amount, Is.EqualTo(0));
        Assert.That(FindStack(afterBattle, "stack-thrower").Lost, Is.EqualTo(10));
    }

    private static RunBattleArmySnapshot Army(params RunBattleStackSnapshot[] stacks)
    {
        int totalValue = 0;
        for (int i = 0; i < stacks.Length; i++)
        {
            totalValue += stacks[i].CombatValue;
        }

        return new RunBattleArmySnapshot("snapshot-before", totalValue, new List<RunBattleStackSnapshot>(stacks));
    }

    private static RunBattleStackSnapshot Stack(string stackId, string unitId, int amount, int combatValue)
    {
        return new RunBattleStackSnapshot(
            stackId,
            unitId,
            unitId,
            "I",
            1,
            amount,
            0,
            combatValue,
            new List<RunBattleSkillState> { new RunBattleSkillState("skill-" + unitId, true) });
    }

    private static RunBattleStackSnapshot FindStack(RunBattleArmySnapshot army, string stackId)
    {
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            if (army.Stacks[i].StackId == stackId)
            {
                return army.Stacks[i];
            }
        }

        return null;
    }
}
