public static class RewardMapArmyFooterSlotMapper
{
    public static int ResolveVisibleSlotIndex(
        RewardMapArmySnapshot displayedArmy,
        RewardMapStackSnapshot changedStack,
        int fallbackSlotIndex)
    {
        if (displayedArmy == null || displayedArmy.Stacks == null)
        {
            return fallbackSlotIndex;
        }

        string changedStackId = changedStack == null ? string.Empty : changedStack.StackId;
        int visibleIndex = 0;
        for (int i = 0; i < displayedArmy.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = displayedArmy.Stacks[i];
            if (stack == null || string.IsNullOrEmpty(stack.UnitId) || stack.Amount <= 0)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(changedStackId) && stack.StackId == changedStackId)
            {
                return visibleIndex;
            }

            visibleIndex++;
        }

        if (changedStack != null && !string.IsNullOrEmpty(changedStack.UnitId) && changedStack.Amount > 0)
        {
            return visibleIndex;
        }

        return fallbackSlotIndex;
    }
}
