using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TosterHexUnit
{
    public readonly int C; public readonly int R; public readonly int S; // column.row
     public  Vector3 vec;


    ///stats

    public string Name = "NoName";
    public int HP = 100;
    public int Att = 1;
    public int Def = 1;
    public int MovmentSpeed = 2;
    public int Initiative = 2;

    ///

   public GameObject ThisToster;
    public GameObject TosterPrefab;
    public HexClass Hex { get; protected set; }

    public delegate void TosterMovedDelegate(HexClass oldH, HexClass newH);
    public event TosterMovedDelegate OnTosterMoved;


    Queue<HexClass> hexPath;


    public void SetHexPath(HexClass[] hexPath)
    {
        this.hexPath = new Queue<HexClass>(hexPath);
    }

    public TosterHexUnit(int c, int r, Vector3 vect, GameObject G, GameObject Toster)
    {
        this.C = c;
        this.R = r;
        this.S = -(c + r);
        vec = vect;
        ThisToster = G;
        TosterPrefab = Toster;
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

    public List<HexClass> HexPathList;

    public void FindTosterPath()
    {
        List<int[]> m = new List<int[]>();
        
        m = Hex.FindN(C,R);
        

    }


    public void DoTurn()
    {
        if(hexPath==null|| hexPath.Count==0)
        {
            return;
        }

        HexClass oldHex = Hex;
        HexClass newHex = hexPath.Dequeue();

        SetHex(newHex);
        Debug.LogError("działa");
    }
}
