using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Tank_Toster : MonoBehaviour
{

    Toster tank = new Toster("tank", 100, 1, 10);

    void OnMouseDown()
    {
        tank.WriteStats();
    }


    void Update()
    {

    }
}
