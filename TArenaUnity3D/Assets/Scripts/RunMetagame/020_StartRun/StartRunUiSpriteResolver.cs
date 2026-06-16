using UnityEngine;

public static class StartRunUiSpriteResolver
{
    public static Sprite LoadUnitSprite(DataMapper dataMapper, string unitId)
    {
        if (dataMapper == null || string.IsNullOrEmpty(unitId))
        {
            return null;
        }

        DataMapper.UnitDefinition unit = dataMapper.FindUnit(unitId);
        if (unit == null || string.IsNullOrEmpty(unit.SpritePath))
        {
            return null;
        }

        return dataMapper.LoadUnitSprite(unit.SpritePath);
    }
}
