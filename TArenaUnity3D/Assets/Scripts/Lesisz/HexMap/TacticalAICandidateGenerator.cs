using System;
using System.Collections.Generic;

public static class TacticalAICandidateGenerator
{
    static readonly TacticalAIActionType[] ActionTypeOrder =
    {
        TacticalAIActionType.Skill,
        TacticalAIActionType.BasicRangedAttack,
        TacticalAIActionType.MoveAndAttack,
        TacticalAIActionType.Move,
        TacticalAIActionType.Wait,
        TacticalAIActionType.Defend
    };

    public static List<TacticalAIActionIntent> GenerateCandidates(
        BattleSnapshot snapshot,
        TacticalAICandidateGenerationOptions options = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        List<TacticalAIActionIntent> orderedCandidates = new List<TacticalAIActionIntent>();
        if (snapshot == null || string.IsNullOrEmpty(snapshot.ActiveUnitId))
        {
            return orderedCandidates;
        }

        options = options ?? TacticalAICandidateGenerationOptions.Default;
        skillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;

        SnapshotIndex index = SnapshotIndex.Build(snapshot);
        BattleUnitSnapshot actor;
        if (index.TryGetUnit(snapshot.ActiveUnitId, out actor) == false || actor == null || actor.IsAlive == false)
        {
            return orderedCandidates;
        }

        Dictionary<TacticalAIActionType, List<TacticalAIActionIntent>> buckets =
            new Dictionary<TacticalAIActionType, List<TacticalAIActionIntent>>();

        buckets[TacticalAIActionType.Skill] = BuildSkillCandidates(actor, options, skillMetadataProvider);
        buckets[TacticalAIActionType.BasicRangedAttack] = BuildBasicRangedAttackCandidates(actor, index, options);
        buckets[TacticalAIActionType.MoveAndAttack] = BuildMoveAndAttackCandidates(actor, index, options);
        buckets[TacticalAIActionType.Move] = BuildMoveCandidates(actor, index, options);
        buckets[TacticalAIActionType.Wait] = BuildWaitCandidates(actor);
        buckets[TacticalAIActionType.Defend] = BuildDefendCandidates(actor);

        for (int i = 0; i < ActionTypeOrder.Length; i++)
        {
            TacticalAIActionType actionType = ActionTypeOrder[i];
            List<TacticalAIActionIntent> bucket;
            if (buckets.TryGetValue(actionType, out bucket) == false || bucket == null)
            {
                continue;
            }

            TrimToPerTypeLimit(bucket, options.MaxCandidatesPerActionType);
            orderedCandidates.AddRange(bucket);
        }

        return orderedCandidates;
    }

    public static bool IsRepeatableToggleSkillId(string skillId)
    {
        return string.IsNullOrEmpty(skillId) == false &&
            (skillId.StartsWith("Melee_Stance", StringComparison.Ordinal) ||
             skillId.StartsWith("Range_Stance", StringComparison.Ordinal));
    }

    static List<TacticalAIActionIntent> BuildWaitCandidates(BattleUnitSnapshot actor)
    {
        List<TacticalAIActionIntent> candidates = new List<TacticalAIActionIntent>();
        if (actor.MovedThisTurn || actor.UsedSkillThisTurn || actor.Waited)
        {
            return candidates;
        }

        candidates.Add(CreateIntent(
            TacticalAIActionType.Wait,
            actor,
            null,
            string.Empty,
            null,
            -1,
            string.Empty,
            BuildStableOrderKey(TacticalAIActionType.Wait, actor.RuntimeUnitId, actor.C, actor.R, -1, string.Empty, null, null)));
        return candidates;
    }

    static List<TacticalAIActionIntent> BuildDefendCandidates(BattleUnitSnapshot actor)
    {
        List<TacticalAIActionIntent> candidates = new List<TacticalAIActionIntent>();
        if (actor.MovedThisTurn || actor.UsedSkillThisTurn)
        {
            return candidates;
        }

        candidates.Add(CreateIntent(
            TacticalAIActionType.Defend,
            actor,
            null,
            string.Empty,
            null,
            -1,
            string.Empty,
            BuildStableOrderKey(TacticalAIActionType.Defend, actor.RuntimeUnitId, actor.C, actor.R, -1, string.Empty, null, null)));
        return candidates;
    }

