using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TimeSpells
{
    public class SpellOverTime
    {
        const string FireSkinDebuffSpellName = "Fire_Skin_Debuff";
        const string TerrifyingPresenceDebuffSpellName = "Terrifying_Presence_Debuff";
        public int Time = 0;
        public TosterHexUnit target, me;
        int hp = 0, att = 0, def = 0, ms = 0, ini = 0, maxdmg = 0, mindmg = 0, dmgovertime = 0, res = 0, SpecialDMGModificator = 0, counterattacks = 0, CoolDown = 0, puredmg = 0;
        public string nameofspell = null;
        public bool isStackable = false;
        List<int> SpecialEvents;
        public TosterHexUnit SourceUnit
        {
            get { return target; }
        }

        public int HpModifier
        {
            get { return hp; }
        }

        public int AttackModifier
        {
            get { return att; }
        }

        public int DefenseModifier
        {
            get { return def; }
        }

        public int MovementModifier
        {
            get { return ms; }
        }

        public int InitiativeModifier
        {
            get { return ini; }
        }

        public int MaxDamageModifier
        {
            get { return maxdmg; }
        }

        public int MinDamageModifier
        {
            get { return mindmg; }
        }

        public int DamageOverTime
        {
            get { return dmgovertime; }
        }

        public int ResistanceModifier
        {
            get { return res; }
        }

        public int CounterAttacksModifier
        {
            get { return counterattacks; }
        }

        public int DamageModifier
        {
            get { return SpecialDMGModificator; }
        }

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
           //     SpecialEvents.Add(me.GetHP() * (me.Amount - 1) + me.TempHP);

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
                me.FlatDMGReduce += 1;
            }
            if (nameofspell == "Hate")
            {
                me.HATED = target;
            }
            if (nameofspell == "Unstoppable_Light")
            {
                me.DefensePenetration = 0.7;
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
                Debug.Log("TargetStartHP : " + TargetStartHP);
                Debug.Log("me.GetHP() : " + me.GetHP());
                int TargetActualHP = me.GetHP() * (me.Amount - 1) + me.TempHP;
                Debug.Log("TargetActualHP : " + TargetActualHP);
                int dmgdone = TargetStartHP - TargetActualHP;
                Debug.Log(dmgdone);
                List<HexClass> hexarea = new List<HexClass>(me.Hex.hexMap.GetHexesWithinRadiusOf(me.Hex, 1));
                List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
                me.SendMsg("Wrząca krew z ran Axeman'a rozbryzguje dookoła (ColdBlood)");
                foreach (HexClass t in hexarea)
                {

                    if (t != null)
                    {
                        if (t.Tosters.Count > 0)
                        {

                            if (t.Tosters.Count > 0 && !t.Tosters.Contains(me))
                            {

                                FrontendResultReveal reveal = t.Tosters[0].DealMeDMGDefForFrontendReveal(Mathf.RoundToInt((float)dmgdone * 0.05f), me, false, FrontendResultRevealSource.Skill);
                                if (reveal != null)
                                {
                                    reveals.Add(reveal);
                                }

                            }

                        }
                    }
                    else { Debug.Log("No Tosters Hit"); }
                }

                SkillPresentationManager.PlaySequencedInstantHits("Cold_Blood", me, reveals, me.GetSkillAnimationState("Cold_Blood"));



       
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
                Debug.Log(puredmg/5);
                me.SpecialPUREDMG = puredmg/5;
                SkillPresentationManager.PlaySequencedCasterEffect("Massochism", me, me.GetSkillAnimationState("Massochism"));
            }
            if (nameofspell == "Hate")
            {
                me.HATED = null;
            }
            if (nameofspell == "Stone_Skin")
            {
                me.FlatDMGReduce -= 1;
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
                List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();

            foreach(HexClass h in hexclass)
                {
                    if(h!=null&&h.Tosters.Count>0 && h.Tosters[0]!=me)
                    {
                        Debug.LogError(h.Tosters[0].Name);
                        h.Tosters[0].AddNewTimeSpell(2, h.Tosters[0], 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0, -10, FireSkinDebuffSpellName, false) ;
                        reveals.Add(h.Tosters[0].BuildStatusFrontendReveal(me, FrontendResultRevealSource.Skill));
                    }
                }
                SkillPresentationManager.PlaySequencedInstantHits("Fire_Skin", me, reveals, me.GetSkillAnimationState("Fire_Skin"));
            }
            if (nameofspell == "Terrifying_Presence")
            {
                HexClass[] hexclass = me.Hex.hexMap.GetHexesWithinRadiusOf(me.Hex, 1);
                List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();

                foreach (HexClass h in hexclass)
                {
                    if (h!=null&&h.Tosters.Count > 0 && h.Tosters[0] != me)
                    {

                        h.Tosters[0].AddNewTimeSpell(2, h.Tosters[0], 0, 0, 0, 0, 0, 0, 0, 0, 0, -5, 0, 0, TerrifyingPresenceDebuffSpellName, false);
                        h.Tosters[0].CounterAttackAvaible = false;
                        Debug.Log(h.Tosters[0].Name);
                        reveals.Add(h.Tosters[0].BuildStatusFrontendReveal(me, FrontendResultRevealSource.Skill));
                    }
                }
                SkillPresentationManager.PlaySequencedInstantHits("Terrifying_Presence", me, reveals, me.GetSkillAnimationState("Terrifying_Presence"));
            }
            if (nameofspell == "Rotting")
            {
                if (me.GetHP() > 30) me.SpecialHP -= 30;
                else me.SpecialHP = -me.HP + 1;
                SkillPresentationManager.PlaySequencedCasterEffect("Rotting", me, me.GetSkillAnimationState("Rotting"));
            }
            if (nameofspell == "Fire_Movement")
            {
                me.Fire_movement = false;
            }
        }

  
    }
}
