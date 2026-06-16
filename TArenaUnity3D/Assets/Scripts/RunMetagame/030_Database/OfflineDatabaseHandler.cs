using UnityEngine;

public class OfflineDatabaseHandler : MonoBehaviour
{
    public static OfflineDatabaseHandler Instance { get; private set; }

    [SerializeField] private bool openOnAwake = true;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool logDatabasePath = true;

    private OfflineDatabaseOpenResult lastOpenResult;

    public OfflineDatabaseOpenResult LastOpenResult
    {
        get { return lastOpenResult; }
    }

    private void Awake()
    {
        if (!RegisterSingleton())
        {
            return;
        }

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (openOnAwake)
        {
            EnsureDatabaseReady();
        }
    }

    public OfflineDatabaseOpenResult EnsureDatabaseReady()
    {
        OfflineDatabaseModule module = new OfflineDatabaseModule();
        lastOpenResult = module.OpenOrCreate();

        if (logDatabasePath)
        {
            Debug.Log("[OfflineDatabaseHandler] Database path: " + module.DatabasePath);
        }

        if (!lastOpenResult.Success)
        {
            Debug.LogError("[OfflineDatabaseHandler] Database open failed: " + lastOpenResult.Message);
            return lastOpenResult;
        }

        Debug.Log(
            "[OfflineDatabaseHandler] Database ready. Provider=" +
            lastOpenResult.ProviderName +
            ", SchemaVersion=" +
            lastOpenResult.SchemaVersion);

        return lastOpenResult;
    }

    private bool RegisterSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            return true;
        }

        if (Instance == this)
        {
            return true;
        }

        Destroy(gameObject);
        return false;
    }
}
