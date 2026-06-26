using System;
using System.Collections.Generic;

public static class BattleHexGridUtility
{
    public static string GetHexKey(int c, int r)
    {
        return c + "|" + r;
    }

    public static List<HexCoord> GetNeighbourCoordinates(BattleSnapshot snapshot, int c, int r)
    {
        return UsesLegacyNeighbours(snapshot)
            ? GetLegacyNeighbourCoordinates(snapshot, c, r)
            : GetOffsetRowNeighbourCoordinates(c, r);
    }

    public static bool AreAdjacent(BattleSnapshot snapshot, int c1, int r1, int c2, int r2)
    {
        List<HexCoord> neighbours = GetNeighbourCoordinates(snapshot, c1, r1);
        for (int i = 0; i < neighbours.Count; i++)
        {
            HexCoord neighbour = neighbours[i];
            if (neighbour.C == c2 && neighbour.R == r2)
            {
                return true;
            }
        }

        return false;
    }

    public static int HexDistance(BattleSnapshot snapshot, int c1, int r1, int c2, int r2)
    {
        if (UsesLegacyNeighbours(snapshot))
        {
            int s1 = -(c1 + r1);
            int s2 = -(c2 + r2);
            return Math.Max(Math.Abs(c1 - c2), Math.Max(Math.Abs(r1 - r2), Math.Abs(s1 - s2)));
        }

        int ac1 = c1 - ((r1 - (r1 & 1)) / 2);
        int ac2 = c2 - ((r2 - (r2 & 1)) / 2);
        int as1 = -ac1 - r1;
        int as2 = -ac2 - r2;
        return Math.Max(Math.Abs(ac1 - ac2), Math.Max(Math.Abs(r1 - r2), Math.Abs(as1 - as2)));
    }

    public static Dictionary<string, int> FindReachableHexCosts(BattleSnapshot snapshot, BattleUnitSnapshot actor)
    {
        Dictionary<string, int> reachable = new Dictionary<string, int>(StringComparer.Ordinal);
        if (snapshot == null || actor == null)
        {
            return reachable;
        }

        Dictionary<string, BattleHexSnapshot> hexesByKey = BuildHexIndex(snapshot);
        Queue<ReachableNode> frontier = new Queue<ReachableNode>();
        frontier.Enqueue(new ReachableNode(actor.C, actor.R, 0));
        reachable[GetHexKey(actor.C, actor.R)] = 0;

        while (frontier.Count > 0)
        {
            ReachableNode current = frontier.Dequeue();
            if (current.Cost >= actor.MovementSpeed)
            {
                continue;
            }

            List<HexCoord> neighbours = GetNeighbourCoordinates(snapshot, current.C, current.R);
            for (int i = 0; i < neighbours.Count; i++)
            {
                HexCoord neighbour = neighbours[i];
                string key = GetHexKey(neighbour.C, neighbour.R);
                BattleHexSnapshot hex;
                if (hexesByKey.TryGetValue(key, out hex) == false || hex == null || hex.IsWalkable == false)
                {
                    continue;
                }

                bool isActorSource = neighbour.C == actor.C && neighbour.R == actor.R;
                if (isActorSource == false && string.IsNullOrEmpty(hex.OccupyingUnitId) == false)
                {
                    continue;
                }

                int nextCost = current.Cost + 1;
                int knownCost;
                if (reachable.TryGetValue(key, out knownCost) && knownCost <= nextCost)
                {
                    continue;
                }

                reachable[key] = nextCost;
                frontier.Enqueue(new ReachableNode(neighbour.C, neighbour.R, nextCost));
            }
        }

        return reachable;
    }

    static List<HexCoord> GetLegacyNeighbourCoordinates(BattleSnapshot snapshot, int c, int r)
    {
        List<HexCoord> neighbours = new List<HexCoord>();
        AddLegacyNeighbour(neighbours, snapshot, c + 1, r);
        if (c - 1 >= 0)
        {
            AddLegacyNeighbour(neighbours, snapshot, c - 1, r);
        }

        AddLegacyNeighbour(neighbours, snapshot, c, r + 1);
        if (r - 1 >= 0)
        {
            AddLegacyNeighbour(neighbours, snapshot, c, r - 1);
            AddLegacyNeighbour(neighbours, snapshot, c + 1, r - 1);
        }

        if (c - 1 >= 0)
        {
            AddLegacyNeighbour(neighbours, snapshot, c - 1, r + 1);
        }

        return neighbours;
    }

    static void AddLegacyNeighbour(List<HexCoord> neighbours, BattleSnapshot snapshot, int c, int r)
    {
        int wrappedC = WrapPositive(c, snapshot != null ? snapshot.MapWidth : 0);
        int wrappedR = WrapPositive(r, snapshot != null ? snapshot.MapHeight : 0);
        if (wrappedC < 0 || wrappedR < 0)
        {
            return;
        }

        for (int i = 0; i < neighbours.Count; i++)
        {
            if (neighbours[i].C == wrappedC && neighbours[i].R == wrappedR)
            {
                return;
            }
        }

        neighbours.Add(new HexCoord(wrappedC, wrappedR));
    }

    static int WrapPositive(int value, int size)
    {
        if (value < 0 || size <= 0)
        {
            return value;
        }

        return value % size;
    }

    static List<HexCoord> GetOffsetRowNeighbourCoordinates(int c, int r)
    {
        List<HexCoord> neighbours = new List<HexCoord>
        {
            new HexCoord(c + 1, r),
            new HexCoord(c - 1, r)
        };

        if ((r & 1) == 0)
        {
            neighbours.Add(new HexCoord(c, r - 1));
            neighbours.Add(new HexCoord(c - 1, r - 1));
            neighbours.Add(new HexCoord(c, r + 1));
            neighbours.Add(new HexCoord(c - 1, r + 1));
        }
        else
        {
            neighbours.Add(new HexCoord(c, r - 1));
            neighbours.Add(new HexCoord(c + 1, r - 1));
            neighbours.Add(new HexCoord(c, r + 1));
            neighbours.Add(new HexCoord(c + 1, r + 1));
        }

        return neighbours;
    }

    static bool UsesLegacyNeighbours(BattleSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Hexes == null || snapshot.Hexes.Count == 0)
        {
            return false;
        }

        if (snapshot.UsesLegacyHexLayout)
        {
            return true;
        }

        int expectedCount = Math.Max(0, snapshot.MapWidth) * Math.Max(0, snapshot.MapHeight);
        return expectedCount > 0 && snapshot.Hexes.Count < expectedCount;
    }

    static Dictionary<string, BattleHexSnapshot> BuildHexIndex(BattleSnapshot snapshot)
    {
        Dictionary<string, BattleHexSnapshot> result = new Dictionary<string, BattleHexSnapshot>(StringComparer.Ordinal);
        if (snapshot == null || snapshot.Hexes == null)
        {
            return result;
        }

        for (int i = 0; i < snapshot.Hexes.Count; i++)
        {
            BattleHexSnapshot hex = snapshot.Hexes[i];
            if (hex == null)
            {
                continue;
            }

            string key = GetHexKey(hex.C, hex.R);
            if (result.ContainsKey(key) == false)
            {
                result.Add(key, hex);
            }
        }

        return result;
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
}
