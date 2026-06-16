using System.Collections.Generic;

public interface IRunBattleEncounterSource
{
    RunBattleEncounterDefinition FindEncounter(string routeNodeId, string encounterId);
}

public interface IRunBattleLaunchAdapter
{
    RunBattleLaunchRecord CreateLaunchRecord(RunBattleLaunchPayload payload);
}

public interface IRunBattleStore
{
    RunBattleLaunchViewData SavePreparedBattle(RunBattleLaunchViewData preparedBattle);
    RunBattleLaunchViewData FindPreparedBattle(string runBattleId);
    RunBattleCompletionRecord SaveCompletion(RunBattleCompletionRecord completionRecord);
}

public class InMemoryRunBattleStore : IRunBattleStore
{
    private readonly List<RunBattleLaunchViewData> preparedBattles = new List<RunBattleLaunchViewData>();
    private readonly List<RunBattleCompletionRecord> completions = new List<RunBattleCompletionRecord>();

    public List<RunBattleLaunchViewData> PreparedBattles
    {
        get { return new List<RunBattleLaunchViewData>(preparedBattles); }
    }

    public List<RunBattleCompletionRecord> Completions
    {
        get { return new List<RunBattleCompletionRecord>(completions); }
    }

    public RunBattleLaunchViewData SavePreparedBattle(RunBattleLaunchViewData preparedBattle)
    {
        if (preparedBattle == null)
        {
            return null;
        }

        for (int i = 0; i < preparedBattles.Count; i++)
        {
            if (preparedBattles[i] != null && preparedBattles[i].RunBattleId == preparedBattle.RunBattleId)
            {
                preparedBattles[i] = preparedBattle;
                return preparedBattle;
            }
        }

        preparedBattles.Add(preparedBattle);
        return preparedBattle;
    }

    public RunBattleLaunchViewData FindPreparedBattle(string runBattleId)
    {
        if (string.IsNullOrEmpty(runBattleId))
        {
            return null;
        }

        for (int i = 0; i < preparedBattles.Count; i++)
        {
            if (preparedBattles[i] != null && preparedBattles[i].RunBattleId == runBattleId)
            {
                return preparedBattles[i];
            }
        }

        return null;
    }

    public RunBattleCompletionRecord SaveCompletion(RunBattleCompletionRecord completionRecord)
    {
        if (completionRecord != null)
        {
            completions.Add(completionRecord);
        }

        return completionRecord;
    }
}
