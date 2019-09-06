
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PanelArmii;
using HPath;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using cakeslice;

public class HexMap : MonoBehaviourPunCallbacks, IQPathWorld
{
    public List<string> ListOfHeroes = new List<string>(new string[] { "Biały Toster", "Czerwony Toster", "Zielony Toster" });
    public GameObject HexPrefab;
    public List<GameObject> TostersPrefabs;
    public List<TeamClass> Teams;
    public GameObject TosterUnit;
    public GameObject Projectile;
    public Material[] HexMaterials;
    // Update is called once per frame
    private HexClass[,] hexes;
    List<HexClass> allhexes;
    private Dictionary<HexClass, GameObject> hextoGameObjectMap;
    // Start is called before the first frame update
    public bool AnimationIsPlaying = false;
    private HashSet<TosterHexUnit> tosters;
    private List<TosterHexUnit> tostersList;
    private Dictionary<TosterHexUnit, GameObject> tostertoGameObjectMap;
    private Dictionary<GameObject, HexClass> gameObjectToHexMap;
    public bool isTraped = false;
    public string TypeOfTrap = "";
    public PlayerP playerPrefab;
    public PlayerP LocalPlayer;
    public PanelArmii.BuildG buildG1, buildG2;
    GameObject bullet;
    public bool isCreated=false;
    bool ready = false;
    bool DidIGetOtherBuild = false;
    private static HexMap instance;

