using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
    public List<string> ListOfHeroes;
    public List<string> ListOfImages;
    public List<string> ListOfUnits;
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
        ListOfUnits = DataMapper.Instance.GetAllUnitNames();
    }
    public void LoadListOfImages()
    {
        ListOfImages = DataMapper.Instance.GetAllUnitSpriteReferences();
    }

    public List<string> GetList()
    {
        return ListOfHeroes;
    }
    public void sprawdz()
    {
        string path = DataMapper.Instance.GetBuildFilePath(1);
    
        for (int i = 0; i < 10; i++)
        {
            path = DataMapper.Instance.GetBuildFilePath(i);
            Debug.Log(path);
            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file = File.OpenRead(path);
                BuildG buildG = (BuildG)formatter.Deserialize(file);

                file.Close();


                Debug.Log("Jest build " + i);
                Sprite hero = sprite;
                Imagess[i].sprite = hero;


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
        string path = DataMapper.Instance.GetBuildFilePath(Buildnr);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        sprawdz();
    }
    private void OnEnable()
    {
        FindObjectOfType<OverlayMainMenu>().back = back;
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
        string path = DataMapper.Instance.GetBuildFilePath(i);
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


          //  Build.hero = PlayerPrefs.GetInt("which");
         //   Build.NazwaBohatera = PlayerPrefs.GetString("NazwaBohatera");
            Build.Units = generator.Units;
          
            Build.NoUnits = generator.UnitsAmount;
            Build.Costs = generator.Costs;
            BinaryFormatter formatter = new BinaryFormatter();
            string path = DataMapper.Instance.GetBuildFilePath(PlayerPrefs.GetString("BuildNumber"));
            FileStream file = File.Create(path);
            formatter.Serialize(file, Build);
            file.Close();
           // Debug.Log(path);
            sprawdz();
        
    }
    // Update is called once per frame
    void Update()
    {

    }
}
