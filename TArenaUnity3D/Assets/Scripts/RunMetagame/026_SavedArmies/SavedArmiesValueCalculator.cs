using System;
using System.Collections.Generic;

public static class SavedArmiesValueCalculator
{
    public static int CalculateArmyValue(List<SavedArmyStackSnapshot> stacks, ISavedArmiesUnitDefinitionSource unitSource)
    {
        int total = 0;
        if (stacks == null)
        {
            return total;
        }

        for (int i = 0; i < stacks.Count; i++)
        {
            total += CalculateStackValue(stacks[i], unitSource);
        }

        return Math.Max(0, total);
    }

    public static int CalculateStackValue(SavedArmyStackSnapshot stack, ISavedArmiesUnitDefinitionSource unitSource)
    {
        if (stack == null)
        {
            return 0;
        }

        return Math.Max(0, stack.Amount) * ResolveUnitValue(stack.UnitId, unitSource);
    }

    public static SavedArmiesUnitDefinition ResolveUnit(string unitId, ISavedArmiesUnitDefinitionSource unitSource)
    {
        SavedArmiesUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(unitId);
        if (unit != null)
        {
            return unit;
        }

        return new SavedArmiesUnitDefinition(unitId, string.IsNullOrEmpty(unitId) ? "Unknown Unit" : unitId, "?", FallbackUnitValue(unitId));
    }

    public static int ResolveUnitValue(string unitId, ISavedArmiesUnitDefinitionSource unitSource)
    {
        SavedArmiesUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(unitId);
        if (unit != null && unit.Cost > 0)
        {
            return unit.Cost;
        }

        return FallbackUnitValue(unitId);
    }

    private static int FallbackUnitValue(string unitId)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            return 0;
        }

        switch (unitId)
        {
            case "Rusher":
                return 6;
            case "Thrower":
                return 7;
            case "Axeman":
                return 8;
            case "Tank":
                return 10;
            case "Wisp":
                return 9;
            case "Fire_Elemental":
                return 18;
            case "StoneGolem":
                return 20;
            case "Trapper":
                return 7;
            default:
                return 5;
        }
    }
}
