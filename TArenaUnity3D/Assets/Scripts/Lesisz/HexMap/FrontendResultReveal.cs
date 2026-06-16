public enum FrontendResultRevealSource
{
    Skill,
    BasicAttack,
    Counterattack
}

public enum FrontendResultRevealKind
{
    Damage,
    Heal,
    Status
}

public enum FrontendTargetReaction
{
    Hit = 0,
    Buff = 1,
    Debuff = 2,
    None = 3
}

public sealed class FrontendResultReveal
{
    public readonly FrontendResultRevealSource SourceType;
    public readonly FrontendResultRevealKind ResultKind;
    public readonly FrontendTargetReaction TargetReaction;
    public readonly TosterHexUnit SourceUnit;
    public readonly TosterHexUnit TargetUnit;
    public readonly TosterView TargetView;
    public readonly int Damage;
    public readonly bool TargetSurvived;
    public readonly bool DamageWasReduced;

    public FrontendResultReveal(
        FrontendResultRevealSource sourceType,
        TosterHexUnit sourceUnit,
        TosterHexUnit targetUnit,
        TosterView targetView,
        int damage,
        bool targetSurvived,
        bool damageWasReduced = false,
        FrontendTargetReaction targetReaction = FrontendTargetReaction.Hit)
        : this(sourceType, FrontendResultRevealKind.Damage, sourceUnit, targetUnit, targetView, damage, targetSurvived, damageWasReduced, targetReaction)
    {
    }

    public FrontendResultReveal(
        FrontendResultRevealSource sourceType,
        FrontendResultRevealKind resultKind,
        TosterHexUnit sourceUnit,
        TosterHexUnit targetUnit,
        TosterView targetView,
        int damage,
        bool targetSurvived,
        bool damageWasReduced = false,
        FrontendTargetReaction targetReaction = FrontendTargetReaction.Hit)
    {
        SourceType = sourceType;
        ResultKind = resultKind;
        TargetReaction = targetReaction;
        SourceUnit = sourceUnit;
        TargetUnit = targetUnit;
        TargetView = targetView;
        Damage = damage;
        TargetSurvived = targetSurvived;
        DamageWasReduced = damageWasReduced;
    }

    public bool ShouldReveal
    {
        get { return TargetUnit != null && TargetView != null && TargetReaction != FrontendTargetReaction.None && (ResultKind != FrontendResultRevealKind.Damage || Damage > 0); }
    }

    public FrontendResultReveal WithTargetReaction(FrontendTargetReaction targetReaction)
    {
        return new FrontendResultReveal(SourceType, ResultKind, SourceUnit, TargetUnit, TargetView, Damage, TargetSurvived, DamageWasReduced, targetReaction);
    }
}

public static class FrontendResultRevealPlayer
{
    public static void Play(FrontendResultReveal reveal)
    {
        if (reveal == null || !reveal.ShouldReveal)
        {
            return;
        }

        reveal.TargetView.StartCoroutine(reveal.TargetUnit.RevealFrontendResult(reveal));
    }
}
