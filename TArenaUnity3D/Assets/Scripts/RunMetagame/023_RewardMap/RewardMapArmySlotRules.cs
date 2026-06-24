using System.Globalization;

public static class RewardMapArmySlotRules
{
    public const int MaxRewardArmyStacks = 7;

    public static int FindFirstFreeFormationSlot(RewardMapArmySnapshot army)
    {
        bool[] occupied = new bool[MaxRewardArmyStacks];
        if (army != null && army.Stacks != null)
        {
            for (int i = 0; i < army.Stacks.Count; i++)
            {
                RewardMapStackSnapshot stack = army.Stacks[i];
                if (stack == null)
                {
                    continue;
                }

                int formationSlot = ResolveFormationSlot(stack.StackId, i);
                if (formationSlot >= 0 && formationSlot < occupied.Length)
                {
                    occupied[formationSlot] = true;
                }
            }
        }

        for (int i = 0; i < occupied.Length; i++)
        {
            if (!occupied[i])
            {
                return i;
            }
        }

        return -1;
    }

    public static string ToFormationSlotStackId(int formationSlot)
    {
        int clamped = formationSlot < 0 ? 0 : formationSlot;
        return "slot-" + clamped.ToString(CultureInfo.InvariantCulture);
    }

    private static int ResolveFormationSlot(string stackId, int fallbackIndex)
    {
        const string prefix = "slot-";
        if (!string.IsNullOrEmpty(stackId) && stackId.StartsWith(prefix))
        {
            int parsed;
            if (int.TryParse(stackId.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
        }

        return fallbackIndex;
    }
}
