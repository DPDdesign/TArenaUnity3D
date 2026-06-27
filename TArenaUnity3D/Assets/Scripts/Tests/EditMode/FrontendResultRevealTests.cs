#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class FrontendResultRevealTests
{
    [Test]
    public void StatusReveal_SelfTarget_DefaultsToBuffReaction()
    {
        TosterHexUnit unit = new TosterHexUnit();

        FrontendResultReveal reveal = unit.BuildStatusFrontendReveal(unit, FrontendResultRevealSource.Skill);

        Assert.That(reveal.TargetReaction, Is.EqualTo(FrontendTargetReaction.Buff));
        Assert.That(reveal.PreserveTargetReaction, Is.True);
    }

    [Test]
    public void StatusReveal_EnemyTarget_DefaultsToDebuffReaction()
    {
        TosterHexUnit source = new TosterHexUnit();
        TosterHexUnit target = new TosterHexUnit();

        FrontendResultReveal reveal = target.BuildStatusFrontendReveal(source, FrontendResultRevealSource.Skill);

        Assert.That(reveal.TargetReaction, Is.EqualTo(FrontendTargetReaction.Debuff));
        Assert.That(reveal.PreserveTargetReaction, Is.True);
    }

    [Test]
    public void DamageReveal_UsesLiveTargetViewFallback_WhenCachedViewIsMissing()
    {
        GameObject targetGo = new GameObject("TargetView");
        try
        {
            TosterHexUnit source = new TosterHexUnit();
            TosterHexUnit target = new TosterHexUnit();
            target.tosterView = targetGo.AddComponent<TosterView>();

            FrontendResultReveal reveal = new FrontendResultReveal(
                FrontendResultRevealSource.Skill,
                source,
                target,
                null,
                5,
                true);

            Assert.That(reveal.ResolvedTargetView, Is.SameAs(target.tosterView));
            Assert.That(reveal.ShouldReveal, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(targetGo);
        }
    }
}
#endif
