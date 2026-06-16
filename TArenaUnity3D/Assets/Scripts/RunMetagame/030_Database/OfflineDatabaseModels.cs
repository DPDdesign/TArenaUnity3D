using System;

public enum OfflineDatabaseError
{
    None,
    InvalidPath,
    ProviderNotFound,
    ProviderInitializationFailed,
    OpenFailed,
    MigrationFailed
}

public class OfflineDatabaseOpenResult
{
    public bool Success;
    public OfflineDatabaseError Error;
    public string Message;
    public string DatabasePath;
    public string ProviderName;
    public int SchemaVersion;

    public OfflineDatabaseOpenResult(
        bool success,
        OfflineDatabaseError error,
        string message,
        string databasePath,
        string providerName,
        int schemaVersion)
    {
        Success = success;
        Error = error;
        Message = message;
        DatabasePath = databasePath;
        ProviderName = providerName;
        SchemaVersion = schemaVersion;
    }
}

internal class OfflineDatabaseProviderResolution
{
    public bool Success;
    public string ProviderName;
    public Type ConnectionType;
    public bool RequiresInitialization;
    public string ErrorMessage;

    public OfflineDatabaseProviderResolution(
        bool success,
        string providerName,
        Type connectionType,
        bool requiresInitialization,
        string errorMessage)
    {
        Success = success;
        ProviderName = providerName;
        ConnectionType = connectionType;
        RequiresInitialization = requiresInitialization;
        ErrorMessage = errorMessage;
    }
}
