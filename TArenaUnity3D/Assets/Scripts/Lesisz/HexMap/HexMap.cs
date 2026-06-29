
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using static PanelArmii;
using HPath;
using System.Linq;

public class HexMap : LocalNetworkBehaviour, IQPathWorld
{
    const float UnitMoveVisualWaitTimeoutSeconds = 5f;
    const float HexGenerationHeight = 2.5f;
    const float LegacyMapCenterColumnUnits = 10.75f;
    const float LegacyMapCenterRowUnits = 6f;

    public List<string> ListOfHeroes = new List<string>(new string[] { "Biały Toster", "Czerwony Toster", "Zielony Toster" });
    public GameObject HexPrefab;
    public List<GameObject> TostersPrefabs;
    public List<TeamClass> Teams;
    public GameObject TosterUnit;
    public GameObject Projectile;
    public Material[] HexMaterials;
    public bool useLegacyMap = true;
    [Min(1)]
    public int Length = 18;
    [Min(1)]
    public int Width = 11;
    public Vector3 MapPositionOffset { get; private set; }
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
        TeamClass Team = new TeamClass();
        Team.ThisTeamNO = PlayerPrefs.GetInt("YourArmy");
        Team.WczytajPlik();

        buildG1 = Team.buildG;
        Team.ThisTeamNO = PlayerPrefs.GetInt("EnemyArmy");
        Team.WczytajPlik();

        buildG2 = Team.buildG;
        CreateWorld();

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
    void UnitsAmount(int i, int j, int k , int t, int p , int l)
    {
        Debug.LogError(i);

        buildG2.NoUnits.Add(i);

        Debug.LogError(buildG2.NoUnits[0]);
        buildG2.NoUnits.Add(j);
        buildG2.NoUnits.Add(k);
        buildG2.NoUnits.Add(t);
        buildG2.NoUnits.Add(p);
        buildG2.NoUnits.Add(l);
 


    }
    [PunRPC]
    void UnitsAmount1(int i, int j, int k, int t, int p, int l)
    {
        Debug.LogError(i);

        buildG1.NoUnits.Add(i);

        Debug.LogError(buildG2.NoUnits[0]);
        buildG1.NoUnits.Add(j);
        buildG1.NoUnits.Add(k);
        buildG1.NoUnits.Add(t);
        buildG1.NoUnits.Add(p);
        buildG1.NoUnits.Add(l);


    }
    [PunRPC]
    void UnitsNames(string i, string j, string k, string t, string p, string l)
    {
       // Debug.LogError(i);

        buildG2.Units.Add(i);

      //  Debug.LogError(buildG2.NoUnits[0]);
        buildG2.Units.Add(j);
        buildG2.Units.Add(k);
        buildG2.Units.Add(t);
        buildG2.Units.Add(p);
        buildG2.Units.Add(l);
  
        photonView.RPC("UnitsAmount1", RpcTarget.Others, new object[] { buildG1.NoUnits[0], buildG1.NoUnits[1], buildG1.NoUnits[2], buildG1.NoUnits[3], buildG1.NoUnits[4], buildG1.NoUnits[5] });
        photonView.RPC("UnitsNames1", RpcTarget.Others, new object[] { buildG1.Units[0], buildG1.Units[1], buildG1.Units[2], buildG1.Units[3], buildG1.Units[4], buildG1.Units[5] });
        DidIGetOtherBuild = true;

    }
    [PunRPC]
    void UnitsNames1(string i, string j, string k, string t, string p, string l)
    {
        // Debug.LogError(i);

        buildG1.Units.Add(i);

        //  Debug.LogError(buildG2.NoUnits[0]);
        buildG1.Units.Add(j);
        buildG1.Units.Add(k);
        buildG1.Units.Add(t);
        buildG1.Units.Add(p);
        buildG1.Units.Add(l);
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
    }


    public void DoTurn()
    {
        Debug.Log("test");
        foreach ( HexClass h in allhexes)
        {
         if(   h.isTraped)
            {
                Debug.Log(h.trap.Time);
                BattleActionResult result = BattleActionAutomaticResultApplier.CreateTrapTurnTickResult(h);
                BattleActionAutomaticResultApplier.ApplyTrapTurnTickResult(h, result);
            }
        }
    }
    private void Awake()
    {
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
            return null;
        }

        if (useLegacyMap)
        {
            if (x < 0 || y < 0)
            {
                return null;
            }

            return hexes[x % 20, y % 11];
        }

        if (x < 0 || y < 0 || x >= hexes.GetLength(0) || y >= hexes.GetLength(1))
        {
            return null;
        }

