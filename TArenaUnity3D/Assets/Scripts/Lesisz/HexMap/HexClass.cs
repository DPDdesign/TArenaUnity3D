using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HPath;
public class HexClass : IPathTile {
    public readonly int C; public readonly int R; public readonly int S; // column.row
    static readonly float WIDTH_MULTIPLIER = Mathf.Sqrt(3) / 2;
    public bool Highlight = false;
    public List<TosterHexUnit> Tosters;
    public GameObject MyHex;
    public readonly HexMap hexMap;

    public HexClass(HexMap hexMap, int c, int r)
    {
        this.hexMap = hexMap;
        this.C = c;
        this.R = r;
        this.S = -(c + r);
        Highlight = false;
        Tosters = new List<TosterHexUnit>();
      
    }

    List<HexClass> Neighbours;
    public List<HexClass> FindN()
    {

        Neighbours.Add(hexMap.GetHexAt(C, R - 1));
        Neighbours.Add(hexMap.GetHexAt(C + 1, R - 1));

        Neighbours.Add(hexMap.GetHexAt(C, R + 1));

        Neighbours.Add(hexMap.GetHexAt(C - 1, R));

        Neighbours.Add(hexMap.GetHexAt(C - 1, R + 1));
        return Neighbours;

    }

    public int[,] ListOfN;
    public List<int[,]> FindNPath(List<int[,]> d)
    {
        List<int[,]> P = new List<int[,]>();



        return P;
    }

    public bool IsListOFunitsEmpty()
    {

        return 0 == Tosters.Count;
    }
  
    float radius = 1f;
    public List<int []> FindN(int c,int r)
    {
        List<int []> m= new List<int []>();

        m.Add(new int[2] { c, r - 1 });
        m.Add(new int[2] { c, r + 1 });
        m.Add(new int[2] { c+1, r - 1 });
        m.Add(new int[2] { c - 1, r + 1 });
        m.Add(new int[2] { c-1, r});
        m.Add(new int[2] { c+1, r});

        return m;

    }
    public Vector3 Position()
    {
        
            
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

    public Vector3 PositionForToster(GameObject G)
    {
       
        float height = radius * 2;
        float width = Mathf.Sqrt(3) / 2 * height;
        float vert = height * 0.75f;
        float horiz = width;
        return new Vector3(
        horiz * (this.C + this.R / 2f),
         0 + G.transform.lossyScale.y / 2,
         vert * this.R
         );
    }
    public void AddToster(TosterHexUnit Toster)
    {
        if (Tosters == null)
        {
            Tosters = new List<TosterHexUnit>();
        }
        Tosters.Add(Toster);
    }
    public void RemoveToster(TosterHexUnit Toster)
    {
        if (Tosters != null)
        {
            Tosters.Remove(Toster);

        }
    }

    public TosterHexUnit[] tosters()
    {
        return Tosters.ToArray();
    }



    public float HexHeight()
    {
        return radius * 2;
    }
    public float HexWidth()
    {
        return WIDTH_MULTIPLIER * HexHeight();
    }
    public float HexVerticalSpacing()
    {
        return HexHeight() * 0.75f;
    }
    public float HexHorizontalSpacing()
    {
        return HexWidth();
    }

    public Vector3 PositionFromCamera(Vector3 cameraPosition, float numRows, float numColumns)
    {
       // float mapHeight = numRows * HexVerticalSpacing();
        float mapWidth = numColumns * HexHorizontalSpacing();

        Vector3 position = Position();
        float howManyWidthsFromCamera = (position.x - cameraPosition.x) / mapWidth;
        if (Mathf.Abs(howManyWidthsFromCamera) <= 0.5f)
        {
            return position;
        }
        if (howManyWidthsFromCamera > 0)
            howManyWidthsFromCamera += 0.5f;
        else
            howManyWidthsFromCamera -= 0.5f;

        int HowManyWidthsToFix = (int)howManyWidthsFromCamera;

        position.x -= HowManyWidthsToFix * mapWidth;

        return position;
    }


    public int BaseMovementCost()
    {
        return 1;
    }
    HexClass[] neighbours;
    public IPathTile[] GetNeighbours()
    {
        if (this.neighbours != null)
            return this.neighbours;

        List<HexClass> neighbours = new List<HexClass>();
        neighbours.Add(hexMap.GetHexAt(C+1, R));
        if (C - 1 >= 0)
            neighbours.Add(hexMap.GetHexAt(C-1, R));
        neighbours.Add(hexMap.GetHexAt(C, R+1));
        if (R - 1 >= 0)
            neighbours.Add(hexMap.GetHexAt(C, R-1));
        if (R - 1 >= 0)
            neighbours.Add(hexMap.GetHexAt(C+1, R-1));
        if (C-1>=0)
        neighbours.Add(hexMap.GetHexAt(C-1, R+1));

        List<HexClass> neighbours2 = new List<HexClass>();
        foreach(HexClass h in neighbours)
        {
            if(h!=null)
            {
                neighbours2.Add(h);
            }
        }
        this.neighbours = neighbours2.ToArray();
        return this.neighbours;
    }

    public float CostToMoveToTile(float costsofar, IPathTile sourceTile, IQPathUnit Unit)
    {
        throw new System.NotImplementedException();
    }

    public static float CostEstimate(IPathTile aa, IPathTile bb)
    {
        return Distance((HexClass)aa, (HexClass)bb);
    }


    public static float Distance(HexClass a, HexClass b)
    {
        int dQ = Mathf.Abs(a.C - b.C);
        int dR = Mathf.Abs(a.R - b.R);
        return Mathf.Max(dQ, dR, Mathf.Abs(a.S - b.S));
    }

    public float AggregateCostToEnter(float costSoFar, IPathTile sourceTile, IQPathUnit theUnit)
    {
        // TODO: We are ignoring source tile right now, this will have to change when
        // we have rivers.
        return ((TosterHexUnit)theUnit).TurnsToGetToHex(this, costSoFar);
    }
}
