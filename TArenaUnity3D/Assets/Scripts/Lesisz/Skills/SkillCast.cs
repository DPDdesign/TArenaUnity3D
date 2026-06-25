using System;
using System.Collections.Generic;

[Serializable]
public class SkillCast
{
    public string ActorUnitId = string.Empty;
    public string SkillId = string.Empty;
    public List<HexCoord> SelectedHexes = new List<HexCoord>();
    public HexCoord DestinationHex;
    public HexCoord ImpactHex;
    public string PrimaryTargetUnitId = string.Empty;
    public List<string> TargetUnitIds = new List<string>();
    public List<string> AffectedUnitIds = new List<string>();
    public List<HexCoord> AffectedHexes = new List<HexCoord>();
    public int CooldownTurns;
    public bool ConsumesTurn;
    public bool CanMoveAfterUse;
    public bool RepeatableInTurn;
    public SkillEffect[] Effects = new SkillEffect[0];

    public SkillCast Clone()
    {
        return new SkillCast
        {
            ActorUnitId = ActorUnitId,
            SkillId = SkillId,
            SelectedHexes = CopyHexes(SelectedHexes),
            DestinationHex = CloneHex(DestinationHex),
            ImpactHex = CloneHex(ImpactHex),
            PrimaryTargetUnitId = PrimaryTargetUnitId,
            TargetUnitIds = new List<string>(TargetUnitIds ?? new List<string>()),
            AffectedUnitIds = new List<string>(AffectedUnitIds ?? new List<string>()),
            AffectedHexes = CopyHexes(AffectedHexes),
            CooldownTurns = CooldownTurns,
            ConsumesTurn = ConsumesTurn,
            CanMoveAfterUse = CanMoveAfterUse,
            RepeatableInTurn = RepeatableInTurn,
            Effects = SkillEffect.CloneArray(Effects)
        };
    }

    static HexCoord CloneHex(HexCoord hex)
    {
        return hex == null ? null : new HexCoord(hex.C, hex.R);
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
