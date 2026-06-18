using UnityEngine;

[DisallowMultipleComponent]
public class RunGenerationSession : MonoBehaviour
{
    public static RunGenerationSession Instance { get; private set; }

    [SerializeField] private ArmyGeneratorRuleSet startingArmyRuleSet;

    private DeterministicRunGenerationCatalog catalog;

    public DeterministicRunGenerationCatalog Catalog
    {
        get { return catalog; }
    }

    public ArmyGeneratorRuleSet StartingArmyRuleSet
    {
        get { return startingArmyRuleSet; }
    }

    public static RunGenerationSession CreateNew(
        DataMapperStartRunUnitSource unitSource,
        StartRunGenerationUnlockContext unlockContext)
    {
        RunGenerationSession session = RequireConfiguredInstance();
        session.Initialize(unitSource, unlockContext);
        DontDestroyOnLoad(session.gameObject);
        Instance = session;
        return session;
    }

    public static RunGenerationSession EnsureActive(
        DataMapperStartRunUnitSource unitSource,
        StartRunGenerationUnlockContext unlockContext)
    {
        if (Instance != null && Instance.Catalog != null)
        {
            return Instance;
        }

        return CreateNew(unitSource, unlockContext);
    }

    public static void DestroyActive()
    {
        if (Instance == null)
        {
            return;
        }

        RunGenerationSession active = Instance;
        active.catalog = null;
    }

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Initialize(
        DataMapperStartRunUnitSource unitSource,
        StartRunGenerationUnlockContext unlockContext)
    {
        if (startingArmyRuleSet == null)
        {
            throw new System.InvalidOperationException(
                "RunGenerationSession requires a Starting Army RuleSet assigned in the Inspector.");
        }

        StartingArmyGeneratorConfig armyConfig = StartingArmyGeneratorConfig.CreateRuntimeRandomized(startingArmyRuleSet);
        RouteGeneratorConfig routeConfig = RouteGeneratorConfig.CreateRuntimeRandomized(armyConfig.Seed.Value);
        catalog = new DeterministicRunGenerationCatalog(unitSource, startingArmyRuleSet, armyConfig, routeConfig, unlockContext);
    }

    private static RunGenerationSession RequireConfiguredInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RunGenerationSession sceneSession = FindObjectOfType<RunGenerationSession>();
        if (sceneSession != null)
        {
            Instance = sceneSession;
            return sceneSession;
        }

        throw new System.InvalidOperationException(
            "RunGenerationSession is missing. Add RunGenerationSession to a scene GameObject and assign a Starting Army RuleSet.");
    }
}
