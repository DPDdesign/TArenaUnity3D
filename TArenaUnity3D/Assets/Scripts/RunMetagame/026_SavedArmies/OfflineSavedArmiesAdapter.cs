public class OfflineSavedArmiesAdapter
{
    private readonly SavedArmiesService service;

    public OfflineSavedArmiesAdapter(SavedArmiesService service)
    {
        this.service = service;
    }

    public SavedArmiesRosterViewData BuildRoster(SavedArmiesRosterRequest request)
    {
        return service.BuildRoster(request);
    }

    public SavedArmyCommandResult LoadSeedArmy(SavedArmyImportCommand command)
    {
        return service.LoadSeedArmy(command);
    }

    public SavedArmyCommandResult ImportFromArena(SavedArmyImportCommand command)
    {
        // Legacy compatibility entry point for existing UI wiring.
        return LoadSeedArmy(command);
    }

    public SavedArmyCommandResult SaveRunArmy(SavedArmySaveRunCommand command)
    {
        return service.SaveRunArmy(command);
    }

    public SavedArmyCommandResult SetDefence(string savedArmyId)
    {
        return service.SetDefence(savedArmyId);
    }
}
