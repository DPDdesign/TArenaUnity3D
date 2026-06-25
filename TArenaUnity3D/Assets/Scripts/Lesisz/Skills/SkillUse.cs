using System;
using System.Collections.Generic;

[Serializable]
public class SkillUse
{
    public string ActorUnitId = string.Empty;
    public string SkillId = string.Empty;
    public List<HexCoord> SelectedHexes = new List<HexCoord>();
    public string ClientRequestId = string.Empty;

    public SkillUse()
    {
    }

    public SkillUse(string actorUnitId, string skillId, IEnumerable<HexCoord> selectedHexes)
    {
        ActorUnitId = actorUnitId ?? string.Empty;
        SkillId = skillId ?? string.Empty;
        SelectedHexes = CopyHexes(selectedHexes);
    }

    static List<HexCoord> CopyHexes(IEnumerable<HexCoord> source)
    {
        List<HexCoord> result = new List<HexCoord>();
        if (source == null)
        {
            return result;
        }

        foreach (HexCoord hex in source)
        {
            if (hex != null)
            {
                result.Add(new HexCoord(hex.C, hex.R));
            }
        }

        return result;
    }
}
