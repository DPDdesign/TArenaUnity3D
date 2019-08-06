using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BClicked : MonoBehaviour
{
    public bool ButtonOn = false;
    public Button MyButton;
    public Sprite Clicked;

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
}
