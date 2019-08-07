using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexClass {
    public readonly int C; public readonly int R; public readonly int S; // column.row
   static readonly float WIDTH_MULTIPLIER = Mathf.Sqrt(3) / 2;
    public HexClass(int c, int r)
    {
        this.C = c;
        this.R = r;
        this.S = -(c + r);
    }
    public Vector3 Position()
    {
        
            float radius = 1f;
        float height = radius * 2;
        float width = Mathf.Sqrt(3) / 2 * height;
        float vert = height * 0.75f;
        float horiz = width;
        return new Vector3(
            horiz*(this.C+this.R/2f),
            0,
            vert * this.R
            );
    }
}
