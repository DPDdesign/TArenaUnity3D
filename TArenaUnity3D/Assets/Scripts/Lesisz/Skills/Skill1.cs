using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill1 : SkillsDefault
{
   public Skill1()
    {
        this.SkillName = "Skill1";
    }

    override public void CastSkill(TosterHexUnit Caster, TosterHexUnit Target)
    {
        Debug.Log("Użyłem Skill1");
    }
}
