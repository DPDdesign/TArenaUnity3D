using System.Collections.Generic;

public interface IStartingArmyTemplateSource
{
    List<StartingArmyTemplate> ListStartingArmies();
}

public interface IRequestedStartingArmyTemplateSource : IStartingArmyTemplateSource
{
    List<StartingArmyTemplate> ListStartingArmies(int requestedOfferCount);
}

public interface IRunRoutePreviewSource
{
    List<RoutePreviewTemplate> ListRoutePreviews();
}

public interface IStartRunUnitDefinitionSource
{
    StartRunUnitDefinition FindUnit(string unitId);
}

public interface IStartRunUnitPoolSource : IStartRunUnitDefinitionSource
{
    List<StartRunUnitDefinition> ListUnits();
}

public interface IStartRunRecordStore
{
    CreatedRunRecord SaveCreatedRun(CreatedRunRecord record);
}

public interface IStartRunSlotAvailabilitySource
{
    StartRunSlotAvailabilityContext LoadAvailabilityContext(string accountPlayerId);
}

public class StartRunSlotAvailabilityContext
{
    public int AccountLevel;
    public bool HasWonRun;

    public StartRunSlotAvailabilityContext(int accountLevel, bool hasWonRun)
    {
        AccountLevel = System.Math.Max(1, accountLevel);
        HasWonRun = hasWonRun;
    }
}

public class StartRunSlotAvailability
{
    public int VisualSlotIndex;
    public bool IsLocked;
    public string LockedReason;

    public StartRunSlotAvailability(int visualSlotIndex, bool isLocked, string lockedReason)
    {
        VisualSlotIndex = System.Math.Max(0, visualSlotIndex);
        IsLocked = isLocked;
        LockedReason = lockedReason ?? string.Empty;
    }
}

public static class StartRunSlotAvailabilityRules
{
    public const string WinRunReason = "Win A Run";
    public const string ReachLevelReason = "Reach Level 5";
    public const string DemoReason = "Unavailable in DEMO";
    public const string ComingSoonReason = "Coming soon";

    public static StartRunSlotAvailability Evaluate(int visualSlotIndex, StartRunSlotAvailabilityContext context)
    {
        StartRunSlotAvailabilityContext safeContext = context ?? new StartRunSlotAvailabilityContext(1, false);
        int slotNumber = visualSlotIndex + 1;

        if (slotNumber == 2 && !safeContext.HasWonRun)
        {
            return new StartRunSlotAvailability(visualSlotIndex, true, WinRunReason);
        }

        if (slotNumber == 3 && safeContext.AccountLevel < 5)
        {
            return new StartRunSlotAvailability(visualSlotIndex, true, ReachLevelReason);
        }

        if (slotNumber == 4)
        {
            return new StartRunSlotAvailability(visualSlotIndex, true, DemoReason);
        }

        if (slotNumber >= 5)
        {
            return new StartRunSlotAvailability(visualSlotIndex, true, ComingSoonReason);
        }

        return new StartRunSlotAvailability(visualSlotIndex, false, string.Empty);
    }
}

public class UnrestrictedStartRunSlotAvailabilitySource : IStartRunSlotAvailabilitySource
{
    public StartRunSlotAvailabilityContext LoadAvailabilityContext(string accountPlayerId)
    {
        return new StartRunSlotAvailabilityContext(99, true);
    }
}

public class InMemoryStartRunRecordStore : IStartRunRecordStore
{
    private readonly List<CreatedRunRecord> records = new List<CreatedRunRecord>();

    public List<CreatedRunRecord> Records
    {
        get { return new List<CreatedRunRecord>(records); }
    }

    public CreatedRunRecord SaveCreatedRun(CreatedRunRecord record)
    {
        if (record != null)
        {
            records.Add(record);
        }

        return record;
    }
}
