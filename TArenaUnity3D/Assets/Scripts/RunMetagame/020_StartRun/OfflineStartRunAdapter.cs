public class OfflineStartRunAdapter
{
    private readonly StartRunService service;

    public OfflineStartRunAdapter()
        : this(OfflineModeDatabaseComposition.CreateStartRunService())
    {
    }

    public OfflineStartRunAdapter(IStartRunRecordStore recordStore)
    {
        DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
        service = new StartRunService(
            catalog,
            catalog,
            new DataMapperStartRunUnitSource(),
            recordStore);
    }

    public OfflineStartRunAdapter(StartRunService service)
    {
        this.service = service;
    }

    public StartRunScreenViewData BuildScreen(string selectedStartingArmyId, string selectedRouteId)
    {
        return service.BuildScreen(selectedStartingArmyId, selectedRouteId);
    }

    public StartRunScreenViewData BuildScreen(
        string selectedStartingArmyId,
        string selectedRouteId,
        int requestedSlotCount,
        string accountPlayerId)
    {
        return service.BuildScreen(selectedStartingArmyId, selectedRouteId, requestedSlotCount, accountPlayerId);
    }

    public StartRunResult BeginRun(
        string accountPlayerId,
        string selectedStartingArmyId,
        string selectedRoutePreviewOptionId)
    {
        return BeginRun(accountPlayerId, selectedStartingArmyId, selectedRoutePreviewOptionId, 0);
    }

    public StartRunResult BeginRun(
        string accountPlayerId,
        string selectedStartingArmyId,
        string selectedRoutePreviewOptionId,
        int requestedSlotCount)
    {
        StartRunCommand command = new StartRunCommand(
            accountPlayerId,
            selectedStartingArmyId,
            selectedStartingArmyId,
            selectedStartingArmyId,
            selectedRoutePreviewOptionId,
            requestedSlotCount);

        return service.BeginRun(command);
    }
}
