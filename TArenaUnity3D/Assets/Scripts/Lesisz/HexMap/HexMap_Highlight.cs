using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HexMap_Highlight : HexMap
{
    void Highlight(int q, int r, int range, float centerHeight = .8f)
    {
        HexClass centerHex = GetHexAt(q, r);

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, range);

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;

            h.Highlight = true;
        }
        UpdateHexVisuals();
    }

    void unHighlight(int q, int r, int range, float centerHeight = .8f)
    {
        HexClass centerHex = GetHexAt(q, r);

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, range);

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;

            h.Highlight = false;
        }
        UpdateHexVisuals();
    }

}
