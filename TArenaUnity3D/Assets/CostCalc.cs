using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CostCalc : MonoBehaviour
{
    // Start is called before the first frame update

    public int n;
   public Generator generator;
    public InputField field;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      
        this.GetComponent<Text>().text =(generator.Costs[n] * int.Parse("0"+field.text)).ToString();
    }
}
