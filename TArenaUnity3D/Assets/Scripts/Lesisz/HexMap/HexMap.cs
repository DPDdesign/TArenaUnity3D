
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PanelArmii;
using HPath;
public class HexMap : MonoBehaviour,    IQPathWorld
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
    public bool AnimationIsPlaying=false;
    private HashSet<TosterHexUnit> tosters;
    private List<TosterHexUnit> tostersList;
    private Dictionary<TosterHexUnit, GameObject> tostertoGameObjectMap;
    private Dictionary<GameObject, HexClass> gameObjectToHexMap;
    void Start()
    {
        LoadArmy();
        GenerateMap();
    
        GenerateToster(2,5, PlayerPrefs.GetInt("LewyToster"));
        
        GenerateToster(5, 5,PlayerPrefs.GetInt("PrawyToster"));
        
    }


    private void Update()
    {
   

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(tosters!=null)
            {
                foreach(TosterHexUnit u in tosters)
                {
                    u.DoMove();
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (tosters != null)
            {
                foreach (TosterHexUnit u in tosters)
                {
                    u.DUMMY_PATHING_FUNCTION();
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (tostersList != null)
            {
                tostersList[0].move = true;
    
                
            }
        }


    }


    public HexClass GetHexAt(int x, int y)
    {
        if(hexes == null)
        {
            Debug.LogError("Hexes not found");
        }
       
        return hexes[x %19 , y%11];
    }

    public Vector3 GetHexPos(int q, int r)
    {
        HexClass h = GetHexAt(q, r);
        return h.Position();

    }

    int mapHeight = 20;
    int mapWidth = 20;

    
    public IEnumerator DoUnitMoves(TosterHexUnit u)
    {
        // Is there any reason we should check HERE if a unit should be moving?
        // I think the answer is no -- DoMove should just check to see if it needs
        // to do anything, or just return immediately.

      
            while (u.DoMove())
            {
         
                while (u.tosterView.AnimationIsPlaying) {  yield return null; }
            }

        

    }
    
    void GenerateMap()     
    {
        hexes = new HexClass[mapHeight, mapWidth];
        hextoGameObjectMap = new Dictionary<HexClass, GameObject>();
        gameObjectToHexMap = new Dictionary<GameObject, HexClass>();
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
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length-1)];

                HexGo.name = string.Format("HEX: {0}, {1}", col, row);
                gameObjectToHexMap[HexGo] = h;
                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}\n {2}", col, row, h.Tosters.Count);
                hexes[col, row] = h;
                hextoGameObjectMap.Add(h, HexGo);
                h.MyHex = HexGo;

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
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length - 1)];
                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}\n {2}", col, row, h.Tosters.Count);
                hexes[col, row] = h;
                hextoGameObjectMap[h] = HexGo;
                gameObjectToHexMap[HexGo] = h;
                h.MyHex = HexGo;
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
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length - 1)];
                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}\n {2}", col, row, h.Tosters.Count);
                hexes[col, row] = h;
                hextoGameObjectMap[h] = HexGo;
                gameObjectToHexMap[HexGo] = h;
                h.MyHex = HexGo;
            }
        }
        //StaticBatchingUtility.Combine(this.gameObject.GetComponentInChildren<MeshFilter>().gameObject);


    }

    public HexClass GetHexFromGameObject(GameObject hexGO)
    {
        if (gameObjectToHexMap.ContainsKey(hexGO))
        {
            return gameObjectToHexMap[hexGO];
        }

        return null;
    }

    public GameObject GetObjectFromHex(HexClass h)
    {
        if (hextoGameObjectMap.ContainsKey(h))
        {
            return hextoGameObjectMap[h];
        }

        return null;
    }



    void GenerateToster(int i , int j, int k)
    {
        HexClass TosterSpawn = GetHexAt(i, j);
       
        if (tosters == null)
        {
            tosters = new HashSet<TosterHexUnit>();
            tostertoGameObjectMap = new Dictionary<TosterHexUnit, GameObject>();
        }
        if (tostersList == null)
        {
            tostersList = new List<TosterHexUnit>();
           
        }


        GameObject HexGo = hextoGameObjectMap[TosterSpawn];
        TosterHexUnit Toster = new TosterHexUnit(i, j, TosterSpawn.Position(), HexGo, TostersPrefabs[k]);
        Toster.SetHex(TosterSpawn);
        GameObject TosterGo = (GameObject)Instantiate(
            Toster.TosterPrefab,
           // TosterSpawn.Position(),
            Toster.Position(TostersPrefabs[0]),
            Quaternion.identity,
            HexGo.transform
            );

        TosterGo.AddComponent<TosterView>();
        Toster.OnTosterMoved += TosterGo.GetComponent<TosterView>().OnTosterMoved;
        Toster.tosterView = TosterGo.GetComponent<TosterView>();
        HexGo.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}\n {2}", i, j, TosterSpawn.Tosters.Count);
        tostersList.Add(Toster);

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




    public HexClass[] GetHexesWithinRadiusOf(HexClass centerhex, int radius)
    {
        List<HexClass> results = new List<HexClass>(); 
        for (int dx = - radius; dx<radius+1; dx++)
        {
            for (int dy = Mathf.Max(-radius, -dx - radius); dy < Mathf.Min(radius, -dx + radius) + 1; dy++)
            {
                if (0 <= centerhex.C + dx && 0 <= centerhex.R + dy && 19 >= centerhex.C + dx && 19 >= centerhex.R + dy)
                {
                  
                    results.Add(hexes[centerhex.C + dx, centerhex.R + dy]);
                }
            }
        }
        return results.ToArray();
    }


   public void Highlight(int q, int r, int range, float centerHeight = .8f)
    {
        HexClass centerHex = GetHexAt(q, r);

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, range);
        
        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null)
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {
                    h.Highlight = true;
                }
            }
        }
        UpdateHexVisuals();
    }

    public void HighlightWithPath(TosterHexUnit hh)
    {
        HexClass centerHex = hh.Hex;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, hh.MovmentSpeed);
       
        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null && hh.IsPathAvaible(h))
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {
                    h.Highlight = true;
                }
            }
        }
        UpdateHexVisuals();
    }

    public void unHighlight(int q, int r, int range, float centerHeight = .8f)
    {
        HexClass centerHex = GetHexAt(q, r);

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, range);

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null)
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {
                    h.Highlight = false;
                }
            }
          
        }
        UpdateHexVisuals();
    }



    public void UpdateHexVisuals()
    {
        for (int column = 0; column < 20; column++)
        {
            for (int row = 0; row < 20; row++)
            {
                HexClass h = hexes[column, row];
                if (h != null)
                {
                    GameObject hexGO = hextoGameObjectMap[h];

                    //  HexComponent hexComp = hexGO.GetComponentInChildren<HexComponent>();
                    MeshRenderer mr = hexGO.GetComponentInChildren<MeshRenderer>();
                    MeshFilter mf = hexGO.GetComponentInChildren<MeshFilter>();


                    if (h.Highlight == true)
                    {
                        mr.material = HexMaterials[1];
                    }
                    else
                    {
                        mr.material = HexMaterials[0];

                    }
                }


            }
        }
    }
}