    static List<TacticalAIActionIntent> BuildMoveCandidates(
        BattleUnitSnapshot actor,
        SnapshotIndex index,
        TacticalAICandidateGenerationOptions options)
    {
        List<TacticalAIActionIntent> candidates = new List<TacticalAIActionIntent>();
        if (CanStartMovement(actor) == false)
        {
            return candidates;
        }

        Dictionary<string, int> reachableCosts = FindReachableHexCosts(actor, index);
        List<MoveCandidateScore> scoredMoves = new List<MoveCandidateScore>();

        foreach (KeyValuePair<string, int> pair in reachableCosts)
        {
            BattleHexSnapshot destination = index.GetHexByKey(pair.Key);
            if (destination == null || destination.C == actor.C && destination.R == actor.R)
            {
                continue;
            }

            int nearestEnemyDistance = FindNearestEnemyDistance(destination.C, destination.R, actor.TeamIndex, index);
            scoredMoves.Add(new MoveCandidateScore
            {
                Destination = destination,
                Steps = pair.Value,
                NearestEnemyDistance = nearestEnemyDistance
            });
        }

        scoredMoves.Sort(CompareMoveScores);
        TrimToPerTypeLimit(scoredMoves, options.MaxMoveCandidates);

        for (int i = 0; i < scoredMoves.Count; i++)
        {
            BattleHexSnapshot destination = scoredMoves[i].Destination;
            candidates.Add(CreateIntent(
                TacticalAIActionType.Move,
                actor,
                new TacticalAIHexCoordinate(destination.C, destination.R),
                string.Empty,
                null,
                -1,
                string.Empty,
                BuildStableOrderKey(TacticalAIActionType.Move, actor.RuntimeUnitId, actor.C, actor.R, -1, string.Empty, destination, null)));
        }

        return candidates;
    }

    static List<TacticalAIActionIntent> BuildMoveAndAttackCandidates(
        BattleUnitSnapshot actor,
        SnapshotIndex index,
        TacticalAICandidateGenerationOptions options)
    {
        List<TacticalAIActionIntent> candidates = new List<TacticalAIActionIntent>();
        if (CanStartMovement(actor) == false || actor.IsRange)
        {
            return candidates;
        }

        Dictionary<string, int> reachableCosts = FindReachableHexCosts(actor, index);
        Dictionary<string, TacticalAIActionIntent> uniqueCandidates =
            new Dictionary<string, TacticalAIActionIntent>(StringComparer.Ordinal);

        for (int enemyIndex = 0; enemyIndex < index.Units.Count; enemyIndex++)
        {
            BattleUnitSnapshot enemy = index.Units[enemyIndex];
            if (enemy == null || enemy.IsAlive == false || enemy.TeamIndex == actor.TeamIndex)
            {
                continue;
            }

            List<TacticalAIHexCoordinate> attackPositions = GetAttackPositions(actor, enemy, index, reachableCosts);
            for (int positionIndex = 0; positionIndex < attackPositions.Count; positionIndex++)
            {
                TacticalAIHexCoordinate destination = attackPositions[positionIndex];
                string key = enemy.RuntimeUnitId + "|" + destination.C + "|" + destination.R;
                if (uniqueCandidates.ContainsKey(key))
                {
                    continue;
                }

                TacticalAIHexCoordinate targetHex = new TacticalAIHexCoordinate(enemy.C, enemy.R);
                uniqueCandidates.Add(
                    key,
                    CreateIntent(
                        TacticalAIActionType.MoveAndAttack,
                        actor,
                        destination,
                        enemy.RuntimeUnitId,
                        targetHex,
                        -1,
                        string.Empty,
                        BuildStableOrderKey(
                            TacticalAIActionType.MoveAndAttack,
                            actor.RuntimeUnitId,
                            actor.C,
                            actor.R,
                            -1,
                            enemy.RuntimeUnitId,
                            new BattleHexSnapshot { C = destination.C, R = destination.R },
                            new BattleHexSnapshot { C = targetHex.C, R = targetHex.R })));
            }
        }

        List<TacticalAIActionIntent> sortedCandidates = new List<TacticalAIActionIntent>(uniqueCandidates.Values);
        sortedCandidates.Sort((left, right) => CompareAttackCandidates(left, right, index));
        TrimToPerTypeLimit(sortedCandidates, options.MaxAttackCandidates);
        candidates.AddRange(sortedCandidates);
        return candidates;
    }

