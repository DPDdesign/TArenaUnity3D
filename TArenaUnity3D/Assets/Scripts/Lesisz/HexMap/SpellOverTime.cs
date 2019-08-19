using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TimeSpells
{
    public class SpellOverTime
    {
        public int Time = 0;
        public TosterHexUnit target, me;
        int hp = 0, att = 0, def = 0, ms = 0, ini = 0, maxdmg = 0, mindmg = 0, dmgovertime = 0, res = 0, SpecialDMGModificator = 0, counterattacks = 0;
        public string nameofspell = null;
        public bool isStackable = false;
        List<int> SpecialEvents;
     public   SpellOverTime(int Time,
                      TosterHexUnit target,
                      TosterHexUnit me,
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
                      string nameofspell,
                      bool isStackable)
        {
            this.Time = Time;
            this.me = me;
            this.target = target;
            this.hp = hp;
            this.att = att;
            this.def = def;
            this.ms = ms;
            this.ini = ini;
            this.maxdmg = maxdmg;
            this.mindmg = mindmg;
            this.res = res;
            this.counterattacks = counterattacks;
            this.dmgovertime = dmgovertime;
            this.nameofspell = nameofspell;
            this.isStackable = isStackable;
            this.SpecialDMGModificator = SpecialDMGModificator;
            this.SpecialEvents = new List<int>();
            StartSpell();
            SpecialThingOnStart();
        }

        void StartSpell()
        {
            me.SpecialHP += hp;
            me.HealMe(hp);
            me.SpecialAtt += att;
            me.SpecialDef += def;
            me.SpecialMS += ms;
            me.SpecialI += ini;
            me.SpecialmaxDMG += maxdmg;
            me.SpecialminDMG += mindmg;
            me.SpecialResistance += res;
            me.SpecialDMGModificator += SpecialDMGModificator;
            me.CounterAttacks += counterattacks;
            me.TempCounterAttacks += counterattacks;
        }
        public void DoTurn()
        {
            Time--;
            if (dmgovertime > 0)
            {
                me.DealMePURE(dmgovertime);
            }
            else if (dmgovertime < 0)
            {
                me.HealMe(dmgovertime);
            }
        }
        public bool IsOver()
        {
            if (Time == 0)
            {
                me.SpecialHP -= hp;
                if (me.TempHP > me.GetHP())
                {
                    me.TempHP = me.GetHP();
                }

                me.SpecialAtt -= att;
                me.SpecialDef -= def;
                me.SpecialMS -= ms;
                me.SpecialI -= ini;
                me.SpecialmaxDMG -= maxdmg;
                me.SpecialminDMG -= mindmg;
                me.SpecialResistance -= res;
                me.SpecialDMGModificator -= SpecialDMGModificator;
                me.CounterAttacks -= counterattacks;
                SpecialThingOnEnd();
                return true;
            }
            else return false;
        }


        public void SpecialThingOnStart()
        {
            if (nameofspell == "Topornik_Skill3" )
            {
                SpecialEvents.Add(me.GetHP());
                SpecialEvents.Add(me.TempHP);
                SpecialEvents.Add(me.Amount);
            }
            if (nameofspell == "Taunt")
            {
                GetTaunted();
            }
            if (nameofspell == "Stun")
            {
                GetStuned();
            }
        }


        public void GetStuned()
        {
            me.Stuned = true;
            if (me.Stuned == true)
            {
                SpellOverTime temp = me.AskForSpell(nameofspell, this);
                Debug.LogError(temp.Time);
                int newtime = this.Time > temp.Time ? this.Time : temp.Time;
                this.Time = newtime;
                me.RemoveSpell(temp);
            }
            else { me.Stuned = true; }
        }
        public void GetTaunted()
        {
            if (me.Taunt == true)
            {
                
                SpellOverTime temp = me.AskForSpell(nameofspell, this);
                Debug.LogError(temp.Time);
                Debug.LogError(this.Time);
                int newtime = this.Time > temp.Time ? this.Time : temp.Time;
                this.Time = newtime;
                Debug.LogError(newtime);
                me.RemoveSpell(temp);

            }
            else { me.Taunt = true; }
          
            me.whoTauntedMe = target;
        }
        public void TauntEnd()
        {
            me.Taunt = false;
            me.whoTauntedMe = null;
        }
        public void StunEnd()
        {
            me.Stuned = false;       
        }



        public void SpecialThingOnEnd()
        {
            if (nameofspell == "Topornik_Skill3")
            {
                int TargetStartHP = (SpecialEvents[0] * (SpecialEvents[2] - 1) + SpecialEvents[1]);
                int TargetActualHP = me.GetHP() * (me.Amount - 1) + me.TempHP;
                int dmgdone = TargetStartHP - TargetActualHP;

                me.DealMePURE(Mathf.RoundToInt(dmgdone*0.1f));
            }
            if (nameofspell == "Taunt")
            {
                TauntEnd();
            }
            if(nameofspell == "Stun")
            {
                StunEnd();
            }
        }




    }
}
