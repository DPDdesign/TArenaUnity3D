using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tostery;



    public class Dps_Toster : Toster
    {
        public GameObject targetObject;
        public Toster targetToster;


    void Setup()
    {
        this.id = 2;
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

        public override void Hello()
        {
            Debug.Log("Jestem Toster DPS!");
        }


        void Update()
        {

        }
    }
