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
        Debug.Log("Toster " + Name + " Stats (Hp/Dmg/Def): " + hp + " / " + BaseDamage + " / " + BaseDefence);

    }

    public virtual void Hello()
    {
        Debug.Log("Jestem Toster bez klasy");
    }

    public virtual void DealHp(int x)
    {
        hp -= x;
    }

    public virtual void AddHp(int x)
    {
        hp += x; 
    }
    
  
    // Update is called once per frame
    void Update()
    {
       
    }

}
