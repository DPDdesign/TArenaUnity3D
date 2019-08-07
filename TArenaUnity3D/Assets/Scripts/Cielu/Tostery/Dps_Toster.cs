using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dps_Toster : MonoBehaviour
{

    Toster dps = new Toster("dps", 50, 10, 1);

    void OnMouseDown()
    {
        dps.AddHp(1);
        Debug.Log(dps.hp);
    }


    void Update()
    {

    }
}
