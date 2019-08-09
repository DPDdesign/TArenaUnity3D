using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tostery;



    public class Dps_Toster : Toster
    {
        public GameObject targetObject;
        public Toster targetToster;

    public override void Start()
    {
        id = 2;
        SetValues(id);
    }

    void Update()
    {
        if (Input.GetKeyDown("space"))
            {
          
        }
    }

    void Wpierdol()
        {

            WriteStats();

            targetObject = GameObject.Find("Tank_Toster");
            if (targetObject == null) { Debug.Log("Nie ma Tostera"); }
            else
            {
                targetToster = targetObject.GetComponent<Toster>();
            }


            if (targetToster == null) { Debug.Log("Zła klasa Tostera", targetToster); }
            else
            {
                Debug.Log(targetToster.name);
                targetToster.DealHp(10);
            }
        }

    }