    static List<TacticalAIActionIntent> BuildBasicRangedAttackCandidates(
        BattleUnitSnapshot actor,
        SnapshotIndex index,
        TacticalAICandidateGenerationOptions options)
    {
        List<TacticalAIActionIntent> candidates = new List<TacticalAIActionIntent>();
        if (actor.IsRange == false || actor.MovedThisTurn || actor.UsedSkillThisTurn)
        {
            return candidates;
        }

        for (int i = 0; i < index.Units.Count; i++)
        {
            BattleUnitSnapshot enemy = index.Units[i];
            if (enemy == null || enemy.IsAlive == false || enemy.TeamIndex == actor.TeamIndex)
            {
                continue;
            }

            TacticalAIHexCoordinate targetHex = new TacticalAIHexCoordinate(enemy.C, enemy.R);
            candidates.Add(CreateIntent(
                TacticalAIActionType.BasicRangedAttack,
                actor,
                null,
                enemy.RuntimeUnitId,
                targetHex,
                -1,
                string.Empty,
                BuildStableOrderKey(
                    TacticalAIActionType.BasicRangedAttack,
                    actor.RuntimeUnitId,
                    actor.C,
                    actor.R,
                    -1,
                    enemy.RuntimeUnitId,
                    null,
                    new BattleHexSnapshot { C = targetHex.C, R = targetHex.R })));
        }

        candidates.Sort((left, right) => CompareAttackCandidates(left, right, index));
        TrimToPerTypeLimit(candidates, options.MaxAttackCandidates);
        return candidates;
    }

    static List<TacticalAIActionIntent> BuildSkillCandidates(
        BattleUnitSnapshot actor,
        TacticalAICandidateGenerationOptions options,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        List<TacticalAIActionIntent> candidates = new List<TacticalAIActionIntent>();
        if (actor == null || actor.SkillIdsBySlot == null || actor.CooldownsBySlot == null || actor.Waited)
        {
            return candidates;
        }

        int slotCount = Math.Min(actor.SkillIdsBySlot.Count, actor.CooldownsBySlot.Count);
        for (int slot = 0; slot < slotCount; slot++)
        {
            string skillId = actor.SkillIdsBySlot[slot];
            TacticalAISkillMetadata metadata = ResolveSkillMetadata(skillId, skillMetadataProvider);
            if (CanStartSkill(actor, slot, skillId, metadata) == false)
            {
                continue;
            }

            candidates.Add(CreateIntent(
                TacticalAIActionType.Skill,
                actor,
                null,
                string.Empty,
                null,
                slot,
                skillId,
                BuildStableOrderKey(TacticalAIActionType.Skill, actor.RuntimeUnitId, actor.C, actor.R, slot, skillId, null, null)));
        }

        candidates.Sort(CompareIntentStableOrder);
        TrimToPerTypeLimit(candidates, options.MaxSkillCandidates);
        return candidates;
    }

    static TacticalAISkillMetadata ResolveSkillMetadata(
        string skillId,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        TacticalAISkillMetadata metadata;
        if (skillMetadataProvider != null && skillMetadataProvider.TryGetSkillMetadata(skillId, out metadata) && metadata != null)
        {
            if (string.IsNullOrEmpty(metadata.SkillId))
            {
                metadata.SkillId = skillId ?? string.Empty;
            }

            metadata.IsRepeatableToggle = metadata.IsRepeatableToggle || IsRepeatableToggleSkillId(skillId);
            return metadata;
        }

        return new TacticalAISkillMetadata
        {
            SkillId = skillId ?? string.Empty,
            IsRepeatableToggle = IsRepeatableToggleSkillId(skillId)
        };
    }

    static bool CanStartMovement(BattleUnitSnapshot actor)
    {
        return actor != null &&
            actor.IsAlive &&
            actor.MovedThisTurn == false &&
            (actor.UsedSkillThisTurn == false || actor.CanMoveAfterSkillThisTurn);
    }

