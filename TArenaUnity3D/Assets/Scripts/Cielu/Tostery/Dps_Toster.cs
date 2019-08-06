using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dps_Toster : MonoBehaviour
{

    public int hp = 70;
    Toster dps = new Toster("dps", 50, 10, 2);

    void OnMouseDown()
    {
        dps.WriteStats();
    }


    void Update()
    {
        
    }
}
