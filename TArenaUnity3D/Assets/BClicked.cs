using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BClicked : MonoBehaviour
{
    public bool ButtonOn = false;
    public Button MyButton;

    public void BeenClicked()
    {
        ButtonOn = !ButtonOn;
        if (ButtonOn)
        {
            MyButton.image.color = Color.HSVToRGB(0, 255, 255);
        }
        else
        {
            MyButton.image.color = Color.white;
        }
    }
}