    public static HexMap Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HexMap>();
            }

            return instance;
        }
    }
    void Start()
    {
        
        if (PlayerPrefs.GetInt("Multi") == 1)
        {
            buildG1 = new BuildG();
            buildG2 = new BuildG();
            if (PhotonNetwork.LocalPlayer.IsMasterClient)
            {

                TeamClass Team = new TeamClass();
                Hashtable t = new Hashtable();
                Team.ThisTeamNO = PlayerPrefs.GetInt("YourArmy");
                Team.WczytajPlik();

                buildG1 = Team.buildG;
                //        Team.ThisTeamNO = PlayerPrefs.GetInt("EnemyArmy");
                //       Team.WczytajPlik();

                //                buildG2 = Team.buildG;
                //   PhotonNetwork.CurrentRoom.GetPlayer(1).SetCustomProperties(t);
            }
            else  //(PhotonNetwork.LocalPlayer == PhotonNetwork.CurrentRoom.GetPlayer(2))
            {
                TeamClass Team = new TeamClass();
                //ASK MASTER FOR HIS BUILD + SEND HIM  YOUR BUILD
                Team.ThisTeamNO = PlayerPrefs.GetInt("YourArmy");/// THIS IS YOUR BUILD - NOW HOW TO SHARE IT?

                Team.WczytajPlik();
                Debug.LogError("THIS IS YOUR BUILD - NOW HOW TO SHARE IT?");
                buildG2 = Team.buildG;
                photonView.RPC("UnitsAmount", RpcTarget.Others, new object[] { buildG2.NoUnits[0], buildG2.NoUnits[1], buildG2.NoUnits[2], buildG2.NoUnits[3], buildG2.NoUnits[4], buildG2.NoUnits[5], buildG2.NoUnits[6] });
                photonView.RPC("UnitsNames", RpcTarget.Others, new object[] { buildG2.Units[0], buildG2.Units[1], buildG2.Units[2], buildG2.Units[3], buildG2.Units[4], buildG2.Units[5], buildG2.Units[6] });

              //  DidIGetOtherBuild = true;

            }
            if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
            {
                if (DidIGetOtherBuild == false)
                {
                    photonView.RPC("IntToSend", RpcTarget.Others, new object[] { 5 });
                    // DO NOTHING / WAIT FOR IT -> GO TO UPDATE()
                }
               // DO I HAVE ALL BUILDS READY? BOOL DID I GET OTHERS BUILD
            }

        }
        else
        {
            Debug.LogError("here");
            TeamClass Team = new TeamClass();
            Team.ThisTeamNO = PlayerPrefs.GetInt("YourArmy");
            Team.WczytajPlik();

            buildG1 = Team.buildG;
            Team.ThisTeamNO = PlayerPrefs.GetInt("EnemyArmy");
            Team.WczytajPlik();

            buildG2 = Team.buildG;
            CreateWorld();
        }

    }
    [PunRPC]
    void InitilizeMyPlayerRPC(BuildG toShare, BuildG toShare2)
    {
        buildG1 = toShare;
        Debug.LogError(buildG1.NazwaBohatera);
        buildG2 = toShare2;

    }
    [PunRPC]
    void READY(bool read)
    {
        ready = read;
    }
    [PunRPC]
    void ListsToSave(List<string> bg1, List<string> bg2, List<int> bg1i, List<int> bg2i)
    {
        Debug.LogError("thishappen");
        buildG1.Units = bg1;
        buildG1.NoUnits = bg1i;

        buildG2.Units = bg2;
        buildG2.NoUnits = bg2i;
        ready = true;
        Debug.LogError(isCreated);
        if (isCreated == false) CreateWorld();
    }

    [PunRPC]
    void IntToSend(int i)
    {
        Debug.Log(i);     
   
    }
    [PunRPC]
    void UnitsAmount(int i, int j, int k , int t, int p , int l , int s)
    {
        Debug.LogError(i);

        buildG2.NoUnits.Add(i);

        Debug.LogError(buildG2.NoUnits[0]);
        buildG2.NoUnits.Add(j);
        buildG2.NoUnits.Add(k);
        buildG2.NoUnits.Add(t);
        buildG2.NoUnits.Add(p);
        buildG2.NoUnits.Add(l);
        buildG2.NoUnits.Add(s);


    }
    [PunRPC]
    void UnitsAmount1(int i, int j, int k, int t, int p, int l, int s)
    {
        Debug.LogError(i);

        buildG1.NoUnits.Add(i);

        Debug.LogError(buildG2.NoUnits[0]);
        buildG1.NoUnits.Add(j);
        buildG1.NoUnits.Add(k);
        buildG1.NoUnits.Add(t);
        buildG1.NoUnits.Add(p);
        buildG1.NoUnits.Add(l);
        buildG1.NoUnits.Add(s);


    }
    [PunRPC]
    void UnitsNames(string i, string j, string k, string t, string p, string l, string s)
    {
       // Debug.LogError(i);

        buildG2.Units.Add(i);

      //  Debug.LogError(buildG2.NoUnits[0]);
        buildG2.Units.Add(j);
        buildG2.Units.Add(k);
        buildG2.Units.Add(t);
        buildG2.Units.Add(p);
        buildG2.Units.Add(l);
        buildG2.Units.Add(s);
        photonView.RPC("UnitsAmount1", RpcTarget.Others, new object[] { buildG1.NoUnits[0], buildG1.NoUnits[1], buildG1.NoUnits[2], buildG1.NoUnits[3], buildG1.NoUnits[4], buildG1.NoUnits[5], buildG1.NoUnits[6] });
        photonView.RPC("UnitsNames1", RpcTarget.Others, new object[] { buildG1.Units[0], buildG1.Units[1], buildG1.Units[2], buildG1.Units[3], buildG1.Units[4], buildG1.Units[5], buildG1.Units[6] });
        DidIGetOtherBuild = true;

    }
    [PunRPC]
    void UnitsNames1(string i, string j, string k, string t, string p, string l, string s)
    {
        // Debug.LogError(i);

        buildG1.Units.Add(i);

        //  Debug.LogError(buildG2.NoUnits[0]);
        buildG1.Units.Add(j);
        buildG1.Units.Add(k);
        buildG1.Units.Add(t);
        buildG1.Units.Add(p);
        buildG1.Units.Add(l);
        buildG1.Units.Add(s);
        DidIGetOtherBuild = true;

    }
    [PunRPC]
    void DidIGetOtherBuildY(bool y)
    {
        DidIGetOtherBuild = y;

    }
    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
    public void CreateWorld()
    {
        allhexes = new List<HexClass>();
        isCreated = true;
        Debug.LogError("this");
        // PlayerP.RefreshInstance(ref LocalPlayer, playerPrefab);
        Debug.LogError("this");
        LoadArmy();
        GenerateMap();
        TeamClass team1 = new TeamClass();
        TeamClass team2 = new TeamClass();
        if (Teams == null)
            Teams = new List<TeamClass>();

        Teams.Add(team1);
        Teams.Add(team2);

        team1.buildG = buildG1;
        team2.buildG = buildG2;
        team1.GenerateTeam(this, PlayerPrefs.GetInt("YourArmy"), true);
        team2.GenerateTeam(this, PlayerPrefs.GetInt("EnemyArmy"), false);
        // GenerateToster(2,5, PlayerPrefs.GetInt("LewyToster"));
    }
    private void Update()
    {
        if (PlayerPrefs.GetInt("Multi") == 1)
        {

            if (PhotonNetwork.CurrentRoom.PlayerCount > 1 && DidIGetOtherBuild == true) /// When can I get OtherBuild - when new player joins, he send it to us. If I am new player, I Ask for build.
            {
          //      Debug.LogError("here");
                if (isCreated == false) CreateWorld();
            }

        }
    }


    public void DoTurn()
    {
        Debug.Log("test");
        foreach ( HexClass h in allhexes)
        {
         if(   h.isTraped)
            {
                Debug.Log(h.trap.Time);
                h.trap.Time--;
                if (h.trap.Time==0)
                {
                    Debug.Log("RemoveTraP");
                    h.RemoveTrap();
                }
            }
        }
    }
    private void Awake()
    {
        if(!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene("MainMenu_Scene");
            return;
        }
    }
    public void ThrowSomething(TosterHexUnit target, TosterHexUnit Shooter,GameObject Projectile)
    {
        
        
        Vector3 m_EulerAngleVelocity = new Vector3(-960, -960, -360);
        bullet = new GameObject();
        bullet = Instantiate(Projectile, Shooter.tosterView.gameObject.transform.position, Quaternion.identity) as GameObject;
    
        bullet.GetComponent<Rigidbody>().AddForce((target.tosterView.gameObject.transform.position- Shooter.tosterView.gameObject.transform.position )* 50);
        bullet.GetComponent<Rigidbody>().AddTorque(m_EulerAngleVelocity);

    }

    public HexClass GetHexAt(int x, int y)
    {
        if (hexes == null)
        {
            Debug.LogError("Hexes not found");
        }

        return hexes[x % 20, y % 11];
    }

    /*
    public List<HexClass> GetHexAround(int x, int y, int radius)
    {
        if (hexes == null)
        {
            Debug.LogError("Hexes not found");
        }
        List<HexClass> 
        return hexes[x % 20, y % 12];
    }
    */




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
            while (u.tosterView.AnimationIsPlaying) { yield return null; }
        }



    }

    void GenerateMap()
    {
        hexes = new HexClass[mapHeight, mapWidth];
        hextoGameObjectMap = new Dictionary<HexClass, GameObject>();
        gameObjectToHexMap = new Dictionary<GameObject, HexClass>();
        for (int col = 0; col < 4; col++)
        {
            for (int row = 11 - ((col + 1) * 2); row < 11; row++)
            {

                HexClass h = new HexClass(this, col, row);

                GameObject HexGo = (GameObject)Instantiate(
                    HexPrefab,
                    h.Position(),
                    Quaternion.identity,
                    this.transform
                    );
                MeshRenderer mr = HexGo.GetComponentInChildren<MeshRenderer>();
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length - 1)];

                HexGo.name = string.Format("HEX: {0}, {1}", col, row);
       //         HexGo.AddComponent<Outline>();
        //        HexGo.GetComponent<Outline>().color = 0;
                gameObjectToHexMap[HexGo] = h;
                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("", col, row, h.Tosters.Count); //{0}, {1}\n {2}
                hexes[col, row] = h;
                hextoGameObjectMap.Add(h, HexGo);
                h.MyHex = HexGo;
                List<GameObject> list = new List<GameObject>();
                list = h.MyHex.GetComponentInChildren<HexInfo>().GiveMe();
                h.crealistofparts(list);
                allhexes.Add(h);

            }

        }


        for (int col = 4; col < 13; col++)
        {
            for (int row = 2; row < 11; row++)
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
          //      HexGo.AddComponent<Outline>();
         //       HexGo.GetComponent<Outline>().color = 2;
                MeshRenderer mr = HexGo.GetComponentInChildren<MeshRenderer>();
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length - 1)];
                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("", col, row, h.Tosters.Count);//{0}, {1}\n {2}
                hexes[col, row] = h;
                hextoGameObjectMap[h] = HexGo;
                gameObjectToHexMap[HexGo] = h;
                h.MyHex = HexGo;
                List<GameObject> list = new List<GameObject>();
                list = h.MyHex.GetComponentInChildren<HexInfo>().GiveMe();
                h.crealistofparts(list);
                allhexes.Add(h);
            }
        }

        for (int col = 13; col < 18; col++)
        {
            for (int row = 2; row < ((17 - col) * 2 + 1); row++)
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
         //       HexGo.AddComponent<Outline>();
          //      HexGo.GetComponent<Outline>().color = 1;
                MeshRenderer mr = HexGo.GetComponentInChildren<MeshRenderer>();
                mr.material = HexMaterials[Random.Range(0, HexMaterials.Length - 1)];
                HexGo.GetComponentInChildren<TextMesh>().text = string.Format("", col, row, h.Tosters.Count);//{0}, {1}\n {2}
                hexes[col, row] = h;
                hextoGameObjectMap[h] = HexGo;
                gameObjectToHexMap[HexGo] = h;
                h.MyHex = HexGo;
                List<GameObject> list = new List<GameObject>();
                list = h.MyHex.GetComponentInChildren<HexInfo>().GiveMe();
                h.crealistofparts(list);
                allhexes.Add(h);
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


    // OLD - UNUSED //
    public void GenerateToster(int i, int j, int k)
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
        HexGo.GetComponentInChildren<TextMesh>().text = string.Format("", i, j, TosterSpawn.Tosters.Count);//{0}, {1}\n {2}
        tostersList.Add(Toster);

        tosters.Add(Toster);
        tostertoGameObjectMap[Toster] = TosterGo;
        Toster.InitateType("TosterDPS");
    }


    // NEW - USED //
    public void GenerateToster(int i, int j, TosterHexUnit toster)
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

        toster.TosterHexUnitAddHex(TosterSpawn.Position(), HexGo);

        toster.SetHex(TosterSpawn);
        GameObject TosterGo = (GameObject)Instantiate(
            toster.TosterPrefab,
            // TosterSpawn.Position(),
            toster.Position(toster.TosterPrefab),
           Quaternion.identity,
            HexGo.transform
            );

        TosterGo.AddComponent<TosterView>();
        toster.OnTosterMoved += TosterGo.GetComponent<TosterView>().OnTosterMoved;
        toster.tosterView = TosterGo.GetComponent<TosterView>();
        HexGo.GetComponentInChildren<TextMesh>().text = string.Format("", i, j, TosterSpawn.Tosters.Count);//{0}, {1}\n {2}
        tostersList.Add(toster);

        tosters.Add(toster);
        tostertoGameObjectMap[toster] = TosterGo;
        //toster.InitateType("TosterDPS");
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
        for (int dx = -radius; dx < radius + 1; dx++)
        {
            for (int dy = Mathf.Max(-radius, -dx - radius); dy < Mathf.Min(radius, -dx + radius) + 1; dy++)
            {
                if (0 <= centerhex.C + dx && 0 <= centerhex.R + dy && 18 >= centerhex.C + dx && 17 >= centerhex.R + dy)
                {

                
                    results.Add(hexes[centerhex.C + dx, centerhex.R + dy]);
                }
            }
        }
        return results.ToArray();
    }

    public void HighlightSlash(TosterHexUnit This, HexClass target)
    {
        HexClass centerHex = This.Hex;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex,1);
        HexClass[] targetHexes = GetHexesWithinRadiusOf(target, 1);

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null && targetHexes.Contains(h) )
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {

                    h.Highlight = true;
                }
            }
        }
        UpdateHexVisuals();
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

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, hh.GetMS());

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

    public void HighlightAroundToster(TosterHexUnit This, TosterHexUnit butremember)
    {
        HexClass centerHex = This.Hex;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, This.GetMS());

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null && This.IsPathAvaible(h))
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {

                    h.Highlight = true;
                }
            }
        }
        UpdateHexVisuals();
    }

    public void unHighlightAroundHex(HexClass what, int radius)
    {
        HexClass centerHex = what;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex,radius);

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null )
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {

                    h.Highlight = false;
                }
            }
        }
        UpdateHexVisuals();
    }


    public void HighlightAroundHex(HexClass what, int radius)
    {
        HexClass centerHex = what;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, radius);

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


    public void HighlightRadiusNoEmpty(HexClass what, int radius)
    {
        HexClass centerHex = what;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, radius);
        HexClass[] areaHexes2 = GetHexesWithinRadiusOf(centerHex, radius-1);
      
        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null && !areaHexes2.Contains(h) && h.Tosters.Count>0)
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {

                    h.Highlight = true;
                }
            }
        }
        UpdateHexVisuals();
    }
   
    public void UpHex(HexClass what, int radius)
    {
        HexClass centerHex = what;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, radius);

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null)
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {

                    Vector3 TestGoUp = h.MyHex.transform.position;
                    TestGoUp.y = -0.1f;
                    h.MyHex.transform.position = TestGoUp;
                }
            }
        }
        UpdateHexVisuals();
    }
    public void DownHex(HexClass what, int radius)
    {
        HexClass centerHex = what;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, radius);

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null)
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {

                    Vector3 TestGoUp = h.MyHex.transform.position;
                    TestGoUp.y = 0f;
                    h.MyHex.transform.position = TestGoUp;
                }
            }
        }
        UpdateHexVisuals();
    }

    public void CheckWithPath(TosterHexUnit hh)
    {
        HexClass centerHex = hh.Hex;

        HexClass[] areaHexes = GetHexesWithinRadiusOf(centerHex, hh.GetMS());

        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null && hh.IsPathAvaible(h))
            {
                if (hextoGameObjectMap.ContainsKey(h) == true)
                {

                    h.Check = true;
                }
            }
        }
        UpdateHexVisuals();
    }

    public void unCheckAround(int q, int r, int range, TosterHexUnit butremember)
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
                    h.Check = false;
                }
            }

        }
        HighlightWithPath(butremember);
    }




    public void unHighlightAround(int q, int r, int range, TosterHexUnit butremember)
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
        HighlightWithPath(butremember);
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
                    if (h.Check == true)
                        mr.material = HexMaterials[2];
                    else
                    if (h.Highlight == true)
                        mr.material = HexMaterials[1];
                   else
                            mr.material = HexMaterials[0];
                    


                }


            }

        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        
        Debug.LogError("test");
       if(PhotonNetwork.CurrentRoom.PlayerCount >1)
        {
           // Instance.CreateWorld();
        }     
    }
   


}



