using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PanelArmii;

public class HexMap : MonoBehaviour
{
    public List<string> ListOfHeroes = new List<string>(new string[] { "Biały Toster", "Czerwony Toster", "Zielony Toster" });
    // Start is called before the first frame update
    void Start()
    {
        LoadArmy();
        GenerateMap();
        GenerateToster(2,5, PlayerPrefs.GetInt("LewyToster"));
        GenerateToster(16, 5,PlayerPrefs.GetInt("PrawyToster"));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
        }
    }
    public GameObject HexPrefab;
    public List<GameObject> Tosters;
    public Material[] HexMaterials;
    // Update is called once per frame

    private HexClass[,] hexes;
    private Dictionary<HexClass, GameObject> hextoGameObjectMap;

    public HexClass GetHexAt(int x, int y)
    {
        if(hexes == null)
        {
            Debug.LogError("Hexes not found");
        }
        return hexes[x %20, y%20];
    }



    void GenerateMap()     
    {
        hexes = new HexClass[20, 20];
        hextoGameObjectMap = new Dictionary<HexClass, GameObject>();
        for (int col = 0; col < 5; col++)
        {
            for (int row = 11-((col + 1) * 2); row < 11; row++)
            {

                HexClass h = new HexClass(col, row);
              
                GameObject HexGo = (GameObject)Instantiate(
                    HexPrefab,
                    h.Position(),
                    Quaternion.identity,
                    this.transform
                    );
                MeshRenderer mr = HexGo.GetComponentInChildren<MeshRenderer>();
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length)];

                HexGo.name = string.Format("HEX: {0}, {1}", col, row);

                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}", col, row);
                hexes[col, row] = h;
                hextoGameObjectMap.Add(h, HexGo);

            }

        }


            for (int col= 5; col<15;col++)
        {
            for (int row = 0; row < 11; row++)
            {
                HexClass h = new HexClass(col, row);
                hexes[col, row] = h;
                GameObject HexGo = (GameObject) Instantiate(
                    HexPrefab,
                    h.Position(),            
                    Quaternion.identity,
                    this.transform
                    ) ;
                HexGo.name = string.Format("HEX: {0}, {1}", col, row);
                MeshRenderer mr = HexGo.GetComponentInChildren<MeshRenderer>();
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length)];
                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}", col, row);
                hexes[col, row] = h;
                hextoGameObjectMap[h] = HexGo;
            }
        }

        for (int col = 15; col < 20; col++)
        {
            for (int row = 0 ; row < ((19-col) * 2+1); row++) 
            {
                HexClass h = new HexClass(col, row);
                hexes[col, row] = h;
                GameObject HexGo = (GameObject)Instantiate(
                    HexPrefab,
                    h.Position(),
                    Quaternion.identity,
                    this.transform
                    );
                HexGo.name = string.Format("HEX: {0}, {1}", col, row);
                MeshRenderer mr = HexGo.GetComponentInChildren<MeshRenderer>();
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length)];
                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}", col, row);
                hexes[col, row] = h;
                hextoGameObjectMap[h] = HexGo;
            }
        }
        StaticBatchingUtility.Combine(this.gameObject);
    }

    


    void GenerateToster(int i , int j, int k)
    {
        HexClass TosterSpawn = GetHexAt(i, j);
        TosterClass Toster = new TosterClass(i, j, TosterSpawn.Position());

        GameObject HexGo = (GameObject)Instantiate(
            Tosters[k],
            Toster.Position(Tosters[0]),
            Quaternion.identity,
            this.transform
            );

    }


    public void LoadArmy()
    {
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

                    PlayerPrefs.SetInt("LewyToster", j);
               
                    j = 100;
                }
            }

        }
        path = Application.persistentDataPath + "/build" + PlayerPrefs.GetInt("EnemyArmy").ToString() + ".d";
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

                    PlayerPrefs.SetInt("PrawyToster", j);

                    j = 100;
                }
            }

        }
    }
}
