using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Traps
{
    public class Traps
    {
      public  int Time = 0;
     public   string NameOfTraps = "";
        string presentationSkillId = "";
    public    HexClass TrapedHex = null;
        public TosterHexUnit TosterWhoSetupThisTrap;
        GameObject spawnedModel;


    public    Traps(int time, string not, HexClass h, TosterHexUnit toster, bool showImmediately = true, string presentationSkillId = null)
        {
            Time = time;
            NameOfTraps = not;
            this.presentationSkillId = string.IsNullOrWhiteSpace(presentationSkillId) ? not : presentationSkillId;
            TrapedHex = h;
            TosterWhoSetupThisTrap = toster;
            if (showImmediately)
            {
                ShowTrap();
            }
        }

       public void StartSpell()
        {

        }
        public void ShowTrap()
        {
            if (spawnedModel != null)
            {
                spawnedModel.SetActive(true);
                return;
            }

            spawnedModel = SkillPresentationManager.SpawnPersistentModel(presentationSkillId, TrapedHex);
            if (spawnedModel != null)
            {
                return;
            }

            ShowLegacyTrapChild();
        }

        internal void Remove()
        { 
            if (spawnedModel != null)
            {
                GameObject.Destroy(spawnedModel);
                spawnedModel = null;
                return;
            }

            HideLegacyTrapChild();
        }

        void ShowLegacyTrapChild()
        {
            Transform legacyTrap = FindLegacyTrapChild();
            if (legacyTrap != null)
            {
                legacyTrap.gameObject.SetActive(true);
            }
        }

        void HideLegacyTrapChild()
        {
            Transform legacyTrap = FindLegacyTrapChild();
            if (legacyTrap != null)
            {
                legacyTrap.gameObject.SetActive(false);
            }
        }

        Transform FindLegacyTrapChild()
        {
            if (TrapedHex == null || TrapedHex.MyHex == null)
            {
                return null;
            }

            if (NameOfTraps == "Fire_Trap")
            {
                return TrapedHex.MyHex.transform.Find("Fire_Trap");
            }

            if (NameOfTraps == "Spike_Trap")
            {
                return TrapedHex.MyHex.transform.Find("trap1");
            }

            return null;
        }

    }
}
