using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HPath;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine.UI;

public class TosterHexUnit : IQPathUnit
{
    public readonly int C; public readonly int R; public readonly int S; // column.row
    public Vector3 vec;



    public int SpecialHP = 0;
    public int SpecialAtt = 0;
    public int SpecialDef = 0;
    public int SpecialMS = 0;
    public int SpecialI = 0;
    public int SpecialAm = 0;
    ///stats
    [XmlAttribute("Name")]
    public string Name = "NoName";
    [XmlAttribute("HP")]
    public int HP = 100;
    public int TempHP = 100;
    public int maxdmg = 1;
    public int mindmg = 1; 
    [XmlAttribute("Attack")]
    public int Att = 1;
    [XmlAttribute("Defense")]
    public int Def = 1;
    [XmlAttribute("Speed")]
    public int MovmentSpeed = 5;
    [XmlAttribute("Initiative")]
    public int Initiative = 2;
    public int Amount = 1;
    public List<SkillsDefault> skills;
    public bool Waited = false;
    public bool DefenceStance = false;
    public bool isDead = false;
    public bool CounterAttackAvaible = true;
    public TosterView tosterView;
    /// <summary>
    /// 
    /// </summary>
    /// 
    public bool isSelected = false;
    public bool move = false;
    public GameObject ThisToster;
    public GameObject TosterPrefab;
    public HexClass Hex { get; protected set; }
    public bool RobieRuch = false;
    public delegate void TosterMovedDelegate(HexClass oldH, HexClass newH);
    public event TosterMovedDelegate OnTosterMoved;
    public bool Moved = false;
    public TeamClass Team;
    public List<HexClass> HexPathList;

    List<HexClass> hexPath;
    #region Pathing
    public void SetHexPath(HexClass[] hexPath)
    {
        this.hexPath = new List<HexClass>(hexPath);
    }
    public void ClearHexPath()
    {
        this.hexPath = new List<HexClass>();
    }
   
