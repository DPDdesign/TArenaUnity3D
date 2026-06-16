using System;
using System.Collections.Generic;
using System.Globalization;

public static class OfflineArmySnapshotFactory
{
    public static OfflineArmySnapshotRecord Create(
        int accountId,
        int runId,
        int savedArmyId,
        int nodeId,
        List<OfflineArmySnapshotStackRecord> stacks)
    {
        return new OfflineArmySnapshotRecord(
            0,
            accountId,
            runId,
            savedArmyId,
            nodeId,
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            true,
            NormalizeStacks(stacks));
    }

    public static OfflineArmySnapshotRecord Clone(OfflineArmySnapshotRecord snapshot)
    {
        if (snapshot == null)
        {
            return Create(0, 0, 0, 0, new List<OfflineArmySnapshotStackRecord>());
        }

        return new OfflineArmySnapshotRecord(
            snapshot.SnapshotId,
            snapshot.AccountId,
            snapshot.RunId,
            snapshot.SavedArmyId,
            snapshot.NodeId,
            snapshot.CreatedAtUtc,
            snapshot.IsActive,
            NormalizeStacks(snapshot.Stacks));
    }

    public static List<OfflineArmySnapshotStackRecord> NormalizeStacks(List<OfflineArmySnapshotStackRecord> stacks)
    {
        List<OfflineArmySnapshotStackRecord> normalized = new List<OfflineArmySnapshotStackRecord>();
        List<OfflineArmySnapshotStackRecord> source = stacks ?? new List<OfflineArmySnapshotStackRecord>();

        for (int i = 0; i < source.Count; i++)
        {
            OfflineArmySnapshotStackRecord stack = source[i];
            if (stack == null || string.IsNullOrEmpty(stack.UnitId))
            {
                continue;
            }

            normalized.Add(
                new OfflineArmySnapshotStackRecord(
                    stack.SnapshotStackId,
                    stack.UnitId,
                    stack.Amount,
                    stack.FormationSlot < 0 ? i : stack.FormationSlot,
                    stack.IsActive,
                    NormalizeSkills(stack.Skills)));
        }

        normalized.Sort(CompareStacks);
        EnsureUniqueFormationSlots(normalized);

        return normalized;
    }

    public static List<OfflineArmySnapshotStackSkillRecord> NormalizeSkills(List<OfflineArmySnapshotStackSkillRecord> skills)
    {
        List<OfflineArmySnapshotStackSkillRecord> normalized = new List<OfflineArmySnapshotStackSkillRecord>();
        List<OfflineArmySnapshotStackSkillRecord> source = skills ?? new List<OfflineArmySnapshotStackSkillRecord>();

        for (int i = 0; i < source.Count; i++)
        {
            OfflineArmySnapshotStackSkillRecord skill = source[i];
            if (skill == null || string.IsNullOrEmpty(skill.SkillId) || ContainsSkill(normalized, skill.SkillId))
            {
                continue;
            }

            normalized.Add(
                new OfflineArmySnapshotStackSkillRecord(
                    skill.SnapshotStackSkillId,
                    skill.SkillId,
                    skill.AcquiredAtRunNodeId,
                    skill.IsActive));
        }

        return normalized;
    }

    private static int CompareStacks(OfflineArmySnapshotStackRecord left, OfflineArmySnapshotStackRecord right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int slotComparison = left.FormationSlot.CompareTo(right.FormationSlot);
        if (slotComparison != 0)
        {
            return slotComparison;
        }

        return string.CompareOrdinal(left.UnitId, right.UnitId);
    }

    private static bool ContainsSkill(List<OfflineArmySnapshotStackSkillRecord> skills, string skillId)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].SkillId == skillId)
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureUniqueFormationSlots(List<OfflineArmySnapshotStackRecord> stacks)
    {
        List<int> usedSlots = new List<int>();

        for (int i = 0; i < stacks.Count; i++)
        {
            OfflineArmySnapshotStackRecord stack = stacks[i];
            if (stack == null)
            {
                continue;
            }

            int slot = stack.FormationSlot;
            while (ContainsSlot(usedSlots, slot))
            {
                slot++;
            }

            stack.FormationSlot = slot;
            usedSlots.Add(slot);
        }
    }

    private static bool ContainsSlot(List<int> usedSlots, int slot)
    {
        for (int i = 0; i < usedSlots.Count; i++)
        {
            if (usedSlots[i] == slot)
            {
                return true;
            }
        }

        return false;
    }
}
