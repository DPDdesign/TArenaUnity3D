using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TosterClass
{
    public readonly int C; public readonly int R; public readonly int S; // column.row
    static readonly float WIDTH_MULTIPLIER = Mathf.Sqrt(3) / 2;
   public  Vector3 vec;
    public TosterClass(int c, int r, Vector3 vect)
    {
        this.C = c;
        this.R = r;
        this.S = -(c + r);
        vec = vect;
    }
    public Vector3 Position(GameObject G)
    {
        return new Vector3(
            vec.x,
            vec.y+G.transform.lossyScale.y/2,
            vec.z
            );
    }

}
