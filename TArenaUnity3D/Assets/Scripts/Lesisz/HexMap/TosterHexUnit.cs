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
    public int MovmentSpeed = 5;
    public int Initiative = 2;
    public TosterView tosterView;
    ///
    public bool move = false;
   public GameObject ThisToster;
    public GameObject TosterPrefab;
    public HexClass Hex { get; protected set; }
    public bool RobieRuch = false;
    public delegate void TosterMovedDelegate(HexClass oldH, HexClass newH);
    public event TosterMovedDelegate OnTosterMoved;


    List<HexClass> hexPath;


    public void SetHexPath(HexClass[] hexPath)
    {
        this.hexPath = new List<HexClass>(hexPath);
       
    }
    public void ClearHexPath()
    {
        this.hexPath = new List<HexClass>();
    }



    public void DUMMY_PATHING_FUNCTION()
    {
        if (move == true)
        {
            HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, Hex.hexMap.GetHexAt(Hex.C + 4, Hex.R), HexClass.CostEstimate);

          //  HexClass[] hs = System.Array.ConvertAll(p, a => (HexClass)a);

            Debug.LogError(p.Length);
            SetHexPath(p);
        }
    }

    public void Pathing_func(HexClass celhex)

    {
       
    
        if (move == true)
        {
           
            HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate);

            //  HexClass[] hs = System.Array.ConvertAll(p, a => (HexClass)a);

     
            SetHexPath(p);
            
        }
    }
    public HexClass[] Pathing(HexClass celhex)

    {
       
        
            HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate);

            //  HexClass[] hs = System.Array.ConvertAll(p, a => (HexClass)a);
            return p;
      
    }


    public bool IsPathAvaible(HexClass celhex)

    {


        HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate);

        //  HexClass[] hs = System.Array.ConvertAll(p, a => (HexClass)a);
        return p.Length<MovmentSpeed+1;

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

      if (this.Hex!=null)
            this.Hex.RemoveToster(this);
       
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
        if (hexPath == null || hexPath.Count == 0)
        {
            return;
        }
        RobieRuch = true;
        while (RobieRuch == true)
        {
            if (hexPath == null || hexPath.Count == 0)
            {
                RobieRuch = false;
            }
            HexClass oldHex = Hex;
            HexClass newHex = hexPath[0];

            SetHex(newHex);
            while (Hex.hexMap.AnimationIsPlaying)
            {
                //w8
            }
        }

    }

    public bool DoMove()
    {
        if (hexPath == null || hexPath.Count == 0)
        {
            return false;
        }
        HexClass HexWeAreLeaving =hexPath[0];
        HexClass newhex = hexPath[1];
        hexPath.RemoveAt(0);
        if(hexPath.Count==1)
        {
            hexPath = null;
        }

        
        SetHex(newhex);
        

        return hexPath != null;
    }
    public void DoAllMoves()

    {
        if (hexPath == null || hexPath.Count == 0)
        {
            return;
                }
        while (hexPath.Count != 0)
        {
          

            HexClass oldHex = Hex;
            HexClass newHex = hexPath[0];

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
        if (!hex.IsListOFunitsEmpty())
        {

            return -99;
        }
        return 1;
    }

    public float CostToEnterHex(IPathTile sourceTile, IPathTile destinationTile)
    {
        return 1;
    }
}
