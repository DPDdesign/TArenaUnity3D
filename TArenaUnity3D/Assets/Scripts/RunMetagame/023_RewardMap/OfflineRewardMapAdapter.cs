public class OfflineRewardMapAdapter
{
    private readonly RewardMapService service;

    public OfflineRewardMapAdapter(RewardMapService service)
    {
        this.service = service;
    }

    public RewardMapChoiceViewData BuildChoice(RewardMapChoiceRequest request, string focusedRewardId)
    {
        return service.BuildChoice(request, focusedRewardId);
    }

    public RewardMapApplyResult Apply(RewardMapApplyCommand command)
    {
        return service.Apply(command);
    }
}
