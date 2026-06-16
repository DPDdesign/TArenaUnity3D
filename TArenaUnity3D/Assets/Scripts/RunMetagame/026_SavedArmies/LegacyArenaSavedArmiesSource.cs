using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class LegacyArenaSavedArmiesSource : ISavedArmiesArenaImportSource
{
    private readonly DataMapper dataMapper;
    private readonly int firstBuildIndex;
    private readonly int buildCount;

    public LegacyArenaSavedArmiesSource()
        : this(DataMapper.Instance, 0, 10)
    {
    }

    public LegacyArenaSavedArmiesSource(DataMapper dataMapper, int firstBuildIndex, int buildCount)
    {
        this.dataMapper = dataMapper;
        this.firstBuildIndex = firstBuildIndex;
        this.buildCount = System.Math.Max(0, buildCount);
    }

    public List<ArenaArmyImportCandidate> ListArenaArmies()
    {
        List<ArenaArmyImportCandidate> options = new List<ArenaArmyImportCandidate>();
        if (dataMapper == null)
        {
            return options;
        }

        for (int i = 0; i < buildCount; i++)
        {
            int buildNumber = firstBuildIndex + i;
            string path = dataMapper.GetBuildFilePath(buildNumber);
            if (!File.Exists(path))
            {
                continue;
            }

            PanelArmii.BuildG build = TryReadBuild(path);
            ArenaArmyImportCandidate option = ConvertBuild(buildNumber, build);
            if (option != null)
            {
                options.Add(option);
            }
        }

        return options;
    }

    public ArenaArmyImportCandidate FindArenaArmy(string arenaArmyId)
    {
        List<ArenaArmyImportCandidate> options = ListArenaArmies();
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].ArenaArmyId == arenaArmyId)
            {
                return options[i];
            }
        }

        return null;
    }

    private static PanelArmii.BuildG TryReadBuild(string path)
    {
        try
        {
            using (FileStream file = File.OpenRead(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(file) as PanelArmii.BuildG;
            }
        }
        catch
        {
            return null;
        }
    }

    private static ArenaArmyImportCandidate ConvertBuild(int buildNumber, PanelArmii.BuildG build)
    {
        if (build == null || build.Units == null || build.NoUnits == null)
        {
            return null;
        }

        List<SavedArmyStackSnapshot> stacks = new List<SavedArmyStackSnapshot>();
        int count = System.Math.Min(build.Units.Count, build.NoUnits.Count);
        for (int i = 0; i < count; i++)
        {
            string unitId = build.Units[i];
            int amount = build.NoUnits[i];
            if (!string.IsNullOrEmpty(unitId) && amount > 0)
            {
                stacks.Add(new SavedArmyStackSnapshot(unitId, amount));
            }
        }

        if (stacks.Count == 0)
        {
            return null;
        }

        string name = string.IsNullOrEmpty(build.NazwaBohatera) ? "Arena Build " + buildNumber : build.NazwaBohatera;
        return new ArenaArmyImportCandidate("arena-build-" + buildNumber, name, stacks);
    }
}

public class FallbackArenaSavedArmiesSource : ISavedArmiesArenaImportSource
{
    private readonly ISavedArmiesArenaImportSource primary;
    private readonly ISavedArmiesArenaImportSource fallback;

    public FallbackArenaSavedArmiesSource(ISavedArmiesArenaImportSource primary, ISavedArmiesArenaImportSource fallback)
    {
        this.primary = primary;
        this.fallback = fallback;
    }

    public List<ArenaArmyImportCandidate> ListArenaArmies()
    {
        List<ArenaArmyImportCandidate> options = primary == null ? new List<ArenaArmyImportCandidate>() : primary.ListArenaArmies();
        if (options.Count > 0)
        {
            return options;
        }

        return fallback == null ? options : fallback.ListArenaArmies();
    }

    public ArenaArmyImportCandidate FindArenaArmy(string arenaArmyId)
    {
        ArenaArmyImportCandidate result = primary == null ? null : primary.FindArenaArmy(arenaArmyId);
        if (result != null)
        {
            return result;
        }

        return fallback == null ? null : fallback.FindArenaArmy(arenaArmyId);
    }
}
