using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    public Button ThisButton;
    public Toggle AI, MultiPlayer;
    public NetworkConnectionManager NetworkConnectionManager;
    public InputField inputField;
    public bool nomulti;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if (nomulti == true)
        {
         
            if (PlayerPrefs.HasKey("YourArmy") && PlayerPrefs.HasKey("EnemyArmy"))
            {
                ThisButton.interactable = true;

            }
            else
                ThisButton.interactable = false;
        }
        else
        {
            if (PlayerPrefs.HasKey("YourArmy"))
            {
                ThisButton.interactable = true;

            }
            else
                ThisButton.interactable = false;
        }
    }

    public void PlayGame()
    {
        if (AI.isOn)
            PlayerPrefs.SetInt("AI", 1);
        else
            PlayerPrefs.SetInt("AI", 0);
        if (MultiPlayer.isOn) {
            PlayerPrefs.SetInt("Multi", 1);
            
                }
        else
            PlayerPrefs.SetInt("Multi", 0);
        NetworkConnectionManager.OnClickConnectToMaster();
 
    }

    public void QuickGame()
    {
        if (AI.isOn)
            PlayerPrefs.SetInt("AI", 1);
        else
            PlayerPrefs.SetInt("AI", 0);
        if (MultiPlayer.isOn)
        {
            PlayerPrefs.SetInt("Multi", 1);

        }
        else
            PlayerPrefs.SetInt("Multi", 0);
        NetworkConnectionManager.OnClickConnectToMaster();

    }
    public void JoinFriend()
    {
        if (AI.isOn)
            PlayerPrefs.SetInt("AI", 1);
        else
            PlayerPrefs.SetInt("AI", 0);
        if (MultiPlayer.isOn)
        {
            PlayerPrefs.SetInt("Multi", 1);

        }
        else
            PlayerPrefs.SetInt("Multi", 0);
        NetworkConnectionManager.OnClickConnectToMasterCustom(inputField.text);

    }
}
