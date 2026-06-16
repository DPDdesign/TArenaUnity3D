using System.Collections.Generic;

public interface IStartingArmyTemplateSource
{
    List<StartingArmyTemplate> ListStartingArmies();
}

public interface IRunRoutePreviewSource
{
    List<RoutePreviewTemplate> ListRoutePreviews();
}

public interface IStartRunUnitDefinitionSource
{
    StartRunUnitDefinition FindUnit(string unitId);
}

public interface IStartRunRecordStore
{
    CreatedRunRecord SaveCreatedRun(CreatedRunRecord record);
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
