using System;

public class BattleResultService
{
    private readonly IBattleResultStore store;

    public BattleResultService(IBattleResultStore store)
    {
        this.store = store;
    }

    public BattleResultViewData Record(BattleResultRecordRequest request)
    {
        if (request == null)
        {
            return Fail(BattleResultError.MissingResult, "Battle result payload is missing.");
        }

        if (request.AttackerArmy == null || string.IsNullOrEmpty(request.AttackerArmy.SavedArmyId))
        {
            return Fail(BattleResultError.MissingAttacker, "Attacker saved army is missing.");
        }

        if (request.DefenderArmy == null || string.IsNullOrEmpty(request.DefenderArmy.SavedArmyId))
        {
            return Fail(BattleResultError.MissingDefender, "Defender saved army is missing.");
        }

        int opponentRank = request.Opponent == null ? request.PlayerRankBefore : request.Opponent.RankBefore;
        int rankDelta = CalculateRankDelta(request.ResultKind, request.PlayerRankBefore, opponentRank);
        int rankAfter = Math.Max(0, request.PlayerRankBefore + rankDelta);
        int xpGained = CalculateAccountXp(request.ResultKind, request.AttackerArmy.ArmyValue, request.DefenderArmy.ArmyValue);
        int xpAfter = request.AccountXpBefore + xpGained;
        string nextUnlockPreview = BuildNextUnlockPreview(xpAfter);
        BattleResultAccountProgress accountProgress = BattleResultAccountProgress.FromTotalXp(xpAfter, nextUnlockPreview);
        BattleResultPreservationRecord preservation = new BattleResultPreservationRecord(
            request.AttackerArmy.SavedArmyId,
            request.DefenderArmy.SavedArmyId,
            true,
            true,
            "No saved army is stolen, destroyed, or edited by this result.");

        BattleResultViewData result = new BattleResultViewData(
            string.IsNullOrEmpty(request.AsyncBattleResultId) ? BuildGeneratedResultId() : request.AsyncBattleResultId,
            BattleResultGameMode.Offline,
            BattleResultAuthoritySource.LocalOfflineAdapter,
            request.ResultKind,
            CloneArmy(request.AttackerArmy),
            CloneArmy(request.DefenderArmy),
            CloneOpponent(request.Opponent),
            request.PlayerRankBefore,
            rankAfter,
            rankDelta,
            request.AccountXpBefore,
            xpGained,
            xpAfter,
            nextUnlockPreview,
            preservation,
            true,
            BattleResultError.None,
            "Offline async battle result recorded.",
            accountProgress);

        if (store != null)
        {
            store.Save(result);
            BattleResultViewData persisted = store.Find(result.AsyncBattleResultId);
            if (persisted != null)
            {
                return persisted;
            }
        }

        return result;
    }

    public BattleResultViewData Find(string asyncBattleResultId)
    {
        if (store == null || string.IsNullOrEmpty(asyncBattleResultId))
        {
            return null;
        }

        return store.Find(asyncBattleResultId);
    }

    private int CalculateRankDelta(BattleResultKind resultKind, int playerRank, int opponentRank)
    {
        bool win = resultKind == BattleResultKind.OffenceWin || resultKind == BattleResultKind.DefenceWin;
        int rankGap = opponentRank - playerRank;
        int baseDelta = win ? 24 : -18;
        int gapAdjustment = Math.Max(-12, Math.Min(18, rankGap / 25));
        if (!win)
        {
            gapAdjustment = -gapAdjustment / 2;
        }

        return baseDelta + gapAdjustment;
    }

    private int CalculateAccountXp(BattleResultKind resultKind, int attackerValue, int defenderValue)
    {
        bool win = resultKind == BattleResultKind.OffenceWin || resultKind == BattleResultKind.DefenceWin;
        int valuePressure = Math.Max(0, defenderValue - attackerValue) / 100;
        return win ? 80 + Math.Min(40, valuePressure * 5) : 25;
    }

    private string BuildNextUnlockPreview(int xpAfter)
    {
        if (xpAfter < 250)
        {
            return "Next unlock: starting-unit skill progress";
        }

        if (xpAfter < 500)
        {
            return "Next unlock: saved army slot progress";
        }

        return "Next unlock: future unit pool progress";
    }

    private BattleResultSavedArmySnapshot CloneArmy(BattleResultSavedArmySnapshot army)
    {
        if (army == null)
        {
            return null;
        }

        return new BattleResultSavedArmySnapshot(
            army.SavedArmyId,
            army.SnapshotId,
            army.DisplayName,
            army.ArmyValue,
            CloneStacks(army.Stacks));
    }

    private static System.Collections.Generic.List<BattleResultStackSnapshot> CloneStacks(System.Collections.Generic.List<BattleResultStackSnapshot> stacks)
    {
        System.Collections.Generic.List<BattleResultStackSnapshot> result = new System.Collections.Generic.List<BattleResultStackSnapshot>();
        if (stacks == null)
        {
            return result;
        }

        for (int i = 0; i < stacks.Count; i++)
        {
            BattleResultStackSnapshot stack = stacks[i];
            if (stack == null)
            {
                continue;
            }

            result.Add(new BattleResultStackSnapshot(
                stack.StackId,
                stack.UnitId,
                stack.DisplayName,
                stack.Amount,
                stack.CombatValue,
                CloneSkills(stack.Skills)));
        }

        return result;
    }

    private static System.Collections.Generic.List<BattleResultSkillState> CloneSkills(System.Collections.Generic.List<BattleResultSkillState> skills)
    {
        System.Collections.Generic.List<BattleResultSkillState> result = new System.Collections.Generic.List<BattleResultSkillState>();
        if (skills == null)
        {
            return result;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            BattleResultSkillState skill = skills[i];
            if (skill != null)
            {
                result.Add(new BattleResultSkillState(skill.SkillId, skill.Unlocked));
            }
        }

        return result;
    }

    private static BattleResultOpponentMetadata CloneOpponent(BattleResultOpponentMetadata opponent)
    {
        if (opponent == null)
        {
            return null;
        }

        return new BattleResultOpponentMetadata(
            opponent.OpponentId,
            opponent.DisplayName,
            opponent.RankBefore,
            opponent.ArmyValue,
            opponent.SimulatedOfflineOpponent);
    }

    private static string BuildGeneratedResultId()
    {
        return "async-battle-result-" + DateTime.UtcNow.Ticks.ToString();
    }

    private BattleResultViewData Fail(BattleResultError error, string message)
    {
        return new BattleResultViewData(
            string.Empty,
            BattleResultGameMode.Offline,
            BattleResultAuthoritySource.LocalOfflineAdapter,
            BattleResultKind.OffenceLoss,
            null,
            null,
            null,
            0,
            0,
            0,
            0,
            0,
            0,
            string.Empty,
            null,
            false,
            error,
            message);
    }
}
