using System.Collections.Generic;

public interface IRunShopUnitDefinitionSource
{
    RunShopUnitDefinition FindUnit(string unitId);
}

public interface IRunShopVisitStore
{
    void SaveVisit(RunShopVisitViewData visit);
    RunShopVisitViewData FindVisit(string visitId);
    RunShopVisitViewData FindVisit(string runId, string routeNodeId);
    bool HasPurchasedOffer(string visitId, string offerId);
    void SavePurchase(RunShopPurchaseRecord record, RunShopVisitViewData updatedVisit);
    RunShopLeaveResult LeaveVisit(RunShopLeaveCommand command);
}

public class InMemoryRunShopVisitStore : IRunShopVisitStore
{
    private readonly List<RunShopVisitViewData> visits = new List<RunShopVisitViewData>();
    private readonly List<RunShopPurchaseRecord> purchases = new List<RunShopPurchaseRecord>();

    public List<RunShopVisitViewData> Visits
    {
        get { return new List<RunShopVisitViewData>(visits); }
    }

    public List<RunShopPurchaseRecord> Purchases
    {
        get { return new List<RunShopPurchaseRecord>(purchases); }
    }

    public void SaveVisit(RunShopVisitViewData visit)
    {
        if (visit == null)
        {
            return;
        }

        for (int i = 0; i < visits.Count; i++)
        {
            if (visits[i] != null && visits[i].VisitId == visit.VisitId)
            {
                visits[i] = visit;
                return;
            }
        }

        visits.Add(visit);
    }

    public RunShopVisitViewData FindVisit(string visitId)
    {
        if (string.IsNullOrEmpty(visitId))
        {
            return null;
        }

        for (int i = 0; i < visits.Count; i++)
        {
            if (visits[i] != null && visits[i].VisitId == visitId)
            {
                return visits[i];
            }
        }

        return null;
    }

    public RunShopVisitViewData FindVisit(string runId, string routeNodeId)
    {
        if (string.IsNullOrEmpty(runId) || string.IsNullOrEmpty(routeNodeId))
        {
            return null;
        }

        for (int i = 0; i < visits.Count; i++)
        {
            RunShopVisitViewData visit = visits[i];
            if (visit != null && visit.RunId == runId && visit.RouteNodeId == routeNodeId)
            {
                return visit;
            }
        }

        return null;
    }

    public bool HasPurchasedOffer(string visitId, string offerId)
    {
        for (int i = 0; i < purchases.Count; i++)
        {
            RunShopPurchaseRecord purchase = purchases[i];
            if (purchase != null && purchase.VisitId == visitId && purchase.OfferId == offerId)
            {
                return true;
            }
        }

        return false;
    }

    public void SavePurchase(RunShopPurchaseRecord record, RunShopVisitViewData updatedVisit)
    {
        if (record != null)
        {
            purchases.Add(record);
        }

        if (updatedVisit != null)
        {
            SaveVisit(updatedVisit);
        }
    }

    public RunShopLeaveResult LeaveVisit(RunShopLeaveCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.VisitId))
        {
            return new RunShopLeaveResult(false, string.Empty, string.Empty, string.Empty, 0, null, "RunMap", "Missing run shop visit.");
        }

        RunShopVisitViewData visit = FindVisit(command.VisitId);
        if (visit == null)
        {
            return new RunShopLeaveResult(false, command.VisitId, string.Empty, string.Empty, 0, null, "RunMap", "Run shop visit was not found.");
        }

        if (!string.IsNullOrEmpty(command.FocusedOfferId))
        {
            visit.FocusedOffer = FindOffer(visit.Offers, command.FocusedOfferId);
        }

        SaveVisit(visit);
        return new RunShopLeaveResult(true, visit.VisitId, visit.RunId, visit.RouteNodeId, visit.RunCurrency, visit.CurrentArmy, "RunMap", "Leave shop accepted.");
    }

    private static RunShopOfferViewData FindOffer(List<RunShopOfferViewData> offers, string offerId)
    {
        if (offers == null || string.IsNullOrEmpty(offerId))
        {
            return null;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i] != null && offers[i].OfferId == offerId)
            {
                return offers[i];
            }
        }

        return null;
    }
}
