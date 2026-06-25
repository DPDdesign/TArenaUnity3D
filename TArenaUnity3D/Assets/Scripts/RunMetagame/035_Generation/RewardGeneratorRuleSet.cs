using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardGeneratorRuleSet", menuName = "TArena/Run Metagame/Reward Generator Rule Set")]
public class RewardGeneratorRuleSet : ScriptableObject
{
    [SerializeField] private float recoveryRateFromLoss = 1.0f;
    [SerializeField] private float recoveryRateFromEnemy = 0.55f;
    [SerializeField] private float growthRate = 0.18f;
    [SerializeField] private float quantityMultiplier = 1.20f;
    [SerializeField] private float tierMultiplier = 1.00f;

    public float RecoveryRateFromLoss { get { return Mathf.Max(0f, recoveryRateFromLoss); } }
    public float RecoveryRateFromEnemy { get { return Mathf.Max(0f, recoveryRateFromEnemy); } }
    public float GrowthRate { get { return Mathf.Max(0f, growthRate); } }
    public float QuantityMultiplier { get { return Mathf.Max(0f, quantityMultiplier); } }
    public float TierMultiplier { get { return Mathf.Max(0f, tierMultiplier); } }

    public int CalculateArmyGrowthReward(int battleLossValue, int enemyArmyValue, int armyValueBeforeBattle)
    {
        float recoveryFromLoss = Mathf.Max(0, battleLossValue) * RecoveryRateFromLoss;
        float recoveryFromEnemy = Mathf.Max(0, enemyArmyValue) * RecoveryRateFromEnemy;
        float growth = Mathf.Max(0, armyValueBeforeBattle) * GrowthRate;
        return Math.Max(1, (int)Math.Round(Math.Min(recoveryFromLoss, recoveryFromEnemy) + growth));
    }

    public int CalculateRewardValue(RewardMapOperationType operationType, int battleLossValue, int enemyArmyValue, int armyValueBeforeBattle)
    {
        int armyGrowthReward = CalculateArmyGrowthReward(battleLossValue, enemyArmyValue, armyValueBeforeBattle);
        float multiplier = IsTierOperation(operationType) ? TierMultiplier : QuantityMultiplier;
        return Math.Max(1, (int)Math.Round(armyGrowthReward * multiplier));
    }

#if UNITY_EDITOR
    public void Configure(float lossRate, float enemyRate, float growth, float quantity, float tier)
    {
        recoveryRateFromLoss = Mathf.Max(0f, lossRate);
        recoveryRateFromEnemy = Mathf.Max(0f, enemyRate);
        growthRate = Mathf.Max(0f, growth);
        quantityMultiplier = Mathf.Max(0f, quantity);
        tierMultiplier = Mathf.Max(0f, tier);
    }
#endif

    private static bool IsTierOperation(RewardMapOperationType operationType)
    {
        return operationType == RewardMapOperationType.PromoteStack ||
            operationType == RewardMapOperationType.DowngradeStack;
    }
}
