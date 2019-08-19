﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class Generator : MonoBehaviour
{
    public bool ButtonOn = false;
    public GameObject PanelA;
    public Text NazwaBohatera;
    public Button ButtonPanelA;
    public List<string> Units;
    public List<int> Costs;
    public int NumberOfUnit;
    public List<InputField> inputFields;
    public List<int> UnitsAmount;
    public List<Button> buttons;
    public List<Button> defaultButtons;
    public Sprite defaultS;
    public PanelArmii PanelArmii;
    public List<Text> CostText;
    public List<GameObject> RacePanels;

    public void Start()
    {



    }

    public void CallMe()
    {
        Units = new List<string>();
        UnitsAmount = new List<int>();
        Costs = new List<int>();
        for (int i = 0; i < 7; i++)
        {
            Units.Add(null);
            UnitsAmount.Add(0);
            Costs.Add(0);
        }
        
    }
    public void Nowy()
    {

        NazwaBohatera.text = "";
        foreach ( InputField inputField in inputFields)
        {
            inputField.text = "";
        }

        int i = 0;

        foreach (Button button in buttons)
        {
        
            SetButtonNew(button);
            i++;
           
        }

    }
    public void Wczytaj()
    {

        NazwaBohatera.text = PanelArmii.LoadedBuild.NazwaBohatera;// PlayerPrefs.GetString("NazwaBohatera");
        if (NazwaBohatera.text == "Biały Toster")
        {
            RacePanels[0].SetActive(true);
            RacePanels[1].SetActive(false);
            RacePanels[2].SetActive(false);
        }
        if (NazwaBohatera.text == "Czerwony Toster")
        {
            RacePanels[0].SetActive(false);
            RacePanels[1].SetActive(true);
            RacePanels[2].SetActive(false);
        }
        if (NazwaBohatera.text == "Zielony Toster")
        {
            RacePanels[0].SetActive(false);
            RacePanels[1].SetActive(false);
            RacePanels[2].SetActive(true);
        }
        int i = 0;
        foreach (int t in PanelArmii.LoadedBuild.NoUnits)
        {
     
            inputFields[i].text = t.ToString();
            UnitsAmount[i] = t;
            i++;
        }
        i = 0;
        Costs = PanelArmii.LoadedBuild.Costs;
      /*  foreach (int t in PanelArmii.LoadedBuild.Costs)
        {

        }*/

        foreach (string t in PanelArmii.LoadedBuild.Units)
        {
       
            if (PanelArmii.ListOfUnits.Contains(t))
            {
              

                ColorBlock cb = defaultButtons[1].colors;
                buttons[i].colors = cb;
                for (int d = 0; d < PanelArmii.ListOfUnits.Count; d++)
                {

                    if (PanelArmii.ListOfUnits[d] == t)
                    {
                        buttons[i].gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(PanelArmii.ListOfImages[d]);
                
                    }

                    Units[i] = t;
                }
            }
            else
            {
                SetButtonNew(buttons[i]);
            }
            i++;
        }
    }
    private void Update()
    {
     
    }


    public void selectW()
    {
        PlayerPrefs.SetInt("which", 1);
        PlayerPrefs.SetString("NazwaBohatera", "Biały Toster");

        RacePanels[0].SetActive(true);
        RacePanels[1].SetActive(false);
        RacePanels[2].SetActive(false);
        NazwaBohatera.text = "Biały Toster";
        
    }
    public void selectR()
    {
        PlayerPrefs.SetInt("which", 2);
        PlayerPrefs.SetString("NazwaBohatera", "Czerwony Toster");
        RacePanels[0].SetActive(false);
        RacePanels[1].SetActive(true);
        RacePanels[2].SetActive(false);
        NazwaBohatera.text = "Czerwony Toster";
    }
    public void selectN()
    {
        PlayerPrefs.SetInt("which", 3);
        PlayerPrefs.SetString("NazwaBohatera", "Zielony Toster");
        RacePanels[0].SetActive(false);
        RacePanels[1].SetActive(false);
        RacePanels[2].SetActive(true);
        NazwaBohatera.text = "Zielony Toster";
    }
    

    public void ReadAllNumbers()
    {
        for(int i=0; i < 7; i++)
        {
           // Debug.LogError(i);
            if (Units[i] != null)
                UnitsAmount[i] = int.Parse("0"+inputFields[i].text);
        }
       
    }

    public void EnablePanel(Button bt)
    {
    
        PanelA.SetActive(true);
        ButtonPanelA = bt;
    }
    public void SaveNumber(int no)
    {

        NumberOfUnit = no;
    }

    public void SetButtonNew(Button b)
    {
       
        b.gameObject.GetComponent<Image>().sprite = defaultButtons[0].gameObject.GetComponent<Image>().sprite;
        b.GetComponentInChildren<Text>().text = "+";
        ColorBlock cb = defaultButtons[0].colors;
        b.colors = cb;
    }
    public void ChooseUnit(Button bt)
    {
        ButtonPanelA.gameObject.GetComponent<Image>().sprite = bt.gameObject.GetComponent<Image>().sprite;
        ColorBlock cb = bt.colors;
        ButtonPanelA.colors = cb;
        ButtonPanelA.GetComponentInChildren<Text>().text = "";
        PanelA.SetActive(false);

    }
    public void SaveUnit(string t)
    {

        Units[NumberOfUnit] = t;


    }
    public void SaveCost(int t)
    {

        Costs[NumberOfUnit] = t;


    }
    


}
