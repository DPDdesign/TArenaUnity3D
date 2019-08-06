using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class Generator : MonoBehaviour
{
    public bool ButtonOn = false;
    public Button MyButton;

    public void BeenClicked()
    {
        ButtonOn = !ButtonOn;
        if (ButtonOn)
        {
            MyButton.image.color = Color.red;
        }
        else
        {
            MyButton.image.color = Color.white;
        }
    }
    public void selectW()
    {
        PlayerPrefs.SetInt("which", 1);
    }
    public void selectR()
    {
        PlayerPrefs.SetInt("which", 2);
    }
    public void selectN()
    {
        PlayerPrefs.SetInt("which", 3);
    }
}
