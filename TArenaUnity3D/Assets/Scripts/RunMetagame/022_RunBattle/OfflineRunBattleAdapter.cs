public class OfflineRunBattleAdapter
{
    private readonly RunBattleService service;

    public OfflineRunBattleAdapter()
        : this(OfflineModeDatabaseComposition.CreateRunBattleService())
    {
    }

    public OfflineRunBattleAdapter(IRunBattleStore store)
    {
        service = new RunBattleService(
            new DefaultRunBattleEncounterCatalog(),
            new OfflineRunBattleLaunchAdapter(),
            store);
    }

    public OfflineRunBattleAdapter(RunBattleService service)
    {
        this.service = service;
    }

    public RunBattleLaunchViewData PrepareBattle(RunBattlePrepareRequest request)
    {
        return service.PrepareBattle(request);
    }

    public RunBattleCompletionResult CompleteBattle(RunBattleCompletionPayload payload)
    {
        return service.CompleteBattle(payload);
    }
}