    /*
    public void Pathing_func(HexClass celhex)
    {
        if (move == true)
        {
            HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate);
            SetHexPath(p);
        }
    }*/
    public void Pathing_func(HexClass celhex, bool ignoreObstacles)
    {
        if (move == true)
        {
           
                HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate, ignoreObstacles);
                SetHexPath(p);
            

           
        }
    }
    public HexClass[] Pathing(HexClass celhex)
    {
        HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate, false);
        return p;
    }
    public bool IsPathAvaible(HexClass celhex)
    {
        HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate, false);
        return p.Length < MovmentSpeed + 1;
    }
    public int MovementCostToEnterHex(HexClass hex)
    {
        return hex.BaseMovementCost();
    }
    public float TurnsToGetToHex(HexClass hex, TosterHexUnit tosterWhoAsk, float MovesToDate)
    {
        float baseMovesToEnterHex = MovementCostToEnterHex(hex) / MovmentSpeed;
        float MoveRemaining = MovmentSpeed;
        float MovestoDateWhole = Mathf.Floor(MovesToDate);
        float MovesToDateFraction = MovesToDate - MovestoDateWhole;
        if (MovesToDateFraction < 0.01 || MovesToDateFraction > 0.99)
        {
            // czy powinniśmy zaokrąglać ruch w skrajnych przypadkach? NARAZIE NIC
        }
        if (!hex.IsListOFunitsEmpty())
        {
           // if (tosterWhoAsk.Team==hex.Tosters[0].Team)
            return -99; // Jeżeli na danym hexie znajduje się jednostka, blokujemy wejście - patrz dalej w wywołaniach
        }
        return 1;
    }
    public float CostToEnterHex(IPathTile sourceTile, IPathTile destinationTile)
    {
        return 1;
    }
    public void SetHex(HexClass hex)
    {
        HexClass oldHex = Hex;
        if (this.Hex != null)
        {
            this.Team.HexesUnderTeam.Remove(oldHex);
            this.Hex.RemoveToster(this);
        }
        Hex = hex;
        Hex.AddToster(this);
        if (OnTosterMoved != null)
        {
            OnTosterMoved(oldHex, hex);
        }
        this.SetTextAmount();
        this.Team.HexesUnderTeam.Add(Hex);
    }
    public void FindTosterPath()
    {
        List<int[]> m = new List<int[]>();
        m = Hex.FindN(C, R);
    }
    #endregion
    #region Hex-related
    public void TosterHexUnitAddHex(Vector3 vect, GameObject G)
    {
        vec = vect;
        ThisToster = G;
    }
    public Vector3 Position(GameObject G)
    {
        return new Vector3(
            vec.x,
            vec.y + G.transform.lossyScale.y / 2,
            vec.z
            );
    }
    #endregion
    #region Wywołania/przypisywanie nowych parametrów
    public void SetMyTeam(TeamClass t)
    {
        Team = t;
    }
    public TosterHexUnit()
    {
        Name = "NoName";
        HP = 100;
        Att = 1;
        Def = 1;
        MovmentSpeed = 5;
        Initiative = 2;
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
    public void SetStats(string newname, int newhp, int newattack, int newdefense, int newinitiative, int newspeed)
    {
        Name = newname;
        HP = newhp;
        TempHP = newhp;
        Att = newattack;
        Def = newdefense;
        Initiative = newinitiative;
        MovmentSpeed = newspeed;
    }
    #region Układ danych w xmlu
    /*
		<Units>
             <Unit>
                 <Name>TosterDPS</Name>
                 <HP>20</HP>
                 <Attack>20</Attack>
                 <Defense>1</Defense>
                 <Initiative>9</Initiative>
                 <Speed>10</Speed>
                 <Skills>
                      <Skill1>Skill</Skill1>
                 </Skills>
            </Unit> 
        </Units>
   */
    #endregion
    public void InitateType(string name) //XML DATA LOAD
    {
        //TODO: VALIDATE SCHEMA/XML
        TextAsset textAsset = (TextAsset)Resources.Load("data/Units");
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.LoadXml(textAsset.text);
        XmlNodeList nodes = xmldoc.SelectNodes("Units/Unit/Name");
        int NumberOfNode = 0;
        bool found = false;
        int i = 0;
        foreach (XmlNode node in nodes)
        {
            if (node.InnerText == name && found == false)
            {
                found = true;
                NumberOfNode = i;
            }
            i++;
        }
        nodes = xmldoc.SelectNodes("Units/Unit");
        if (found == true)
        {
            XmlNodeList UnitNodes = nodes[NumberOfNode].ChildNodes;
            SetStats(
                UnitNodes[0].InnerText,
                int.Parse(UnitNodes[1].InnerText),
                int.Parse(UnitNodes[2].InnerText),
                int.Parse(UnitNodes[3].InnerText),
                int.Parse(UnitNodes[4].InnerText),
                int.Parse(UnitNodes[5].InnerText));
        }
    } 
    public void SetTosterPrefab(HexMap h)
    {
        foreach (GameObject GO in h.TostersPrefabs)
        {
            if (GO.name == this.Name)
            {
                this.TosterPrefab = GO;
            }
        }

    }
    public void SetTextAmount()
    {
        //Amount--;

        if (tosterView != null)
        {
     
            tosterView.gameObject.GetComponentInChildren<TextMesh>().text = Amount.ToString();
          
     
            //TosterPrefab
        }
        else
        {
            TosterPrefab.gameObject.GetComponentInChildren<TextMesh>().text = Amount.ToString();
        }
    }
    public void SetAmount(int Amount)
    {
        this.Amount = Amount;
    }
    #endregion
    #region Tury/ruchy
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
        if (hexPath == null || hexPath.Count <= 1)
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
    public void DoAllMoves() // OLD
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

    public void AttackMe(TosterHexUnit t)
    {
        int newhp = (HP * (Amount - 1) + TempHP) - Mathf.Max(t.Att / Def * Random.Range(t.mindmg, t.maxdmg),1)*t.Amount;

        Amount = Mathf.FloorToInt(newhp / HP);

        TempHP = (newhp - Amount * HP);

        if (TempHP>=1 )
        {
         
            Amount++;

        }
        else TempHP = HP;

        if (Amount < 1)
        {
            Died();
        }
        else
        {
            SetTextAmount();
            if (CounterAttackAvaible==true)
            {
                CounterAttackAvaible = false;
                 newhp = (t.HP * (t.Amount - 1) + t.TempHP) - Mathf.Max(Att / t.Def * Random.Range(mindmg, maxdmg), 1) * Amount;

               t.Amount = Mathf.FloorToInt(newhp / t.HP);

                t.TempHP = (newhp - t.Amount *t.HP);

                if (t.TempHP >= 1)
                {

                    t.Amount++;

                }
                else t.TempHP = t.HP;

                if (t.Amount < 1)
                {
                    t.Died();
                    
                }
                else
                    t.SetTextAmount();
            }
        }

    }

    #endregion


    public void Died()
    {
        tosterView.gameObject.GetComponentInChildren<TextMesh>().text = "DEAD";
        isDead = true;
        Moved = true;
        Team.HexesUnderTeam.Remove(this.Hex);
        Hex.RemoveToster(this);
        tosterView.gameObject.transform.localScale = new Vector3(1f, 0.1f, 1f);
    }
}
