using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TotalCost : MonoBehaviour
{
    public List<Text> Txt;
    public Text This;
    public Button SafeAndExit;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    
    // Update is called once per frame
    void Update()
    {
        int TotalCost = 0;
        foreach (Text i in Txt)
        {
            TotalCost += int.Parse(i.text);
     
        }
        if (TotalCost > 25000)
        {
            This.text = TotalCost.ToString() + "   (MAX: 25000)";
            This.color = Color.red;
            SafeAndExit.gameObject.SetActive(false);

        }
        else
        {
            This.text = TotalCost.ToString() + "   (MAX: 25000)";
            This.color = Color.white;
            SafeAndExit.gameObject.SetActive(true);
        }
    }
}
