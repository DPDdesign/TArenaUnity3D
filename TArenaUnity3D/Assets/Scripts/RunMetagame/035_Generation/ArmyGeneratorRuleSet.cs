using UnityEngine;

[CreateAssetMenu(fileName = "ArmyGeneratorRuleSet", menuName = "TArena/Run Metagame/Army Generator Rule Set")]
public class ArmyGeneratorRuleSet : ScriptableObject
{
    [SerializeField] private int[] allowedFactionIds = new[] { 1, 2, 3 };
    [SerializeField] private int factionMixCount = 1;
    [SerializeField] private int minPower = 1450;
    [SerializeField] private int maxPower = 1750;
    [SerializeField] private int maxTier1Count = 4;
    [SerializeField] private int maxTier2Count = 3;
    [SerializeField] private int maxTier3Count = 1;
    [SerializeField] private int maxTier4Count = 0;
    [SerializeField] private int offerCount = 3;
    [SerializeField] private int stackCount = 4;
    [SerializeField] private int startingGold = 150;
    [SerializeField] private int startingRerollTokens = 1;
    [SerializeField] private int battleSkipTokens = 0;

    public int[] AllowedFactionIds { get { return allowedFactionIds == null ? new int[0] : (int[])allowedFactionIds.Clone(); } }
    public int FactionMixCount { get { return Mathf.Max(1, factionMixCount); } }
    public int MinPower { get { return Mathf.Max(0, minPower); } }
    public int MaxPower { get { return Mathf.Max(MinPower, maxPower); } }
    public int MaxTier1Count { get { return Mathf.Max(0, maxTier1Count); } }
    public int MaxTier2Count { get { return Mathf.Max(0, maxTier2Count); } }
    public int MaxTier3Count { get { return Mathf.Max(0, maxTier3Count); } }
    public int MaxTier4Count { get { return Mathf.Max(0, maxTier4Count); } }
    public int OfferCount { get { return Mathf.Max(1, offerCount); } }
    public int StackCount { get { return Mathf.Max(1, stackCount); } }
    public int StartingGold { get { return Mathf.Max(0, startingGold); } }
    public int StartingRerollTokens { get { return Mathf.Max(0, startingRerollTokens); } }
    public int BattleSkipTokens { get { return Mathf.Max(0, battleSkipTokens); } }

#if UNITY_EDITOR
    public void ConfigureMockDefaults()
    {
        allowedFactionIds = new[] { 1, 2, 3 };
        factionMixCount = 1;
        minPower = 1450;
        maxPower = 1750;
        maxTier1Count = 4;
        maxTier2Count = 3;
        maxTier3Count = 1;
        maxTier4Count = 0;
        offerCount = 3;
        stackCount = 4;
        startingGold = 150;
        startingRerollTokens = 1;
        battleSkipTokens = 0;
    }
#endif
}