    static bool CanStartSkill(BattleUnitSnapshot actor, int slot, string skillId, TacticalAISkillMetadata metadata)
    {
        if (actor == null ||
            slot < 0 ||
            slot >= actor.CooldownsBySlot.Count ||
            actor.CooldownsBySlot[slot] > 0 ||
            string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        if (metadata.IsPassive)
        {
            return false;
        }

        if (metadata.IsRepeatableToggle)
        {
            return true;
        }

        if (actor.MovedThisTurn && metadata.CanUseAfterMove == false)
        {
            return false;
        }

        if (actor.UsedSkillIdsThisTurn != null && actor.UsedSkillIdsThisTurn.Contains(skillId))
        {
            return false;
        }

        return true;
    }

    static Dictionary<string, int> FindReachableHexCosts(BattleUnitSnapshot actor, SnapshotIndex index)
    {
        Dictionary<string, int> reachableCosts = new Dictionary<string, int>(StringComparer.Ordinal);
        BattleHexSnapshot source = index.GetHex(actor.C, actor.R);
        if (source == null)
        {
            return reachableCosts;
        }

        Queue<ReachableNode> frontier = new Queue<ReachableNode>();
        string sourceKey = SnapshotIndex.GetHexKey(actor.C, actor.R);
        frontier.Enqueue(new ReachableNode(actor.C, actor.R, 0));
        reachableCosts[sourceKey] = 0;

        while (frontier.Count > 0)
        {
            ReachableNode current = frontier.Dequeue();
            if (current.Cost >= actor.MovementSpeed)
            {
                continue;
            }

            List<TacticalAIHexCoordinate> neighbours = GetNeighbourCoordinates(current.C, current.R);
            for (int i = 0; i < neighbours.Count; i++)
            {
                TacticalAIHexCoordinate neighbour = neighbours[i];
                BattleHexSnapshot hex = index.GetHex(neighbour.C, neighbour.R);
                if (hex == null || hex.IsWalkable == false)
                {
                    continue;
                }

                bool isActorSource = neighbour.C == actor.C && neighbour.R == actor.R;
                if (isActorSource == false && string.IsNullOrEmpty(hex.OccupyingUnitId) == false)
                {
                    continue;
                }

                int nextCost = current.Cost + 1;
                string key = SnapshotIndex.GetHexKey(neighbour.C, neighbour.R);
                int knownCost;
                if (reachableCosts.TryGetValue(key, out knownCost) && knownCost <= nextCost)
                {
                    continue;
                }

                reachableCosts[key] = nextCost;
                frontier.Enqueue(new ReachableNode(neighbour.C, neighbour.R, nextCost));
            }
        }

        return reachableCosts;
    }

    static List<TacticalAIHexCoordinate> GetAttackPositions(
        BattleUnitSnapshot actor,
        BattleUnitSnapshot enemy,
        SnapshotIndex index,
        Dictionary<string, int> reachableCosts)
    {
        List<TacticalAIHexCoordinate> positions = new List<TacticalAIHexCoordinate>();
        if (enemy == null)
        {
            return positions;
        }

        if (AreAdjacent(actor.C, actor.R, enemy.C, enemy.R))
        {
            positions.Add(new TacticalAIHexCoordinate(actor.C, actor.R));
        }

        List<TacticalAIHexCoordinate> neighbours = GetNeighbourCoordinates(enemy.C, enemy.R);
        for (int i = 0; i < neighbours.Count; i++)
        {
            TacticalAIHexCoordinate candidate = neighbours[i];
            BattleHexSnapshot hex = index.GetHex(candidate.C, candidate.R);
            if (hex == null || hex.IsWalkable == false)
            {
                continue;
            }

            bool isActorSource = candidate.C == actor.C && candidate.R == actor.R;
            if (isActorSource == false && string.IsNullOrEmpty(hex.OccupyingUnitId) == false)
            {
                continue;
            }

            if (reachableCosts.ContainsKey(SnapshotIndex.GetHexKey(candidate.C, candidate.R)))
            {
                positions.Add(candidate);
            }
        }

        positions.Sort(CompareCoordinates);
        RemoveDuplicateCoordinates(positions);
        return positions;
    }

    static int FindNearestEnemyDistance(int c, int r, int actorTeamIndex, SnapshotIndex index)
    {
        int nearestDistance = int.MaxValue;
        for (int i = 0; i < index.Units.Count; i++)
        {
            BattleUnitSnapshot unit = index.Units[i];
            if (unit == null || unit.IsAlive == false || unit.TeamIndex == actorTeamIndex)
            {
                continue;
            }

            int distance = HexDistance(c, r, unit.C, unit.R);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
            }
        }

        return nearestDistance == int.MaxValue ? int.MaxValue / 2 : nearestDistance;
    }

