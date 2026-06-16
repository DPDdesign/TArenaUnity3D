public class OfflineBattleResultAdapter
{
    private readonly BattleResultService service;

    public OfflineBattleResultAdapter(BattleResultService service)
    {
        this.service = service;
    }

    public BattleResultViewData Record(BattleResultRecordRequest request)
    {
        return service.Record(request);
    }

    public BattleResultViewData Find(string asyncBattleResultId)
    {
        return service.Find(asyncBattleResultId);
    }
}
