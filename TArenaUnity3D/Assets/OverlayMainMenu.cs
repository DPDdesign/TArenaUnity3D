using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverlayMainMenu : MonoBehaviour
{
    public List<GameObject> panels = new List<GameObject>();
    public List<GameObject> buttons = new List<GameObject>();
    public List<Color> buttonColorsOn = new List<Color>();
    public List<Color> buttonColorsOff = new List<Color>();
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
    public Button back;
    // Start is called before the first frame update
    void Start()
    {
        PlayFabControler localBackend = PlayFabControler.EnsureInstance();
        localBackend.GetCatalog();
        localBackend.GetInventory();
        localBackend.GetStats();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            back.onClick.Invoke();
        }
        ShowStats();
    }

    // Start is called before the first frame update
  
    public void OnClickOverlayButton(int i)
    {


    }

    public void OnClickShowDetails()
    {
        DetailsPanel.SetActive(!DetailsPanel.activeSelf);
    }

    public void OnClickShowShop()
    {
        ShowPanelOnly(1);
        if (shop.activeSelf == true)  buttons[1].GetComponent<Image>().color = new Color(1,0.6f,0,1);
        else buttons[1].GetComponent<Image>().color = new Color(1,0.9f,0,1);
    }


public void ShowPanelOnly(int PanelNumber)
{
     panels[PanelNumber].SetActive(true);
     buttons[PanelNumber].GetComponent<Image>().color = buttonColorsOn[PanelNumber];
      buttons[PanelNumber].GetComponentInChildren<Text>().color = Color.white;
        int i = 0;
    foreach (var GameObject in panels)
    {
        if(i!=PanelNumber)
        {
         panels[i].SetActive(false);   
          buttons[i].GetComponent<Image>().color = buttonColorsOff[i];
          buttons[i].GetComponentInChildren<Text>().color = Color.black;
           
        }
        i++;
    }
}

    float WinRatio;
    void ShowStats()
    {
        PlayFabControler localBackend = PlayFabControler.EnsureInstance();

       // PlayFabControler.PFC.GetStats();
        TCn.text = localBackend.tCoins.ToString();
        ATn.text = localBackend.aTokens.ToString();
        ExpText.text = localBackend.Experience.ToString();
        WinText.text = localBackend.Wins.ToString();
        LossesText.text = localBackend.Losses.ToString();
        int gamesPlayed = localBackend.Losses + localBackend.Wins;
        WinRatio = gamesPlayed == 0 ? 0 : ((float)localBackend.Wins / (float)gamesPlayed) * 100;
        WinRatioText.text = WinRatio.ToString("##")+" %";
        RankPointText.text = localBackend.RankPoints.ToString();
        int nextLevelIndex = Mathf.Min(localBackend.Level + 1, PlayFabControler.ExpRequiredForLevel.Length - 1);
        int currentLevelIndex = Mathf.Min(localBackend.Level, PlayFabControler.ExpRequiredForLevel.Length - 1);
        MaxExpText.text = PlayFabControler.ExpRequiredForLevel[nextLevelIndex].ToString();
        LevelText.text = localBackend.Level.ToString();
        NicknameText.text = localBackend.UserName;
        LevelProgress = (float)(localBackend.Experience) / (float)PlayFabControler.ExpRequiredForLevel[currentLevelIndex];
        LevelSlider.value = LevelProgress;
    }
}
