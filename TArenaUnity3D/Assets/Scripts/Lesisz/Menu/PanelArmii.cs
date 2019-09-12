using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;
 
public class PanelArmii : MonoBehaviour
{
    public string Buildnr;
    public Generator generator;
    public List<Button> AdditionalButtons;
    public List<Text> texts;
    public Button back;
    public List<Image> Imagess;
    public List<string> ListOfHeroes;// = new List<string>(new string[] { "Biały Toster", "Czerwony Toster", "Zielony Toster" });
    public List<string> ListOfImages;// = new List<string>(new string[] { "Sprites/wT1", "Sprites/redT2", "Sprites/gT2", "Sprites/Szaman1" });
    public List<string> ListOfUnits;// = new List<string>(new string[] { "TosterDPS", "TosterTANK", "TosterHEAL", "Szaman", "zodyn"});
    public BuildG LoadedBuild;
    public Sprite sprite;
    [System.Serializable]
    public class BuildG
    {
        public int hero;
        public string NazwaBohatera;
        public List<string> Units;
        public List<int> NoUnits;
        public List<int> Costs;

     public   BuildG()
        {
            Units = new List<string>();
            NoUnits = new List<int>();
            Costs = new List<int>();
    }
    }
    void Start()
    {
      

    }




    public void LoadListOfUnits()
    {

        ListOfUnits = new List<string>();
            //TODO: VALIDATE SCHEMA/XML
            TextAsset textAsset = (TextAsset)Resources.Load("data/Units");
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(textAsset.text);
            XmlNodeList nodes = xmldoc.SelectNodes("Units/Unit/Name");
            foreach (XmlNode node in nodes)
            {
            ListOfUnits.Add(node.InnerText);
          //  Debug.LogError(node.InnerText);
            }
           
    }
    public void LoadListOfImages()
    {
        ListOfImages = new List<string>();
        //TODO: VALIDATE SCHEMA/XML
        TextAsset textAsset = (TextAsset)Resources.Load("data/Units");
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.LoadXml(textAsset.text);
        XmlNodeList nodes = xmldoc.SelectNodes("Units/Unit/Sprite");
        foreach (XmlNode node in nodes)
        {
            ListOfImages.Add(node.InnerText);
          //  Debug.LogError(node.InnerText);
        }
    }

    public List<string> GetList()
    {
        return ListOfHeroes;
    }
    public void sprawdz()
    {
        string path = Application.persistentDataPath + "/build1.d";
    
        for (int i = 0; i < 10; i++)
        {
            path = Application.persistentDataPath + "/build" + i.ToString() + ".d";
            Debug.Log(path);
            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file = File.OpenRead(path);
                BuildG buildG = (BuildG)formatter.Deserialize(file);

                file.Close();


                Debug.Log("Jest build " + i);
                for (int j=0; j < ListOfHeroes.Count; j++)
                {
                    if (ListOfHeroes[j] == buildG.NazwaBohatera)
                    {

                        Sprite hero = sprite;
                        Imagess[i].sprite = hero;
             

                        j = 100;
                    }
                }
               
             //   AdditionalButtons[i*2].gameObject.SetActive(false);
           //        AdditionalButtons[i*2+1].gameObject.SetActive(true);
//                    AdditionalButtons[i*2+2].gameObject.SetActive(true);
               
                    Imagess[i].gameObject.SetActive(true);
                    AdditionalButtons[i].onClick.RemoveListener(AddBuild);
                    AdditionalButtons[i].onClick.AddListener(EditBuild);
                    
              
                

            }
            else
            {
                Debug.Log("nie ma buildu nr " + i);
            //    AdditionalButtons[i * 2].gameObject.SetActive(true);
            //    AdditionalButtons[i * 2 + 1].gameObject.SetActive(false);
            //    AdditionalButtons[i * 2 + 2].gameObject.SetActive(false);
                Imagess[i].gameObject.SetActive(false);
                AdditionalButtons[i].onClick.RemoveListener(EditBuild);
                AdditionalButtons[i].onClick.AddListener(AddBuild);
                
            }
        }
    
    }


public void Test()
{Debug.Log("TEST");}
    //onClick.AddListener(Function);

public void EditBuild()
{
    generator.CallMe();
    generator.Wczytaj();
}


public void AddBuild()
{
    generator.CallMe();
    generator.Nowy();
}

    public void RemoveBuild()
    {
        string path = Application.persistentDataPath + "/build"+Buildnr+".d";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        sprawdz();
    }
    private void OnEnable()
    {
        LoadListOfUnits();
        LoadListOfImages();
        sprawdz();
    }
    public void BuildNumber(string i)
    {
        PlayerPrefs.SetString("BuildNumber", i);
        WczytajPlik(i);
        Debug.Log("XDDDDDDDDDDDDDDDDDDDDDDDD:  " + i);
        Buildnr = i;
    }

public void SetBuildnr(string x)
{
    Buildnr = x;
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
            LoadedBuild = buildG;
        }
    }


    public void SaveBuild()
    { 

        BuildG Build = new BuildG();

        if (PlayerPrefs.HasKey("which"))
        {
            Build.hero = PlayerPrefs.GetInt("which");
            Build.NazwaBohatera = PlayerPrefs.GetString("NazwaBohatera");
            Build.Units = generator.Units;
          
            Build.NoUnits = generator.UnitsAmount;
            Build.Costs = generator.Costs;
            BinaryFormatter formatter = new BinaryFormatter();
            string path = Application.persistentDataPath + "/build"+PlayerPrefs.GetString("BuildNumber")+".d";
            FileStream file = File.Create(path);
            formatter.Serialize(file, Build);
            file.Close();
           // Debug.Log(path);
            sprawdz();
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            back.onClick.Invoke();
        }
    }
}
