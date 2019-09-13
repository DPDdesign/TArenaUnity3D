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
    public    HexClass TrapedHex = null;
        public TosterHexUnit TosterWhoSetupThisTrap;


    public    Traps(int time, string not, HexClass h, TosterHexUnit toster)
        {
            Time = time;
            NameOfTraps = not;
            TrapedHex = h;
            TosterWhoSetupThisTrap = toster;
            ShowTrap();
        }

       public void StartSpell()
        {

        }
        public void ShowTrap()
        {
            if (NameOfTraps=="Spike_Trap")
               TrapedHex.MyHex.transform.Find("trap1").gameObject.SetActive(true);
            if (NameOfTraps == "Fire_Trap")
                TrapedHex.MyHex.transform.Find("Fire_Trap").gameObject.SetActive(true);
        }

        internal void Remove()
        { 
          
            if (NameOfTraps == "Fire_Trap")
            {
            

                
                TrapedHex.MyHex.transform.Find("Fire_Trap").gameObject.SetActive(false);
            }
            else
            {
 
                TrapedHex.MyHex.transform.Find("trap1").gameObject.SetActive(false);
            }
        }



    }
}
