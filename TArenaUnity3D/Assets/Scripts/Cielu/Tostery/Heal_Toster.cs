using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Heal_Toster : MonoBehaviour
{
 
    Toster healer = new Toster("healer", 70, 1, 3);
    public GameObject targetObject;
    public Tank_Toster targetToster;
    void OnMouseDown()
    {
        targetToster = GameObject.Find("Tank_Toster").GetComponent<Tank_Toster>();
        if (targetToster == null) { Debug.Log("Nie Ma Tostera", targetToster); }
        else
        {
            Debug.Log(targetToster.name);
            targetToster.Heal(2);
        }

    }


    void Update()
    {

    } 
}
