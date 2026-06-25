using System;

[Serializable]
public class SkillTarget
{
    public HexCoord Hex;
    public SkillTargetRole Role = SkillTargetRole.None;
    public string UnitId = string.Empty;
    public bool IsLegal = true;
    public string Reason = string.Empty;

    public SkillTarget()
    {
    }

    public SkillTarget(int c, int r, SkillTargetRole role, string unitId = "")
    {
        Hex = new HexCoord(c, r);
        Role = role;
        UnitId = unitId ?? string.Empty;
        IsLegal = true;
    }
}
