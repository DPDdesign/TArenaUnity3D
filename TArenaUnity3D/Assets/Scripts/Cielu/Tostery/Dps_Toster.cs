using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dps_Toster : MonoBehaviour
{
    public GameObject targetObject;
    public Tank_Toster targetToster;
    Toster dps = new Toster("dps", 50, 10, 1);

    void OnMouseDown()
    {
        targetToster = GameObject.Find("Tank_Toster").GetComponent<Tank_Toster>();
        if (targetToster == null) { Debug.Log("Nie Ma Tostera", targetToster); }
        else
        {
            Debug.Log(targetToster.name);
            targetToster.Deal(2);
        }
    }


        void Update()
    {

    }
}
