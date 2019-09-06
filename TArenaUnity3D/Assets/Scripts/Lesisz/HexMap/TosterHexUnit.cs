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
using Photon;
using Photon.Realtime;
using Photon.Pun;

public class TosterHexUnit : IQPathUnit
{
    public readonly int C; public readonly int R; public readonly int S; // column.row
    public Vector3 vec;

    CastManager cm;
    List<SpellOverTime> SpellsGoingOn;
    public int FlatDMGReduce = 0;
    public int SpecialHP = 0;
    public int SpecialAtt = 0;
    public int SpecialPUREDMG = 0;
    public int SpecialDef = 0;
    public int SpecialMS = 1;
    public int SpecialI = 0;
    public int SpecialAm = 0;
    public int SpecialmaxDMG = 0;
    public int SpecialminDMG = 0;
    public int SpecialResistance = 0; // +20 oznacza że otrzymany dmg zostanie zmniejszony o 20%
    public int SpecialDMGModificator = 0;// +20 oznacza że zadawany dmg zostanie zmniejszony o 20%
    public bool isRange = false;
    public double DefensePenetration = 0;
    public GameObject Projectile;
    public string TextToSend;
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
    public bool teamN = true; // True znaczy ze jest z teamu z "lewej" strony
    public List<string> skillstrings;
    public List<int> cooldowns;
    public bool Waited = false;
    public bool DefenceStance = false;
    public bool isDead = false;
    public bool CounterAttackAvaible = true;
    public bool Fire_movement = false;
    public bool Stuned = false;
    public bool Blinded = false;
    public bool Rooted = false;
    public bool Taunt = false;
    public TosterHexUnit whoTauntedMe = null;
    public TosterHexUnit HATED = null;
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
        if (CounterAttacks > 0)
        {
            CounterAttackAvaible = true;
            TempCounterAttacks = CounterAttacks;
            Debug.Log(TempCounterAttacks);
        }
    }



    public void CounterAttackBools()
    {
        //CounterAttackAvaible = true;
        TempCounterAttacks--;
        if (TempCounterAttacks < 1) CounterAttackAvaible = false;
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



    public List<string> ListOfAutocasts;
   
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
        
        if (hex.isTraped)
        {
            if (hex.trap.NameOfTraps == "Rope_Trap")
            {

                Pathing_func(hex,true);
                this.AddNewTimeSpell(1, this, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 30, "Rope_Trap", false);
                hex.RemoveTrap();
            }
            if (hex.trap.NameOfTraps == "Fire_Trap")
            {
                TextToSend = "";
                TextToSend += "Fire_Trap zadał " + this.Amount + " obrażeń " + this.Name;
                Quaternion q;
                if (this.teamN == true)
                {
                    Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Master);
                }
                else
                {
                    Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Client);
                }
                this.DealMePURE(this.Amount);
              //  hex.RemoveTrap();
            }
            if (hex.trap.NameOfTraps == "Spike_Trap")
            {

              
                this.AddNewTimeSpell(2, this, 0, 0, 0, -2, 0, 0, 0, 0, 0, 0, 0, 0, "Spike_Trap", false);
                if (hexPath.Count>1)
                {
                    hexPath.RemoveAt(hexPath.Count - 1);
                    hexPath.RemoveAt(hexPath.Count - 1);
                }
                if (hexPath.Count == 1)
                {
                    hexPath.RemoveAt(hexPath.Count - 1);

                }

                hex.RemoveTrap();
            }
        }
        if(Fire_movement==true && oldHex!=null)
        {
            oldHex.AddTrap("Fire_Trap",2);
        }
        Hex = hex;
        Hex.AddToster(this);
        if (OnTosterMoved != null)
        {
            OnTosterMoved(oldHex, hex);
        }
        this.SetTextAmount();
        if (this.tosterView != null)
        {
            Animator d = this.tosterView.GetComponentInChildren<Animator>();
            if (d != null)
            {
                Debug.Log(d);
                d.Play("Move");

            }
        }
        if (this.tosterView != null)
        {
            int tC, tR;
            tC = oldHex.C - Hex.C;
            tR = oldHex.R - Hex.R;
            if (tC == 0 && tR == 1)
            {

                this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 120, 0);
            }
            if (tC == 0 && tR == -1)
            {

                this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, -60, 0);
            }
            if (tC == -1 && tR == 1)
            {

                this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 60, 0);

            }
            if (tC == 1 && tR == -1)
            {

                this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, -120, 0);

            }
            if (tC == 1 && tR == 0)
            {

                this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            if (tC == -1 && tR == 0)
            {

                this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 0, 0);
            }

        }


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
            vec.y,// + G.transform.lossyScale.y / 2,
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
        cooldowns = new List<int>();
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
      
                this.TosterPrefab = Resources.Load<GameObject>("Models/TosterModels/" + this.Name);
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

    public void StartAutocast()
    {
        ListOfAutocasts = new List<string>(new string[] { "Massochism", "Cold_Blood", "Stone_Skin", "Unstoppable_Light","Fire_Movement","Fire_Skin","Terrifying_Presence", "Rotting"});


        foreach (string s in skillstrings)
        {
            cooldowns.Add(0);
            if (ListOfAutocasts.Contains(s))
            {
                AddNewTimeSpell(1, this, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0, s, false);
            }
        }
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
      if (TempHP+h < GetHP()) { TempHP += h; }
        else { TempHP = GetHP(); }
    }

    public double CalculateDamageBetweenTosters(TosterHexUnit attacker, TosterHexUnit defender, double modifier)
    {

        bool isReduced = false;

        int ai = attacker.GetAtt();
        int di = defender.GetDef();
        double M = 0;
   
        double a = Convert.ToDouble(ai);
        double d = Convert.ToDouble(di);
        d = d * (1.0 - attacker.DefensePenetration);
        double ADD = a - d;

        if (ADD == 0) M = 1;
        else if (ADD > 0) M = 0.04;
        else if(ADD<0) M = 0.014;
        
 
        double R5 = isReduced ? 0.5 : 0;

        double DMGb = Random.Range(attacker.GetMinDmg(), attacker.GetMaxDMG()) * attacker.Amount * (1+(ADD*M)) * (((100.0 - attacker.SpecialDMGModificator) / 100.0)); ;
      


        double DMGf = DMGb * (((100.0 - defender.SpecialResistance) / 100.0));
        //  Debug.Log("Toster name: " + attacker.Name + " attacks for: " + Math.Ceiling(DMGf));
        DMGf += attacker.SpecialPUREDMG;
        attacker.SpecialPUREDMG = 0;
        if (defender == attacker.HATED)
        {
            DMGf += DMGf / 2;
        }

        DMGf -=defender.FlatDMGReduce * attacker.Amount;

        if (DMGf < 0)
        {
            DMGf = 0;
        }
        return Math.Ceiling(DMGf);

    }
    public double ReCalculateDamageBetweenTosters(TosterHexUnit attacker, TosterHexUnit defender, double modifier, int dmgtodo)
    {

        bool isReduced = false;

        int ai = attacker.GetAtt();
        int di = defender.GetDef();
        double M = 0;
   
        double a = Convert.ToDouble(ai);
        double d = Convert.ToDouble(di);
        Debug.Log(d);
        d = d * (1.0-attacker.DefensePenetration);
        Debug.Log(d);
        double ADD = a - d;

        if (ADD == 0) M = 1;
        else if (ADD > 0) M = 0.04;
        else if (ADD < 0) M = 0.014;


        double R5 = isReduced ? 0.5 : 0;

        double DMGb = dmgtodo * attacker.Amount * (1 + (ADD * M)) * (((100.0 - attacker.SpecialDMGModificator) / 100.0)); ;



        double DMGf = DMGb * (((100.0 - defender.SpecialResistance) / 100.0));
        //  Debug.Log("Toster name: " + attacker.Name + " attacks for: " + Math.Ceiling(DMGf));
        DMGf += attacker.SpecialPUREDMG;
        attacker.SpecialPUREDMG = 0;
        if (defender == attacker.HATED)
        {
            DMGf += DMGf / 2;
        }

        DMGf -= defender.FlatDMGReduce * attacker.Amount;

        if (DMGf<0)
        {
            DMGf = 0;
        }
          Debug.Log(DMGf);
        return Math.Ceiling(DMGf);

    }
    public double CalculateDamageBetweenTostersH3(TosterHexUnit attacker, TosterHexUnit defender, double modifier)
    {

        bool isReduced = false;

        int ai = attacker.GetAtt();
        int di = defender.GetDef();
        double M = 0;

        double a = Convert.ToDouble(ai);
        double d = Convert.ToDouble(di);
        double ADD = a - d;

        if (ADD == 0) M = 1;
        else if (ADD > 0) M = 0.05;
        else if (ADD < 0) M = 0.025;


        double R5 = isReduced ? 0.5 : 0;

        double DMGb = Random.Range(attacker.GetMinDmg(), attacker.GetMaxDMG()) * attacker.Amount * (1 + (ADD * M)) * (((100.0 - attacker.SpecialDMGModificator) / 100.0)); ;



        double DMGf = DMGb * (((100.0 - defender.SpecialResistance) / 100.0));
        Debug.Log("Toster name: " + attacker.Name + " attacks for: " + Math.Ceiling(DMGf));


        return Math.Ceiling(DMGf);

    }
    public void AttackMe(TosterHexUnit t)
    {
       
          Quaternion _lookRotation;
     Vector3 _direction;
    //  double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
    double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        int tC, tR;
        TextToSend = "";
        TextToSend +=  t.Name + " zadał " + Convert.ToInt32(dmgdouble) + " obrażeń " + this.Name;
            Quaternion q;
        if (t.teamN == true)
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Master);
        }
        else
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Client);
        }
        this.DealMePURE(Convert.ToInt32(dmgdouble));

        
        Animator d = null;
        if (CounterAttackAvaible == true)
        {
            CounterAttackBools();

            // dmgdouble = CalculateDamageBetweenTostersH3(this, t, 1);

            dmgdouble = CalculateDamageBetweenTosters(this, t, 1);
            TextToSend = "";
            TextToSend +=  this.Name + " zadał " + Convert.ToInt32(dmgdouble) + " obrażeń " + t.Name;
            if (t.teamN == false)
            {
                Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Master);
            }
            else
            {
                Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Client);
            }
            t.DealMePURE(Convert.ToInt32(dmgdouble));
          

            d = this.tosterView.GetComponentInChildren<Animator>();

            if (d != null)
            {
                q = this.tosterView.transform.rotation;

                tC = t.Hex.C - this.Hex.C;
                tR = t.Hex.R - this.Hex.R;
                if (tC == 0 && tR == 1)
                {

                    this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, -60, 0);
                }
                if (tC == 0 && tR == -1)
                {

                    this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 120, 0);
                }
                if (tC == -1 && tR == 1)
                {

                    this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, -120, 0);

                }
                if (tC == 1 && tR == -1)
                {

                    this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 60, 0);

                }
                if (tC == 1 && tR == 0)
                {

                    this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                if (tC == -1 && tR == 0)
                {

                    this.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 180, 0);
                }

                Debug.Log("t.C: " + t.Hex.C + "  t.R: " + t.Hex.R);
                Debug.Log("this.C: " + this.Hex.C + "  this.R: " + this.Hex.R);
                Debug.Log(d);
                d.Play("Atak");

       //    this.tosterView.transform.rotation = q;
            }
        }
      
        d = t.tosterView.GetComponentInChildren<Animator>();
        if (d != null)
        {
           
            q = this.tosterView.transform.rotation;
            tC = t.Hex.C - this.Hex.C;
            tR = t.Hex.R - this.Hex.R;
            if (tC == 0 && tR == 1)
            {

                t.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 120, 0);
            }
            if (tC == 0 && tR == -1)
            {

                t.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, -60, 0);
            }
            if (tC == -1 && tR == 1)
            {

                t.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 60, 0);

            }
            if (tC == 1 && tR == -1)
            {

                t.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, -120, 0);

            }
            if (tC == 1 && tR == 0)
            {

                t.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            if (tC == -1 && tR == 0)
            {

                t.tosterView.GetComponentInChildren<Renderer>().transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        
            Debug.Log(d);
            d.Play("Atak");
            
        //    this.tosterView.transform.rotation = q;
        }
    }


    public void ShootME(TosterHexUnit t, bool sth)
    {
        //  double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
        double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        HexClass[] Distance = Pathing(t.Hex,true);

        if (Distance.Length>6)
        {
            dmgdouble -= dmgdouble / 2;
        }
        TextToSend = "";
        TextToSend += t.Name + " zadał " + Convert.ToInt32(dmgdouble) + " obrażeń " + this.Name;
        Quaternion q;
        if (t.teamN == true)
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Master);
        }
        else
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Client);
        }
        DealMePURE(Convert.ToInt32(dmgdouble));
        if (sth == true)
        {
            Hex.hexMap.ThrowSomething(this, t, t.Projectile);
        }
   

    }
    public void AttackMeS(TosterHexUnit t)
    {
      //  double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
                                                                        double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        if (DealMePURESim(Convert.ToInt32(dmgdouble)))

            if (CounterAttackAvaible == true)
            {
             //   Debug.LogWarning("CounterAttack");
                CounterAttackBools();

             //   dmgdouble = CalculateDamageBetweenTostersH3(this, t, 1);

                    dmgdouble = CalculateDamageBetweenTosters(this, t, 1);

                t.DealMePURESim(Convert.ToInt32(dmgdouble));
            }


    }



    public void DealMeDMG(TosterHexUnit t)
    {
       // double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
                                                                       double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        TextToSend = "";
        TextToSend += t.Name + " zadał " + Convert.ToInt32(dmgdouble) + " obrażeń " + this.Name;
        Quaternion q;
        if (t.teamN == true)
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Master);
        }
        else
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Client);
        }
        DealMePURE(Convert.ToInt32(dmgdouble));
    
    }
    public void DealMeDMGS(TosterHexUnit t)
    {
        //double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
                                                                        double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        DealMePURESim(Convert.ToInt32(dmgdouble));

    }


    public void CalculateResult(double dmgdouble)
    {

    }
    public bool DealMePURE(int i)
    {
        
 
        this.Blinded = false;
        int newhp = (GetHP() * (Amount - 1) + TempHP) - i;
        Debug.Log(this.Name +" lost " + (Amount - Mathf.FloorToInt(newhp / GetHP())-1) + " units");
        int tempamout = Amount;
        Amount = Mathf.FloorToInt(newhp / GetHP());

        TempHP = (newhp - Amount * GetHP());

        if (TempHP >= 1)
        {

            Amount++;

        }
        else TempHP = GetHP();
        TextToSend = "";
        TextToSend += this.Name + " stracił " +(tempamout-Amount) + " jednostek";
        if (this.teamN != false)
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Master);
        }
        else
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Client);
        }
        if (Amount < 1)
        {
            Died();
            return false;
        }
        else { SetTextAmount(); return true; }
    }
    public void DealMeDMGDef(int i, TosterHexUnit t)
    {

        Debug.LogError(i);
        i =Convert.ToInt32(ReCalculateDamageBetweenTosters(t,this,1,i));
        TextToSend = "";
        TextToSend += t.Name + " zadał " + Convert.ToInt32(i) + " obrażeń " + this.Name;
        Quaternion q;
        if (t.teamN == true)
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Master);
        }
        else
        {
            Chat.chat.SendMessageToChat(TextToSend, Msg.MessageType.Client);
        }
        this.DealMePURE(Convert.ToInt32(i));

    }

    public bool DealMePURESim(int i)
    {
      //  Debug.Log("Dmg: " + i);
        int newhp = (GetHP() * (Amount - 1) + TempHP) - i;


        int tempamount = Amount;

        Amount = Mathf.FloorToInt(newhp / GetHP());

        TempHP = (newhp - Amount * GetHP());

        if (TempHP >= 1)
        {
           
         
            Amount++;
       //     Debug.Log("Toster: " + this.Name + " lost " + (tempamount - Amount) + " units");
        }
        else
        {
          //  Debug.Log("Toster: " + this.Name + " lost " + (tempamount - Amount) + " units");    
            TempHP = GetHP();
        }

        if (Amount < 1)
        {
           // Debug.LogError(this.Name+ ": DIED");
            return false;
        }
        else {  return true; }
    }
    #endregion


    public void Died()
    {

            this.tosterView.GetComponentInChildren<Animator>().enabled=false;
       

        tosterView.gameObject.GetComponentInChildren<TextMesh>().text = "";
        isDead = true;
        Moved = true;
        Team.HexesUnderTeam.Remove(this.Hex);
        Hex.RemoveToster(this);
        //tosterView.Destroy();
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

        //int i = 0;
        for (int i=0; i<cooldowns.Count;i++)
        {
            if (cooldowns[i] > 0)
            {
                cooldowns[i]--;
            }
   
        }
        foreach (string s in skillstrings)
        {
            if (ListOfAutocasts.Contains(s))
            {
                AddNewTimeSpell(1, this, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0, s, false);
            }
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
    public SpellOverTime AskForSpell(string str)
    {
        List<SpellOverTime> SpellsToRemove = new List<SpellOverTime>();
        foreach (SpellOverTime s in SpellsGoingOn)
        {
            Debug.LogError(s.nameofspell);
            if (s.nameofspell == str)
            {
                return s;
            }

        }
        return null;
    }
    public void SetOver(SpellOverTime spell)
    {
        List<SpellOverTime> SpellsToRemove = new List<SpellOverTime>();
        foreach (SpellOverTime s in SpellsGoingOn)
        {
            Debug.LogError(s.nameofspell);
            if (s == spell)
            {
                Debug.Log("Toster");
                s.Time = 0;
                s.IsOver();
                SpellsToRemove.Add(s);
            }

        }
        foreach (SpellOverTime s in SpellsToRemove)
        {
            SpellsGoingOn.Remove(s);
        }
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
                  int SpecialResistance,
                  string nameofspell,
                  bool isStackable)
    {
        SpellOverTime spell = new SpellOverTime(Time, target, this, hp, att, def, ms, ini, maxdmg, mindmg, dmgovertime, res, counterattacks, SpecialDMGModificator, SpecialResistance, nameofspell, isStackable);
        SpellsGoingOn.Add(spell);
    }
    public void AddNewTimeSpell(SpellOverTime spell)
    {
        spell.me = this;
        SpellOverTime spelll = new SpellOverTime(spell);
     
        SpellsGoingOn.Add(spelll);
    }
}
