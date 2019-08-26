using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CastManager : MonoBehaviour
{
    public MouseControler mouseControler;
    int kochamizabelke = 0;
  public  int cooldown = 0;
    TosterHexUnit tempToster;
    public bool RangeSelectingenemy = false;
    public bool Rangeselectingfriend = false;
    public bool unselectaround = false;
    public bool RangeisAoE = false;
    public bool MeleeisAoE = false;
    public bool isInProgress = false;
    public bool SelfCast = false;
    public bool EndAfter = false;
    public bool Global = false;
    public bool SingleTarget = false;
    public bool isTurn = false;
    public int aoeradius = 0;

    void Start()
    {

    }
    public void startSpell(string spellID)
    {
        Type type = this.GetType();
        MethodInfo method = type.GetMethod(spellID);
        method.Invoke(this, null);

    }
    public void getMode(string spellID)
    {
        Type type = this.GetType();
        MethodInfo method = type.GetMethod(spellID + "M");
        method.Invoke(this, null);
    }

    public void SetFalse()
    {
        RangeSelectingenemy = false;
        Rangeselectingfriend = false;
        unselectaround = false;
        RangeisAoE = false;
        MeleeisAoE = false;
        SelfCast = false;
        aoeradius = 0;
        Global = false;
        SingleTarget = false;
    MouseControler.SkillState = false;
        tempToster = null;
        cooldown = 0;
    }
    public HexClass getHexUM()
    {
      return mouseControler.getHexUnderMouse();
    }
    public TosterHexUnit SelectedT()
    {
        return mouseControler.getSelectedToster();
    }


    public int[] getHexUnderMouseCoordinates()
    {
        int x = getHexUM().C;
        int y = getHexUM().R;
        int[] coords = { x, y };
        return coords;
    }

    public HexClass getHexNextToMouse(int x, int y)
    {

        int[] test = getHexUnderMouseCoordinates();
        return getHexUM().hexMap.GetHexAt(test[0] + x, test[1] + y);

    }

    #region Skill1 - Triple shot 
    public void Skill1()
    {
        Debug.LogError("działam");

        if (kochamizabelke < 2 && getHexUM() != SelectedT().Hex && !SelectedT().Team.HexesUnderTeam.Contains(getHexUM()) && getHexUM().Tosters.Count > 0)
        {
            isInProgress = true;
            TosterHexUnit trgt = getHexUM().Tosters[0];
            trgt.DealMePURE(100);
            Debug.Log("Zaatakowalem: " + trgt.Name);
            kochamizabelke++;
        }

        else if (kochamizabelke == 2)
        { kochamizabelke = 0; SetFalse(); }

        else
        {
            Debug.Log("Nie ma tosta na tym polu");
            //yield return null;
        }


    }


    public void Skill1M()
    {
        unselectaround = true;
        RangeSelectingenemy = true;
    }
    #endregion

    #region Skill2 - aoe fireball
    public void Skill2()
    {
        Debug.LogError("działam");
        List<HexClass> hexarea = new List<HexClass>(getHexUM().hexMap.GetHexesWithinRadiusOf(getHexUM(), aoeradius));
        foreach (HexClass t in hexarea)
        {
            if (t != null)
                if (t.Tosters.Count > 0)
                {
                    t.Tosters[0].DealMePURE(100);
                }
        }

        SetFalse();




    }

    public void Skill2M()
    {
        unselectaround = true;
        aoeradius = 2;
        RangeisAoE = true;

    }
    #endregion

    #region Skill3  - Heal
    public void Skill3()
    {
        if (SelectedT().Team.HexesUnderTeam.Contains(getHexUM()) && getHexUM().Tosters.Count > 0)
        {
            TosterHexUnit trgt = getHexUM().Tosters[0];
            trgt.HealMe(25);

            SetFalse();

        }

    }

    public void Skill3M()
    {
        unselectaround = true;
        Rangeselectingfriend = true;

    }
    #endregion

    #region Teleport Other Toster
   
    public void TeleportOT() //Taunt
    {

//        Debug.LogError(getHexUM().Tosters[0].Name);
        if (getHexUM().Tosters.Count > 0 && Global == false)
        {
            tempToster = getHexUM().Tosters[0];
    
            Global = true;
            SingleTarget = true;
            RangeSelectingenemy = false;
            Rangeselectingfriend = false;
            mouseControler.CastSkillOnlyBooleans();
        }
        else
        if (Global == true && SingleTarget == true && tempToster != null)
        {
            if (getHexUM() != tempToster.Hex && getHexUM().Tosters.Count == 0)
            {
      
                HexClass hextomove = getHexUM();
                if (hextomove.Highlight == true)
                {
                    tempToster.TeleportToHex(hextomove);//SetHex(hextomove);

                    SetFalse();
                }
            }
        }
    }

    public void TeleportOTM()
    {
        unselectaround = true;
        RangeSelectingenemy = true;
        Rangeselectingfriend = true;
        isTurn = true;
        tempToster = new TosterHexUnit();
    }



    #endregion

    // SelectedT().Team.HexesUnderTeam.Contains(getHexUT())
    // getSelectedToster().Hex
    // getHexUnderMouse()

    #region Barbarian skills:


    #region Topornik skills (TIER III)

    #region Topornik_Skill1 - Zadaje 1 dmg per unit wszystkim dookoła

    public void Topornik_Skill1()
    {
        List<HexClass> hexarea = new List<HexClass>(SelectedT().Hex.hexMap.GetHexesWithinRadiusOf(SelectedT().Hex, aoeradius));
        foreach (HexClass t in hexarea)
        {

            if (t != null)
            {
                if (t.Tosters.Count > 0)
                {

                    if (t.Tosters.Count > 0 && !t.Tosters.Contains(SelectedT()) && t.Tosters[0].Team != SelectedT().Team)
                    {
                        t.Tosters[0].DealMePURE(100);
                    }

                }
            }
            else { Debug.Log("No Tosters Hit"); }
        }
        SetFalse();
    }

    public void Topornik_Skill1M()
    {
        isTurn = true;
        unselectaround = true;
        aoeradius = 2;
        MeleeisAoE = true;
    }

    #endregion

    #region Topornik_Skill2 - Zabiera 10% swojego całkowitego HP, zadaje +80% dmg (do konca tury)

    public void Topornik_Skill2()
    {
        TosterHexUnit trgt = SelectedT();
        double dmg = Convert.ToDouble(trgt.GetHP()) * trgt.Amount * 0.1;

        trgt.DealMePURE(Convert.ToInt16(dmg));
        trgt.AddNewTimeSpell(2, null, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, "Topornik_Skill2", true);
        mouseControler.SetCD();
        SetFalse();
    }

    public void Topornik_Skill2M()
    {
        isTurn =  false;
        cooldown = 2;
        unselectaround = true;
        SelfCast = true;
        Debug.Log("Pain... drives me!");
    }
    #endregion

    #region Topornik_Skill3 - 10% otrzymanych obrażeń przechodzi na koniec następnej tury


    public void Topornik_Skill3()
    {
        TosterHexUnit trgt = SelectedT();
        trgt.AddNewTimeSpell(1, null, 0, 0, 0, 0, 0, 0, 0, 0, 10,0, 0, "Topornik_Skill3", true);
        //   trgt.Def++;
        SetFalse();
    }

    public void Topornik_Skill3M()
    {
        isTurn = false;
        unselectaround = true;
        SelfCast = true;
        Debug.Log("Huh! I can hold it.");
    }

    #endregion


    #endregion

    #region Rzutnik


    #region Rzutnik_Skill1

    public Array SelectMultipleEnemy(int x)
    {
        int i = 0;
        TosterHexUnit[] enemies = new TosterHexUnit[x];

        while (i < x)
        {
            if (i < 2 && getHexUM() != SelectedT().Hex && !SelectedT().Team.HexesUnderTeam.Contains(getHexUM()) && getHexUM().Tosters.Count > 0)
            {
                isInProgress = true;
                enemies[i] = getHexUM().Tosters[0];
                Debug.Log("Wybrałem: " + enemies[i].Name);
                i++;
            }
        }
        return enemies;
    }



    TosterHexUnit[] Rzutnik_Skill1_trgt = new TosterHexUnit[2];
    short Rzutnik_Skill1_Counter = 0;

    public void Rzutnik_Skill1()
    {

        if (Rzutnik_Skill1_Counter < 2 && getHexUM() != SelectedT().Hex && !SelectedT().Team.HexesUnderTeam.Contains(getHexUM()) && getHexUM().Tosters.Count > 0)
        {
            isInProgress = true;
            Rzutnik_Skill1_trgt[Rzutnik_Skill1_Counter] = getHexUM().Tosters[0];
            Debug.Log("Wybrałem: " + Rzutnik_Skill1_trgt[Rzutnik_Skill1_Counter].Name);
            Rzutnik_Skill1_Counter++;
        }


        else if (Rzutnik_Skill1_Counter == 2)
        {
            //     SelectedT().AddNewTimeSpell(1, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, -40, "Rzutnik_skill1", true);
            SelectedT().SpecialDMGModificator += 40;
            Rzutnik_Skill1_trgt[0].DealMePURE(Convert.ToInt32(SelectedT().CalculateDamageBetweenTosters(SelectedT(), Rzutnik_Skill1_trgt[0], 1)));
            Rzutnik_Skill1_trgt[1].DealMePURE(Convert.ToInt32(SelectedT().CalculateDamageBetweenTosters(SelectedT(), Rzutnik_Skill1_trgt[1], 1)));
            SelectedT().SpecialDMGModificator -= 40;
            Rzutnik_Skill1_Counter = 0; SetFalse(); }

        else
        {
            Debug.Log("Nie ma tosta na tym polu");
            //yield return null;
        }
    }

    public void Rzutnik_Skill1M()
    {
        unselectaround = true;
        RangeSelectingenemy = true;
    }

    #endregion

    #region Rzutnik_Skill2

    public void Rzutnik_Skill2()
    {
        TosterHexUnit trgt = SelectedT();
        double dmg = Convert.ToDouble(trgt.HP) * 0.1;
        trgt.DealMePURE(Convert.ToInt16(dmg));
            trgt.AddNewTimeSpell(2, trgt, 0, 25, -10, 0, 0, 0, 0, 0, 0, 0,8, "Rzutnik_Skill2", true);
        mouseControler.SetCD();
        SetFalse();
    }

    public void Rzutnik_Skill2M()
    {
        isTurn = false;
        cooldown = 3;
        unselectaround = true;
        SelfCast = true;
        Debug.Log("Pain... drives me!");
    }

    #endregion

    #region Rzutnik_Skill3

    public void Rzutnik_Skill3()
    {
        TosterHexUnit trgt = SelectedT();
        trgt.AddNewTimeSpell(2, trgt, 0, 0, 25, 0, 0, 0, 0, 0, 0, 0, 0, "Rzutnik_Skill3", true);
        SetFalse();
    }

    public void Rzutnik_Skill3M()
    {
        isTurn = false;
        unselectaround = true;
        SelfCast = true;
        Debug.Log("Huh! I can hold it.");
    }

    #endregion

    #region Rzutnik_Skill4

    public void Rzutnik_Skill4()
    {

    }

    public void Rzutnik_Skill4M()
    {

    }

    #endregion

    #endregion

    #region Tank


    #region Tank_Skill1 - Tauntuje wszystkich w odległości 1 oraz dodaje +2 CounterAttacks do końca następnej tury
    public void Tank_Skill1() //Taunt
    {
        SelectedT().AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, "Tank_Skill1", false);
        List<HexClass> hexarea = new List<HexClass>(SelectedT().Hex.hexMap.GetHexesWithinRadiusOf(SelectedT().Hex, aoeradius));
        foreach (HexClass t in hexarea)
        {

            if (t != null)
            {
                if (t.Tosters.Count > 0)
                {

                    if (t.Tosters.Count > 0 && !t.Tosters.Contains(SelectedT()) )
                    {
                        t.Tosters[0].AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0,  0, 0, 0, 0,0, 0, "Taunt", false);
                    }

                }
            }
            else { Debug.Log("No Tosters Hit"); }
        }
        SetFalse();
    }

    public void Tank_Skill1M()
    {
        isTurn = true;
        unselectaround = true;
        aoeradius = 1;
        MeleeisAoE = true;
    }



    #endregion


    #region Tank_Skill2- Teleport XD
    public void Tank_Skill2() //teleport
    {
        if (getHexUM() != SelectedT().Hex && getHexUM().Tosters.Count == 0)
        {

            HexClass hextomove = getHexUM();
            if (hextomove.Highlight == true)
            {
                SelectedT().TeleportToHex(hextomove);//SetHex(hextomove);

                SetFalse();
            }
        }
    }

    public void Tank_Skill2M()
    {
        isTurn = true;
        Global = true;
        SingleTarget = true;
    }



    #endregion 
    #endregion

    #endregion
    #region UNIT STRUCUTRE
    /*
      
    /////////*** FRACTION_NAME *** //////////// 


            #region Unit_Name


                    #region Unit_Name_Skill1
    
                        public void Unit_Name_Skill1()
                        {
                        
                        }

                        public void Unit_Name_Skill1M()
                        {

                        }
    
                    #endregion

                 #region Unit_Name_Skill2
    
                        public void Unit_Name_Skill2()
                        {
                        
                        }

                        public void Unit_Name_Skill2M()
                        {

                        }
    
                    #endregion

                     #region Unit_Name_Skill3
    
                        public void Unit_Name_Skill3()
                        {
                        
                        }

                        public void Unit_Name_Skill3M()
                        {

                        }
    
                    #endregion

                     #region Unit_Name_Skill4
    
                        public void Unit_Name_Skill4()
                        {
                        
                        }

                        public void Unit_Name_Skill4M()
                        {

                        }
    
                    #endregion

            #endregion

    */
    #endregion

    /////////*** Barbarians *** //////////// 
    #region Barbarians

    #region Szaman


    #region Szaman_Skill_1

    #endregion

    #region Szaman_Skill_2

    #endregion


    #region Szaman_Skill_3

    #endregion


    #region Szaman_Skill_4

    #endregion

    #endregion

    #region Rzutnik



    // 10 % otrzymanych obrażeń przechodzi na koniec następnej tury
    #region Rzutnik_Skill_1


    #endregion

    #region Rzutnik_Skill_2
    // Zabiera 10 % swojego całkowitego HP, zadaje +8% dmg

    #endregion


    #region Rzutnik_Skill_3
    // Rzut w dwóch przeciwników na raz(po 40% dmg)

    #endregion


    #region Rzutnik_Skill_4

    #endregion

    #endregion

    #endregion




}
