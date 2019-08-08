using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Heal_Toster : Toster
{
 
    Toster healer = new Toster("healer", 70, 1, 3);
    public GameObject targetObject;
    public Toster targetToster;


    public Heal_Toster(string nm, int bhp, int bdmg, int bdef) : base(nm, bhp, bdmg, bdef)
    {
        Debug.Log("DPS TOSTER!!!");
    }


    void OnMouseDown()
    {
        targetToster = GameObject.Find("Tank_Toster").GetComponent<Toster>();
        if (targetToster == null) { Debug.Log("Nie Ma Tostera", targetToster); }
        else
        {
            targetToster.Hello();
        }

    }

    public override void Hello()
    {
        Debug.Log("Jestem Toster Healer!");
    }

    void Update()
    {

    } 
}
