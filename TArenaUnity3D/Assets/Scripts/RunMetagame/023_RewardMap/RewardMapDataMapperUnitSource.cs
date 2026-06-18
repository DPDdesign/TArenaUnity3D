using System.Collections.Generic;

public class RewardMapDataMapperUnitSource : IRewardMapUnitPoolSource
{
    private readonly DataMapper dataMapper;

    public RewardMapDataMapperUnitSource()
        : this(DataMapper.Instance)
    {
    }

    public RewardMapDataMapperUnitSource(DataMapper dataMapper)
    {
        this.dataMapper = dataMapper;
    }

    public RunShopUnitDefinition FindUnit(string unitId)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            return null;
        }

        if (dataMapper == null)
        {
            return null;
        }

        return RunMetagameUnitDefinitionMapper.ToRunShopUnitDefinition(dataMapper.FindUnit(unitId));
    }

    public List<RunShopUnitDefinition> ListUnits()
    {
        List<RunShopUnitDefinition> result = new List<RunShopUnitDefinition>();
        if (dataMapper == null)
        {
            return result;
        }

        List<DataMapper.UnitDefinition> units = dataMapper.GetAllUnits();
        for (int i = 0; units != null && i < units.Count; i++)
        {
            RunShopUnitDefinition unit = RunMetagameUnitDefinitionMapper.ToRunShopUnitDefinition(units[i]);
            if (unit != null)
            {
                result.Add(unit);
            }
        }

        return result;
    }
}
