using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Skill1 : SkillsDefault
{
   public Skill1()
    {
        this.SkillName = "Skill1";
    }

    /* Heal Ally
        override public void CastSkill()
        {
            if (hexUnderMouse != SelectedToster.Hex && SelectedToster.Team.HexesUnderTeam.Contains(hexUnderMouse))
            {
                if (hexUnderMouse.Tosters.Count > 0)
                {
                    TosterHexUnit trgt = hexUnderMouse.Tosters[0];
                    trgt.HealMe(5);
                    Debug.Log(trgt.Name);
                }
            }  
        }

    */

    #region Tripple shot

    override public void CastSkill()
    {
        if (hexUnderMouse != SelectedToster.Hex && SelectedToster.Team.HexesUnderTeam.Contains(hexUnderMouse))
        {
            if (hexUnderMouse.Tosters.Count > 0)
            {
                TosterHexUnit trgt = hexUnderMouse.Tosters[0];
                trgt.AttackMe(trgt);
                Debug.Log(trgt.Name);
            }
        }
    }

    #endregion

}

