using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Symulator : MonoBehaviour
{

   public SimButtonCh.TosterStats Toster1;
   public SimButtonCh.TosterStats Toster2;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SetToster1(SimButtonCh.TosterStats s)
    {
        Toster1 = s;
    }
    public void SetToster2(SimButtonCh.TosterStats s)
    {
        Toster2 = s;
    }



    internal void SaveCost(int cost)
    {
        throw new NotImplementedException();
    }

    internal void SaveUnit(string name)
    {
        throw new NotImplementedException();
    }
}
