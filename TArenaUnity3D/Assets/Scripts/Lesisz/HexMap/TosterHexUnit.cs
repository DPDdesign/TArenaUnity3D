using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HPath;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
using TimeSpells;
public class TosterHexUnit : IQPathUnit
{
    public readonly int C; public readonly int R; public readonly int S; // column.row
    public Vector3 vec;
  

    List<SpellOverTime> SpellsGoingOn;
    public int SpecialHP = 0;
    public int SpecialAtt = 0;
    public int SpecialDef = 0;
    public int SpecialMS = 0;
    public int SpecialI = 0;
    public int SpecialAm = 0;
    public int SpecialmaxDMG = 0;
    public int SpecialminDMG = 0;
    public int SpecialResistance = 0;
    public int SpecialDMGModificator = 0;
    ///stats
    [XmlAttribute("Name")]
    public string Name = "NoName";
    [XmlAttribute("HP")]
    public int HP = 100;
    public int GetHP()
    {
        return HP + SpecialHP;
    }
    public int TempHP = 100;
    public int maxdmg = 1;
    public int GetMaxDMG()
    {
        return maxdmg + SpecialmaxDMG;
    }
    public int mindmg = 1;
    public int GetMinDmg()
    {
        return mindmg + SpecialminDMG;
    }
    [XmlAttribute("Attack")]
    public int Att = 1;
    public int GetAtt()
    {
        return Att + SpecialAtt;
    }
    [XmlAttribute("Defense")]
    public int Def = 1;
    public int GetDef()
    {
        return Def + SpecialDef;
    }
    [XmlAttribute("Speed")]
    public int MovmentSpeed = 5;
    public int GetMS()
    {
        return MovmentSpeed + SpecialMS;
    }
    [XmlAttribute("Initiative")]
    public int Initiative = 2;
    public int GetIni()
    {
        return Initiative + SpecialI;
    }
    public int Amount = 1;
    public int TempCounterAttacks = 1;
    public int CounterAttacks = 1;
    public bool teamN = true;
    public List<string> skillstrings;
    public bool Waited = false;
    public bool DefenceStance = false;
    public bool isDead = false;
    public bool CounterAttackAvaible = true;

    public bool Stuned = false;
    public bool Blinded = false;
    public bool Rooted = false;
    public bool Taunt = false;
    public TosterHexUnit whoTauntedMe = null;
    public bool Disarm = false;
    public bool Berserk = false;
 //   private HexMap hexMap;

    public void isStuned()
    {
        if (Stuned == true)
        {
            Moved = true;
        }
    }

    public void isBlinded() { }



  public  void ResetCounterAttack()
    {
        CounterAttackAvaible = true;
        TempCounterAttacks = CounterAttacks;
    }



    public void CounterAttackBools()
    {
        //CounterAttackAvaible = true;
        TempCounterAttacks--;
        if (TempCounterAttacks == 0) CounterAttackAvaible = false;
    }







