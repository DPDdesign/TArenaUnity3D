using System.Collections.Generic;

public static class OfflineArmySnapshotDiff
{
    public static OfflineArmySnapshotLossDiff BuildLosses(OfflineArmySnapshotRecord beforeSnapshot, OfflineArmySnapshotRecord afterSnapshot)
    {
        List<OfflineArmySnapshotStackLoss> losses = new List<OfflineArmySnapshotStackLoss>();
        List<OfflineArmySnapshotStackRecord> beforeStacks = beforeSnapshot == null ? null : beforeSnapshot.Stacks;
        List<OfflineArmySnapshotStackRecord> afterStacks = afterSnapshot == null ? null : afterSnapshot.Stacks;

        if (beforeStacks == null)
        {
            return new OfflineArmySnapshotLossDiff(losses);
        }

        for (int i = 0; i < beforeStacks.Count; i++)
        {
            OfflineArmySnapshotStackRecord beforeStack = beforeStacks[i];
            if (beforeStack == null)
            {
                continue;
            }

            OfflineArmySnapshotStackRecord afterStack = FindStack(afterStacks, beforeStack.FormationSlot);
            int afterAmount = afterStack == null ? 0 : afterStack.Amount;
            int lostAmount = beforeStack.Amount - afterAmount;
            if (lostAmount <= 0)
            {
                continue;
            }

            losses.Add(
                new OfflineArmySnapshotStackLoss(
                    beforeStack.FormationSlot,
                    beforeStack.UnitId,
                    beforeStack.Amount,
                    afterAmount));
        }

        return new OfflineArmySnapshotLossDiff(losses);
    }

    private static OfflineArmySnapshotStackRecord FindStack(List<OfflineArmySnapshotStackRecord> stacks, int formationSlot)
    {
        if (stacks == null)
        {
            return null;
        }

        for (int i = 0; i < stacks.Count; i++)
        {
            OfflineArmySnapshotStackRecord stack = stacks[i];
            if (stack != null && stack.FormationSlot == formationSlot)
            {
                return stack;
            }
        }

        return null;
    }
}
