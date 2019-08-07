using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dps_Toster : MonoBehaviour
{
    public GameObject targetObject;
    public Tank_Toster targetToster;
    Toster dps = new Toster("dps", 100, 10, 1);

    void OnMouseDown()
    {
        targetObject = GameObject.Find("Tank_Toster");
            if (targetObject == null) { Debug.Log("Nie ma Tostera"); }
        else {
            targetToster = targetObject.GetComponent<Tank_Toster>(); }


        if (targetToster == null) { Debug.Log("Zła klasa Tostera", targetToster); }
        else
        {
            Debug.Log(targetToster.name);
            targetToster.Deal(10);
        }
    }



    void Update()
    {

    }
}
