using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HPath;
public class TosterHexUnit : IQPathUnit
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

        this.hexPath.Dequeue(); //first is one we are standing on
    }



    public void DUMMY_PATHING_FUNCTION()
    {

        IPathTile[] p = HPath.HPath.FindPath(Hex.hexMap, this, Hex, Hex.hexMap.GetHexAt(Hex.C+2,Hex.R+2), HexClass.CostEstimate);
        
        HexClass[] hs = System.Array.ConvertAll ( p, a=>(HexClass)a);

        Debug.LogError(hs.Length);
        SetHexPath(hs);
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
       
    }
    public void DoAllMoves()
    {
        while (hexPath.Count != 0)
        {
            if (hexPath == null || hexPath.Count == 0)
            {
                return;
            }

            HexClass oldHex = Hex;
            HexClass newHex = hexPath.Dequeue();

            SetHex(newHex);
        
        }
    }


    public int MovementCostToEnterHex(HexClass hex)
    {
        return hex.BaseMovementCost();
    }

public float TurnsToGetToHex(HexClass hex, float MovesToDate)
    {

        float baseMovesToEnterHex = MovementCostToEnterHex(hex) / MovmentSpeed;
        float MoveRemaining = MovmentSpeed;

        float MovestoDateWhole = Mathf.Floor(MovesToDate);
        float MovesToDateFraction = MovesToDate - MovestoDateWhole;

        if (MovesToDateFraction < 0.01 || MovesToDateFraction > 0.99)
        {

        }

        return 1;
    }

    public float CostToEnterHex(IPathTile sourceTile, IPathTile destinationTile)
    {
        return 1;
    }
}
