using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Tank_Toster : Toster
{

    public Tank_Toster(string nm, int bhp, int bdmg, int bdef) : base(nm, bhp, bdmg, bdef)
    {
        Debug.Log("DPS TOSTER!!!");
    }


    Toster tank = new Toster("tank", 100, 1, 10);

    void OnMouseDown()
    {
       WriteStats();
    }

    public override void Hello()
    {
        Debug.Log("Jestem Toster Tank!");
    }


    void Update()
    {
        
    }
}
