public class OfflineSummaryValueAdapter
{
    private readonly SummaryValueService service;

    public OfflineSummaryValueAdapter(SummaryValueService service)
    {
        this.service = service;
    }

    public SummaryValueScreenViewData BuildSummary(SummaryValueBuildRequest request)
    {
        return service.BuildSummary(request);
    }

    public SummaryValueSaveResult Save(SummaryValueSaveCommand command)
    {
        return service.Save(command);
    }
}
