using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CastManager : MonoBehaviour
{
    public MouseControler mouseControler;
    int kochamizabelke = 0;

    public bool RangeSelectingenemy = false;
    public bool Rangeselectingfriend = false;
    public bool unselectaround = false;
    public bool RangeisAoE = false;
    public bool MeleeisAoE = false;
    public bool isInProgress = false;
    public bool SelfCast = false;
    public bool EndAfter = false;
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

        MouseControler.SkillState = false;
    }


    public int[] getHexUnderMouseCoordinates()
    {
        int x = mouseControler.getHexUnderMouse().C;
        int y = mouseControler.getHexUnderMouse().R;
        int[] coords = { x, y };
        return coords;
    }

    public HexClass getHexNextToMouse(int x, int y)
    {

        int[] test = getHexUnderMouseCoordinates();
        return mouseControler.getHexUnderMouse().hexMap.GetHexAt(test[0] + x, test[1] + y);

    }

    #region Skill1 - Triple shot 
    public void Skill1()
    {
        Debug.LogError("działam");

        if (kochamizabelke < 2 && mouseControler.getHexUnderMouse() != mouseControler.getSelectedToster().Hex && !mouseControler.getSelectedToster().Team.HexesUnderTeam.Contains(mouseControler.getHexUnderMouse()) && mouseControler.getHexUnderMouse().Tosters.Count > 0)
        {
            isInProgress = true;
            TosterHexUnit trgt = mouseControler.getHexUnderMouse().Tosters[0];
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
        List<HexClass> hexarea = new List<HexClass>(mouseControler.getHexUnderMouse().hexMap.GetHexesWithinRadiusOf(mouseControler.getHexUnderMouse(), aoeradius));
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
        if (mouseControler.getSelectedToster().Team.HexesUnderTeam.Contains(mouseControler.getHexUnderMouse()) && mouseControler.getHexUnderMouse().Tosters.Count > 0)
        {
            TosterHexUnit trgt = mouseControler.getHexUnderMouse().Tosters[0];
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


    // mouseControler.getSelectedToster().Team.HexesUnderTeam.Contains(mouseControler.getHexUnderMouse())
    // getSelectedToster().Hex
    // getHexUnderMouse()

    #region Barbarian skills:


    #region Topornik skills (TIER III)

    #region Topornik_Skill1 - Zadaje 1 dmg per unit wszystkim dookoła









    public void Topornik_Skill1()
    {
        List<HexClass> hexarea = new List<HexClass>(mouseControler.getSelectedToster().Hex.hexMap.GetHexesWithinRadiusOf(mouseControler.getSelectedToster().Hex, aoeradius));
        foreach (HexClass t in hexarea)
        {

            if (t != null)
            {
                if (t.Tosters.Count > 0)
                {

                    if (t.Tosters.Count > 0 && !t.Tosters.Contains(mouseControler.getSelectedToster()) && t.Tosters[0].Team != mouseControler.getSelectedToster().Team)
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
        unselectaround = true;
        aoeradius = 2;
        MeleeisAoE = true;
    }

    #endregion

    #region Topornik_Skill2 - Zabiera 10% swojego całkowitego HP, zadaje +8% dmg


    public void Topornik_Skill2()
    {
        TosterHexUnit trgt = mouseControler.getSelectedToster();
        double dmg = Convert.ToDouble(trgt.HP) * 0.1;
        trgt.DealMePURE(Convert.ToInt16(dmg));
        trgt.Att++;

        SetFalse();
    }

    public void Topornik_Skill2M()
    {
        unselectaround = true;
        SelfCast = true;
        Debug.Log("Pain... drives me!");
    }
    #endregion

    #region Topornik_Skill3 - 10% otrzymanych obrażeń przechodzi na koniec następnej tury


    public void Topornik_Skill3()
    {
        TosterHexUnit trgt = mouseControler.getSelectedToster();
        trgt.AddNewTimeSpell(1, trgt, 0, 0, 0, 0, 0, 0, 0, 0, 10, "Topornik_Skill3", true);
     //   trgt.Def++;
        SetFalse();
    }

    public void Topornik_Skill3M()
    {
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
            if (i < 2 && mouseControler.getHexUnderMouse() != mouseControler.getSelectedToster().Hex && !mouseControler.getSelectedToster().Team.HexesUnderTeam.Contains(mouseControler.getHexUnderMouse()) && mouseControler.getHexUnderMouse().Tosters.Count > 0)
            {
                isInProgress = true;
                enemies[i] = mouseControler.getHexUnderMouse().Tosters[0];
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
  
        if (Rzutnik_Skill1_Counter < 2 && mouseControler.getHexUnderMouse() != mouseControler.getSelectedToster().Hex && !mouseControler.getSelectedToster().Team.HexesUnderTeam.Contains(mouseControler.getHexUnderMouse()) && mouseControler.getHexUnderMouse().Tosters.Count > 0)
            {
            isInProgress = true;
            Rzutnik_Skill1_trgt[Rzutnik_Skill1_Counter] = mouseControler.getHexUnderMouse().Tosters[0];
              Debug.Log("Wybrałem: " + Rzutnik_Skill1_trgt[Rzutnik_Skill1_Counter].Name);
                Rzutnik_Skill1_Counter++;
            }
        

        else if (Rzutnik_Skill1_Counter == 2)
        {

            Rzutnik_Skill1_trgt[0].DealMePURE(100);
          Rzutnik_Skill1_trgt[1].DealMePURE(100);
          Rzutnik_Skill1_Counter = 0; SetFalse(); }

        else
        {
            Debug.Log("Nie ma tosta na tym polu");
            //yield return null;
        }
    }

    public void Rzutnik_Skill1M()
    {

    }

    #endregion

    #region Rzutnik_Skill2

    public void Rzutnik_Skill2()
    {
        TosterHexUnit trgt = mouseControler.getSelectedToster();
        double dmg = Convert.ToDouble(trgt.HP) * 0.1;
        trgt.DealMePURE(Convert.ToInt16(dmg));
        trgt.Att++;

        SetFalse();
    }

    public void Rzutnik_Skill2M()
    {

        unselectaround = true;
        SelfCast = true;
        Debug.Log("Pain... drives me!");
    }

    #endregion

    #region Rzutnik_Skill3

    public void Rzutnik_Skill3()
    {
        TosterHexUnit trgt = mouseControler.getSelectedToster();
        trgt.Def++;
        SetFalse();
    }

    public void Rzutnik_Skill3M()
    {
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
