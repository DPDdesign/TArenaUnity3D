public class RewardMapDataMapperUnitSource : IRewardMapUnitDefinitionSource
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
}