    static int CompareMoveScores(MoveCandidateScore left, MoveCandidateScore right)
    {
        int nearestEnemyCompare = left.NearestEnemyDistance.CompareTo(right.NearestEnemyDistance);
        if (nearestEnemyCompare != 0)
        {
            return nearestEnemyCompare;
        }

        int stepsCompare = left.Steps.CompareTo(right.Steps);
        if (stepsCompare != 0)
        {
            return stepsCompare;
        }

        return CompareCoordinates(
            new TacticalAIHexCoordinate(left.Destination.C, left.Destination.R),
            new TacticalAIHexCoordinate(right.Destination.C, right.Destination.R));
    }

    static int CompareAttackCandidates(
        TacticalAIActionIntent left,
        TacticalAIActionIntent right,
        SnapshotIndex index)
    {
        BattleUnitSnapshot leftTarget = index.GetUnitOrNull(left.TargetUnitId);
        BattleUnitSnapshot rightTarget = index.GetUnitOrNull(right.TargetUnitId);

        int leftDurability = GetUnitDurability(leftTarget);
        int rightDurability = GetUnitDurability(rightTarget);
        int durabilityCompare = leftDurability.CompareTo(rightDurability);
        if (durabilityCompare != 0)
        {
            return durabilityCompare;
        }

        return CompareIntentStableOrder(left, right);
    }

    static int CompareIntentStableOrder(TacticalAIActionIntent left, TacticalAIActionIntent right)
    {
        return string.CompareOrdinal(
            left != null ? left.StableOrderKey : string.Empty,
            right != null ? right.StableOrderKey : string.Empty);
    }

    static TacticalAIActionIntent CreateIntent(
        TacticalAIActionType actionType,
        BattleUnitSnapshot actor,
        TacticalAIHexCoordinate destinationHex,
        string targetUnitId,
        TacticalAIHexCoordinate targetHex,
        int skillSlot,
        string skillId,
        string stableOrderKey)
    {
        return new TacticalAIActionIntent
        {
            ActionType = actionType,
            ActorUnitId = actor.RuntimeUnitId,
            SourceHex = new TacticalAIHexCoordinate(actor.C, actor.R),
            DestinationHex = destinationHex,
            TargetUnitId = targetUnitId ?? string.Empty,
            TargetHex = targetHex,
            SkillSlot = skillSlot,
            SkillId = skillId ?? string.Empty,
            StableOrderKey = stableOrderKey,
            PredictedPriority = 0
        };
    }

    static string BuildStableOrderKey(
        TacticalAIActionType actionType,
        string actorUnitId,
        int sourceC,
        int sourceR,
        int skillSlot,
        string targetUnitId,
        BattleHexSnapshot destinationHex,
        BattleHexSnapshot targetHex)
    {
        return actionType + "|" +
            (actorUnitId ?? string.Empty) + "|" +
            sourceC + "|" + sourceR + "|" +
            skillSlot + "|" +
            (targetUnitId ?? string.Empty) + "|" +
            (destinationHex != null ? destinationHex.C.ToString() : string.Empty) + "|" +
            (destinationHex != null ? destinationHex.R.ToString() : string.Empty) + "|" +
            (targetHex != null ? targetHex.C.ToString() : string.Empty) + "|" +
            (targetHex != null ? targetHex.R.ToString() : string.Empty);
    }

    static int GetUnitDurability(BattleUnitSnapshot unit)
    {
        if (unit == null)
        {
            return int.MaxValue;
        }

        int livingStacksBeforeFront = Math.Max(0, unit.Amount - 1);
        return livingStacksBeforeFront * Math.Max(1, unit.BaseHP) + Math.Max(0, unit.TempHP);
    }

    static bool AreAdjacent(int c1, int r1, int c2, int r2)
    {
        List<TacticalAIHexCoordinate> neighbours = GetNeighbourCoordinates(c1, r1);
        for (int i = 0; i < neighbours.Count; i++)
        {
            if (neighbours[i].C == c2 && neighbours[i].R == r2)
            {
                return true;
            }
        }

        return false;
    }

    static int HexDistance(int c1, int r1, int c2, int r2)
    {
        int s1 = -(c1 + r1);
        int s2 = -(c2 + r2);
        return Math.Max(Math.Abs(c1 - c2), Math.Max(Math.Abs(r1 - r2), Math.Abs(s1 - s2)));
    }

