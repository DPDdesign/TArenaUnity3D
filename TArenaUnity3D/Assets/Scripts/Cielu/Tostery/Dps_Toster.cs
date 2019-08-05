using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dps_Toster : MonoBehaviour
{
    public static int hp = 50;
    public static int dmg = 5;
    public static int def = 2;
    string name = "Dps Toster";

    void OnMouseDown()
    {
        hp--;
        Debug.Log(name + " HP: " + hp);
    }

    void Update()
    {
        
    }
}
