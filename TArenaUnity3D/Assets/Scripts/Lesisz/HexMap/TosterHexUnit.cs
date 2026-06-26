using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HPath;
using System.Xml.Serialization;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
using TimeSpells;

public class TosterHexUnit : IQPathUnit
{
    private const float CombatAnimationMaxWaitSeconds = 1.25f;
    private const float CombatHitAtAttackProgress = 0.5f;
    public const float FireMovementTrapRevealDelaySeconds = 0.3f;
    public const float PassiveResolveMaxWaitSeconds = 15f;
    public static readonly string[] AutocastTurnOrder = new string[] { "Massochism", "Cold_Blood", "Stone_Skin", "Unstoppable_Light", "Fire_Movement", "Fire_Skin", "Terrifying_Presence", "Rotting" };
    public readonly int C; public readonly int R; public readonly int S; // column.row
    public Vector3 vec;
    public string TosterSpriteName;
    public List<SpellOverTime> SpellsGoingOn;
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
    public bool MovedThisTurn = false;
    public bool UsedSkillThisTurn = false;
    public List<string> UsedSkillIdsThisTurn = new List<string>();
    public bool CanMoveAfterSkillThisTurn = false;
    public TeamClass Team;
    public List<HexClass> HexPathList;
    private const float CameraFacingYawOffsetLimit = 15f;
    private const string DefaultMovementAnimationState = "walk";
    private string movementAnimationOverrideState;

    List<HexClass> hexPath;

    public void SetMovementAnimationOverride(string stateName)
    {
        movementAnimationOverrideState = stateName;
    }

    public void ClearMovementAnimationOverride()
    {
        movementAnimationOverrideState = null;
    }

    public void ClearUsedSkillIdsThisTurn()
    {
        if (UsedSkillIdsThisTurn == null)
        {
            UsedSkillIdsThisTurn = new List<string>();
            return;
        }

        UsedSkillIdsThisTurn.Clear();
    }

    public bool HasUsedSkillIdThisTurn(string skillId)
    {
        return string.IsNullOrEmpty(skillId) == false &&
            UsedSkillIdsThisTurn != null &&
            UsedSkillIdsThisTurn.Contains(skillId);
    }

    public void AddUsedSkillIdThisTurn(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return;
        }

        if (UsedSkillIdsThisTurn == null)
        {
            UsedSkillIdsThisTurn = new List<string>();
        }

