using System.Collections.Generic;

public interface IRunMapPathCatalog
{
    List<RunMapPathDefinition> BuildPaths(string selectedRouteChoiceId);
}

public interface IRunMapStore
{
    RunMapStateRecord Save(RunMapStateRecord state);
    RunMapStateRecord Find(string runId);
}

public class InMemoryRunMapStore : IRunMapStore
{
    private readonly List<RunMapStateRecord> states = new List<RunMapStateRecord>();

    public RunMapStateRecord Save(RunMapStateRecord state)
    {
        if (state == null)
        {
            return null;
        }

        for (int i = 0; i < states.Count; i++)
        {
            if (states[i] != null && states[i].RunId == state.RunId)
            {
                states[i] = state;
                return state;
            }
        }

        states.Add(state);
        return state;
    }

    public RunMapStateRecord Find(string runId)
    {
        if (string.IsNullOrEmpty(runId))
        {
            return null;
        }

        for (int i = 0; i < states.Count; i++)
        {
            if (states[i] != null && states[i].RunId == runId)
            {
                return states[i];
            }
        }

        return null;
    }
}