    static List<TacticalAIHexCoordinate> GetNeighbourCoordinates(int c, int r)
    {
        return new List<TacticalAIHexCoordinate>
        {
            new TacticalAIHexCoordinate(c, r - 1),
            new TacticalAIHexCoordinate(c, r + 1),
            new TacticalAIHexCoordinate(c + 1, r - 1),
            new TacticalAIHexCoordinate(c - 1, r + 1),
            new TacticalAIHexCoordinate(c - 1, r),
            new TacticalAIHexCoordinate(c + 1, r)
        };
    }

    static int CompareCoordinates(TacticalAIHexCoordinate left, TacticalAIHexCoordinate right)
    {
        int cCompare = left.C.CompareTo(right.C);
        if (cCompare != 0)
        {
            return cCompare;
        }

        return left.R.CompareTo(right.R);
    }

    static void RemoveDuplicateCoordinates(List<TacticalAIHexCoordinate> coordinates)
    {
        for (int i = coordinates.Count - 1; i > 0; i--)
        {
            TacticalAIHexCoordinate current = coordinates[i];
            TacticalAIHexCoordinate previous = coordinates[i - 1];
            if (current.C == previous.C && current.R == previous.R)
            {
                coordinates.RemoveAt(i);
            }
        }
    }

    static void TrimToPerTypeLimit<T>(List<T> list, int limit)
    {
        if (list == null || limit < 0 || list.Count <= limit)
        {
            return;
        }

        if (limit == 0)
        {
            list.Clear();
            return;
        }

        list.RemoveRange(limit, list.Count - limit);
    }

    struct ReachableNode
    {
        public readonly int C;
        public readonly int R;
        public readonly int Cost;

        public ReachableNode(int c, int r, int cost)
        {
            C = c;
            R = r;
            Cost = cost;
        }
    }

    sealed class SnapshotIndex
    {
        readonly Dictionary<string, BattleUnitSnapshot> unitsById;
        readonly Dictionary<string, BattleHexSnapshot> hexesByKey;

        SnapshotIndex(List<BattleUnitSnapshot> units, List<BattleHexSnapshot> hexes)
        {
            Units = units ?? new List<BattleUnitSnapshot>();
            Hexes = hexes ?? new List<BattleHexSnapshot>();
            unitsById = new Dictionary<string, BattleUnitSnapshot>(StringComparer.Ordinal);
            hexesByKey = new Dictionary<string, BattleHexSnapshot>(StringComparer.Ordinal);

            for (int i = 0; i < Units.Count; i++)
            {
                BattleUnitSnapshot unit = Units[i];
                if (unit != null && string.IsNullOrEmpty(unit.RuntimeUnitId) == false && unitsById.ContainsKey(unit.RuntimeUnitId) == false)
                {
                    unitsById.Add(unit.RuntimeUnitId, unit);
                }
            }

            for (int i = 0; i < Hexes.Count; i++)
            {
                BattleHexSnapshot hex = Hexes[i];
                if (hex == null)
                {
                    continue;
                }

                string key = GetHexKey(hex.C, hex.R);
                if (hexesByKey.ContainsKey(key) == false)
                {
                    hexesByKey.Add(key, hex);
                }
            }
        }

        public List<BattleUnitSnapshot> Units { get; private set; }
        public List<BattleHexSnapshot> Hexes { get; private set; }

        public static SnapshotIndex Build(BattleSnapshot snapshot)
        {
            return new SnapshotIndex(snapshot != null ? snapshot.Units : null, snapshot != null ? snapshot.Hexes : null);
        }

        public static string GetHexKey(int c, int r)
        {
            return c + "|" + r;
        }

        public bool TryGetUnit(string runtimeUnitId, out BattleUnitSnapshot unit)
        {
            return unitsById.TryGetValue(runtimeUnitId ?? string.Empty, out unit);
        }

        public BattleUnitSnapshot GetUnitOrNull(string runtimeUnitId)
        {
            BattleUnitSnapshot unit;
            unitsById.TryGetValue(runtimeUnitId ?? string.Empty, out unit);
            return unit;
        }

        public BattleHexSnapshot GetHex(int c, int r)
        {
            return GetHexByKey(GetHexKey(c, r));
        }

        public BattleHexSnapshot GetHexByKey(string key)
        {
            BattleHexSnapshot hex;
            hexesByKey.TryGetValue(key ?? string.Empty, out hex);
            return hex;
        }
    }

    struct MoveCandidateScore
    {
        public BattleHexSnapshot Destination;
        public int Steps;
        public int NearestEnemyDistance;
    }
}
