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


    public    Traps(int time, string not, HexClass h)
        {
            Time = time;
            NameOfTraps = not;
            TrapedHex = h;
            ShowTrap();
        }

       public void StartSpell()
        {

        }
        public void ShowTrap()
        {
            if (NameOfTraps=="Spike_Trap")
            TrapedHex.MyHex.transform.Find("trap1").gameObject.SetActive(true);
        }

        internal void Remove()
        {
            TrapedHex.MyHex.transform.Find("trap1").gameObject.SetActive(false);
        }
    }
}
