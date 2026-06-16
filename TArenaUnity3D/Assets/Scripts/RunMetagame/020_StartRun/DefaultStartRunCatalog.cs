using System.Collections.Generic;

public class DefaultStartRunCatalog : IStartingArmyTemplateSource, IRunRoutePreviewSource
{
    public List<StartingArmyTemplate> ListStartingArmies()
    {
        return new List<StartingArmyTemplate>
        {
            new StartingArmyTemplate(
                "barbarian-starter",
                "barbarian-starter-v1",
                "Barbarian Starter",
                "Direct melee pressure with a small support line.",
                0,
                new List<StartRunStackTemplate>
                {
                    Stack("Rusher", "I", 1, 28, Unlocked("Chope"), Locked("Rush")),
                    Stack("Thrower", "I", 1, 10, Unlocked("Range_Stance_Barb"), Unlocked("Double_Throw"), Locked("Axe_Rain")),
                    Stack("Healer", "I", 1, 5, Unlocked("Tough_Skin"), Locked("Defence_Ritual")),
                    Stack("Wisp", "I", 1, 22, Unlocked("Blind_by_light"), Locked("Unstoppable_Light"))
                }),
            new StartingArmyTemplate(
                "lizard-breakout",
                "lizard-breakout-v1",
                "Lizard Breakout",
                "Traps, repositioning, and a stronger mid-run pivot.",
                0,
                new List<StartRunStackTemplate>
                {
                    Stack("Trapper", "I", 1, 24, Unlocked("Range_Stance_Lizard"), Unlocked("Spike_Trap"), Locked("Rope_Trap")),
                    Stack("Healer", "I", 1, 6, Unlocked("Tough_Skin"), Locked("Defence_Ritual")),
                    Stack("Specialist", "II", 1, 3, Unlocked("Force_Pull"), Locked("Stone_Stance")),
                    Stack("Wisp", "I", 1, 18, Unlocked("Blind_by_light"), Locked("Unstoppable_Light"))
                }),
            new StartingArmyTemplate(
                "stone-spark",
                "stone-spark-v1",
                "Stone Spark",
                "Low mass start with one tough stack and many fragile bodies.",
                0,
                new List<StartRunStackTemplate>
                {
                    Stack("Wisp", "I", 1, 34, Unlocked("Blind_by_light"), Locked("Unstoppable_Light")),
                    Stack("StoneGolem", "II", 1, 7, Unlocked("Stone_Throw"), Locked("Stone_Skin")),
                    Stack("Rusher", "I", 1, 18, Unlocked("Chope"), Locked("Rush")),
                    Stack("Healer", "I", 1, 4, Unlocked("Tough_Skin"), Locked("Defence_Ritual"))
                })
        };
    }

    public List<RoutePreviewTemplate> ListRoutePreviews()
    {
        return new List<RoutePreviewTemplate>
        {
            new RoutePreviewTemplate(
                "iron-line",
                "Iron Line",
                "Steady battles, one shop, balanced reward hints.",
                1650),
            new RoutePreviewTemplate(
                "relic-trail",
                "Relic Trail",
                "Earlier pivot reward, then shop into a heavier final.",
                1550),
            new RoutePreviewTemplate(
                "risk-road",
                "Risk Road",
                "More pressure before the shop, better growth if losses are controlled.",
                1800)
        };
    }

    private static StartRunStackTemplate Stack(string unitId, string tier, int level, int amount, params StartRunSkillTemplate[] skills)
    {
        return new StartRunStackTemplate(unitId, tier, level, amount, new List<StartRunSkillTemplate>(skills));
    }

    private static StartRunSkillTemplate Unlocked(string skillId)
    {
        return new StartRunSkillTemplate(skillId, true);
    }

    private static StartRunSkillTemplate Locked(string skillId)
    {
        return new StartRunSkillTemplate(skillId, false);
    }
}
