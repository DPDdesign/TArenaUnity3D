#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public class LegacyDamageTraceCleanupTests
{
    [Test]
    public void ProductionLesiszScripts_DoNotContainLegacyDamageMethodNames()
    {
        string root = ResolveProductionLesiszPath();
        string[] bannedTokens =
        {
            "CalculateDamageBetweenTosters",
            "ReCalculateDamageBetweenTosters",
            "CalculateDamageBetweenTostersH3",
            "CalculateDamageBetweenTostersWithQ",
            "CalculateResult"
        };

        List<string> violations = new List<string>();
        foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            for (int i = 0; i < bannedTokens.Length; i++)
            {
                if (text.Contains(bannedTokens[i]))
                {
                    violations.Add(Relative(file) + " contains " + bannedTokens[i]);
                }
            }
        }

        Assert.That(violations, Is.Empty);
    }

    [Test]
    public void ProductionLesiszScripts_KeepRandomRangeOutOfCommittedCombatDamage()
    {
        string root = ResolveProductionLesiszPath();
        HashSet<string> allowedRandomFiles = new HashSet<string>
        {
            Normalize(Path.Combine(root, "HexMap", "BattleActionLifecycle.cs")),
            Normalize(Path.Combine(root, "HexMap", "CombatSfxManager.cs")),
            Normalize(Path.Combine(root, "HexMap", "HexMap.cs"))
        };

        List<string> violations = new List<string>();
        foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            if (text.Contains("Random.Range") && allowedRandomFiles.Contains(Normalize(file)) == false)
            {
                violations.Add(Relative(file));
            }
        }

        Assert.That(violations, Is.Empty);
    }

    static string ResolveProductionLesiszPath()
    {
        string cwd = Directory.GetCurrentDirectory();
        string[] candidates =
        {
            Path.Combine(cwd, "Assets", "Scripts", "Lesisz"),
            Path.Combine(cwd, "TArenaUnity3D", "Assets", "Scripts", "Lesisz")
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (Directory.Exists(candidates[i]))
            {
                return candidates[i];
            }
        }

        Assert.Fail("Could not resolve production Lesisz scripts path from " + cwd);
        return string.Empty;
    }

    static string Relative(string path)
    {
        return Normalize(path).Replace(Normalize(Directory.GetCurrentDirectory()) + "/", string.Empty);
    }

    static string Normalize(string path)
    {
        return Path.GetFullPath(path).Replace('\\', '/');
    }
}
#endif
