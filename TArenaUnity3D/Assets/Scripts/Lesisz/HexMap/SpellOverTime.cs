﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TimeSpells
{
    public class SpellOverTime
    {
        public int Time = 0;
        public TosterHexUnit target;
        int hp = 0, att = 0, def = 0, ms = 0, ini = 0, maxdmg = 0, mindmg = 0, dmgovertime = 0, res = 0;
        public string nameofspell = null;
        public bool isStackable = false;
        public int[] SpecialEvents;
     public   SpellOverTime(int Time,
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
                      string nameofspell,
                      bool isStackable)
        {
            this.Time = Time;
            this.target = target;
            this.hp = hp;
            this.att = att;
            this.def = def;
            this.ms = ms;
            this.ini = ini;
            this.maxdmg = maxdmg;
            this.mindmg = mindmg;
            this.res = res;
            this.dmgovertime = dmgovertime;
            this.nameofspell = nameofspell;
            this.isStackable = isStackable;
            StartSpell();
        }

        void StartSpell()
        {
            target.SpecialHP += hp;
            target.HealMe(hp);
            target.SpecialAtt += att;
            target.SpecialDef += def;
            target.SpecialMS += ms;
            target.SpecialI += ini;
            target.SpecialmaxDMG += maxdmg;
            target.SpecialminDMG += mindmg;
        }
        public void DoTurn()
        {
            Time--;
            if (dmgovertime > 0)
            {
                target.DealMePURE(dmgovertime);
            }
            else if (dmgovertime < 0)
            {
                target.HealMe(dmgovertime);
            }
        }
        public bool IsOver()
        {
            if (Time == 0)
            {
                target.SpecialHP -= hp;
                if (target.TempHP > target.GetHP())
                {
                    target.TempHP = target.GetHP();
                }
                target.SpecialAtt -= att;
                target.SpecialDef -= def;
                target.SpecialMS -= ms;
                target.SpecialI -= ini;
                target.SpecialmaxDMG -= maxdmg;
                target.SpecialminDMG -= mindmg;
                return true;
            }
            else return false;
        }


        public void SpecialThing()
        {
            /if (nameofspell == )
        }

       


    }
}
