﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +1);
    }
    public void Start()
    {
    


        //Debug.LogError(PlayFabControler.PFC.storeObjects[0].Id);
    }

    public void GoZestawy()
    {

    }


    public void ExitGame()
    {
        Application.Quit();
    }
}
