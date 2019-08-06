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
    public Text NazwaBohatera;
    public void BeenClicked()
    {
        ButtonOn = !ButtonOn;
        if (ButtonOn)
        {
            MyButton.image.color = new Color(0, 153, 153);
        }
        else
        {
            MyButton.image.color = Color.white;
        }
    }
    public void OnEnable()
    {
       
    }
    public void Nowy()
    {
        NazwaBohatera.text = "";
    }
    public void Wczytaj()
    {
        NazwaBohatera.text = PlayerPrefs.GetString("NazwaBohatera");

    }
    public void selectW()
    {
        PlayerPrefs.SetInt("which", 1);
        PlayerPrefs.SetString("NazwaBohatera", "Biały Toster");

  
        NazwaBohatera.text = "Biały Toster";
    }
    public void selectR()
    {
        PlayerPrefs.SetInt("which", 2);
        PlayerPrefs.SetString("NazwaBohatera", "Czerwony Toster");

        NazwaBohatera.text = "Czerwony Toster";
    }
    public void selectN()
    {
        PlayerPrefs.SetInt("which", 3);
        PlayerPrefs.SetString("NazwaBohatera", "Zielony Toster");

        NazwaBohatera.text = "Zielony Toster";
    }
}
