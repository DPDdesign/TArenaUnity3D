using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class OfflineDatabaseDevReset
{
    public static OfflineDatabaseOpenResult RebuildDefaultDatabase()
    {
        string databasePath = OfflineDatabaseModule.GetDefaultDatabasePath();
        if (!string.IsNullOrEmpty(databasePath) && File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        return new OfflineDatabaseModule(databasePath).OpenOrCreate();
    }

#if UNITY_EDITOR
    [MenuItem("TArena/Offline Database/Delete And Rebuild Default DB")]
    public static void RebuildDefaultDatabaseFromMenu()
    {
        OfflineDatabaseOpenResult result = RebuildDefaultDatabase();
        if (result == null || !result.Success)
        {
            throw new InvalidOperationException(result == null ? "Offline database rebuild failed." : result.Message);
        }

        UnityEngine.Debug.Log("[OfflineDatabaseDevReset] Rebuilt DB at: " + result.DatabasePath);
    }
#endif
}
