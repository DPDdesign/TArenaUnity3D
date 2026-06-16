using System.Collections.Generic;

public class DataMapperOfflineArmySnapshotCatalogResolver : IOfflineArmySnapshotCatalogResolver
{
    private readonly DataMapper dataMapper;

    public DataMapperOfflineArmySnapshotCatalogResolver(DataMapper dataMapper)
    {
        this.dataMapper = dataMapper;
    }

    public OfflineArmySnapshotUnitCatalogEntry FindUnit(string unitId)
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

        return new OfflineArmySnapshotUnitCatalogEntry(
            unit.Name,
            unit.Name,
            unit.Tier,
            unit.Cost,
            unit.SkillNames == null ? new List<string>() : new List<string>(unit.SkillNames));
    }
}
