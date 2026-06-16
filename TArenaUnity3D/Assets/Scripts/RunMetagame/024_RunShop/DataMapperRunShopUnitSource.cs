public class DataMapperRunShopUnitSource : IRunShopUnitDefinitionSource
{
    private readonly DataMapper dataMapper;

    public DataMapperRunShopUnitSource()
        : this(DataMapper.Instance)
    {
    }

    public DataMapperRunShopUnitSource(DataMapper dataMapper)
    {
        this.dataMapper = dataMapper;
    }

    public RunShopUnitDefinition FindUnit(string unitId)
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

        return RunMetagameUnitDefinitionMapper.ToRunShopUnitDefinition(unit);
    }
}
