using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Heal_Toster : MonoBehaviour
{
   public int hp = 70;
    Toster healer = new Toster("healer", 70, 1, 3);

    void OnMouseDown()
    {
        healer.HealAll();
    }


    void Update()
    {

    }
}
