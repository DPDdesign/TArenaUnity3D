using System.Collections.Generic;

public class DataMapperStartRunUnitSource : IStartRunUnitPoolSource
{
    private readonly DataMapper dataMapper;

    public DataMapperStartRunUnitSource()
        : this(DataMapper.Instance)
    {
    }

    public DataMapperStartRunUnitSource(DataMapper dataMapper)
    {
        this.dataMapper = dataMapper;
    }

    public StartRunUnitDefinition FindUnit(string unitId)
    {
        if (dataMapper == null || string.IsNullOrEmpty(unitId))
        {
            return null;
        }

        DataMapper.UnitDefinition unit = dataMapper.FindUnit(unitId);
        if (unit == null)
        {
            return null;
        }

        return RunMetagameUnitDefinitionMapper.ToStartRunUnitDefinition(unit);
    }

    public List<StartRunUnitDefinition> ListUnits()
    {
        List<StartRunUnitDefinition> result = new List<StartRunUnitDefinition>();
        if (dataMapper == null)
        {
            return result;
        }

        List<DataMapper.UnitDefinition> units = dataMapper.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            StartRunUnitDefinition mapped = RunMetagameUnitDefinitionMapper.ToStartRunUnitDefinition(units[i]);
            if (mapped != null)
            {
                result.Add(mapped);
            }
        }

        return result;
    }
}
