using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using static PanelArmii;

public class SelectMenu : MonoBehaviour
{

    public List<Button> YourButtons;
    public List<Button> EnemyButtons;
    public List<Button> BigButtons;
    public List<Button> RemoveButtons;
    Sprite test;
    public List<Image> Imagess;
    public List<Image> EImagess;
    public List<Image> BigImagess;
    public List<string> ListOfHeroes = new List<string>(new string[] { "Biały Toster", "Czerwony Toster", "Zielony Toster" });
    public List<string> ListOfImages = new List<string>(new string[] { "Sprites/wT1", "Sprites/redT2", "Sprites/gT2" });
    public GameObject PanelBuilds,ThisPanel;
    public Button ButtonBuilds, ThisButton;
    void Start()
    {
       test = BigImagess[0].sprite;

        if (PlayerPrefs.HasKey("YourArmy")){
            string path = Application.persistentDataPath + "/build" + PlayerPrefs.GetInt("YourArmy").ToString() + ".d";
            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file = File.OpenRead(path);
                BuildG buildG = (BuildG)formatter.Deserialize(file);
                file.Close();
                for (int j = 0; j < ListOfHeroes.Count; j++)
                {
                    if (ListOfHeroes[j] == buildG.NazwaBohatera)
                    {

                      
                        Sprite hero = Resources.Load<Sprite>(ListOfImages[j]);
                        BigImagess[0].sprite = hero;
                        BigImagess[0].GetComponentInChildren<Text>().text = "";
                        RemoveButtons[0].gameObject.SetActive(true);
                        j = 100;
                    }
                }

            }
            else PlayerPrefs.DeleteKey("YourArmy");
        }
            if (PlayerPrefs.HasKey("EnemyArmy")){
            string path = Application.persistentDataPath + "/build" + PlayerPrefs.GetInt("EnemyArmy").ToString() + ".d";

            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file = File.OpenRead(path);
                BuildG buildG = (BuildG)formatter.Deserialize(file);
                file.Close();
                for (int j = 0; j < ListOfHeroes.Count; j++)
                {
                    if (ListOfHeroes[j] == buildG.NazwaBohatera)
                    {
                
                  
                        Sprite hero = Resources.Load<Sprite>(ListOfImages[j]);
                        BigImagess[1].sprite = hero;
                        BigImagess[1].GetComponentInChildren<Text>().text = "";
                        j = 100;
                        RemoveButtons[1].gameObject.SetActive(true);
                    }
                }
               
            }
            else PlayerPrefs.DeleteKey("EnemyArmy");
        }
    }


    public void CheckYours()
    {
        string path = Application.persistentDataPath + "/build1.d";

        for (int i = 0; i < 10; i++)
        {
            path = Application.persistentDataPath + "/build" + i.ToString() + ".d";

            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file = File.OpenRead(path);
                BuildG buildG = (BuildG)formatter.Deserialize(file);
                file.Close();
                for (int j = 0; j < ListOfHeroes.Count; j++)
                {
                    if (ListOfHeroes[j] == buildG.NazwaBohatera)
                    {

                        Sprite hero = Resources.Load<Sprite>(ListOfImages[j]);
                        Imagess[i].sprite = hero;
                        j = 100;
                       
                    }
                }
                YourButtons[i].gameObject.SetActive(true);
                YourButtons[i].GetComponentInChildren<Text>().text=" ";
                Imagess[i].gameObject.SetActive(true);
                Imagess[i].color = Color.white;
            }
            else
            {
                YourButtons[i].GetComponent<Button>().onClick.AddListener(AddBuild);
                //Imagess[i].gameObject.SetActive(false);
            }
        }

    }

    public void AddBuild()
    {
        ThisPanel.SetActive(false);
        PanelBuilds.SetActive(true);
        ButtonBuilds.GetComponentInChildren<Text>().color = Color.white;
        ThisButton.GetComponentInChildren<Text>().color = Color.black;
    }

    public void CheckEnemy()
    {
        string path = Application.persistentDataPath + "/build1.d";

        for (int i = 0; i < 10; i++)
        {
            path = Application.persistentDataPath + "/build" + i.ToString() + ".d";

            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file = File.OpenRead(path);
                BuildG buildG = (BuildG)formatter.Deserialize(file);
                file.Close();
                for (int j = 0; j < ListOfHeroes.Count; j++)
                {
                    if (ListOfHeroes[j] == buildG.NazwaBohatera)
                    {
                     
                        Sprite hero = Resources.Load<Sprite>(ListOfImages[j]);
                        EImagess[i].sprite = hero;
                        j = 100;
                    }
                }
                EnemyButtons[i].gameObject.SetActive(true);
                EnemyButtons[i].GetComponentInChildren<Text>().text=" ";
                EImagess[i].gameObject.SetActive(true);
                EImagess[i].color = Color.white;
            }
            else
            {
                EnemyButtons[i].GetComponent<Button>().onClick.AddListener(AddBuild);
                //EImagess[i].gameObject.SetActive(false);
            }
        }

    }
    public Sprite sprite;
    public void PickArmyYours(int i)
    {
        string path = Application.persistentDataPath + "/build" + i.ToString() + ".d";

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.OpenRead(path);
            BuildG buildG = (BuildG)formatter.Deserialize(file);
            file.Close();
            for (int j = 0; j < ListOfHeroes.Count; j++)
            {
                if (ListOfHeroes[j] == buildG.NazwaBohatera)
                {
                    PlayerPrefs.SetInt("YourArmy", i);
                    Debug.Log("tutaj");
                    Sprite hero = sprite;
                    BigImagess[0].sprite = hero;
                    BigImagess[0].GetComponentInChildren<Text>().text = "";
                    RemoveButtons[0].gameObject.SetActive(true);
                    Debug.Log(Application.persistentDataPath + ListOfImages[j]);
                    j = 100;
                }
            }
            
        }
       
    }
    public void RemoveHero1(int i)
    {
        BigImagess[i].sprite = test;
        BigImagess[i].GetComponentInChildren<Text>().text = "+";
        if (i == 0) PlayerPrefs.DeleteKey("YourArmy");
        else PlayerPrefs.DeleteKey("EnemyArmy");

    }
    public void PickArmyEnemy(int i)
    {
        string path = Application.persistentDataPath + "/build" + i.ToString() + ".d";

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.OpenRead(path);
            BuildG buildG = (BuildG)formatter.Deserialize(file);
            file.Close();
            for (int j = 0; j < ListOfHeroes.Count; j++)
            {
                if (ListOfHeroes[j] == buildG.NazwaBohatera)
                {
                    PlayerPrefs.SetInt("EnemyArmy", i);
                    Debug.Log("tutaj");
                    Sprite hero = sprite;
                    //Sprite hero = Resources.Load<Sprite>(ListOfImages[j]);
                    BigImagess[1].sprite = hero;
                    BigImagess[1].GetComponentInChildren<Text>().text = "";
                    Debug.Log(Application.persistentDataPath + ListOfImages[j]);
                    j = 100;
                    RemoveButtons[1].gameObject.SetActive(true);
                }
            }

        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
