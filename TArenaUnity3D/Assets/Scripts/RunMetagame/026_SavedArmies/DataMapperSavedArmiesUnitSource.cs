public class DataMapperSavedArmiesUnitSource : ISavedArmiesUnitDefinitionSource
{
    private readonly DataMapper dataMapper;

    public DataMapperSavedArmiesUnitSource()
        : this(DataMapper.Instance)
    {
    }

    public DataMapperSavedArmiesUnitSource(DataMapper dataMapper)
    {
        this.dataMapper = dataMapper;
    }

    public SavedArmiesUnitDefinition FindUnit(string unitId)
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

        return new SavedArmiesUnitDefinition(unit.Name, unit.Name, unit.Tier, unit.Cost);
    }
}
