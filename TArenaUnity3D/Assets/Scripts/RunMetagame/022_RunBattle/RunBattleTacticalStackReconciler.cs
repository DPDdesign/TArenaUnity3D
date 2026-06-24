using System;
using System.Collections.Generic;

public sealed class RunBattleTacticalStackState
{
    public string StackId;
    public string UnitId;
    public int BattleInputOrder;
    public int Amount;
    public bool IsDead;

    public RunBattleTacticalStackState(
        string stackId,
        string unitId,
        int battleInputOrder,
        int amount,
        bool isDead)
    {
        StackId = stackId;
        UnitId = unitId;
        BattleInputOrder = battleInputOrder;
        Amount = Math.Max(0, amount);
        IsDead = isDead;
    }
}

public static class RunBattleTacticalStackReconciler
{
    public static RunBattleArmySnapshot BuildPlayerArmyAfterBattle(
        RunBattleArmySnapshot beforeBattle,
        List<RunBattleTacticalStackState> tacticalStacks,
        bool playerWon)
    {
        List<RunBattleStackSnapshot> resultStacks = new List<RunBattleStackSnapshot>();
        List<PreparedBattleInputStack> preparedInputOrder = BuildPreparedBattleInputOrder(beforeBattle);
        List<RunBattleTacticalStackState> runtimeStacks = tacticalStacks ?? new List<RunBattleTacticalStackState>();
        bool[] usedRuntimeStacks = new bool[runtimeStacks.Count];
        int totalValue = 0;

        if (beforeBattle != null && beforeBattle.Stacks != null)
        {
            for (int i = 0; i < beforeBattle.Stacks.Count; i++)
            {
                RunBattleStackSnapshot beforeStack = beforeBattle.Stacks[i];
                if (beforeStack == null)
                {
                    continue;
                }

                int battleInputOrder = FindBattleInputOrder(preparedInputOrder, beforeStack);
                int amountAfter = playerWon
                    ? ResolveRemainingAmount(beforeStack, battleInputOrder, runtimeStacks, usedRuntimeStacks)
                    : 0;
                int unitValue = beforeStack.Amount <= 0
                    ? beforeStack.CombatValue
                    : beforeStack.CombatValue / Math.Max(1, beforeStack.Amount);
                int combatValue = Math.Max(0, amountAfter * Math.Max(0, unitValue));
                totalValue += combatValue;

                resultStacks.Add(new RunBattleStackSnapshot(
                    beforeStack.StackId,
                    beforeStack.UnitId,
                    beforeStack.DisplayName,
                    beforeStack.Tier,
                    beforeStack.Level,
                    amountAfter,
                    Math.Max(0, beforeStack.Amount - amountAfter),
                    combatValue,
                    CloneSkills(beforeStack.Skills)));
            }
        }

        return new RunBattleArmySnapshot(beforeBattle == null ? string.Empty : beforeBattle.SnapshotId, totalValue, resultStacks);
    }

    public static List<RunBattleStackSnapshot> BuildPreparedStacksInBattleInputOrder(RunBattleArmySnapshot army)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>();
        if (army == null || army.Stacks == null)
        {
            return stacks;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = army.Stacks[i];
            if (stack == null || stack.Amount <= 0 || string.IsNullOrEmpty(ResolveBattleInputUnitId(stack)))
            {
                continue;
            }

            stacks.Add(stack);
        }

