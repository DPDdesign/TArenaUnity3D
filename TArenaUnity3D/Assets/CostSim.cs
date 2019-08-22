using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CostSim : MonoBehaviour
{
    public Text This;
    public Text Cost;
    public InputField TCost;
    public InputField Amount;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       
        This.text = "Total Cost:  " + ((Int32.Parse("0"+Cost.text)+Int32.Parse(TCost.text + "0") / 10 )* Int32.Parse(Amount.text+"0") / 10).ToString();
    }
}
