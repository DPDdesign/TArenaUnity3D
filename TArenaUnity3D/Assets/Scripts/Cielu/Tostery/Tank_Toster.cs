using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Tank_Toster : MonoBehaviour
{

     Toster tank = new Toster("tank", 100, 1, 10);

    void OnMouseDown()
    {
        tank.WriteStats();
    }

    public void Heal(int x)
    {

        tank.AddHp(x);
        Debug.Log("Mowi: zostalem uleczony o: " + x + " Moje nowe hp to: " + tank.hp);
    }

    public void Deal(int y)
    {

        tank.DealHp(y);
        Debug.Log("Mowi: zostalem uleczony o: " + y + " Moje nowe hp to: " + tank.hp);
        CheckHP();
    }



    void CheckHP()
    {
        if (tank.hp <= 20)
        {
            Destroy(gameObject);
        }
    }


    void Update()
    {
        
    }
}
