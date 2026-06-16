public class OfflineRunShopAdapter
{
    private readonly RunShopService service;

    public OfflineRunShopAdapter()
        : this(OfflineModeDatabaseComposition.CreateRunShopService())
    {
    }

    public OfflineRunShopAdapter(IRunShopVisitStore visitStore)
    {
        service = new RunShopService(new DataMapperRunShopUnitSource(), visitStore);
    }

    public OfflineRunShopAdapter(RunShopService service)
    {
        this.service = service;
    }

    public RunShopVisitViewData BuildVisit(RunShopVisitRequest request, string focusedOfferId)
    {
        return service.BuildVisit(request, focusedOfferId);
    }

    public RunShopPurchaseResult BuyFocusedOffer(RunShopVisitViewData visit)
    {
        if (visit == null || visit.FocusedOffer == null)
        {
            return new RunShopPurchaseResult(
                false,
                RunShopPurchaseError.MissingOffer,
                "Select a shop offer.",
                null,
                null,
                0,
                null);
        }

        return service.Purchase(new RunShopPurchaseCommand(
            visit.VisitId,
            visit.FocusedOffer.OfferId,
            visit.RunCurrency,
            visit.CurrentArmy));
    }

    public RunShopPurchaseResult Purchase(RunShopPurchaseCommand command)
    {
        return service.Purchase(command);
    }

    public RunShopLeaveResult Leave(RunShopLeaveCommand command)
    {
        return service.LeaveVisit(command);
    }
}