        return hexes[x, y];
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
        if (h == null)
        {
            return Vector3.zero;
        }

        return h.Position();

    }

    public int CurrentLength
    {
        get
        {
            if (hexes != null)
            {
                return hexes.GetLength(0);
            }

            return useLegacyMap ? 20 : Mathf.Max(1, Length);
        }
    }

    public int CurrentWidth
    {
        get
        {
            if (hexes != null)
            {
                return hexes.GetLength(1);
            }

            return useLegacyMap ? 20 : Mathf.Max(1, Width);
        }
    }

    public bool IsBattleReadyForTacticalActions
    {
        get
        {
            if (isCreated == false || hexes == null || Teams == null || Teams.Count == 0)
            {
                return false;
            }

            for (int teamIndex = 0; teamIndex < Teams.Count; teamIndex++)
            {
                TeamClass team = Teams[teamIndex];
                if (team == null || team.Tosters == null)
                {
                    return false;
                }

                for (int unitIndex = 0; unitIndex < team.Tosters.Count; unitIndex++)
                {
                    TosterHexUnit unit = team.Tosters[unitIndex];
                    if (unit == null)
                    {
                        return false;
                    }

                    if (unit.isDead || unit.Amount <= 0)
                    {
                        continue;
                    }

                    if (unit.Hex == null || unit.skillstrings == null || unit.cooldowns == null)
                    {
                        return false;
                    }

                    if (unit.cooldowns.Count < unit.skillstrings.Count)
                    {
                        return false;
                    }

                    HexClass mapHex = GetHexAt(unit.Hex.C, unit.Hex.R);
                    if (mapHex != unit.Hex || unit.Hex.Tosters == null || unit.Hex.Tosters.Contains(unit) == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }


    public IEnumerator DoUnitMoves(TosterHexUnit u)
    {
        // Is there any reason we should check HERE if a unit should be moving?
        // I think the answer is no -- DoMove should just check to see if it needs
        // to do anything, or just return immediately.


        while (u != null)
        {
            bool hasMoreMoves = u.DoMove();
            yield return WaitForTosterVisualMovement(u);

            if (!hasMoreMoves)
            {
                break;
            }
        }

        if (u.tosterView != null)
        {
            u.tosterView.ResetAnimatorToDefault();
        }



    }

    IEnumerator WaitForTosterVisualMovement(TosterHexUnit unit)
    {
        if (unit == null || unit.tosterView == null)
        {
            yield break;
        }

        float startedAt = Time.time;
        while (unit.tosterView.AnimationIsPlaying)
        {
            if (Time.time - startedAt > UnitMoveVisualWaitTimeoutSeconds)
            {
                Debug.LogWarning("Toster visual movement timed out. Snapping to final logical hex.");
                if (unit.Hex != null)
                {
                    unit.tosterView.TeleportTo(unit.Hex);
                }

                unit.tosterView.AnimationIsPlaying = false;
                yield break;
            }

            yield return null;
        }
    }

    void GenerateMap()
    {
        if (useLegacyMap)
        {
            GenerateLegacyMap();
            return;
        }

        GenerateSizedMap();
    }

    void PrepareMapStorage(int length, int width)
    {
        hexes = new HexClass[length, width];
        hextoGameObjectMap = new Dictionary<HexClass, GameObject>();
        gameObjectToHexMap = new Dictionary<GameObject, HexClass>();
    }

    void GenerateSizedMap()
    {
        int mapLength = Mathf.Max(1, Length);
        int mapWidth = Mathf.Max(1, Width);
        PrepareMapStorage(mapLength, mapWidth);
        MapPositionOffset = GetLegacyMapCenterPosition() - GetSizedMapCenterPosition(mapLength, mapWidth);

        for (int col = 0; col < mapLength; col++)
        {
            for (int row = 0; row < mapWidth; row++)
            {
                CreateHex(col, row);
            }
        }
    }

    void GenerateLegacyMap()
    {
        PrepareMapStorage(20, 20);
        MapPositionOffset = Vector3.zero;

        for (int col = 0; col < 4; col++)
        {
            for (int row = 11 - ((col + 1) * 2); row < 11; row++)
            {
                CreateHex(col, row);
            }
        }


        for (int col = 4; col < 13; col++)
        {
            for (int row = 2; row < 11; row++)
            {
                CreateHex(col, row);
            }
        }

        for (int col = 13; col < 18; col++)
        {
            for (int row = 2; row < ((17 - col) * 2 + 1); row++)
            {
                CreateHex(col, row);
            }
        }
        //StaticBatchingUtility.Combine(this.gameObject.GetComponentInChildren<MeshFilter>().gameObject);
    }

    Vector3 GetSizedMapCenterPosition(int mapLength, int mapWidth)
    {
        float maxRowOffset = mapWidth > 1 ? 0.5f : 0f;
        float centerColumnUnits = ((mapLength - 1) + maxRowOffset) * 0.5f;
        float centerRowUnits = (mapWidth - 1) * 0.5f;
        return GetHexPositionFromUnits(centerColumnUnits, centerRowUnits);
    }

    Vector3 GetLegacyMapCenterPosition()
    {
        return GetHexPositionFromUnits(LegacyMapCenterColumnUnits, LegacyMapCenterRowUnits);
    }

    Vector3 GetHexPositionFromUnits(float columnUnits, float rowUnits)
    {
        float height = HexGenerationHeight;
        float width = Mathf.Sqrt(3) / 2 * height;
        float vert = height * 0.75f;
        float horiz = width;

        return new Vector3(
            horiz * columnUnits,
            0,
            vert * rowUnits
            );
    }

    void CreateHex(int col, int row)
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
        HexGo.GetComponentInChildren<TextMesh>().text = string.Format("", col, row, h.Tosters.Count);//{0}, {1}\n {2}
        hextoGameObjectMap[h] = HexGo;
        gameObjectToHexMap[HexGo] = h;
        h.MyHex = HexGo;
        List<GameObject> list = h.MyHex.GetComponentInChildren<HexInfo>().GiveMe();
        h.crealistofparts(list);
        allhexes.Add(h);
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
        if (TosterSpawn == null)
        {
            Debug.LogWarning(string.Format("Cannot spawn toster outside map at {0}, {1}.", i, j));
            return;
        }

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
        Toster.ApplyTeamVisualFacing();
        HexGo.GetComponentInChildren<TextMesh>().text = string.Format("", i, j, TosterSpawn.Tosters.Count);//{0}, {1}\n {2}
        tostersList.Add(Toster);

        tosters.Add(Toster);
        tostertoGameObjectMap[Toster] = TosterGo;
        Toster.InitateType("TosterDPS");
        ApplyInitialThrowerRangeStance(Toster);
    }


    // NEW - USED //
    public void GenerateToster(int i, int j, TosterHexUnit toster)
    {
        HexClass TosterSpawn = GetHexAt(i, j);
        if (TosterSpawn == null)
        {
            Debug.LogWarning(string.Format("Cannot spawn toster outside map at {0}, {1}.", i, j));
            return;
        }

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
        toster.ApplyTeamVisualFacing();
        ApplyInitialThrowerRangeStance(toster);
        HexGo.GetComponentInChildren<TextMesh>().text = string.Format("", i, j, TosterSpawn.Tosters.Count);//{0}, {1}\n {2}
        tostersList.Add(toster);

        tosters.Add(toster);
        tostertoGameObjectMap[toster] = TosterGo;
        //toster.InitateType("TosterDPS");
    }

    public void ApplyInitialThrowerRangeStance(TosterHexUnit toster)
    {
        if (toster == null || IsThrowerStanceUnit(toster) == false)
        {
            return;
        }

        if (toster.InitialThrowerRangeStanceApplied == false)
        {
            toster.isRange = true;
            toster.SpecialDMGModificator = 20;
            toster.SpecialResistance = 20;
            if (HasSkillId(toster, "Range_Stance_Barb"))
            {
                ReplaceSkillId(toster, "Range_Stance_Barb", "Melee_Stance_Barb");
            }
            toster.InitialThrowerRangeStanceApplied = true;
        }

        if (toster.tosterView == null)
        {
            return;
        }

        ApplyThrowerStancePresentation(toster, true);
    }

    public void RefreshThrowerStancePresentation(TosterHexUnit toster)
    {
        if (toster == null || IsThrowerStanceUnit(toster) == false || toster.tosterView == null)
        {
            return;
        }

        if (toster.InitialThrowerRangeStanceApplied == false && HasAnyThrowerStanceSkill(toster))
        {
            ApplyInitialThrowerRangeStance(toster);
            return;
        }

        ApplyThrowerStancePresentation(toster, false);
    }

    static void ApplyThrowerStancePresentation(TosterHexUnit toster, bool forceAnimatorState)
    {
        ThrowerStanceVisuals visuals = FindThrowerStanceVisuals(toster);
        if (visuals != null)
        {
            visuals.SetRangedStance(toster.isRange);
        }

        string defaultState = ResolveThrowerStanceAnimatorState(toster.tosterView, toster.isRange);
        toster.tosterView.SetDefaultAnimatorStateOverride(defaultState);
        if (forceAnimatorState)
        {
            toster.tosterView.PlayAnimatorStateImmediate(defaultState, false);
        }
        else
        {
            toster.tosterView.EnsureDefaultAnimatorStateOverrideApplied();
        }
    }

    static string ResolveThrowerStanceAnimatorState(TosterView tosterView, bool isRange)
    {
        string preferredState = isRange ? "Combat_1H_Ready" : "Combat_2HL_Ready";
        if (tosterView != null && tosterView.HasAnimatorState(preferredState))
        {
            return preferredState;
        }

        return "Ready";
    }

    static bool HasAnyThrowerStanceSkill(TosterHexUnit toster)
    {
        return HasSkillId(toster, "Range_Stance_Barb") || HasSkillId(toster, "Melee_Stance_Barb");
    }

    static bool IsThrowerStanceUnit(TosterHexUnit toster)
    {
        if (HasAnyThrowerStanceSkill(toster))
        {
            return true;
        }

        return FindThrowerStanceVisuals(toster) != null;
    }

    static ThrowerStanceVisuals FindThrowerStanceVisuals(TosterHexUnit toster)
    {
        if (toster == null || toster.tosterView == null)
        {
            return null;
        }

        ThrowerStanceVisuals visuals = toster.tosterView.GetComponentInParent<ThrowerStanceVisuals>();
        if (visuals == null)
        {
            visuals = toster.tosterView.GetComponentInChildren<ThrowerStanceVisuals>(true);
        }

        return visuals;
    }

    static bool HasSkillId(TosterHexUnit toster, string skillId)
    {
        if (toster == null || toster.skillstrings == null)
        {
            return false;
        }

        for (int i = 0; i < toster.skillstrings.Count; i++)
        {
            if (toster.skillstrings[i] == skillId)
            {
                return true;
            }
        }

        return false;
    }

    static void ReplaceSkillId(TosterHexUnit toster, string currentSkillId, string replacementSkillId)
    {
        if (toster == null || toster.skillstrings == null)
        {
            return;
        }

        for (int i = 0; i < toster.skillstrings.Count; i++)
        {
            if (toster.skillstrings[i] == currentSkillId)
            {
                toster.skillstrings[i] = replacementSkillId;
            }
        }
    }




    public void LoadArmy()
    {
        string path = DataMapper.Instance.GetBuildFilePath(PlayerPrefs.GetInt("YourArmy"));
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
        path = DataMapper.Instance.GetBuildFilePath(PlayerPrefs.GetInt("EnemyArmy"));
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
        if (centerhex == null || centerhex.hexMap != this)
        {
            return results.ToArray();
        }

        int safeRadius = Mathf.Max(0, radius);
        Queue<HexClass> frontier = new Queue<HexClass>();
        Dictionary<HexClass, int> distances = new Dictionary<HexClass, int>();

        frontier.Enqueue(centerhex);
        distances[centerhex] = 0;
        results.Add(centerhex);

        while (frontier.Count > 0)
        {
            HexClass current = frontier.Dequeue();
            int currentDistance = distances[current];
            if (currentDistance >= safeRadius)
            {
                continue;
            }

            IPathTile[] neighbours = current.GetNeighbours();
            for (int i = 0; i < neighbours.Length; i++)
            {
                HexClass neighbour = neighbours[i] as HexClass;
                if (neighbour == null || distances.ContainsKey(neighbour))
                {
                    continue;
                }

                distances[neighbour] = currentDistance + 1;
                frontier.Enqueue(neighbour);
                results.Add(neighbour);
            }
        }

        return results.ToArray();
    }

    HexClass GetHexAtWithoutWrap(int x, int y)
    {
        if (hexes == null || x < 0 || y < 0 || x >= hexes.GetLength(0) || y >= hexes.GetLength(1))
        {
            return null;
        }

        return hexes[x, y];
    }

    public void HighlightSlash(HexClass centerHex, HexClass target)
    {
        //HexClass centerHex = This.Hex;

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
        if (what == null)
        {
            ClearHighlights();
            return;
        }

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

    public void ClearHighlights()
    {
        foreach (HexClass h in allhexes)
        {
            if (h != null)
            {
                h.Highlight = false;
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
      
        foreach (HexClass h in areaHexes)
        {
            //if(h.Elevation < 0)
            //h.Elevation = 0;
            if (h != null && h != centerHex && h.Tosters.Count>0)
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
        for (int column = 0; column < hexes.GetLength(0); column++)
        {
            for (int row = 0; row < hexes.GetLength(1); row++)
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

    public void OnPlayerEnteredRoom()
    {
        Debug.LogError("test");
    }
   


}



