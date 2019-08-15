using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Skill1 : SkillsDefault
{

 /*   public Skill1()
    {
        this.SkillName = "Skill1";
    }*/

    #region Heal Ally
    //override public void CastSkill()
    //{
    //    if (hexUnderMouse != SelectedToster.Hex && SelectedToster.Team.HexesUnderTeam.Contains(hexUnderMouse)&& hexUnderMouse.Tosters.Count > 0)
    //    {
    //        if (hexUnderMouse.Tosters.Count > 0)
    //        {
    //            TosterHexUnit trgt = hexUnderMouse.Tosters[0];
    //            trgt.HealMe(5);
    //            Debug.Log(trgt.Name);
    //        }
    //    }
    //}
    #endregion


    #region  Triple Shot
    int kochamizabelke = 0;
    /*
    override public void CastSkill()
    {
        MouseControler.SkillState = true;
       
            if (kochamizabelke < 2 && hexUnderMouse != SelectedToster.Hex && !SelectedToster.Team.HexesUnderTeam.Contains(hexUnderMouse) && hexUnderMouse.Tosters.Count > 0 && hexUnderMouse.Tosters.Count > 0)
            {
                        TosterHexUnit trgt = hexUnderMouse.Tosters[0];
                        trgt.AttackMe(SelectedToster);
                        Debug.Log("Zaatakowalem: " + trgt.Name);
            kochamizabelke++;
            }

            else if (kochamizabelke == 2)
        { kochamizabelke = 0;  MouseControler.SkillState = false; }

            else { Debug.Log("Nie ma tosta na tym polu");
            //yield return null;
            }
        
    }
    */
    #endregion

}

