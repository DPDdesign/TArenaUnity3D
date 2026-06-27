using UnityEngine;

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
    public readonly bool PreserveTargetReaction;
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
        : this(sourceType, FrontendResultRevealKind.Damage, sourceUnit, targetUnit, targetView, damage, targetSurvived, damageWasReduced, targetReaction, false)
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
        FrontendTargetReaction targetReaction = FrontendTargetReaction.Hit,
        bool preserveTargetReaction = false)
    {
        SourceType = sourceType;
        ResultKind = resultKind;
        TargetReaction = targetReaction;
        PreserveTargetReaction = preserveTargetReaction;
        SourceUnit = sourceUnit;
        TargetUnit = targetUnit;
        TargetView = targetView;
        Damage = damage;
        TargetSurvived = targetSurvived;
        DamageWasReduced = damageWasReduced;
    }

    public TosterView ResolvedTargetView
    {
        get { return TargetView != null ? TargetView : TargetUnit != null ? TargetUnit.tosterView : null; }
    }

    public bool ShouldReveal
    {
        get { return TargetUnit != null && ResolvedTargetView != null && TargetReaction != FrontendTargetReaction.None; }
    }

    public FrontendResultReveal WithTargetReaction(FrontendTargetReaction targetReaction)
    {
        return new FrontendResultReveal(SourceType, ResultKind, SourceUnit, TargetUnit, TargetView, Damage, TargetSurvived, DamageWasReduced, targetReaction, PreserveTargetReaction);
    }
}

public static class FrontendResultRevealPlayer
{
    public static void Play(FrontendResultReveal reveal)
    {
        if (reveal == null || !reveal.ShouldReveal)
        {
            Debug.Log("[DEBUG-HITFLOW] FrontendResultRevealPlayer.Play skipped reveal=" +
                (reveal == null ? "<null>" : "kind=" + reveal.ResultKind +
                " reaction=" + reveal.TargetReaction +
                " damage=" + reveal.Damage +
                " target=" + (reveal.TargetUnit != null ? reveal.TargetUnit.Name : "<null>") +
                " targetView=" + (reveal.ResolvedTargetView != null ? reveal.ResolvedTargetView.name : "<null>") +
                " should=" + reveal.ShouldReveal));
            return;
        }

        Debug.Log("[DEBUG-HITFLOW] FrontendResultRevealPlayer.Play reveal kind=" + reveal.ResultKind +
            " reaction=" + reveal.TargetReaction +
            " damage=" + reveal.Damage +
            " target=" + (reveal.TargetUnit != null ? reveal.TargetUnit.Name : "<null>") +
            " targetView=" + (reveal.ResolvedTargetView != null ? reveal.ResolvedTargetView.name : "<null>"));
        reveal.ResolvedTargetView.StartCoroutine(reveal.TargetUnit.RevealFrontendResult(reveal));
    }
}
