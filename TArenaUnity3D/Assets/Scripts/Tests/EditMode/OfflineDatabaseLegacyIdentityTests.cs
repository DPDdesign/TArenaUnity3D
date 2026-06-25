#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class OfflineDatabaseLegacyIdentityTests
{
    [Test]
    public void TryParseIntId_SupportsLegacyPrefixesAndSlots()
    {
        int id;

        Assert.That(OfflineDatabaseLegacyIdentity.TryParseIntId("17", out id), Is.True);
        Assert.That(id, Is.EqualTo(17));

        Assert.That(OfflineDatabaseLegacyIdentity.TryParseIntId("snapshot-42", out id), Is.True);
        Assert.That(id, Is.EqualTo(42));

        Assert.That(OfflineDatabaseLegacyIdentity.TryParseIntId("saved-army-108", out id), Is.True);
        Assert.That(id, Is.EqualTo(108));

        Assert.That(OfflineDatabaseLegacyIdentity.TryParseIntId("slot-03", out id), Is.True);
        Assert.That(id, Is.EqualTo(3));
    }

    [Test]
    public void ParseSlotIndexOrDefault_ConvertsPhysicalSlotLabelToZeroBasedIndex()
    {
        Assert.That(OfflineDatabaseLegacyIdentity.ParseSlotIndexOrDefault("slot-01"), Is.EqualTo(0));
        Assert.That(OfflineDatabaseLegacyIdentity.ParseSlotIndexOrDefault("slot-08"), Is.EqualTo(7));
        Assert.That(OfflineDatabaseLegacyIdentity.ParseSlotIndexOrDefault(string.Empty, 2), Is.EqualTo(2));
    }

    [Test]
    public void FromSavedArmy_PreservesNumericIdentityFromLegacyStrings()
    {
        SavedArmy army = new SavedArmy(
            "saved-army-14",
            "snapshot-55",
            true,
            new List<SavedArmyStackSnapshot>
            {
                new SavedArmyStackSnapshot("Knight", 12)
            });

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotMapper.FromSavedArmy(army);

        Assert.That(shared.SavedArmyId, Is.EqualTo(14));
        Assert.That(shared.SnapshotId, Is.EqualTo(55));
    }

    [Test]
    public void CreateRouteMapSeed_AssignsIntegerIdsAndResolvesNextNodeLinks()
    {
        List<RunMapPathDefinition> paths = new DefaultRunMapPathCatalog().BuildPaths("route-balanced-frontier");

        OfflineRouteMapSeedRecord seed = OfflineRouteMapSeedFactory.Create(15, 30, "route-balanced-frontier", paths, 100, 200);

        Assert.That(seed.RunId, Is.EqualTo(15));
        Assert.That(seed.RouteMapId, Is.EqualTo(30));
        Assert.That(seed.Paths.Count, Is.EqualTo(4));
        Assert.That(seed.Paths[0].RoutePathId, Is.EqualTo(100));
        Assert.That(seed.Paths[0].Nodes[0].NodeId, Is.EqualTo(200));
        Assert.That(seed.Paths[0].Nodes[0].CatalogNodeId, Is.EqualTo("node-pressure-1"));
        Assert.That(seed.Paths[0].Nodes[0].NextNodeId, Is.EqualTo(201));
        Assert.That(seed.Paths[0].Nodes[1].NextNodeId, Is.GreaterThan(0));
        Assert.That(seed.Paths[3].Nodes[0].CatalogNodeId, Is.EqualTo("node-final"));
        Assert.That(seed.Paths[3].Nodes[0].NextNodeId, Is.EqualTo(0));
    }
}
#endif
