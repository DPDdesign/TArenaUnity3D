using System;
using System.Collections.Generic;

public sealed class EnemyEncounterArmyMaterializer
{
    private readonly IStartRunUnitPoolSource unitSource;
    private readonly EnemyEncounterRuleCatalog ruleCatalog;

    public EnemyEncounterArmyMaterializer(
        IStartRunUnitPoolSource unitSource,
        EnemyEncounterRuleCatalog ruleCatalog)
    {
        this.unitSource = unitSource;
        this.ruleCatalog = ruleCatalog;
    }

    public OfflineArmySnapshotRecord BuildSnapshot(
        OfflineRouteNodeSeedRecord node,
        int accountId,
        int runId,
        int runSeed)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (unitSource == null)
        {
            throw new InvalidOperationException("Enemy encounter materialization requires a unit pool source.");
        }

        if (ruleCatalog == null)
        {
            throw new InvalidOperationException("Enemy encounter materialization requires an EnemyEncounterRuleCatalog.");
        }

        EnemyEncounterRuleLookupResult lookup = ruleCatalog.Resolve(node.EncounterDifficulty);
        if (lookup == null || !lookup.Success || lookup.Rule == null)
        {
            throw new InvalidOperationException(lookup == null ? "Enemy encounter rule lookup failed." : lookup.Message);
        }

        if (lookup.Rule.IsPredefined)
        {
            throw new InvalidOperationException(
                "Predefined enemy encounters are reserved but army definitions are not implemented yet: " +
                lookup.Rule.ResolvedPredefinedEnemyId);
        }

        ArmyGeneratorRuleSet ruleSet = lookup.Rule.ResolvedArmyGeneratorRuleSet;
        if (ruleSet == null)
        {
            throw new InvalidOperationException("Generated enemy encounter is missing an ArmyGeneratorRuleSet.");
        }

        StartingArmyGeneratorConfig config = StartingArmyGeneratorConfig.CreateDefault(ruleSet);
        config.Seed = new RunGenerationSeed(BuildSeed(runSeed, node.NodeId, (int)node.EncounterDifficulty));

        DeterministicRunGenerationCatalog generator = new DeterministicRunGenerationCatalog(
            unitSource,
            ruleSet,
            config,
            RouteGeneratorConfig.CreateDefault(),
            new StartRunGenerationUnlockContext(null, null));

        List<StartingArmyTemplate> armies = generator.ListStartingArmies(1);
        if (armies.Count == 0 || armies[0] == null)
        {
            throw new InvalidOperationException("Enemy encounter generator did not produce an army.");
        }

        return OfflineArmySnapshotMapper.FromStartRun(ToRunArmySnapshot(armies[0]), accountId, runId, node.NodeId);
    }

    private static RunArmySnapshot ToRunArmySnapshot(StartingArmyTemplate army)
    {
        List<RunArmyStackSnapshot> stacks = new List<RunArmyStackSnapshot>();
        int totalValue = 0;

        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            StartRunStackTemplate stack = army.Stacks[i];
            if (stack == null)
            {
                continue;
            }

            List<StartRunSkillViewData> skills = new List<StartRunSkillViewData>();
            for (int skillIndex = 0; stack.Skills != null && skillIndex < stack.Skills.Count; skillIndex++)
            {
                StartRunSkillTemplate skill = stack.Skills[skillIndex];
                if (skill != null)
                {
                    skills.Add(new StartRunSkillViewData(skill.SkillId, skill.Unlocked));
                }
            }

            stacks.Add(new RunArmyStackSnapshot(
                stack.UnitId,
                stack.Tier,
                stack.Level,
                stack.Amount,
                0,
                skills));
        }

        return new RunArmySnapshot(army == null ? string.Empty : army.TemplateId, totalValue, stacks);
    }

    private static int BuildSeed(int runSeed, int nodeId, int difficulty)
    {
        unchecked
        {
            int seed = 17;
            seed = seed * 31 + runSeed;
            seed = seed * 31 + nodeId;
            seed = seed * 31 + difficulty;
            return seed & 0x7fffffff;
        }
    }
}
