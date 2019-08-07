using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    public Button ThisButton;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerPrefs.HasKey("YourArmy") && PlayerPrefs.HasKey("EnemyArmy"))
        {
            ThisButton.interactable = true;
    
        }
        else
            ThisButton.interactable = false;
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(2);
    }

}
