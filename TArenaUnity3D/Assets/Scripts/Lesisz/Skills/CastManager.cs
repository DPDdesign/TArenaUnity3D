using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CastManager : MonoBehaviourPunCallbacks
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
    public bool MeleeisAoEOnlyRadius = false; 
    public bool isInProgress = false;
    public bool SelfCast = false;
    public bool EndAfter = false;
    public bool Global = false;
    public bool SingleTarget = false;
    public bool isTurn = false;
    public int aoeradius = 0;
    public int MeleeisAoEbetweenRadiusInt = 0;
    public bool rush = false;
    public bool isAvailable = true;
    public bool SlashTarget = false;
    HexClass hexum;
    void Start()
    {
        mouseControler = FindObjectOfType<MouseControler>();
    }
    public void startSpell(string spellID, HexClass hex)
    {
        hexum = hex;
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
        MeleeisAoEOnlyRadius = false;
        SelfCast = false;
        aoeradius = 0;
        Global = false;
        SingleTarget = false;
        MouseControler.SkillState = false;
        tempToster = null;
        cooldown = 0;
        rush = false;
        isAvailable = true;
        isInProgress = false;
        SlashTarget = false;
    }
    public HexClass getHexUM()
    {
        return hexum;
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
    #region Rusher skills (T1)
    #region Skill 1 - Chope

    public void Chope() // kręci się dookoła i zadaje 40% obrażen wszystkim jednostkom, traci kontratak
    {
        List<HexClass> hexarea = new List<HexClass>(SelectedT().Hex.hexMap.GetHexesWithinRadiusOf(SelectedT().Hex, aoeradius));
        foreach (HexClass t in hexarea)
        {
         
            if (t != null)
            {
                if (t.Tosters.Count > 0)
                {
                    
                    if (t.Tosters.Count > 0 && !t.Tosters.Contains(SelectedT()))
                    {
                       
                        t.Tosters[0].DealMeDMG(SelectedT());
                        
                    }

                }
            }
            else { Debug.Log("No Tosters Hit"); }
        }
        SelectedT().SpecialDMGModificator = 0;
        SelectedT().CounterAttackAvaible = false;
        SelectedT().CounterAttacks = 0;
        SetFalse();
    }
    public void ChopeM()
    {
        isTurn = true;
        unselectaround = true;
        aoeradius = 1;
        MeleeisAoE = true;
    }

    #endregion
    #region Skill 2 - Rush
    public void Rush() //Biegnie przed siebie atakuje pierwszego napotkanego przeciwnika ??? +1 do ataku, traci 8 % jednostek, nie może zostać użyte poniżej 30.
    {
 

        if (getHexUM() != null && getHexUM().Highlight == true)
        {
            if (getHexUM().Tosters.Count > 0)
            {
                SelectedT().Amount = Convert.ToInt32(SelectedT().Amount * 0.92);
                SelectedT().AddNewTimeSpell(1, null, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Rush", true);
                HexClass temp;
                if (SelectedT().teamN ==true)
                {
                    temp = getHexUM().hexMap.GetHexAt(getHexUM().C - 1, getHexUM().R);
                }
                else
                {
                  
                    
                   temp = getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R);
                    
                }
                Debug.Log("this");
           //     mouseControler.photonView.RPC.StartCoroutine(mouseControler.DoMoveAndAttackWithoutCheck(temp, getHexUM().Tosters[0]));
                mouseControler.photonView.RPC("StartCoroutineDoMoveAndAttackWithoutCheck", RpcTarget.All, new object[] { temp.C, temp.R, getHexUM().C, getHexUM().R });
            }
            else
            {

                mouseControler.photonView.RPC("StartCoroutineDoMoveAndAttackWithoutCheck", RpcTarget.All, new object[] { getHexUM().C, getHexUM().R, -5, -5 });
              //  mouseControler.StartCoroutine(mouseControler.DoMoveAndAttackWithoutCheck(getHexUM(), null));
                SetFalse();
            }
        }

    }
    public void RushM()
    {
        if (SelectedT().Amount<30)
        {
            isAvailable = false;
        }
        unselectaround = true;
        rush = true;
    }

    #endregion
    #endregion
    #region Thrower skills (T2)
    #region Range_Stance //TODO: CIELU
    public void Range_Stance()
    {

    }
    public void Range_StanceM()
    {
        if(SelectedT().isRange = !SelectedT().isRange)
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 20;
            SelectedT().SpecialResistance = 20;
        }
        else
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 0;
            SelectedT().SpecialResistance = 0;
        }
        isTurn = false;
        SetFalse();
        
    }
    #endregion
    #region Double_Throw 
    public void Double_Throw()
    {
        if (Rzutnik_Skill1_Counter < 2 && getHexUM() != SelectedT().Hex && !SelectedT().Team.HexesUnderTeam.Contains(getHexUM()) && getHexUM().Tosters.Count > 0)
        {
            isInProgress = true;
            Rzutnik_Skill1_trgt[Rzutnik_Skill1_Counter] = getHexUM().Tosters[0];
            Debug.Log("Wybrałem: " + Rzutnik_Skill1_trgt[Rzutnik_Skill1_Counter].Name);
            Rzutnik_Skill1_Counter++;
        }


        if (Rzutnik_Skill1_Counter == 2)
        {
            //     SelectedT().AddNewTimeSpell(1, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, -40, "Rzutnik_skill1", true);
            SelectedT().SpecialDMGModificator += 60;
            Rzutnik_Skill1_trgt[0].ShootME(SelectedT());
            Rzutnik_Skill1_trgt[1].ShootME(SelectedT());
            SelectedT().SpecialDMGModificator -= 60;
            Rzutnik_Skill1_Counter = 0; SetFalse();
        }

       
    }

    public void Double_ThrowM()
    {
        isTurn = true;
        unselectaround = true;
        RangeSelectingenemy = true;
    }

  
    #endregion

    #region Axe_Rain 
    public void Axe_Rain()
    {
        Debug.LogError("działam");
        List<HexClass> hexarea = new List<HexClass>(getHexUM().hexMap.GetHexesWithinRadiusOf(getHexUM(), aoeradius));

        foreach (HexClass t in hexarea)
        {
            if (t != SelectedT().Hex && t!=null)
            {
                if (t == getHexUM() && t.Tosters.Count>0)
                {
                    SelectedT().SpecialDMGModificator = 20;
                    t.Tosters[0].ShootME(SelectedT());
                    SelectedT().SpecialDMGModificator = 0;
                }
                else
                if (t != null)
                    if (t.Tosters.Count > 0)
                    {
                        SelectedT().SpecialDMGModificator = 70;
                        t.Tosters[0].ShootME(SelectedT());
                        SelectedT().SpecialDMGModificator = 0;
                    }
            }
        }

        SetFalse();
    }
    public void Axe_RainM()
    {
        unselectaround = true;
        aoeradius = 1;
        RangeisAoE = true;
        isTurn = true;
    }
    #endregion
    #endregion
    #region Axeman (T3)
    #region Slash 

    public void Slash()
    {
        if (getHexUM().Highlight==true)
        {
            photonView.RPC("slash", RpcTarget.All, new object[] { });
        }
    }
    [PunRPC]

    public void slash()
    {

        HexClass[] hexarray = getHexUM().hexMap.GetHexesWithinRadiusOf(getHexUM(), 1);
       // SelectedT().SpecialDMGModificator = 60;
        Debug.LogError(SelectedT().Name);
        if (SelectedT().Team == getHexUM().hexMap.Teams[1] && PhotonNetwork.LocalPlayer.IsMasterClient)
        {

        }
        else
                if (SelectedT().Team == getHexUM().hexMap.Teams[0] && !PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            return;
        }
        else
        foreach (HexClass h in hexarray)
        {

            if (h.Highlight == true)
            {
                if (h.Tosters.Count > 0 && h.Tosters[0] != SelectedT())
                {
                    mouseControler.photonView.RPC("JustDmg", RpcTarget.All, new object[] { h.C, h.R, 60 });


                }
            }
        }
     //   SelectedT().SpecialDMGModificator = 0;
        SelectedT().CounterAttackAvaible = false;
        SelectedT().CounterAttacks = 0;
        SetFalse();
    }
    public void SlashM()
    {
        unselectaround = true;
        
        SlashTarget = true;
        isTurn = true;

    }
    #endregion
    #region Hate 

    public void Hate() 
    {
        if (getHexUM().Highlight==true)
        {
            SelectedT().AddNewTimeSpell(2, getHexUM().Tosters[0], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0, "Hate", false);
            getHexUM().Tosters[0].AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0, "Hate", false);
            mouseControler.SetCD();
            SetFalse();
        }
    }
    public void HateM()
    {
        isTurn = false;
        cooldown = 2;
        unselectaround = true;
        RangeSelectingenemy = true;
    }
    #endregion
    #region Cold_Blood

    public void Cold_Blood() //PASSIVE = AUTOCAST
    {

    }
    public void Cold_BloodM()
    {
        SetFalse();
    }
    #endregion
    #endregion
    #region Heavy Hitter (T4)
    #region Insult

    public void Insult()
    {
        if (SelectedT().Team == getHexUM().hexMap.Teams[0])
        {
            foreach (TosterHexUnit tost in getHexUM().hexMap.Teams[1].Tosters)
            {
                
                tost.AddNewTimeSpell(2, tost, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0,0, "Insult", false);
            }
        }
        else
        {
            foreach (TosterHexUnit tost in getHexUM().hexMap.Teams[0].Tosters)
            {

                tost.AddNewTimeSpell(2, tost, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0,0, "Insult", false);
            }
        }
        mouseControler.SetCD();
        SetFalse();
    }
    public void InsultM()
    {
        cooldown = 3;
        isTurn = false;
        unselectaround = true;
        RangeSelectingenemy = true;
    }
    #endregion
    #region Rage  //Cielu

    public void Rage()
    {
        if (getHexUM() == SelectedT().Hex)
        {
            SelectedT().AddNewTimeSpell(2, SelectedT(), 0, SelectedT().GetDef(), -SelectedT().GetDef(), 0, 0, 0, 0, 0, 0, 0, 0, 0, "Rage", false);
            mouseControler.SetCD();
            SetFalse();
        }
    }
    public void RageM()
    {
        cooldown = 3;
        isTurn = false;
        unselectaround = true;
        SelfCast = true;
    }
    #endregion
    #region Massochism 

    public void Massochism() //PASSIVE = AUTOCAST
    {
    //  SelectedT().AddNewTimeSpell(1, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Massochism", false);
    }
    public void MassochismM()
    {
        SetFalse();
    }
    #endregion
    #endregion
    #region StareSkille/Jednostki
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
        trgt.AddNewTimeSpell(2, null, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80,0, "Topornik_Skill2", true);
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
        trgt.AddNewTimeSpell(1, null, 0, 0, 0, 0, 0, 0, 0, 0, 10,0, 0,0, "Topornik_Skill3", true);
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
            trgt.AddNewTimeSpell(2, trgt, 0, 25, -10, 0, 0, 0, 0, 0, 0, 0,8,0, "Rzutnik_Skill2", true);
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
        trgt.AddNewTimeSpell(2, trgt, 0, 0, 25, 0, 0, 0, 0, 0, 0, 0, 0,0, "Rzutnik_Skill3", true);
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
        SelectedT().AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0,0, "Tank_Skill1", false);
        List<HexClass> hexarea = new List<HexClass>(SelectedT().Hex.hexMap.GetHexesWithinRadiusOf(SelectedT().Hex, aoeradius));
        foreach (HexClass t in hexarea)
        {

            if (t != null)
            {
                if (t.Tosters.Count > 0)
                {

                    if (t.Tosters.Count > 0 && !t.Tosters.Contains(SelectedT()) )
                    {
                        t.Tosters[0].AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0,  0, 0, 0, 0,0, 0,0, "Taunt", false);
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
    #endregion


    #region Lizards skills:
    #region Trapper  skills (T1) Trapy...
    #region Skill 1 - Range_Stance

    public void Range_StanceT() 
    {
     
    }
    public void Range_StanceTM()
    {
        if (SelectedT().isRange = !SelectedT().isRange)
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 20;
            SelectedT().SpecialResistance = 20;
        }
        else
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 0;
            SelectedT().SpecialResistance = 0;
        }
        isTurn = false;
        SetFalse();
    }

    #endregion
    #region Skill 2 - Spike_trap
    public void Spike_Trap() 
    {
        if (getHexUM()!=null)
        {
            getHexUM().AddTrap("Spike_Trap");
            mouseControler.SetCD();
            SetFalse();
            
        }


    }
    public void Spike_TrapM()
    {
        unselectaround = true;
        Global = true;
        SingleTarget = true;
        isTurn = true;
        cooldown = 3;
    }

    #endregion
    #region Skill 3 - Rope_trap
    public void Rope_Trap() 
    {

        if (getHexUM() != null)
        {
            getHexUM().AddTrap("Rope_Trap");
            mouseControler.SetCD();
            SetFalse();

        }
    }
    public void Rope_TrapM()
    {
        unselectaround = true;
        Global = true;
        SingleTarget = true;
        isTurn = true;
        cooldown = 2;
    }

    #endregion
    #endregion
    #region Healer  skills (T2)
    #region Tough_Skin 
    public void Tough_Skin()
    {
        if(getHexUM().Highlight==true)
        {
            getHexUM().Tosters[0].AddNewTimeSpell(2, getHexUM().Tosters[0], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,-20, "Tough_Skin", false);
            mouseControler.SetCD();
            SetFalse();
        }
        
    }
    public void Tough_SkinM()
    {
            cooldown = 2;
            unselectaround = true;
            Rangeselectingfriend = true;
            SelfCast = true;
        isTurn = true;
    }
    #endregion
    #region Defence_Ritual 
    public void Defence_Ritual()
    {
        if (SelectedT().Team == getHexUM().hexMap.Teams[0])
        {
            foreach (TosterHexUnit tost in getHexUM().hexMap.Teams[0].Tosters)
            {

                tost.AddNewTimeSpell(2, tost, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Insult", false);
            }
        }
        else
        {
            foreach (TosterHexUnit tost in getHexUM().hexMap.Teams[1].Tosters)
            {

                tost.AddNewTimeSpell(2, tost, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Insult", false);
            }
        }
        mouseControler.SetCD();
        SetFalse();
    }
    public void Defence_RitualM()
    {
        cooldown = 3;
        unselectaround = true;
        Rangeselectingfriend = true;
        SelfCast = true;
        isTurn = true;
    }

    #endregion

    #region Cleanse  //TODO: 
    public void Cleanse()
    {
        List<string> SpellsToRemove = new List<string>(new string[] { "Slow", "Insult" });
        if (getHexUM().Tosters.Count > 0 && getHexUM().Highlight == true)
        {
            foreach (string s in SpellsToRemove)
            {
                TimeSpells.SpellOverTime spell = getHexUM().Tosters[0].AskForSpell(s);
              
                if (spell!= null)
                {
                    Debug.Log(spell.nameofspell);
                    getHexUM().Tosters[0].SetOver(spell);
                    SelectedT().AddNewTimeSpell(spell);
                  
                
                }
            }
        }
    }
    public void CleanseM()
    {


        unselectaround = true;
        Rangeselectingfriend = true;
        isTurn = true;
        /*
         *
         * 
         *  może być różnie, narazie dla Ciela
         * 
         */
    }
    #endregion
    #endregion
    #region Specialist  (T3)
    #region Force_Pull  //Cielu





    public void Force_Pull()
    {
        if (getHexUM().Tosters.Count > 0 && MeleeisAoE == false)
        {
            tempToster = getHexUM().Tosters[0];


            SingleTarget = true;
            RangeSelectingenemy = false;
            Rangeselectingfriend = false;
            MeleeisAoE = true;
            aoeradius = 2;
            mouseControler.CastSkillOnlyBooleans();
        }
        else
     if (MeleeisAoE == true && SingleTarget == true && tempToster != null)
        {
            if (getHexUM() != tempToster.Hex && getHexUM().Tosters.Count == 0)
            {

                HexClass hextomove = getHexUM();
                if (hextomove.Highlight == true)
                {
                    mouseControler.photonView.RPC("TeleportToster", RpcTarget.All, new object[] { tempToster.Hex.C, tempToster.Hex.R,hextomove.C,hextomove.R });
                  //  tempToster.TeleportToHex(hextomove);//SetHex(hextomove);

                    SetFalse();
                }
            }
        }
    }
    public void Force_PullM()
    {
        unselectaround = true;
     
        Rangeselectingfriend = true;
        isTurn = true;
        tempToster = new TosterHexUnit();
    }
    #endregion
    #region Stone_Stance 

    public void Stone_Stance()
    {
        SelectedT().AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, -SelectedT().CounterAttacks, 0,-100, "Stone_Stance", false);
        SelectedT().CounterAttackAvaible = false;
        SetFalse();
    }
    public void Stone_StanceM()
    { 
        isTurn = true;
        SelfCast = true;
        unselectaround = true;
    }
    #endregion
    #region Brak_Weny //Lesisz

    public void Brak_Weny()
    {

    }
    public void Brak_WenyM()
    {

    }
    #endregion
    #endregion
    #region Tank  (T4)
    #region Toxic_Fume  

    public void Toxic_Fume()
    {


        SelectedT().AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, -SelectedT().CounterAttacks, 0, 0, "Toxic_Fume", false);
        SelectedT().CounterAttackAvaible = false;
        List<HexClass> hexarea = new List<HexClass>(SelectedT().Hex.hexMap.GetHexesWithinRadiusOf(SelectedT().Hex, aoeradius));
        foreach (HexClass t in hexarea)
        {

            if (t != null)
            {
                if (t.Tosters.Count > 0)
                {

                    if (t.Tosters.Count > 0 && !t.Tosters.Contains(SelectedT())&& t.Tosters[0].Team!=SelectedT().Team)
                    {
                        t.Tosters[0].AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Taunt", false);
                    }

                }
            }
            else { Debug.Log("No Tosters Hit"); }
        }
        SetFalse();
    }
    public void Toxic_FumeM()
    {
        isTurn = true;
        unselectaround = true;
        aoeradius = 1;
        MeleeisAoE = true;
    }
    #endregion
    #region Shapeshift   //Cielu

    public void Shapeshift()
    {
        int temp = SelectedT().MovmentSpeed;
        SelectedT().MovmentSpeed = SelectedT().Initiative;
        SelectedT().Initiative = temp;
        SetFalse();
    }
    public void ShapeshiftM()
    {
        SelfCast = true;
        isTurn = false;
        unselectaround = true;
    }
    #endregion
    #region Long_Lick

    public void Long_Lick()
    {
        if (getHexUM().Tosters.Count>0)
        {
            if (getHexUM().hexMap.GetHexAt((SelectedT().Hex.C + getHexUM().C) / 2, (SelectedT().Hex.R + getHexUM().R) / 2).Tosters.Count == 0)
            {
                getHexUM().Tosters[0].SetHex(getHexUM().hexMap.GetHexAt((SelectedT().Hex.C + getHexUM().C) / 2, (SelectedT().Hex.R + getHexUM().R) / 2));
                SetFalse();
                return;
            }
            HexClass[] hexes = getHexUM().hexMap.GetHexesWithinRadiusOf(SelectedT().Hex, 1);

            foreach(HexClass h in hexes)
            {
                            
                if (h!= null && h.Tosters.Count==0)
                {
                    getHexUM().Tosters[0].SetHex(h);
                    SetFalse();
                    return;
                }

            }
            Debug.Log("All Hexes full!!");
        }
    }
    public void Long_LickM()
    {
        unselectaround = true;
        MeleeisAoEOnlyRadius = true;
        aoeradius = 2;
        isTurn = true;

    }
    #endregion
    #endregion
    #region StareSkille/Jednostki
    #region Topornik skills (TIER III)

    #region Topornik_Skill1 - Zadaje 1 dmg per unit wszystkim dookoła


    #endregion
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
