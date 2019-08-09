using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HPath
{
    public interface IQPathUnit
    {
        float CostToEnterHex(IPathTile sourceTile, IPathTile destinationTile);
    }

}