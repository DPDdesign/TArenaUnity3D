using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tostery;


public class Heal_Toster : Toster
{


    public GameObject targetObject;
    public Toster targetToster;
    
    public override void Start()
    {
        id = 1;
        SetValues(id);
    }

    void Update()
    {

    } 

    void GetTarget()
    {
        targetObject = GameObject.Find("Tank_Toster");
        targetToster = GameObject.Find("Tank_Toster").GetComponent<Toster>();
        if (targetToster == null) { Debug.Log("Nie Ma Tostera", targetToster); }
        else
        {

        }
    }
}
