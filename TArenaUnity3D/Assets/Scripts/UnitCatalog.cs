using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitCatalog", menuName = "TArena/Units/Unit Catalog")]
public class UnitCatalog : ScriptableObject
{
    [SerializeField] private List<UnitDefinitionAsset> units = new List<UnitDefinitionAsset>();

    private Dictionary<string, UnitDefinitionAsset> unitLookup;

    public List<UnitDefinitionAsset> GetUnits()
    {
        return units == null ? new List<UnitDefinitionAsset>() : new List<UnitDefinitionAsset>(units);
    }

    public UnitDefinitionAsset FindUnitAsset(string unitName)
    {
        EnsureLookup();

        UnitDefinitionAsset asset;
        if (string.IsNullOrEmpty(unitName) || !unitLookup.TryGetValue(unitName, out asset))
        {
            return null;
        }

        return asset;
    }

    private void OnEnable()
    {
        ClearLookup();
    }

    private void OnValidate()
    {
        ClearLookup();
    }

    private void EnsureLookup()
    {
        if (unitLookup != null)
        {
            return;
        }

        unitLookup = new Dictionary<string, UnitDefinitionAsset>();
        List<UnitDefinitionAsset> sourceUnits = units ?? new List<UnitDefinitionAsset>();
        for (int i = 0; i < sourceUnits.Count; i++)
        {
            UnitDefinitionAsset unit = sourceUnits[i];
            if (unit == null || string.IsNullOrEmpty(unit.UnitName) || unitLookup.ContainsKey(unit.UnitName))
            {
                continue;
            }

            unitLookup.Add(unit.UnitName, unit);
        }
    }

    private void ClearLookup()
    {
        unitLookup = null;
    }

#if UNITY_EDITOR
    public void SetUnits(List<UnitDefinitionAsset> newUnits)
    {
        units = newUnits == null ? new List<UnitDefinitionAsset>() : new List<UnitDefinitionAsset>(newUnits);
        ClearLookup();
    }
#endif
}
