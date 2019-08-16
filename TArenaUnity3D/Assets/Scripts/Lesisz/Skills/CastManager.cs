﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CastManager : MonoBehaviour
{
    public MouseControler mouseControler;
    int kochamizabelke = 0;
    public bool RangeSelectingenemy = false;
    public bool selectingfriend = false;
    public bool unselectaround = false;
    public bool isAoE = false;
    public bool isInProgress = false;
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
        selectingfriend = false;
        unselectaround = false;
        isAoE = false;
        aoeradius = 0; MouseControler.SkillState = false;
    }


    #region Skill1
    public void Skill1()
    {
        Debug.LogError("działam");

        if (kochamizabelke < 2 && mouseControler.getHexUnderMouse() != mouseControler.getSelectedToster().Hex && !mouseControler.getSelectedToster().Team.HexesUnderTeam.Contains(mouseControler.getHexUnderMouse()) && mouseControler.getHexUnderMouse().Tosters.Count > 0 && mouseControler.getHexUnderMouse().Tosters.Count > 0)
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


    #region Skill2
    public void Skill2() //aoefireball
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
        isAoE = true;

    }
    #endregion


    #region Skill3
    public void Skill3() //heal
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
        selectingfriend = true;

    }
    #endregion


    #region FRACTION STRUCUTRE
    /*
      
    /////////*** FRACTION_NAME *** //////////// 

        #region Fraction_Name

            #region Unit_Name


                    #region Unit_Name_Skill_1

                    #endregion

                    #region Unit_Name_Skill_2

                    #endregion


                    #region Unit_Name_Skill_3

                    #endregion


                    #region Unit_Name_Skill_4

                    #endregion

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
