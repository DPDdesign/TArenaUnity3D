using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OverlayMainMenu : MonoBehaviour
{
    float LevelProgress;
    public Slider LevelSlider;
    public Text TCn, ATn;
    public GameObject DetailsPanel;
    public GameObject shop;
    public Text LevelText;
    public Text ExpText;
    public Text WinText;
    public Text LossesText;
    public Text WinRatioText;
    public Text RankPointText;
    public Text NicknameText;
    public Text MaxExpText;
    // Start is called before the first frame update
    void Start()
    {
        
        if (FindObjectOfType<PlayFabControler>())
        {
            PlayFabControler.PFC.GetCatalog();
            PlayFabControler.PFC.GetInventory();
            PlayFabControler.PFC.GetStats();
        }
        else SceneManager.LoadScene("LogIn");
    }

    // Update is called once per frame
    void Update()
    {
        ShowStats();
    }

    // Start is called before the first frame update
  

    public void OnClickShowDetails()
    {
        DetailsPanel.SetActive(!DetailsPanel.activeSelf);
      


    }
    public void OnClickShowShop()
    {
        shop.SetActive(!shop.activeSelf);



    }
    float WinRatio;
    void ShowStats()
    {

       // PlayFabControler.PFC.GetStats();
        TCn.text = PlayFabControler.PFC.tCoins.ToString();
        ATn.text = PlayFabControler.PFC.aTokens.ToString();
        ExpText.text = PlayFabControler.PFC.Experience.ToString();
        WinText.text = PlayFabControler.PFC.Wins.ToString();
        LossesText.text = PlayFabControler.PFC.Losses.ToString();
        WinRatio = ((float)PlayFabControler.PFC.Wins / ((float)PlayFabControler.PFC.Losses+ (float)PlayFabControler.PFC.Wins))*100;
        WinRatioText.text = WinRatio.ToString("##")+" %";
        RankPointText.text = PlayFabControler.PFC.RankPoints.ToString();
        MaxExpText.text = PlayFabControler.ExpRequiredForLevel[PlayFabControler.PFC.Level + 1].ToString();
        LevelText.text = PlayFabControler.PFC.Level.ToString();
        NicknameText.text = PlayFabControler.PFC.UserName;
        LevelProgress = (float)(PlayFabControler.PFC.Experience) / (float)PlayFabControler.ExpRequiredForLevel[PlayFabControler.PFC.Level];
        LevelSlider.value = LevelProgress;
    }
}
