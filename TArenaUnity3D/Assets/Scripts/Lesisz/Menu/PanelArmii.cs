using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
 
public class PanelArmii : MonoBehaviour
{
    // Start is called before the first frame update
    public List<Button> AdditionalButtons;
    public List<Text> texts;
    public List<Image> Imagess;
    public List<string> ListOfHeroes = new List<string>(new string[] { "Biały Toster", "Czerwony Toster", "Zielony Toster" });
    public List<string> ListOfImages = new List<string>(new string[] { "Sprites/wT1", "Sprites/redT2", "Sprites/gT2" });
    [System.Serializable]
    public class BuildG
    {
        public int hero;
        public string NazwaBohatera;
    }
    void Start()
    {
 



    }
    public void sprawdz()
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

                for (int j=0; j < ListOfHeroes.Count; j++)
                {
                    if (ListOfHeroes[j] == buildG.NazwaBohatera)
                    {
                        Debug.Log("tutaj");
                        Sprite hero = Resources.Load<Sprite>(ListOfImages[j]);
                        Imagess[i].sprite = hero;
                        Debug.Log(Application.persistentDataPath + ListOfImages[j]);

                        j = 100;
                    }
                }
               
                AdditionalButtons[i*3].gameObject.SetActive(false);
                    AdditionalButtons[i*3+1].gameObject.SetActive(true);
                    AdditionalButtons[i*3+2].gameObject.SetActive(true);
               
                    Imagess[i].gameObject.SetActive(true);
              
                

            }
            else
            {
                AdditionalButtons[i * 3].gameObject.SetActive(true);
                AdditionalButtons[i * 3 + 1].gameObject.SetActive(false);
                AdditionalButtons[i * 3 + 2].gameObject.SetActive(false);
                Imagess[i].gameObject.SetActive(false);
            }
        }
    
    }

    public void RemoveBuild(string i)
    {
        string path = Application.persistentDataPath + "/build"+i+".d";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        sprawdz();
    }
    private void OnEnable()
    {
        sprawdz();
    }
    public void BuildNumber(string i)
    {
        PlayerPrefs.SetString("BuildNumber", i);
        WczytajPlik(i);
    }
    public void WczytajPlik(string i)
    {
        string path = Application.persistentDataPath + "/build" + i + ".d";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.OpenRead(path);
            BuildG buildG = (BuildG)formatter.Deserialize(file);
            file.Close();
            PlayerPrefs.SetString("NazwaBohatera", buildG.NazwaBohatera);
        }
    }


    public void SaveBuild()
    { 

        BuildG Build = new BuildG();

        if (PlayerPrefs.HasKey("which"))
        {
            Build.hero = PlayerPrefs.GetInt("which");
            Build.NazwaBohatera = PlayerPrefs.GetString("NazwaBohatera");
            BinaryFormatter formatter = new BinaryFormatter();
            string path = Application.persistentDataPath + "/build"+PlayerPrefs.GetString("BuildNumber")+".d";
            FileStream file = File.Create(path);
            formatter.Serialize(file, Build);
            file.Close();
            Debug.Log(path);
            sprawdz();
        }
    }
    // Update is called once per frame
    void Update()
    {

    }
}
