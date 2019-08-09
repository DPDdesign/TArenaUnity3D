using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class __Tostery_Staty : MonoBehaviour
{
 const    int ilosc_tosterow = 3;
  const   int ilsoc_statystyk = 3;

    public static int[,] statystyki = new int[ilosc_tosterow, ilsoc_statystyk]
    {
        {100,1,10},
        {70,1,5},
        {50,10,3},
    };

    public static string[] nazwy = new string[ilosc_tosterow]
    {
        ("Tank"),
        ("Heal"),
        ("Dps"),
    };




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //dd
    }
}
