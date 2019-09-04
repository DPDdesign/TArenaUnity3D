using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Details : MonoBehaviour
{
     public GameObject DetailsPanel;
     public Text LevelText;
     public Text ExpText;
     public Text WinText;
     public Text LossesText;
     public Text WinRatioText;
     public Text RankPointText;
     public Text  NicknameText;
     public Text MaxExpText;
   // Start is called before the first frame update
    void Start()
    {  
       
         NicknameText.text = PlayFabControler.PFC.tosterName;
         
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickShowDetails()
    {
    DetailsPanel.SetActive(!DetailsPanel.activeSelf);
           ShowStats();
     
    
    }

float WinRatio;
    void ShowStats()
    { 
        
        PlayFabControler.PFC.GetStats();
        
        ExpText.text = PlayFabControler.PFC.Experience.ToString();
        WinText.text = PlayFabControler.PFC.Wins.ToString();
        LossesText.text = PlayFabControler.PFC.Losses.ToString();
        WinRatio = ((float)PlayFabControler.PFC.Wins/(float)PlayFabControler.PFC.Losses);
        WinRatioText.text = WinRatio.ToString("0.##");
        RankPointText.text = PlayFabControler.PFC.RankPoints.ToString();
        MaxExpText.text = PlayFabControler.ExpRequiredForLevel[PlayFabControler.PFC.Level + 1].ToString();
        LevelText.text = PlayFabControler.PFC.Level.ToString();
    }
}
