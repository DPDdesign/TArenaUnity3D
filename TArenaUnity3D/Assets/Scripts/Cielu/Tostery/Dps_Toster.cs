using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dps_Toster : Toster
{
    public GameObject targetObject;
    public Toster targetToster;


    public Dps_Toster(string nm, int bhp, int bdmg, int bdef) : base(nm, bhp, bdmg, bdef)
    {
        Debug.Log("DPS TOSTER!!!");
    }


    void OnMouseDown()
    {

        WriteStats();

        targetObject = GameObject.Find("Tank_Toster");
            if (targetObject == null) { Debug.Log("Nie ma Tostera"); }
        else {
            targetToster = targetObject.GetComponent<Toster>(); }


        if (targetToster == null) { Debug.Log("Zła klasa Tostera", targetToster); }
        else
        {
            Debug.Log(targetToster.name);
            targetToster.DealHp(10);
        }
    }

    public override void Hello()
    {
        Debug.Log("Jestem Toster DPS!");
    }


    void Update()
    {

    }
}
