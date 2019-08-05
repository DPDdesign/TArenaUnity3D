using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Heal_Toster : MonoBehaviour
{
    public static int hp = 50;
    public static int dmg = 1;
    public static int def = 3;

    string name = "Heal Toster";

    void OnMouseDown()
    {
        hp--;
        Debug.Log(name + " HP: " + hp);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
