public static class OfflineModeDatabaseComposition
{
    private static StartRunService runtimeStartRunService;

    public static OfflineDatabaseOpenResult OpenDefaultDatabase()
    {
        return new OfflineDatabaseModule().OpenOrCreate();
    }

    public static void ResetStartRunOfferSession()
    {
        runtimeStartRunService = null;
        RunGenerationSession.DestroyActive();
    }

    public static void StartNewRunGenerationSession(string accountPlayerId)
    {
        runtimeStartRunService = null;
        DataMapperStartRunUnitSource unitSource = new DataMapperStartRunUnitSource();
        StartRunGenerationUnlockContext unlockContext = new OfflineStartRunUnlockContextSource()
            .LoadUnlockContext(string.IsNullOrEmpty(accountPlayerId) ? "offline-player" : accountPlayerId);
        RunGenerationSession.CreateNew(unitSource, unlockContext);
    }

    public static void EndRunGenerationSession()
    {
        runtimeStartRunService = null;
        RunGenerationSession.DestroyActive();
    }

    public static OfflineStartRunAdapter CreateStartRunAdapter()
    {
        return new OfflineStartRunAdapter(CreateStartRunService());
    }

    public static StartRunService CreateStartRunService()
    {
        EnsureDefaultDatabase();
        if (runtimeStartRunService == null)
        {
            DataMapperStartRunUnitSource unitSource = new DataMapperStartRunUnitSource();
            StartRunGenerationUnlockContext unlockContext = new OfflineStartRunUnlockContextSource().LoadUnlockContext("offline-player");
            RunGenerationSession session = CreateRuntimeRunGenerationSession(unitSource, unlockContext);
            DeterministicRunGenerationCatalog catalog = session.Catalog;
            runtimeStartRunService = new StartRunService(
                catalog,
                catalog,
                unitSource,
                new OfflineStartRunDbStore(null, catalog, CreateSnapshotResolver(), unitSource, session.EnemyEncounterRuleCatalog),
                new OfflineStartRunSlotAvailabilitySource());
        }

        return runtimeStartRunService;
    }

    public static OfflineRunMapAdapter CreateRunMapAdapter()
    {
        return new OfflineRunMapAdapter(CreateRunMapService());
    }

    public static RunMapService CreateRunMapService()
    {
        EnsureDefaultDatabase();
        DeterministicRunGenerationCatalog catalog = CreateRuntimeRunGenerationCatalog(new DataMapperStartRunUnitSource());
        return new RunMapService(catalog, new OfflineRunMapDbStore(null, catalog));
    }

    public static OfflineRunBattleAdapter CreateRunBattleAdapter()
    {
        return new OfflineRunBattleAdapter(CreateRunBattleService());
    }

    public static RunBattleService CreateRunBattleService()
    {
        EnsureDefaultDatabase();
        IRunBattleEncounterSource encounterCatalog = new OfflineRunBattleEncounterCatalog(null, new DefaultRunBattleEncounterCatalog());
        RewardMapDataMapperUnitSource rewardUnitSource = new RewardMapDataMapperUnitSource(DataMapper.Instance);
        return new RunBattleService(
            encounterCatalog,
            new OfflineRunBattleLaunchAdapter(null, new OfflineArmySnapshotDbRepository(), CreateSnapshotResolver(), DataMapper.Instance),
            new OfflineRunBattleDbStore(null, CreateSnapshotResolver(), encounterCatalog, rewardUnitSource));
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
        DeterministicRunGenerationCatalog catalog = CreateRuntimeRunGenerationCatalog(new DataMapperStartRunUnitSource());
        return new RunShopService(new DataMapperRunShopUnitSource(), new OfflineRunShopDbStore(null, CreateSnapshotResolver(), catalog));
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

    public static OfflineRunContextDbReader CreateRunContextReader()
    {
        EnsureDefaultDatabase();
        return new OfflineRunContextDbReader(null, CreateSnapshotResolver());
    }

    private static IOfflineArmySnapshotCatalogResolver CreateSnapshotResolver()
    {
        return new DataMapperOfflineArmySnapshotCatalogResolver(DataMapper.Instance);
    }

    private static DeterministicRunGenerationCatalog CreateRuntimeRunGenerationCatalog(DataMapperStartRunUnitSource unitSource)
    {
        return CreateRuntimeRunGenerationCatalog(unitSource, null);
    }

    private static DeterministicRunGenerationCatalog CreateRuntimeRunGenerationCatalog(
        DataMapperStartRunUnitSource unitSource,
        StartRunGenerationUnlockContext unlockContext)
    {
        return CreateRuntimeRunGenerationSession(unitSource, unlockContext).Catalog;
    }

    private static RunGenerationSession CreateRuntimeRunGenerationSession(
        DataMapperStartRunUnitSource unitSource,
        StartRunGenerationUnlockContext unlockContext)
    {
        return RunGenerationSession.EnsureActive(unitSource, unlockContext);
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
