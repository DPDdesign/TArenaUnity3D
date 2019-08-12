﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICanvas : MonoBehaviour
{
    public List<Text> InfoTextsList;
    public GameObject StatsPanel;
    // Start is called before the first frame update
    void Start()
    {
        
    }


    public void UpdateCHP(int chp)
    {
        InfoTextsList[1].text = chp.ToString();
    }

    public void UpdateAllStats(int mhp, int chp, int att, int def, int dmg , int ms , int INT, string N)
    {
        InfoTextsList[0].text = mhp.ToString();
        InfoTextsList[1].text = chp.ToString();
        InfoTextsList[2].text = att.ToString();
        InfoTextsList[3].text = def.ToString();
        InfoTextsList[4].text = dmg.ToString();
        InfoTextsList[5].text = ms.ToString();
        InfoTextsList[6].text = INT.ToString();
        InfoTextsList[7].text = N;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
