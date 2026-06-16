public static class OfflineModeDatabaseComposition
{
    public static OfflineDatabaseOpenResult OpenDefaultDatabase()
    {
        return new OfflineDatabaseModule().OpenOrCreate();
    }

    public static OfflineStartRunAdapter CreateStartRunAdapter()
    {
        return new OfflineStartRunAdapter(CreateStartRunService());
    }

    public static StartRunService CreateStartRunService()
    {
        EnsureDefaultDatabase();
        DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
        return new StartRunService(
            catalog,
            catalog,
            new DataMapperStartRunUnitSource(),
            new OfflineStartRunDbStore());
    }

    public static OfflineRunMapAdapter CreateRunMapAdapter()
    {
        return new OfflineRunMapAdapter(CreateRunMapService());
    }

    public static RunMapService CreateRunMapService()
    {
        EnsureDefaultDatabase();
        return new RunMapService(new DefaultRunMapPathCatalog(), new OfflineRunMapDbStore());
    }

    public static OfflineRunBattleAdapter CreateRunBattleAdapter()
    {
        return new OfflineRunBattleAdapter(CreateRunBattleService());
    }

    public static RunBattleService CreateRunBattleService()
    {
        EnsureDefaultDatabase();
        DefaultRunBattleEncounterCatalog encounterCatalog = new DefaultRunBattleEncounterCatalog();
        return new RunBattleService(
            encounterCatalog,
            new OfflineRunBattleLaunchAdapter(),
            new OfflineRunBattleDbStore(null, CreateSnapshotResolver(), encounterCatalog));
    }

    public static OfflineRewardMapAdapter CreateRewardMapAdapter()
    {
        return new OfflineRewardMapAdapter(CreateRewardMapService());
    }

    public static RewardMapService CreateRewardMapService()
    {
        EnsureDefaultDatabase();
        return new RewardMapService(
            new DefaultRewardMapTemplateCatalog(),
            new RewardMapDataMapperUnitSource(DataMapper.Instance),
            new OfflineRewardMapDbStore(null, CreateSnapshotResolver()));
    }

    public static OfflineRunShopAdapter CreateRunShopAdapter()
    {
        return new OfflineRunShopAdapter(CreateRunShopService());
    }

    public static RunShopService CreateRunShopService()
    {
        EnsureDefaultDatabase();
        return new RunShopService(new DataMapperRunShopUnitSource(), new OfflineRunShopDbStore(null, CreateSnapshotResolver()));
    }

    public static OfflineSummaryValueDbStore CreateSummaryValueStore()
    {
        EnsureDefaultDatabase();
        return new OfflineSummaryValueDbStore(null, CreateSnapshotResolver());
    }

    public static OfflineSummaryValueAdapter CreateSummaryValueAdapter(ISummaryValueRosterStore rosterStore)
    {
        return new OfflineSummaryValueAdapter(CreateSummaryValueService(rosterStore));
    }

    public static SummaryValueService CreateSummaryValueService(ISummaryValueRosterStore rosterStore)
    {
        EnsureDefaultDatabase();
        return new SummaryValueService(rosterStore);
    }

    public static OfflineSavedArmiesDbStore CreateSavedArmiesStore()
    {
        EnsureDefaultDatabase();
        return new OfflineSavedArmiesDbStore(null);
    }

    public static OfflineSavedArmiesAdapter CreateSavedArmiesAdapter(
        ISavedArmiesRosterStore rosterStore,
        ISavedArmiesSeedSource seedSource,
        ISavedArmiesAttackHistoryStore historyStore)
    {
        return new OfflineSavedArmiesAdapter(CreateSavedArmiesService(rosterStore, seedSource, historyStore));
    }

    public static SavedArmiesService CreateSavedArmiesService(
        ISavedArmiesRosterStore rosterStore,
        ISavedArmiesSeedSource seedSource,
        ISavedArmiesAttackHistoryStore historyStore)
    {
        EnsureDefaultDatabase();
        return new SavedArmiesService(
            rosterStore,
            seedSource,
            historyStore,
            new DataMapperSavedArmiesUnitSource());
    }

    public static OfflineBattleResultAdapter CreateBattleResultAdapter()
    {
        return new OfflineBattleResultAdapter(CreateBattleResultService());
    }

    public static BattleResultService CreateBattleResultService()
    {
        EnsureDefaultDatabase();
        return new BattleResultService(new OfflineBattleResultDbStore(null));
    }

    private static IOfflineArmySnapshotCatalogResolver CreateSnapshotResolver()
    {
        return new DataMapperOfflineArmySnapshotCatalogResolver(DataMapper.Instance);
    }

    private static void EnsureDefaultDatabase()
    {
        OfflineDatabaseOpenResult result = OpenDefaultDatabase();
        if (result == null || !result.Success)
        {
            throw new System.InvalidOperationException(result == null ? "Offline database could not be opened." : result.Message);
        }
    }
}
