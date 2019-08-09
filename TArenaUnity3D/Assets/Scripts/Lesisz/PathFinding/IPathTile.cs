using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HPath
{
   public interface IPathTile
    {
        IPathTile[] GetNeighbours();

        float CostToMoveToTile(float costsofar, IPathTile sourceTile, IQPathUnit Unit) ;
        float AggregateCostToEnter(float costSoFar, IPathTile sourceTile, IQPathUnit theUnit);
    }
}
