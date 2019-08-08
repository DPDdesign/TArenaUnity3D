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
    public GameObject HexPrefab;
    public List<GameObject> TostersPrefabs;
    public GameObject TosterUnit;
    public Material[] HexMaterials;
    // Update is called once per frame

    private HexClass[,] hexes;
    private Dictionary<HexClass, GameObject> hextoGameObjectMap;
    // Start is called before the first frame update

    private HashSet<TosterHex> tosters;
    private Dictionary<TosterHex, GameObject> tostertoGameObjectMap;

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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(tosters!=null)
            {
                foreach(TosterHex u in tosters)
                {
                    u.DoTurn();
                }
            }
        }
    }


    public HexClass GetHexAt(int x, int y)
    {
        if(hexes == null)
        {
            Debug.LogError("Hexes not found");
        }
        return hexes[x %20, y%20];
    }

    public Vector3 GetHexPos(int q, int r)
    {
        HexClass h = GetHexAt(q, r);
        return h.Position();

    }



    void GenerateMap()     
    {
        hexes = new HexClass[20, 20];
        hextoGameObjectMap = new Dictionary<HexClass, GameObject>();
        for (int col = 0; col < 5; col++)
        {
            for (int row = 11-((col + 1) * 2); row < 11; row++)
            {

                HexClass h = new HexClass(this ,col, row);
              
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
                HexClass h = new HexClass(this,col, row);
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
                HexClass h = new HexClass(this, col, row);
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
       
        if (tosters == null)
        {
            tosters = new HashSet<TosterHex>();
            tostertoGameObjectMap = new Dictionary<TosterHex, GameObject>();
        }



        GameObject HexGo = hextoGameObjectMap[TosterSpawn];
        TosterHex Toster = new TosterHex(i, j, TosterSpawn.Position(), HexGo);
        Toster.SetHex(TosterSpawn);
        GameObject TosterGo = (GameObject)Instantiate(
            TostersPrefabs[k],
           // TosterSpawn.Position(),
            Toster.Position(TostersPrefabs[0]),
            Quaternion.identity,
            HexGo.transform
            );

        TosterGo.AddComponent<TosterView>();
        Toster.OnTosterMoved += TosterGo.GetComponent<TosterView>().OnTosterMoved;


        tosters.Add(Toster);
        tostertoGameObjectMap[Toster] = TosterGo;
      
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