        stacks.Sort(CompareBattleInputStackIds);
        return stacks;
    }

    public static string ResolveBattleInputUnitId(RunBattleStackSnapshot stack)
    {
        if (stack == null)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(stack.UnitId) ? stack.DisplayName : stack.UnitId;
    }

    private static int ResolveRemainingAmount(
        RunBattleStackSnapshot beforeStack,
        int battleInputOrder,
        List<RunBattleTacticalStackState> runtimeStacks,
        bool[] usedRuntimeStacks)
    {
        int matchIndex = FindByStackId(beforeStack.StackId, runtimeStacks, usedRuntimeStacks);
        if (matchIndex < 0)
        {
            matchIndex = FindByBattleInputOrder(beforeStack, battleInputOrder, runtimeStacks, usedRuntimeStacks);
        }

        if (matchIndex < 0)
        {
            // Legacy tactical callers may only expose unit names. This fallback is ambiguous
            // for duplicate unit stacks, so stack id and battle-input order are tried first.
            matchIndex = FindByUnitId(ResolveBattleInputUnitId(beforeStack), runtimeStacks, usedRuntimeStacks);
        }

        if (matchIndex < 0)
        {
            return 0;
        }

        usedRuntimeStacks[matchIndex] = true;
        RunBattleTacticalStackState runtimeStack = runtimeStacks[matchIndex];
        return runtimeStack == null || runtimeStack.IsDead ? 0 : Math.Max(0, runtimeStack.Amount);
    }

    private static int FindByStackId(
        string stackId,
        List<RunBattleTacticalStackState> runtimeStacks,
        bool[] usedRuntimeStacks)
    {
        if (string.IsNullOrEmpty(stackId))
        {
            return -1;
        }

        for (int i = 0; i < runtimeStacks.Count; i++)
        {
            RunBattleTacticalStackState runtimeStack = runtimeStacks[i];
            if (!usedRuntimeStacks[i] && runtimeStack != null && runtimeStack.StackId == stackId)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindByBattleInputOrder(
        RunBattleStackSnapshot beforeStack,
        int battleInputOrder,
        List<RunBattleTacticalStackState> runtimeStacks,
        bool[] usedRuntimeStacks)
    {
        if (battleInputOrder < 0)
        {
            return -1;
        }

        string unitId = ResolveBattleInputUnitId(beforeStack);
        for (int i = 0; i < runtimeStacks.Count; i++)
        {
            RunBattleTacticalStackState runtimeStack = runtimeStacks[i];
            if (usedRuntimeStacks[i] || runtimeStack == null || runtimeStack.BattleInputOrder != battleInputOrder)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(unitId) && !string.IsNullOrEmpty(runtimeStack.UnitId) && runtimeStack.UnitId != unitId)
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private static int FindByUnitId(
        string unitId,
        List<RunBattleTacticalStackState> runtimeStacks,
        bool[] usedRuntimeStacks)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            return -1;
        }

        for (int i = 0; i < runtimeStacks.Count; i++)
        {
            RunBattleTacticalStackState runtimeStack = runtimeStacks[i];
            if (!usedRuntimeStacks[i] && runtimeStack != null && runtimeStack.UnitId == unitId)
            {
                return i;
            }
        }

        return -1;
    }

    private static List<PreparedBattleInputStack> BuildPreparedBattleInputOrder(RunBattleArmySnapshot beforeBattle)
    {
        List<RunBattleStackSnapshot> orderedStacks = BuildPreparedStacksInBattleInputOrder(beforeBattle);
        List<PreparedBattleInputStack> result = new List<PreparedBattleInputStack>();
        for (int i = 0; i < orderedStacks.Count; i++)
        {
            result.Add(new PreparedBattleInputStack(orderedStacks[i], i));
        }

        return result;
    }

    private static int FindBattleInputOrder(List<PreparedBattleInputStack> preparedInputOrder, RunBattleStackSnapshot stack)
    {
        for (int i = 0; i < preparedInputOrder.Count; i++)
        {
            if (preparedInputOrder[i].Stack == stack)
            {
                return preparedInputOrder[i].BattleInputOrder;
            }
        }

        return -1;
    }

    private static int CompareBattleInputStackIds(RunBattleStackSnapshot left, RunBattleStackSnapshot right)
    {
        string leftId = left == null ? string.Empty : left.StackId;
        string rightId = right == null ? string.Empty : right.StackId;
        return string.CompareOrdinal(leftId, rightId);
    }

    private static List<RunBattleSkillState> CloneSkills(List<RunBattleSkillState> skills)
    {
        List<RunBattleSkillState> result = new List<RunBattleSkillState>();
        if (skills == null)
        {
            return result;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            RunBattleSkillState skill = skills[i];
            if (skill != null)
            {
                result.Add(new RunBattleSkillState(skill.SkillId, skill.Unlocked));
            }
        }

        return result;
    }

    private sealed class PreparedBattleInputStack
    {
        public readonly RunBattleStackSnapshot Stack;
        public readonly int BattleInputOrder;

        public PreparedBattleInputStack(RunBattleStackSnapshot stack, int battleInputOrder)
        {
            Stack = stack;
            BattleInputOrder = battleInputOrder;
        }
    }
}
