using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace HPath
{
    public class IQPath_AStar
    {
        Queue<IPathTile> path;
        IQPathWorld world;
        IQPathUnit Unit;
        IPathTile startTile;
        IPathTile endT;
        CostEstimateDelegate costEstimateFunc;
        public IQPath_AStar(IQPathWorld world, IQPathUnit Unit, IPathTile startTile, IPathTile endT, CostEstimateDelegate costEstimateFunc)
        {
            this.world = world;
            this.Unit = Unit;
            this.startTile = startTile;
            this.endT = endT;
            this.costEstimateFunc = costEstimateFunc;
        }
        public void DoWork()
        {
            path = new Queue<IPathTile>();
            HashSet<IPathTile> closedSet = new HashSet<IPathTile>();
            PathfindingPriorityQueue<IPathTile> openSet = new PathfindingPriorityQueue<IPathTile>();
            openSet.Enqueue(startTile, 0);


            Dictionary<IPathTile, IPathTile> came_from = new Dictionary<IPathTile, IPathTile>();


            Dictionary<IPathTile, float> g_Score = new Dictionary<IPathTile, float>();
            g_Score[startTile] = 0;
            Dictionary<IPathTile, float> f_Score = new Dictionary<IPathTile, float>();
            f_Score[startTile] = costEstimateFunc(startTile,endT);
            while (openSet.Count > 0)
            {
                 IPathTile current = openSet.Dequeue();

                // Check to see if we are there.
                if (System.Object.ReferenceEquals(current, endT))
                {
                    Reconstruct_path(came_from, current);
                    return;
                }

                closedSet.Add(current);

                foreach (IPathTile edge_neighbour in current.GetNeighbours())
                {
                    IPathTile neighbour = edge_neighbour;

                    if (closedSet.Contains(neighbour))
                    {
                        continue; // ignore this already completed neighbor
                    }

                    float total_pathfinding_cost_to_neighbor =
                        neighbour.AggregateCostToEnter(g_Score[current], current, Unit);

                    if (total_pathfinding_cost_to_neighbor < 0)
                    {
                        // Values less than zero represent an invalid/impassable tile
                        continue;
                    }
                    //Debug.Log(total_pathfinding_cost_to_neighbor);

                    float tentative_g_score = total_pathfinding_cost_to_neighbor;

                    // Is the neighbour already in the open set?
                    //   If so, and if this new score is worse than the old score,
                    //   discard this new result.
                    if (openSet.Contains(neighbour) && tentative_g_score >= g_Score[neighbour])
                    {
                        continue;
                    }

                    // This is either a new tile or we just found a cheaper route to it
                    came_from[neighbour] = current;
                    g_Score[neighbour] = tentative_g_score;
                    f_Score[neighbour] = g_Score[neighbour] + costEstimateFunc(neighbour, endT);

                    openSet.EnqueueOrUpdate(neighbour, f_Score[neighbour]);
                } // foreach neighbour
            } // while


        }

        private void Reconstruct_path(
            Dictionary<IPathTile, IPathTile> came_From,
            IPathTile current)
        {
            // So at this point, current IS the goal.
            // So what we want to do is walk backwards through the Came_From
            // map, until we reach the "end" of that map...which will be
            // our starting node!
            Queue<IPathTile> total_path = new Queue<IPathTile>();
            total_path.Enqueue(current); // This "final" step is the path is the goal!

            while (came_From.ContainsKey(current))
            {
                /*    Came_From is a map, where the
            *    key => value relation is real saying
            *    some_node => we_got_there_from_this_node
            */

                current = came_From[current];
                total_path.Enqueue(current);
            }

            // At this point, total_path is a queue that is running
            // backwards from the END tile to the START tile, so let's reverse it.
            path = new Queue<IPathTile>(total_path.Reverse());
        }

        public IPathTile[] GetList()
        {
            return path.ToArray();
        }




    }
}