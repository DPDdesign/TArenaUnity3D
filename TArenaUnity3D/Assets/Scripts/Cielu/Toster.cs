using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toster : MonoBehaviour
{


        public string Name;
        public int BaseHealthPoints;
        public int BaseDamage;
        public int BaseDefence;
        public int hp;


        public Toster(string nm, int bhp, int bdmg, int bdef)
        {
            Name = nm;
            BaseHealthPoints = bhp;
            BaseDamage = bdmg;
            BaseDefence = bdef;
            hp = bhp;
        }
    
    // Start is called before the first frame update
    void Start()
    {
      

    }
   public void WriteStats()
    {
        Debug.Log("Toster " + this.Name + " Stats (Hp/Dmg/Def): " + this.hp + " / " + this.BaseDamage + " / " + this.BaseDefence);

    }

    public void DealHp(int x)
    {
        this.hp -= x;
    }

    public void AddHp(int x)
    {
        this.hp += x; 
    }
    // Update is called once per frame
    void Update()
    {
        
    }

}
