using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HPath
{
    public static class HPath
    {
        public static IPathTile[] FindPath(IQPathWorld world, IQPathUnit Unit, IPathTile startTile, IPathTile endT, CostEstimateDelegate costEstimateFunc)
        {
            if (world == null || Unit == null || startTile == null || endT == null)
            {
                Debug.LogError("null values passed to HPath::FindPath");
                Debug.LogError(Unit);
                Debug.LogError(startTile);
                Debug.LogError(world);
                Debug.LogError(endT);

                return null;
            }

            IQPath_AStar resolver = new IQPath_AStar(world, Unit, startTile, endT, costEstimateFunc);
            resolver.DoWork();

            return resolver.GetList();
        }
    }
    public delegate float CostEstimateDelegate(IPathTile a, IPathTile b);

}

        

    

