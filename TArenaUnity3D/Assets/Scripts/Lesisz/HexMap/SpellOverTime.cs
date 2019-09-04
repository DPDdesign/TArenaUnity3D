using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TimeSpells
{
    public class SpellOverTime
    {
        public int Time = 0;
        public TosterHexUnit target, me;
        int hp = 0, att = 0, def = 0, ms = 0, ini = 0, maxdmg = 0, mindmg = 0, dmgovertime = 0, res = 0, SpecialDMGModificator = 0, counterattacks = 0, CoolDown = 0, puredmg = 0;
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
                    int SpecialResistance,
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
            this.res = SpecialResistance;
            this.SpecialDMGModificator = SpecialDMGModificator;
            this.SpecialEvents = new List<int>();
            StartSpell();
            SpecialThingOnStart();
        }
        public SpellOverTime(SpellOverTime spell)
        {
            this.Time = spell.Time;
            this.me = spell.me;
            this.target = spell.target;
            this.hp = spell.hp;
            this.att = spell.att;
            this.def = spell.def;
            this.ms = spell.ms;
            this.ini = spell.ini;
            this.maxdmg = spell.maxdmg;
            this.mindmg = spell.mindmg;
            this.res = spell.res;
            this.counterattacks = spell.counterattacks;
            this.dmgovertime = spell.dmgovertime;
            this.nameofspell = spell.nameofspell;
            this.isStackable = spell.isStackable;
            this.res = spell.res;
            this.SpecialDMGModificator = spell.SpecialDMGModificator;
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
            if (Time <= 0)
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
            if (nameofspell == "Cold_Blood")
            {
                SpecialEvents.Add(me.GetHP());
                SpecialEvents.Add(me.TempHP);
                SpecialEvents.Add(me.Amount);
                res = 20;
                me.SpecialResistance += 20;
            }
            if (nameofspell == "Taunt")
            {
                GetTaunted();
            }
            if (nameofspell == "Stun")
            {
                GetStuned();
            }
            if (nameofspell == "Massochism")
            {
              
                SpecialEvents.Add(me.GetHP()*(me.Amount-1)+me.TempHP);
          //      Debug.Log(me.GetHP() * me.Amount + me.TempHP);
            }
            if(nameofspell=="Stone_Skin")
            {
                me.FlatDMGReduce += 2;
            }
            if (nameofspell == "Hate")
            {
                me.HATED = target;
            }
            if (nameofspell == "Unstoppable_Light")
            {
                me.DefensePenetration = 1;
            }
            if (nameofspell == "Blind")
            {
                me.Blinded = true;
            }
            if(nameofspell == "Fire_Movement")
            {
                me.Fire_movement = true;
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
            if (nameofspell == "Cold_Blood")
            {
                int TargetStartHP = (SpecialEvents[0] * (SpecialEvents[2] - 1) + SpecialEvents[1]);
                int TargetActualHP = me.GetHP() * (me.Amount - 1) + me.TempHP;
                int dmgdone = TargetStartHP - TargetActualHP;

                me.DealMePURE(Mathf.RoundToInt(dmgdone*0.2f));
                Debug.Log(Mathf.RoundToInt(dmgdone * 0.2f));
            }
            if (nameofspell == "Taunt")
            {
                TauntEnd();
            }
            if(nameofspell == "Stun")
            {
                StunEnd();
            }
            if (nameofspell == "Massochism")
            {
                puredmg = (SpecialEvents[0] - (me.GetHP() * (me.Amount-1) + me.TempHP));
                Debug.Log(puredmg/2);
                me.SpecialPUREDMG = puredmg/2;
            }
            if (nameofspell == "Hate")
            {
                me.HATED = null;
            }
            if (nameofspell == "Stone_Skin")
            {
                me.FlatDMGReduce -= 2;
            }
            if (nameofspell == "Unstoppable_Light")
            {
                me.DefensePenetration = 0;
            }
            if (nameofspell == "Blind")
            {
                me.Blinded = false;
            }
            if (nameofspell == "Fire_Skin")
            {
              HexClass[] hexclass =  me.Hex.hexMap.GetHexesWithinRadiusOf(me.Hex, 1);

            foreach(HexClass h in hexclass)
                {
                    if(h!=null&&h.Tosters.Count>0 && h.Tosters[0]!=me)
                    {
                        h.Tosters[0].AddNewTimeSpell(2, h.Tosters[0], 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0, -10, "Fire_skin", false) ;
                    }
                }
            }
            if (nameofspell == "Terrifying_Presence")
            {
                HexClass[] hexclass = me.Hex.hexMap.GetHexesWithinRadiusOf(me.Hex, 1);

                foreach (HexClass h in hexclass)
                {
                    if (h!=null&&h.Tosters.Count > 0 && h.Tosters[0] != me)
                    {
                        h.Tosters[0].AddNewTimeSpell(2, h.Tosters[0], 0, 0, 0, 0, 0, 0, 0, 0, 0, -5, 0, 0, "Terrifying", false);
                        h.Tosters[0].CounterAttackAvaible = false;
                        Debug.Log(h.Tosters[0].Name);
                    }
                }
            }
            if (nameofspell == "Rotting")
            {
                if (me.GetHP() > 16) me.SpecialHP -= 16;
                else me.SpecialHP = -me.HP + 1;
            }
            if (nameofspell == "Fire_Movement")
            {
                me.Fire_movement = false;
            }
        }

  
    }
}
