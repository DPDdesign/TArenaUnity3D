using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Heal_Toster : MonoBehaviour
{
  
    Toster healer = new Toster("healer", 70, 1, 3);
    public GameObject targetObject;
    public Dps_Toster targetToster;
    void OnMouseDown()
    {
        targetToster = GameObject.Find("Dps_Toster").GetComponent<Dps_Toster>();
        if (targetToster == null) { Debug.Log("cipa", targetToster); }
        else {Debug.Log(targetToster.name); }
        //   Debug.Log("Nowe hp dps tostera to: " + targetToster.hp);
        //   Debug.Log("Nowe hp dps tostera to: " + targetToster.hp);
    }


    void Update()
    {

    }
}
