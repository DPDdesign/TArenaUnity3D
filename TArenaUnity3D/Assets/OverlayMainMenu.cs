using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        
        if (FindObjectOfType<PlayFabControler>())
        {
            PlayFabControler.PFC.GetCatalog();
            PlayFabControler.PFC.GetInventory();
            PlayFabControler.PFC.GetStats();
        }

      //  else Debug.Log("OFFLINE");
        else SceneManager.LoadScene("LogIn");
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