    public void TeleportToHex(HexClass hex)
    {
        tosterView.TeleportTo(hex);
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
    public HexClass[] Pathing(HexClass celhex,bool ignore)
    {
        HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate, ignore);
        return p;
    }
    public bool IsPathAvaible(HexClass celhex)
    {
        HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate, false);
        return p.Length < GetMS() + 1;
    }
    public int MovementCostToEnterHex(HexClass hex)
    {
        return hex.BaseMovementCost();
    }
    public float TurnsToGetToHex(HexClass hex, TosterHexUnit tosterWhoAsk, float MovesToDate)
    {
        float baseMovesToEnterHex = MovementCostToEnterHex(hex) / GetMS();
        float MoveRemaining = GetMS();
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

    
        skillstrings = new List<string>();
        SpellsGoingOn = new List<SpellOverTime>();
        /*
        Type type = Type.GetType(p, true);  
        Skill1 s1 = new Skill1();
        Debug.Log(type.FullName);
        skills.Add(s1); 
        */
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
    public void SetStats(string newname, int newhp, int newattack, int newdefense, int newinitiative, int newspeed, List<string> spells, int min, int max)
    {
       
        Name = newname;
        HP = newhp;
        TempHP = newhp;
        Att = newattack;
        Def = newdefense;
        Initiative = newinitiative;
        MovmentSpeed = newspeed;
        skillstrings = spells;
        mindmg = min;
        maxdmg = max;
    }
    #region Układ danych w xmlu
    /*
		<Units>
             <Unit>
                0 <Name>TosterDPS</Name>
                1 <HP>20</HP>
                2 <Attack>20</Attack>
                3 <Defense>1</Defense>
                4 <Initiative>9</Initiative>
                5 <Speed>10</Speed>
                6 <Skills>
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
      //  
        if (found == true)
        {
            XmlNodeList UnitNodes = nodes[NumberOfNode].ChildNodes;
            XmlNodeList spells = UnitNodes[8].ChildNodes;
        
            List<string> sp = new List<string>();
            foreach (XmlNode s in spells)
            {
                
                sp.Add(s.InnerText);

            }

            
            SetStats(
                UnitNodes[0].InnerText,
                int.Parse(UnitNodes[1].InnerText),
                int.Parse(UnitNodes[2].InnerText),
                int.Parse(UnitNodes[3].InnerText),
                int.Parse(UnitNodes[4].InnerText),
                int.Parse(UnitNodes[5].InnerText),
                sp,
                int.Parse(UnitNodes[6].InnerText),
                int.Parse(UnitNodes[7].InnerText));
        }
    } 
    public void SetTosterPrefab(HexMap h)
    {
      
                this.TosterPrefab = Resources.Load<GameObject>("Models/TosterPrefabs/" + this.Name);
        if (this.teamN == true) this.TosterPrefab.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 0, 0);
                else this.TosterPrefab.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 180, 0);

        //  Debug.Log( this.TosterPrefab.GetComponentInChildren<Renderer>().transform.SetPositionAndRotation(new Vector3(0,0,0),new Quaternion.Euler(0,180,0)));




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

    // TempHP = current 
    public void HealMe(int h)
    {
      if (TempHP-h < GetHP()) { TempHP += h; }
        else { TempHP = GetHP(); }
    }

    public double CalculateDamageBetweenTosters(TosterHexUnit attacker, TosterHexUnit defender, double modifier)
    {

        bool isReduced = false;

        int ai = attacker.GetAtt();
        int di = defender.GetDef();
        double I1=0;
        double R1=0;
   
        double a = Convert.ToDouble(ai);
        double d = Convert.ToDouble(di);
        double ADD = a - d;

        if (ADD >= 60) I1 = 3;
        else if (ADD > 0 && ADD < 60) I1 = 0.05 * (a - d);
        else if (ADD < 0 && ADD > -28) R1 = 0.025 * (d - a);
        else if (ADD < -28) R1 = 0.7;
        
        Debug.LogError("I1 " + I1);
        Debug.LogError("R1 " + R1);

        double R5 = isReduced ? 0.5 : 0;

        double DMGb = Random.Range(attacker.GetMinDmg(), attacker.GetMaxDMG()) * attacker.Amount * /*modifier*/(((100.0 - attacker.SpecialDMGModificator) / 100.0)); 
        Debug.Log(DMGb);

        double DMGf = DMGb * (1 + I1) * (1 - R1) * (1 - R5) * (((100.0 - defender.SpecialResistance) / 100.0));
        Debug.Log(((100.0 - defender.SpecialResistance) / 100.0));
        Debug.Log((DMGf));

        return DMGf;

    }

    public void AttackMe(TosterHexUnit t)
    {
        double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        if (DealMePURE(Convert.ToInt32(dmgdouble)))

            if (CounterAttackAvaible == true)
            {
                CounterAttackBools();

                dmgdouble = CalculateDamageBetweenTosters(this, t, 1);

                t.DealMePURE(Convert.ToInt32(dmgdouble));
            }


    }


    public void DealMeDMG(TosterHexUnit t)
    {
        double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        DealMePURE(Convert.ToInt32(dmgdouble));
    }

    public void CalculateResult(double dmgdouble)
    {

    }
    public bool DealMePURE(int i)
    {
        int newhp = (GetHP() * (Amount - 1) + TempHP) - i;

        Amount = Mathf.FloorToInt(newhp / GetHP());

        TempHP = (newhp - Amount * GetHP());

        if (TempHP >= 1)
        {

            Amount++;

        }
        else TempHP = GetHP();

        if (Amount < 1)
        {
            Died();
            return false;
        }
        else { SetTextAmount(); return true; }
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


    public void CheckSpells()
    {
        List<SpellOverTime> SpellsToRemove = new List<SpellOverTime>();
        foreach (SpellOverTime s in SpellsGoingOn)
        {
            Debug.LogError(s.Time);
            Debug.LogError(s.me.Name);
            s.DoTurn();
            if (s.IsOver())
            {
                SpellsToRemove.Add(s);
               
            }
        }

        foreach (SpellOverTime s in SpellsToRemove)
        {
            SpellsGoingOn.Remove(s);
        }
    }


    public SpellOverTime AskForSpell(string str, SpellOverTime spell)
    {
        List<SpellOverTime> SpellsToRemove = new List<SpellOverTime>();
        foreach (SpellOverTime s in SpellsGoingOn)
        {
            if (s.nameofspell == str&&s!=spell)
            {
                return s;
            }

        }
        return null;
    }
    public void RemoveSpell(SpellOverTime spell)
    {
        SpellsGoingOn.Remove(spell);
    }



    public void AddNewTimeSpell(int Time,
                  TosterHexUnit target,
                  int hp,
                  int att,
                  int def,
                  int ms,
                  int ini,
                  int maxdmg,
                  int mindmg,
                  int dmgovertime,
                  int res,
                  int counterattacks,
                  int SpecialDMGModificator,
                  string nameofspell,
                  bool isStackable)
    {
        SpellOverTime spell = new SpellOverTime(Time, target, this, hp, att, def, ms, ini, maxdmg, mindmg, dmgovertime, res, counterattacks, SpecialDMGModificator, nameofspell, isStackable);
        SpellsGoingOn.Add(spell);
    }
}
