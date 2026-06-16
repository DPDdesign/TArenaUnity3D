using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class OfflineModeProductionCompositionTests
{
    [Test]
    public void RuntimeRunMetagameSource_DoesNotCreateInMemoryStores()
    {
        List<string> offenders = FindRuntimeSourceMatches("new InMemory");

        Assert.That(offenders, Is.Empty, "Production RunMetagame source must not construct in-memory stores.");
    }

    [Test]
    public void RuntimeDbStoreConstruction_StaysInOfflineModeDatabaseComposition()
    {
        List<string> offenders = FindRuntimeDbStoreConstructionOutsideComposition();

        Assert.That(offenders, Is.Empty, "Runtime DB-backed store composition should stay in OfflineModeDatabaseComposition.");
    }

    private static List<string> FindRuntimeSourceMatches(string text)
    {
        List<string> offenders = new List<string>();
        string root = Path.Combine(Application.dataPath, "Scripts", "RunMetagame");
        string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            string file = NormalizePath(files[i]);
            if (IsIgnoredSourcePath(file))
            {
                continue;
            }

            string source = File.ReadAllText(file);
            if (source.Contains(text))
            {
                offenders.Add(ToAssetsPath(file));
            }
        }

        return offenders;
    }

    private static List<string> FindRuntimeDbStoreConstructionOutsideComposition()
    {
        List<string> offenders = new List<string>();
        string root = Path.Combine(Application.dataPath, "Scripts", "RunMetagame");
        string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            string file = NormalizePath(files[i]);
            if (IsIgnoredSourcePath(file) || file.EndsWith("/OfflineModeDatabaseComposition.cs"))
            {
                continue;
            }

            string source = File.ReadAllText(file);
            if (source.Contains("new Offline") && source.Contains("DbStore"))
            {
                offenders.Add(ToAssetsPath(file));
            }
        }

        return offenders;
    }

    private static bool IsIgnoredSourcePath(string file)
    {
        return file.Contains("/Tests/") || file.Contains("/Editor/");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string ToAssetsPath(string path)
    {
        string assetsRoot = NormalizePath(Application.dataPath);
        string normalizedPath = NormalizePath(path);
        return normalizedPath.StartsWith(assetsRoot)
            ? "Assets" + normalizedPath.Substring(assetsRoot.Length)
            : normalizedPath;
    }
}
