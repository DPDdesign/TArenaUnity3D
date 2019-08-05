using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Tank_Toster : MonoBehaviour
{
    public static int hp = 100;
    public static int dmg = 1;
    public static int def = 10;
    string name = "Tank Toster";

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
