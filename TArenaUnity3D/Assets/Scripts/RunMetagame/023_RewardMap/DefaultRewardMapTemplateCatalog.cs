using System.Collections.Generic;

public class DefaultRewardMapTemplateCatalog : IRewardMapTemplateCatalog
{
    public List<RewardMapTemplate> ListTemplates()
    {
        return new List<RewardMapTemplate>
        {
            Template("reward-grow-rusher", RewardMapFamily.Mass, RewardMapIntention.Strengthen, RewardMapRarity.Common, "Grow", "Rusher Reinforcements", "More bodies for the final army.", new RewardMapOperation(RewardMapOperationType.AddUnits, "stack-rusher", "Rusher", string.Empty, string.Empty, string.Empty, 12, 0)),
            Template("reward-recover-losses", RewardMapFamily.Recovery, RewardMapIntention.Stabilize, RewardMapRarity.Common, "Revive", "Recover Losses", "Restore part of the most damaged stack.", new RewardMapOperation(RewardMapOperationType.RecoverLosses, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 8, 0)),
            Template("reward-earn-run-gold", RewardMapFamily.Economy, RewardMapIntention.Stabilize, RewardMapRarity.Common, "Earn", "Battle Salvage", "Take RUN GOLD for the next shop.", new RewardMapOperation(RewardMapOperationType.GainCurrency, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, 60)),
            Template("reward-promote-rusher", RewardMapFamily.Quality, RewardMapIntention.Strengthen, RewardMapRarity.Uncommon, "Promote", "Rusher To Axeman", "Trade mass for a stronger melee stack.", new RewardMapOperation(RewardMapOperationType.PromoteStack, "stack-rusher", "Rusher", "Axeman", string.Empty, string.Empty, 9, 0)),
            Template("reward-add-trapper", RewardMapFamily.Width, RewardMapIntention.Pivot, RewardMapRarity.Uncommon, "Add", "Recruit Trappers", "Add a new trap role.", new RewardMapOperation(RewardMapOperationType.AddStack, string.Empty, "Trapper", string.Empty, string.Empty, "reward-stack-trapper", 10, 0)),
            Template("reward-teach-rush", RewardMapFamily.Skill, RewardMapIntention.Pivot, RewardMapRarity.Uncommon, "Teach", "Teach Rush", "Unlock Rush on a legal Rusher stack.", new RewardMapOperation(RewardMapOperationType.TeachSkill, "stack-rusher", "Rusher", string.Empty, "Rush", string.Empty, 0, 0)),
            Template("reward-grow-thrower", RewardMapFamily.Mass, RewardMapIntention.Strengthen, RewardMapRarity.Common, "Grow", "Thrower Reserves", "More ranged bodies.", new RewardMapOperation(RewardMapOperationType.AddUnits, "stack-thrower", "Thrower", string.Empty, string.Empty, string.Empty, 5, 0)),
            Template("reward-add-wisp", RewardMapFamily.Width, RewardMapIntention.Pivot, RewardMapRarity.Rare, "Add", "Wisp Signal", "Add a fragile utility stack.", new RewardMapOperation(RewardMapOperationType.AddStack, string.Empty, "Wisp", string.Empty, string.Empty, "reward-stack-wisp", 18, 0)),
            Template("reward-teach-defence", RewardMapFamily.Skill, RewardMapIntention.Strengthen, RewardMapRarity.Rare, "Teach", "Teach Defence Ritual", "Give a support skill to a legal Healer.", new RewardMapOperation(RewardMapOperationType.TeachSkill, "stack-healer", "Healer", string.Empty, "Defence_Ritual", string.Empty, 0, 0)),
            Template("reward-promote-wisp", RewardMapFamily.Quality, RewardMapIntention.Pivot, RewardMapRarity.Rare, "Promote", "Wisp To Stone Golem", "Trade fragile count for durable value.", new RewardMapOperation(RewardMapOperationType.PromoteStack, "stack-wisp", "Wisp", "StoneGolem", string.Empty, string.Empty, 4, 0)),
            Template("reward-heal-healer", RewardMapFamily.Recovery, RewardMapIntention.Stabilize, RewardMapRarity.Uncommon, "Heal", "Protect Support", "Recover a damaged Healer stack.", new RewardMapOperation(RewardMapOperationType.RecoverLosses, "stack-healer", "Healer", string.Empty, string.Empty, string.Empty, 5, 0)),
            Template("reward-run-gold-large", RewardMapFamily.Economy, RewardMapIntention.Pivot, RewardMapRarity.Uncommon, "Earn", "Risk Payout", "Take more RUN GOLD instead of direct power.", new RewardMapOperation(RewardMapOperationType.GainCurrency, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, 90))
        };
    }

    private static RewardMapTemplate Template(string id, RewardMapFamily family, RewardMapIntention intention, RewardMapRarity rarity, string verb, string title, string detail, RewardMapOperation operation)
    {
        return new RewardMapTemplate(id, family, intention, rarity, verb, title, detail, operation);
    }
}
