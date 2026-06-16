using System;
using System.Data;
using System.IO;
using System.Reflection;

internal static class OfflineDatabaseProvider
{
    private const string MonoSqliteConnectionType = "Mono.Data.Sqlite.SqliteConnection, Mono.Data.Sqlite";
    private const string MicrosoftSqliteConnectionType = "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite";
    private const string SqlitePclBatteriesV2Type = "SQLitePCL.Batteries_V2, SQLitePCLRaw.batteries_v2";
    private const string SqlitePclBatteriesType = "SQLitePCL.Batteries, SQLitePCLRaw.batteries_v2";

    private static readonly object InitializationLock = new object();
    private static bool sqlitePclInitialized;

    public static OfflineDatabaseProviderResolution Resolve()
    {
        Type microsoftType = Type.GetType(MicrosoftSqliteConnectionType, false);
        if (microsoftType != null)
        {
            return new OfflineDatabaseProviderResolution(true, "Microsoft.Data.Sqlite", microsoftType, true, string.Empty);
        }

        Type monoType = Type.GetType(MonoSqliteConnectionType, false);
        if (monoType != null)
        {
            return new OfflineDatabaseProviderResolution(true, "Mono.Data.Sqlite", monoType, false, string.Empty);
        }

        return new OfflineDatabaseProviderResolution(
            false,
            string.Empty,
            null,
            false,
            "No supported SQLite provider assembly was found. Expected Mono.Data.Sqlite or Microsoft.Data.Sqlite.");
    }

    public static string Initialize(OfflineDatabaseProviderResolution provider)
    {
        if (provider == null || !provider.Success || !provider.RequiresInitialization)
        {
            return string.Empty;
        }

        lock (InitializationLock)
        {
            if (sqlitePclInitialized)
            {
                return string.Empty;
            }

            try
            {
                if (!TryInvokeStaticInit(SqlitePclBatteriesV2Type) && !TryInvokeStaticInit(SqlitePclBatteriesType))
                {
                    return "Could not find SQLitePCL batteries initializer for Microsoft.Data.Sqlite.";
                }

                sqlitePclInitialized = true;
                return string.Empty;
            }
            catch (Exception ex)
            {
                return "SQLite provider initialization failed: " + ex.Message;
            }
        }
    }

    public static IDbConnection CreateConnection(OfflineDatabaseProviderResolution provider, string databasePath)
    {
        if (provider == null || !provider.Success || provider.ConnectionType == null)
        {
            return null;
        }

        string fullPath = Path.GetFullPath(databasePath);
        string connectionString = BuildConnectionString(provider.ProviderName, fullPath);
        object connection = Activator.CreateInstance(provider.ConnectionType, connectionString);
        return connection as IDbConnection;
    }

    private static string BuildConnectionString(string providerName, string fullPath)
    {
        if (providerName == "Microsoft.Data.Sqlite")
        {
            return "Data Source=" + fullPath;
        }

        return "URI=file:" + fullPath;
    }

    private static bool TryInvokeStaticInit(string typeName)
    {
        Type batteriesType = Type.GetType(typeName, false);
        if (batteriesType == null)
        {
            return false;
        }

        MethodInfo initMethod = batteriesType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
        if (initMethod == null)
        {
            return false;
        }

        initMethod.Invoke(null, null);
        return true;
    }
}
