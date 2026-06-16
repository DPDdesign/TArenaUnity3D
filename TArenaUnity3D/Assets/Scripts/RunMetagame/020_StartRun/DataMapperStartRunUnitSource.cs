public class DataMapperStartRunUnitSource : IStartRunUnitDefinitionSource
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
}
