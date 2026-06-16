using System.Collections.Generic;

public interface IBattleResultStore
{
    void Save(BattleResultViewData result);
    BattleResultViewData Find(string asyncBattleResultId);
}

public class InMemoryBattleResultStore : IBattleResultStore
{
    private readonly List<BattleResultViewData> results = new List<BattleResultViewData>();

    public void Save(BattleResultViewData result)
    {
        if (result == null)
        {
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i] != null && results[i].AsyncBattleResultId == result.AsyncBattleResultId)
            {
                results[i] = result;
                return;
            }
        }

        results.Add(result);
    }

    public BattleResultViewData Find(string asyncBattleResultId)
    {
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i] != null && results[i].AsyncBattleResultId == asyncBattleResultId)
            {
                return results[i];
            }
        }

        return null;
    }
}
