using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toster : MonoBehaviour
{


        public string Name="Zwkly Toster";
        public int BaseHealthPoints=1;
        public int BaseDamage=1;
        public int BaseDefence=1;
        public int hp=1;


        public Toster(string nm, int bhp, int bdmg, int bdef)
        {
            Name = nm;
            BaseHealthPoints = bhp;
            BaseDamage = bdmg;
            BaseDefence = bdef;
            hp = bhp;
        }

    void Start()
    {
    }
   public void WriteStats()
    {
        Debug.Log("Toster " + Name + " Stats (Hp/Dmg/Def): " + hp + " / " + BaseDamage + " / " + BaseDefence);

    }


    public virtual void Hello()
    {
        Debug.Log("Jestem Toster bez klasy");
    }

    public virtual void DealHp(int x)
    {
        hp -= x;
        Debug.Log(hp);
    }

    public virtual void AddHp(int x)
    {
        hp += x; 
    }
    
  
    void Update()
    {
       
    }

}
