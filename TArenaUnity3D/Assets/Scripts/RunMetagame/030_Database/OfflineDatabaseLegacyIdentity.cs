using System;
using System.Globalization;

public static class OfflineDatabaseLegacyIdentity
{
    public static bool TryParseIntId(string value, out int id)
    {
        id = 0;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        int parsed;
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed > 0)
        {
            id = parsed;
            return true;
        }

        string suffix = ExtractNumericSuffix(trimmed);
        if (!string.IsNullOrEmpty(suffix) && int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed > 0)
        {
            id = parsed;
            return true;
        }

        return false;
    }

    public static int ParseIntIdOrDefault(string value, int fallback = 0)
    {
        int id;
        return TryParseIntId(value, out id) ? id : fallback;
    }

    public static int ParseSlotIndexOrDefault(string slotId, int fallback = 0)
    {
        int parsed = ParseIntIdOrDefault(slotId, fallback + 1);
        return Math.Max(0, parsed - 1);
    }

    public static string ToLegacyRunId(int runId)
    {
        return ToPrefixedId("run", runId);
    }

    public static string ToLegacySnapshotId(int snapshotId)
    {
        return ToPrefixedId("snapshot", snapshotId);
    }

    public static string ToLegacyRouteMapId(int routeMapId)
    {
        return ToPrefixedId("route-map", routeMapId);
    }

    public static string ToLegacyRewardChoiceId(int rewardChoiceId)
    {
        return ToPrefixedId("reward-choice", rewardChoiceId);
    }

    public static string ToLegacyRunBattleId(int runBattleId)
    {
        return ToPrefixedId("run-battle", runBattleId);
    }

    public static string ToLegacyShopVisitId(int shopVisitId)
    {
        return ToPrefixedId("shop-visit", shopVisitId);
    }

    public static string ToLegacySavedArmyId(int savedArmyId)
    {
        return ToPrefixedId("saved-army", savedArmyId);
    }

    public static string ToLegacyAsyncBattleResultId(int asyncBattleResultId)
    {
        return ToPrefixedId("async-battle-result", asyncBattleResultId);
    }

    public static string ToLegacySlotId(int slotIndex)
    {
        return "slot-" + (slotIndex + 1).ToString("00", CultureInfo.InvariantCulture);
    }

    public static string ToLegacyFormationSlotId(int formationSlot)
    {
        return "slot-" + formationSlot.ToString(CultureInfo.InvariantCulture);
    }

    private static string ToPrefixedId(string prefix, int id)
    {
        if (id <= 0)
        {
            return prefix + "-unsaved";
        }

        return prefix + "-" + id.ToString(CultureInfo.InvariantCulture);
    }

    private static string ExtractNumericSuffix(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        int end = value.Length - 1;
        while (end >= 0 && char.IsDigit(value[end]))
        {
            end--;
        }

        if (end == value.Length - 1)
        {
            return string.Empty;
        }

        return value.Substring(end + 1);
    }
}
