public class OfflineRunMapAdapter
{
    private readonly RunMapService service;

    public OfflineRunMapAdapter()
        : this(OfflineModeDatabaseComposition.CreateRunMapService())
    {
    }

    public OfflineRunMapAdapter(RunMapService service)
    {
        this.service = service;
    }

    public RunMapScreenViewData CreateOrLoad(RunMapCreateRequest request, string selectedNodeId)
    {
        return service.CreateOrLoad(request, selectedNodeId);
    }

    public RunMapTravelResult Travel(RunMapTravelCommand command)
    {
        return service.Travel(command);
    }
}
