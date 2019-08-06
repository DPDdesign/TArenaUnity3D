using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toster : MonoBehaviour
{
    public string Name;
    public  int BaseHealthPoints;
    public int BaseDamage;
    public int BaseDefence;


    GameObject targetObject;
    Toster targetToster;

    public Toster(string nm, int bhp, int bdmg, int bdef)
    {
        Name = nm;
        BaseHealthPoints = bhp;
        BaseDamage = bdmg;
        BaseDefence = bdef;
    }
     
    // Start is called before the first frame update
    void Start()
    {
      

    }
   public void WriteStats()
    {
        Debug.Log("Toster " + this.Name + " Stats (Hp/Dmg/Def): " + this.BaseHealthPoints + " / " + this.BaseDamage + " / " + this.BaseDefence);

    }

    public void HealAll()
    {

        targetObject = GameObject.Find("Dps_Toster");
        targetToster = targetObject.GetComponent<Toster>();
 

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
