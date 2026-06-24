using System;

public sealed class TacticalAICastManagerSkillIntentExecutor : ITacticalAISkillIntentExecutor
{
    static TacticalAICastManagerSkillIntentExecutor instance;

    public static TacticalAICastManagerSkillIntentExecutor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new TacticalAICastManagerSkillIntentExecutor();
            }

            return instance;
        }
    }

    public bool TryExecuteSkillIntent(
        TacticalAIExecutionRuntimeContext runtimeContext,
        TacticalAIActionIntent intent,
        TacticalAIRevalidatedIntent revalidatedIntent,
        out string failureReason)
    {
        failureReason = string.Empty;
        if (runtimeContext == null || runtimeContext.HasRequiredReferences == false)
        {
            failureReason = "Skill executor is missing live tactical runtime references.";
            return false;
        }

        if (runtimeContext.MouseControler == null)
        {
            failureReason = "MouseControler reference is missing.";
            return false;
        }

        if (revalidatedIntent == null || revalidatedIntent.Actor == null)
        {
            failureReason = "Revalidated skill intent has no actor.";
            return false;
        }

        TosterHexUnit actor = ResolveLiveUnit(runtimeContext, revalidatedIntent.Actor);
        if (actor == null)
        {
            failureReason = "Could not resolve live actor for skill intent " + revalidatedIntent.Actor.RuntimeUnitId + ".";
            return false;
        }

        HexClass targetHex = ResolveSkillTargetHex(runtimeContext, revalidatedIntent);
        if (targetHex == null && revalidatedIntent.TargetHex != null)
        {
            failureReason = "Could not resolve live target hex for skill intent.";
            return false;
        }

        bool started = runtimeContext.MouseControler.TryStartSkillAction(
            actor,
            revalidatedIntent.SkillSlot,
            revalidatedIntent.SkillId,
            targetHex,
            out failureReason);

        if (started == false && string.IsNullOrEmpty(failureReason))
        {
            failureReason = "MouseControler rejected the skill action during live CastManager execution.";
        }

        return started;
    }

    static TosterHexUnit ResolveLiveUnit(
        TacticalAIExecutionRuntimeContext runtimeContext,
        BattleUnitSnapshot snapshotUnit)
    {
        if (runtimeContext == null ||
            runtimeContext.HexMap == null ||
            runtimeContext.HexMap.Teams == null ||
            snapshotUnit == null ||
            snapshotUnit.TeamIndex < 0 ||
            snapshotUnit.TeamIndex >= runtimeContext.HexMap.Teams.Count)
        {
            return null;
        }

        TeamClass team = runtimeContext.HexMap.Teams[snapshotUnit.TeamIndex];
        if (team == null ||
            team.Tosters == null ||
            snapshotUnit.RosterIndexWithinTeam < 0 ||
            snapshotUnit.RosterIndexWithinTeam >= team.Tosters.Count)
        {
            return null;
        }

        TosterHexUnit unit = team.Tosters[snapshotUnit.RosterIndexWithinTeam];
        if (unit == null ||
            string.Equals(unit.Name, snapshotUnit.UnitName, StringComparison.Ordinal) == false ||
            unit.isDead ||
            unit.Amount <= 0)
        {
            return null;
        }

        HexClass liveHex = unit.Hex;
        int liveC = liveHex != null ? liveHex.C : unit.C;
        int liveR = liveHex != null ? liveHex.R : unit.R;
        if (liveC != snapshotUnit.C || liveR != snapshotUnit.R)
        {
            return null;
        }

        return unit;
    }

    static HexClass ResolveSkillTargetHex(
        TacticalAIExecutionRuntimeContext runtimeContext,
        TacticalAIRevalidatedIntent revalidatedIntent)
    {
        TacticalAIHexCoordinate targetCoordinate = revalidatedIntent.TargetHex ?? revalidatedIntent.DestinationHex;
        if (targetCoordinate == null || runtimeContext.HexMap == null)
        {
            return null;
        }

        return runtimeContext.HexMap.GetHexAt(targetCoordinate.C, targetCoordinate.R);
    }
}
