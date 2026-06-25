#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleSnapshotBuilderTests
{
    [Test]
    public void RuntimeUnitId_UsesTeamAndRosterIndex()
    {
        Assert.That(BattleSnapshotRuntimeIds.CreateUnitId(1, 3), Is.EqualTo("team-1-slot-3"));
    }

    [Test]
    public void Build_ProducesStableHashForEquivalentStateRegardlessOfInputOrder()
    {
        BattleSnapshot first = BattleSnapshotBuilder.Build(
            2,
            2,
            new[]
            {
                Hex(1, 0, true, "team-0-slot-0"),
                Hex(0, 0, true, string.Empty)
            },
            new[]
            {
                Unit(
                    "team-0-slot-0",
                    0,
                    0,
                    1,
                    0,
                    12,
                    new List<int> { 2, 0 },
                    new List<string> { "Slash", "WaitSkill" },
                    new List<string> { "SkillB", "SkillA" },
                    new List<BattleStatusSnapshot>
                    {
                        Status("Stone_Skin", "team-1-slot-0", 2, defenseModifier: 3),
                        Status("Fire_Movement", "team-1-slot-0", 1, damageOverTime: 2)
                    })
            },
            "team-0-slot-0",
            TurnState(3, false, false, string.Empty));

        BattleSnapshot second = BattleSnapshotBuilder.Build(
            2,
            2,
            new[]
            {
                Hex(0, 0, true, string.Empty),
                Hex(1, 0, true, "team-0-slot-0")
            },
            new[]
            {
                Unit(
                    "team-0-slot-0",
                    0,
                    0,
                    1,
                    0,
                    12,
                    new List<int> { 2, 0 },
                    new List<string> { "Slash", "WaitSkill" },
                    new List<string> { "SkillA", "SkillB" },
                    new List<BattleStatusSnapshot>
                    {
                        Status("Fire_Movement", "team-1-slot-0", 1, damageOverTime: 2),
                        Status("Stone_Skin", "team-1-slot-0", 2, defenseModifier: 3)
                    })
            },
            "team-0-slot-0",
            TurnState(3, false, false, string.Empty));

        Assert.That(second.SnapshotHash, Is.EqualTo(first.SnapshotHash));
        Assert.That(second.Units[0].UsedSkillIdsThisTurn, Is.EqualTo(new[] { "SkillA", "SkillB" }));
        Assert.That(second.Units[0].Statuses[0].StatusId, Is.EqualTo("Fire_Movement"));
        Assert.That(second.Units[0].Statuses[1].StatusId, Is.EqualTo("Stone_Skin"));
    }

    [Test]
    public void Hash_ChangesWhenUnitPositionChanges()
    {
        string baselineHash = BuildSingleUnitSnapshot(0, 0, 12, 1, "team-0-slot-0", null).SnapshotHash;
        string movedHash = BuildSingleUnitSnapshot(1, 0, 12, 1, "team-0-slot-0", null).SnapshotHash;

        Assert.That(movedHash, Is.Not.EqualTo(baselineHash));
    }

    [Test]
    public void Hash_ChangesWhenUnitAmountChanges()
    {
        string baselineHash = BuildSingleUnitSnapshot(0, 0, 12, 1, "team-0-slot-0", null).SnapshotHash;
        string changedHash = BuildSingleUnitSnapshot(0, 0, 11, 1, "team-0-slot-0", null).SnapshotHash;

        Assert.That(changedHash, Is.Not.EqualTo(baselineHash));
    }

    [Test]
    public void Hash_ChangesWhenCooldownChanges()
    {
        string baselineHash = BuildSingleUnitSnapshot(0, 0, 12, 1, "team-0-slot-0", null).SnapshotHash;
        string changedHash = BuildSingleUnitSnapshot(0, 0, 12, 2, "team-0-slot-0", null).SnapshotHash;

        Assert.That(changedHash, Is.Not.EqualTo(baselineHash));
    }

    [Test]
    public void Hash_ChangesWhenActiveUnitChanges()
    {
        string firstHash = BuildSingleUnitSnapshot(0, 0, 12, 1, "team-0-slot-0", null).SnapshotHash;
        string secondHash = BuildSingleUnitSnapshot(0, 0, 12, 1, "team-1-slot-0", null).SnapshotHash;

        Assert.That(secondHash, Is.Not.EqualTo(firstHash));
    }

    [Test]
    public void Hash_ChangesWhenStatusChanges()
    {
        string baselineHash = BuildSingleUnitSnapshot(0, 0, 12, 1, "team-0-slot-0", null).SnapshotHash;
        string changedHash = BuildSingleUnitSnapshot(
            0,
            0,
            12,
            1,
            "team-0-slot-0",
            new List<BattleStatusSnapshot> { Status("Stone_Skin", "team-1-slot-0", 2, defenseModifier: 3) }).SnapshotHash;

        Assert.That(changedHash, Is.Not.EqualTo(baselineHash));
    }

    [Test]
    public void SnapshotModel_DoesNotReferenceUnityObjects()
    {
        Type[] snapshotTypes =
        {
            typeof(BattleSnapshot),
            typeof(BattleHexSnapshot),
            typeof(BattleUnitSnapshot),
            typeof(BattleStatusSnapshot),
            typeof(BattleTurnStateSnapshot)
        };

        for (int typeIndex = 0; typeIndex < snapshotTypes.Length; typeIndex++)
        {
            FieldInfo[] fields = snapshotTypes[typeIndex].GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                Assert.That(
                    ContainsUnityObjectReference(fields[fieldIndex].FieldType),
                    Is.False,
                    snapshotTypes[typeIndex].Name + "." + fields[fieldIndex].Name + " should stay pure data.");
            }
        }
    }

    static BattleSnapshot BuildSingleUnitSnapshot(
        int c,
        int r,
        int amount,
        int firstCooldown,
        string activeUnitId,
        List<BattleStatusSnapshot> statuses)
    {
        return BattleSnapshotBuilder.Build(
            2,
            2,
            new[]
            {
                Hex(0, 0, true, c == 0 && r == 0 ? "team-0-slot-0" : string.Empty),
                Hex(1, 0, true, c == 1 && r == 0 ? "team-0-slot-0" : string.Empty)
            },
            new[]
            {
                Unit(
                    "team-0-slot-0",
                    0,
                    0,
                    c,
                    r,
                    amount,
                    new List<int> { firstCooldown, 0 },
                    new List<string> { "Slash", "Guard" },
                    new List<string>(),
                    statuses)
            },
            activeUnitId,
            TurnState(1, false, false, string.Empty));
    }

    static BattleHexSnapshot Hex(int c, int r, bool isWalkable, string occupyingUnitId)
    {
        return new BattleHexSnapshot
        {
            C = c,
            R = r,
            IsWalkable = isWalkable,
            OccupyingUnitId = occupyingUnitId
        };
    }

    static BattleUnitSnapshot Unit(
        string runtimeUnitId,
        int teamIndex,
        int rosterIndex,
        int c,
        int r,
        int amount,
        List<int> cooldowns,
        List<string> skillIds,
        List<string> usedSkills,
        List<BattleStatusSnapshot> statuses)
    {
        return new BattleUnitSnapshot
        {
            RuntimeUnitId = runtimeUnitId,
            TeamIndex = teamIndex,
            RosterIndexWithinTeam = rosterIndex,
            UnitName = "Rusher",
            UnitType = "Rusher",
            C = c,
            R = r,
            Amount = amount,
            TempHP = 30,
            BaseHP = 30,
            Attack = 5,
            Defense = 3,
            MovementSpeed = 6,
            Initiative = 7,
            MinDamage = 2,
            MaxDamage = 4,
            IsAlive = true,
            CooldownsBySlot = cooldowns,
            SkillIdsBySlot = skillIds,
            UsedSkillIdsThisTurn = usedSkills,
            Statuses = statuses ?? new List<BattleStatusSnapshot>()
        };
    }

    static BattleStatusSnapshot Status(
        string statusId,
        string sourceUnitId,
        int remainingDurationOrTurns,
        int defenseModifier = 0,
        int damageOverTime = 0)
    {
        return new BattleStatusSnapshot
        {
            StatusId = statusId,
            SourceSkillId = statusId,
            SourceUnitId = sourceUnitId,
            RemainingDurationOrTurns = remainingDurationOrTurns,
            DefenseModifier = defenseModifier,
            DamageOverTime = damageOverTime
        };
    }

    static BattleTurnStateSnapshot TurnState(
        int roundNumber,
        bool isResolvingNewTurnSequence,
        bool isActionBlocking,
        string activeActionKind)
    {
        return new BattleTurnStateSnapshot
        {
            RoundNumber = roundNumber,
            IsResolvingNewTurnSequence = isResolvingNewTurnSequence,
            IsActionBlocking = isActionBlocking,
            ActiveActionKind = activeActionKind
        };
    }

    static bool ContainsUnityObjectReference(Type type)
    {
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            return true;
        }

        if (type.IsArray)
        {
            return ContainsUnityObjectReference(type.GetElementType());
        }

        if (type.IsGenericType)
        {
            Type[] arguments = type.GetGenericArguments();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (ContainsUnityObjectReference(arguments[i]))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
#endif
