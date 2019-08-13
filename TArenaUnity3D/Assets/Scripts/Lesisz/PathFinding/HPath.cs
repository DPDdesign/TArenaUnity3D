using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HPath
{
    public static class HPath
    {
        public static T[] FindPath<T>(IQPathWorld world, IQPathUnit Unit, T startTile, T endT, CostEstimateDelegate costEstimateFunc, bool ignore ) where T : IPathTile
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

            IQPath_AStar<T> resolver = new IQPath_AStar<T>(world, Unit, startTile, endT, costEstimateFunc);
            resolver.DoWork(ignore);

            return  resolver.GetList();
        }
    }
    public delegate float CostEstimateDelegate(IPathTile a, IPathTile b);

}

        

    

