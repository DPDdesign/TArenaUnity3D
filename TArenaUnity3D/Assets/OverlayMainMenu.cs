using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverlayMainMenu : MonoBehaviour
{
    public Text TCn, ATn;
    // Start is called before the first frame update
    void Start()
    {
        PlayFabControler.PFC.GetCatalog();
        PlayFabControler.PFC.GetInventory();
    }

    // Update is called once per frame
    void Update()
    {
        TCn.text =PlayFabControler.PFC.tCoins.ToString();
        ATn.text = PlayFabControler.PFC.aTokens.ToString();
    }
}
