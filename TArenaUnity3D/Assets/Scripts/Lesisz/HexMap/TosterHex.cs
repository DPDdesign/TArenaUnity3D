using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TosterHex
{
    public readonly int C; public readonly int R; public readonly int S; // column.row
     public  Vector3 vec;


    ///stats

    public string Name = "NoName";
    public int HP = 100;
    public int Att = 1;
    public int Def = 1;
    public int MovmentSpeed = 5;

    ///

   public GameObject ThisToster;



    
    public TosterHex(int c, int r, Vector3 vect, GameObject G)
    {
        this.C = c;
        this.R = r;
        this.S = -(c + r);
        vec = vect;
        ThisToster = G;
    }
    public Vector3 Position(GameObject G)
    {
        return new Vector3(
            vec.x,
            vec.y+G.transform.lossyScale.y/2,
            vec.z
            );
    }
    private void OnMouseOver()
    {
        Debug.Log("Jestem Tosterem");
    }


    public HexClass Hex { get; protected set; }

    public delegate void TosterMovedDelegate(HexClass oldH, HexClass newH);
    public event TosterMovedDelegate OnTosterMoved;



    public void SetHex(HexClass hex)
    {
        HexClass oldHex = Hex;
        if (hex != null)
        {
            hex.RemoveToster(this);
        }
        Hex = hex;
        Hex.AddToster(this);
        if (OnTosterMoved != null)
        {
            OnTosterMoved(oldHex, hex);
        }
    }




    public void DoTurn()
    {
        HexClass oldHex = Hex;
        HexClass newHex = oldHex.hexMap.GetHexAt(oldHex.C, oldHex.R+1);

        SetHex(newHex);
        Debug.LogError("działa");
    }
}