        if (UsedSkillIdsThisTurn.Contains(skillId) == false)
        {
            UsedSkillIdsThisTurn.Add(skillId);
        }
    }

    public string GetMovementAnimationState()
    {
        return string.IsNullOrWhiteSpace(movementAnimationOverrideState)
            ? DefaultMovementAnimationState
            : movementAnimationOverrideState;
    }

    public void ApplyTeamVisualFacing()
    {
        SetVisualFacingYaw(teamN ? 0f : 180f);
    }

    public void SetVisualFacingYaw(float baseYaw)
    {
        List<Transform> visualRoots = GetVisualRoots();
        if (visualRoots.Count == 0)
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, GetTeamBiasedYaw(baseYaw), 0f);
        foreach (Transform visualRoot in visualRoots)
        {
            visualRoot.rotation = rotation;
        }
    }

    private List<Transform> GetVisualRoots()
    {
        List<Transform> visualRoots = new List<Transform>();
        Transform root = null;
        if (tosterView != null)
        {
            root = tosterView.transform;
        }
        else if (TosterPrefab != null)
        {
            root = TosterPrefab.transform;
        }

        if (root == null)
        {
            return visualRoots;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.GetComponent<TextMesh>() != null)
            {
                continue;
            }

            Transform visualRoot = GetTopVisualChild(root, renderer.transform);
            if (visualRoot != null && !visualRoots.Contains(visualRoot))
            {
                visualRoots.Add(visualRoot);
            }
        }

        return visualRoots;
    }

    private Transform GetTopVisualChild(Transform root, Transform visual)
    {
        if (root == null || visual == null)
        {
            return null;
        }

        Transform current = visual;
        while (current.parent != null && current.parent != root)
        {
            current = current.parent;
        }

        return current;
    }

    private float GetTeamBiasedYaw(float baseYaw)
    {
        return baseYaw + (teamN ? CameraFacingYawOffsetLimit : -CameraFacingYawOffsetLimit);
    }

    #region Pathing
    public void SetHexPath(HexClass[] hexPath)
    {
        this.hexPath = new List<HexClass>(hexPath);
    }
    public void ClearHexPath()
    {
        this.hexPath = new List<HexClass>();
    }

    internal void TrimHexPathTail(int maxSteps)
    {
        if (hexPath == null || maxSteps <= 0)
        {
            return;
        }

        for (int i = 0; i < maxSteps && hexPath.Count > 0; i++)
        {
            hexPath.RemoveAt(hexPath.Count - 1);
        }
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

    public HexClass[] Pathing2(HexClass celhex)
    {
        HexClass[] p = HPath.HPath.FindPath<HexClass>(Hex.hexMap, this, Hex, celhex, HexClass.CostEstimate, true);
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
        float baseMovesToEnterHex = MovementCostToEnterHex(hex) / (GetMS()!=0 ? GetMS():1);
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
    private IEnumerator WaitForAnimation(Animation animation)
    {
        do
        {
            yield return null;
        } while (animation.isPlaying);
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
            BattleActionResult trapResult = BattleActionAutomaticResultApplier.CreateTrapTriggeredResult(this, hex);
            if (hex.trap != null && hex.trap.NameOfTraps == "Rope_Trap")
            {
                Pathing_func(hex,true);
            }

            BattleActionAutomaticResultApplier.ApplyTrapResult(this, hex, trapResult);
        }
        if(Fire_movement==true && oldHex!=null)
        {
            BattleActionResult fireMovementResult = BattleActionAutomaticResultApplier.CreateFireMovementTrapResult(this, oldHex);
            BattleActionAutomaticResultApplier.ApplyFireMovementTrapResult(this, oldHex, fireMovementResult);
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
                d.Play(GetMovementAnimationState());

            }
        }
        if (this.tosterView != null)
        {
            int tC, tR;
            tC = oldHex.C - Hex.C;
            tR = oldHex.R - Hex.R;
            if (tC == 0 && tR == 1)
            {

                SetVisualFacingYaw(120f);
            }
            if (tC == 0 && tR == -1)
            {

                SetVisualFacingYaw(-60f);
            }
            if (tC == -1 && tR == 1)
            {

                SetVisualFacingYaw(60f);

            }
            if (tC == 1 && tR == -1)
            {

                SetVisualFacingYaw(-120f);

            }
            if (tC == 1 && tR == 0)
            {

                SetVisualFacingYaw(180f);
            }
            if (tC == -1 && tR == 0)
            {

                SetVisualFacingYaw(0f);
            }

        }


        this.Team.HexesUnderTeam.Add(Hex);
    }

    internal double CalculateDamageBetweenTostersWithQ(TosterHexUnit toster, TosterHexUnit ai, int v1, int v2)
    {
        throw new NotImplementedException();
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
                0 <Name>UnitName</Name>
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
        DataMapper.UnitDefinition definition = DataMapper.Instance.FindUnit(name);
        if (definition != null)
        {
            SetStats(
                definition.Name,
                definition.HP,
                definition.Attack,
                definition.Defense,
                definition.Initiative,
                definition.Speed,
                new List<string>(definition.SkillNames),
                definition.DamageMinimum,
                definition.DamageMaximum);
            TosterSpriteName = definition.SpritePath;
        }
    } 
    public void SetTosterPrefab(HexMap h)
    {
        this.TosterPrefab = DataMapper.Instance.LoadUnitPrefab(this.Name);
        ApplyTeamVisualFacing();

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
        ListOfAutocasts = new List<string>(AutocastTurnOrder);


        foreach (string s in skillstrings)
        {
            cooldowns.Add(0);
            if (ListOfAutocasts.Contains(s))
            {
                BattleActionResult result = BattleActionAutomaticResultApplier.CreateAutocastStatusResult(this, s);
                BattleActionAutomaticResultApplier.ApplyAutocastStatusResult(this, result);
            }
        }
    }

    public string GetSkillAnimationState(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || skillstrings == null)
        {
            return null;
        }

        int skillIndex = skillstrings.IndexOf(skillId);
        if (skillIndex < 0)
        {
            return null;
        }

        return "skill" + (skillIndex + 1);
    }

    public static IEnumerator RevealFireTrapAfterDelay(HexClass hex, float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        if (hex == null || !hex.isTraped || hex.trap == null || hex.trap.NameOfTraps != "Fire_Trap")
        {
            yield break;
        }

        hex.trap.ShowTrap();
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

        Debug.Log("Przed redukcją :  " + DMGf);
        Debug.Log("defender.FlatDMGReduce:  " + defender.FlatDMGReduce);
        Debug.Log("attacker.Amount  " + attacker.Amount);
        double dmgft = DMGf;
        dmgft -= defender.FlatDMGReduce * attacker.Amount;
        if (dmgft < DMGf * 0.7f)
        {
            DMGf = DMGf * 0.7f;
        }
        else
        {
            DMGf = dmgft;
        }

        if (DMGf < 0)
        {
            DMGf = 0;
        }
        return Math.Ceiling(DMGf);

    }
    public double ReCalculateDamageBetweenTosters(TosterHexUnit attacker, TosterHexUnit defender, double modifier, int dmgtodo, bool isStackable)
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

        double DMGb = dmgtodo * (isStackable ? attacker.Amount : 1)* (1 + (ADD * M)) * (((100.0 - attacker.SpecialDMGModificator) / 100.0)); ;



        double DMGf = DMGb * (((100.0 - defender.SpecialResistance) / 100.0));
        //  Debug.Log("Toster name: " + attacker.Name + " attacks for: " + Math.Ceiling(DMGf));
        DMGf += attacker.SpecialPUREDMG;
        attacker.SpecialPUREDMG = 0;
        if (defender == attacker.HATED)
        {
            DMGf += DMGf / 2;
        }

        Debug.Log("Przed redukcją :  " + DMGf);
        Debug.Log("defender.FlatDMGReduce:  " + defender.FlatDMGReduce);
        Debug.Log("attacker.Amount  " + attacker.Amount);
        double dmgft = DMGf;
        dmgft -= defender.FlatDMGReduce * attacker.Amount;
        if (dmgft < DMGf * 0.7f)
        {
            DMGf = DMGf * 0.7f;
        }
        else
        {
            DMGf = dmgft;
        }
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
    private void PlayAnimatorState(string stateName)
    {
        if (this.tosterView == null)
        {
            return;
        }

        this.tosterView.PlayAnimatorStateAndReturnToDefault(stateName);
    }

    private void FaceTowards(TosterHexUnit target)
    {
        if (target == null || target.Hex == null || this.Hex == null)
        {
            return;
        }

        int tC = target.Hex.C - this.Hex.C;
        int tR = target.Hex.R - this.Hex.R;

        if (tC == 0 && tR == 1)
        {
            SetVisualFacingYaw(-60f);
        }
        if (tC == 0 && tR == -1)
        {
            SetVisualFacingYaw(120f);
        }
        if (tC == -1 && tR == 1)
        {
            SetVisualFacingYaw(-120f);
        }
        if (tC == 1 && tR == -1)
        {
            SetVisualFacingYaw(60f);
        }
        if (tC == 1 && tR == 0)
        {
            SetVisualFacingYaw(0f);
        }
        if (tC == -1 && tR == 0)
        {
            SetVisualFacingYaw(180f);
        }
    }

    private void PlayCombatAnimation(string stateName, TosterHexUnit target)
    {
        FaceTowards(target);
        PlayWeaponTrailsForCombatAttack(stateName);
        PlayAnimatorState(stateName);
    }

    private IEnumerator PlayAnimatorStateAndWait(string stateName)
    {
        if (this.tosterView == null)
        {
            yield break;
        }

        yield return this.tosterView.PlayAnimatorStateAndWaitForDefault(stateName, CombatAnimationMaxWaitSeconds);
    }

    private IEnumerator PlayDeathAnimationAndWait()
    {
        if (this.tosterView == null)
        {
            yield break;
        }

        yield return this.tosterView.PlayAnimatorStateAndHoldLastFrame("death", CombatAnimationMaxWaitSeconds);
    }

    private IEnumerator PlayCombatAnimationAndWait(string stateName, TosterHexUnit target)
    {
        FaceTowards(target);
        yield return PlayAnimatorStateAndWait(stateName);
    }

    private IEnumerator PlayCombatAnimationUntilHitMoment(string stateName, TosterHexUnit target)
    {
        FaceTowards(target);
        if (this.tosterView == null)
        {
            yield break;
        }

        PlayWeaponTrailsForCombatAttack(stateName);
        yield return this.tosterView.PlayAnimatorStateAndWaitForProgress(stateName, CombatHitAtAttackProgress, CombatAnimationMaxWaitSeconds);
    }

    private void PlayWeaponTrailsForCombatAttack(string stateName)
    {
        if (stateName != "attack" || this.tosterView == null)
        {
            return;
        }

        this.tosterView.TryPlayWeaponTrails(CombatAnimationMaxWaitSeconds);
    }

    public IEnumerator AttackMeSequence(TosterHexUnit t)
    {
        //  double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
        double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        int damage = Convert.ToInt32(dmgdouble);
        SendDamageMsg(t, damage);
        yield return t.PlayCombatAnimationUntilHitMoment("attack", this);
        FrontendResultReveal reveal = this.DealMePUREForFrontendReveal(damage, t, FrontendResultRevealSource.BasicAttack);
        yield return RevealFrontendResult(reveal);
        if (!t.isDead && t.tosterView != null)
        {
            t.tosterView.ResetAnimatorToDefault();
        }

        if (CounterAttackAvaible == true)
        {
            CounterAttackBools();

            // dmgdouble = CalculateDamageBetweenTostersH3(this, t, 1);

            dmgdouble = CalculateDamageBetweenTosters(this, t, 1);
            damage = Convert.ToInt32(dmgdouble);
            t.SendDamageMsg(this, damage);
            yield return this.PlayCombatAnimationUntilHitMoment("attack", t);
            reveal = t.DealMePUREForFrontendReveal(damage, this, FrontendResultRevealSource.Counterattack);
            yield return t.RevealFrontendResult(reveal);
            if (!this.isDead && this.tosterView != null)
            {
                this.tosterView.ResetAnimatorToDefault();
            }
        }
    }

    public IEnumerator PlayBasicAttackRevealSequence(TosterHexUnit attacker, int damage, FrontendResultRevealSource sourceType)
    {
        if (attacker != null)
        {
            yield return attacker.PlayCombatAnimationUntilHitMoment("attack", this);
        }

        FrontendResultReveal reveal = DealMePUREForFrontendReveal(damage, attacker, sourceType);
        yield return RevealFrontendResult(reveal);

        if (attacker != null && !attacker.isDead && attacker.tosterView != null)
        {
            attacker.tosterView.ResetAnimatorToDefault();
        }
    }

    public void AttackMe(TosterHexUnit t)
    {
        //  double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
        double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        t.PlayCombatAnimation("attack", this);
        int damage = Convert.ToInt32(dmgdouble);
        SendDamageMsg(t, damage);
        FrontendResultReveal reveal = this.DealMePUREForFrontendReveal(damage, t, FrontendResultRevealSource.BasicAttack);
        FrontendResultRevealPlayer.Play(reveal);

        if (CounterAttackAvaible == true)
        {
            CounterAttackBools();

            // dmgdouble = CalculateDamageBetweenTostersH3(this, t, 1);

            dmgdouble = CalculateDamageBetweenTosters(this, t, 1);
            this.PlayCombatAnimation("attack", t);
            damage = Convert.ToInt32(dmgdouble);
            t.SendDamageMsg(this, damage);
            reveal = t.DealMePUREForFrontendReveal(damage, this, FrontendResultRevealSource.Counterattack);
            FrontendResultRevealPlayer.Play(reveal);
        }
    }


    public void ShootME(TosterHexUnit t, bool sth)
    {
        int damage = CalculateShootDamageAndSendMessage(t);
        if (sth == true)
        {
            FrontendResultReveal reveal = DealMePUREForFrontendReveal(damage, t, FrontendResultRevealSource.BasicAttack);
            SkillPresentationManager.PlayBasicRangedAttack(t, this, reveal);
        }
        else
        {
            PlayStoneSkinDamageReductionFeedback(damage);
            DealMePURE(damage);
        }
   

    }

    public FrontendResultReveal ShootMEForFrontendReveal(TosterHexUnit t, FrontendResultRevealSource sourceType)
    {
        int damage = CalculateShootDamageAndSendMessage(t);
        return DealMePUREForFrontendReveal(damage, t, sourceType);
    }

    int CalculateShootDamageAndSendMessage(TosterHexUnit t)
    {
        //  double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
        double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        HexClass[] Distance = Pathing(t.Hex, true);

        if (Distance.Length > 6)
        {
            dmgdouble -= dmgdouble / 2;
        }

        int damage = Convert.ToInt32(dmgdouble);
        SendDamageMsg(t, damage);

        return damage;
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
        Quaternion q;
        int damage = Convert.ToInt32(dmgdouble);
        SendDamageMsg(t, damage);
        PlayStoneSkinDamageReductionFeedback(damage);
        DealMePURE(damage);
    
    }

    public FrontendResultReveal DealMeDMGForFrontendReveal(TosterHexUnit t, FrontendResultRevealSource sourceType)
    {
       // double dmgdouble = CalculateDamageBetweenTostersH3(t, this, 1);//h3
                                                                       double dmgdouble = CalculateDamageBetweenTosters(t, this, 1);
        int damage = Convert.ToInt32(dmgdouble);
        SendDamageMsg(t, damage);
        return DealMePUREForFrontendReveal(damage, t, sourceType);
    
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
        return DealMePURE(i, true);
    }

    public FrontendResultReveal DealMePUREForFrontendReveal(int i, TosterHexUnit source, FrontendResultRevealSource sourceType)
    {
        TosterView targetView = tosterView;
        bool damageWasReduced = i > 0 && FlatDMGReduce > 0;
        bool survived = DealMePURE(i, false);
        return new FrontendResultReveal(sourceType, source, this, targetView, i, survived, damageWasReduced);
    }

    void PlayStoneSkinDamageReductionFeedback(int finalDamage)
    {
        if (finalDamage <= 0 || FlatDMGReduce <= 0)
        {
            return;
        }

        SkillPresentationManager.PlayImpact("Stone_Skin", this, this, Hex);
    }

    public FrontendResultReveal BuildHealFrontendReveal(TosterHexUnit source, FrontendResultRevealSource sourceType, FrontendTargetReaction targetReaction = FrontendTargetReaction.Hit)
    {
        return new FrontendResultReveal(sourceType, FrontendResultRevealKind.Heal, source, this, tosterView, 0, true, false, targetReaction);
    }

    public FrontendResultReveal BuildStatusFrontendReveal(TosterHexUnit source, FrontendResultRevealSource sourceType, FrontendTargetReaction targetReaction = FrontendTargetReaction.Hit)
    {
        return new FrontendResultReveal(sourceType, FrontendResultRevealKind.Status, source, this, tosterView, 0, true, false, targetReaction);
    }

    string GetAnimatorStateForTargetReaction(FrontendTargetReaction targetReaction)
    {
        switch (targetReaction)
        {
            case FrontendTargetReaction.Buff:
                return "buff";
            case FrontendTargetReaction.Debuff:
                return "debuff";
            case FrontendTargetReaction.Hit:
                return "hit";
            default:
                return null;
        }
    }

    public IEnumerator RevealFrontendResult(FrontendResultReveal reveal)
    {
        if (reveal == null || reveal.TargetUnit != this || !reveal.ShouldReveal)
        {
            yield break;
        }

        if (reveal.DamageWasReduced)
        {
            PlayStoneSkinDamageReductionFeedback(reveal.Damage);
        }

        if (reveal.ResultKind == FrontendResultRevealKind.Damage && !reveal.TargetSurvived)
        {
            yield return PlayDeathAnimationAndWait();
        }
        else
        {
            string stateName = GetAnimatorStateForTargetReaction(reveal.TargetReaction);
            if (!string.IsNullOrEmpty(stateName))
            {
                yield return PlayAnimatorStateAndWait(stateName);
            }
        }
    }

    public bool DealMePURE(int i, bool playHitAnimation)
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
        SendUnitLossMsg(tempamout - Amount);
        if (i > 0 && playHitAnimation)
        {
            PlayAnimatorState(Amount < 1 ? "death" : "hit");
        }
        if (Amount < 1)
        {
            Died(playHitAnimation);
            return false;
        }
        else { SetTextAmount(); return true; }
    }
    public void DealMeDMGDef(int i, TosterHexUnit t, bool isStackable)
    {

        Debug.LogError(i);
        i =Convert.ToInt32(ReCalculateDamageBetweenTosters(t,this,1,i, isStackable));
        PlayStoneSkinDamageReductionFeedback(i);
        Quaternion q;
        SendDamageMsg(t, Convert.ToInt32(i));
        this.DealMePURE(Convert.ToInt32(i));

    }

    public FrontendResultReveal DealMeDMGDefForFrontendReveal(int i, TosterHexUnit t, bool isStackable, FrontendResultRevealSource sourceType)
    {

        Debug.LogError(i);
        i =Convert.ToInt32(ReCalculateDamageBetweenTosters(t,this,1,i, isStackable));
        SendDamageMsg(t, Convert.ToInt32(i));
        return this.DealMePUREForFrontendReveal(Convert.ToInt32(i), t, sourceType);

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
        Died(true);
    }

    void Died(bool applyImmediateVisual)
    {
        if (tosterView != null)
        {
            TextMesh amountText = tosterView.gameObject.GetComponentInChildren<TextMesh>();
            if (amountText != null)
            {
                amountText.text = "";
            }
        }

        isDead = true;
        Moved = true;
        Team.HexesUnderTeam.Remove(this.Hex);
        Hex.RemoveToster(this);
        //tosterView.Destroy();
        if (applyImmediateVisual && tosterView != null)
        {
            tosterView.gameObject.transform.localScale = new Vector3(1f, 0.1f, 1f);
        }
    }


    public void CheckSpells()
    {
        List<SpellOverTime> spellSnapshot = new List<SpellOverTime>(SpellsGoingOn);
        foreach (SpellOverTime s in spellSnapshot)
        {
            if (s == null || SpellsGoingOn.Contains(s) == false)
            {
                continue;
            }

            Debug.LogError(s.Time);
            Debug.LogError(s.me.Name);
            BattleActionResult result = BattleActionAutomaticResultApplier.CreateStatusTickResult(this, s);
            BattleActionAutomaticResultApplier.ApplyStatusTickResult(this, s, result);
        }

        TickCooldowns();
        QueueAutocastsForNextTurn();

    }

    public IEnumerator CheckSpellsSequence()
    {
        List<SpellOverTime> spellSnapshot = new List<SpellOverTime>(SpellsGoingOn);
        for (int i = 0; i < spellSnapshot.Count; i++)
        {
            SpellOverTime s = spellSnapshot[i];
            if (s == null || SpellsGoingOn.Contains(s) == false)
            {
                continue;
            }

            if (CanContinuePassiveSequence() == false)
            {
                yield break;
            }

            if (ResolveNewTurnSpell(s) == false)
            {
                yield break;
            }

            yield return SkillPresentationManager.WaitForBlockingPresentation(PassiveResolveMaxWaitSeconds);

            if (CanContinuePassiveSequence() == false)
            {
                yield break;
            }
        }

        FinishNewTurnSpellProcessing();
    }

    public bool ResolveNewTurnSpell(SpellOverTime s, bool requireAlive = true)
    {
        if (s == null || SpellsGoingOn.Contains(s) == false || (requireAlive && CanContinuePassiveSequence() == false))
        {
            return false;
        }

        Debug.LogError(s.Time);
        Debug.LogError(s.me.Name);
        BattleActionResult result = BattleActionAutomaticResultApplier.CreateStatusTickResult(this, s);
        BattleActionAutomaticResultApplier.ApplyStatusTickResult(this, s, result);

        return true;
    }

    public void FinishNewTurnSpellProcessing()
    {
        TickCooldowns();
        if (CanContinuePassiveSequence() == false)
        {
            return;
        }

        QueueAutocastsForNextTurn();
    }

    bool CanContinuePassiveSequence()
    {
        return isDead == false && Amount > 0;
    }

    void TickCooldowns()
    {
        //int i = 0;
        for (int i=0; i<cooldowns.Count;i++)
        {
            if (cooldowns[i] > 0)
            {
                cooldowns[i]--;
            }
   
        }
    }

    void QueueAutocastsForNextTurn()
    {
        foreach (string s in skillstrings)
        {
            if (ListOfAutocasts.Contains(s))
            {
                BattleActionResult result = BattleActionAutomaticResultApplier.CreateAutocastStatusResult(this, s);
                BattleActionAutomaticResultApplier.ApplyAutocastStatusResult(this, result);
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

    public void SendMsg(string s)
    {
        Chat.chat.SendUnitTextMessage(this, s);
    }

    void SendDamageMsg(TosterHexUnit attacker, int damage)
    {
        string attackerName = attacker != null ? attacker.Name : "";
        TextToSend = attackerName + " zadał " + damage + " obrażeń " + this.Name;
        Chat.chat.SendDamageMessage(attacker, damage, this);
    }

    void SendUnitLossMsg(int amountLost)
    {
        TextToSend = this.Name + " stracił " + amountLost + " jednostek";
        Chat.chat.SendUnitLossMessage(this, amountLost);
    }

    internal void SendTrapTriggeredMsg(string trapName, TosterHexUnit trapOwner)
    {
        TextToSend = this.Name + " wszedł w " + trapName;
        Chat.chat.SendTrapTriggeredMessage(this, trapName, trapOwner);
    }
}
