using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VfxMapTestBootstrap : MonoBehaviour
{
    const string SceneName = "VFX Map Test";
    const string UnitChangeResourcePath = "Z_LEGACY/VFXMAP/TESTING_UNITCHANGE";
    const string LocalEnemyUnitResourcePath = "Z_LEGACY/VFXMAP/VFXMapEnemyDummy";
    const string PlayerUnitId = "Rusher";
    const string FallbackVisualUnitId = "Rusher";

    static bool sceneHooked;

    readonly List<DataMapper.UnitDefinition> units = new List<DataMapper.UnitDefinition>();
    UnitDefinitionAsset localEnemyUnitAsset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RuntimeInitialize()
    {
        if (!sceneHooked)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneHooked = true;
        }

        TryCreate(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    static void TryCreate(Scene scene)
    {
        if (scene.name != SceneName || FindObjectOfType<VfxMapTestBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrap = new GameObject("_VFXMapTestBootstrap");
        bootstrap.AddComponent<VfxMapTestBootstrap>();
    }

    IEnumerator Start()
    {
        yield return StartCoroutine(SetupForkedBattle());
        BuildUnitPanel();
    }

    IEnumerator SetupForkedBattle()
    {
        HexMap hexMap = FindObjectOfType<HexMap>();
        if (hexMap == null)
        {
            yield break;
        }

        while (hexMap.isCreated == false)
        {
            yield return null;
        }

        OverrideBattleTeams(hexMap);
    }

    void OverrideBattleTeams(HexMap hexMap)
    {
        if (hexMap == null)
        {
            return;
        }

        EnsureTeams(hexMap);
        ClearGeneratedTeams(hexMap);

        TosterHexUnit player = CreateUnit(PlayerUnitId, 100, hexMap.Teams[0], true);
        TosterHexUnit enemy = CreateLocalEnemyUnit(hexMap.Teams[1]);

        hexMap.GenerateToster(0, 10, player);
        hexMap.GenerateToster(12, 10, enemy);

        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.SetNewTurn();
            turnManager.GetTostersQueue();
        }
    }

    void BuildUnitPanel()
    {
        Canvas canvas = FindBattleCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("VFX Map Test: Canvas not found.");
            return;
        }

        LoadSortedUnits();
        if (units.Count == 0)
        {
            Debug.LogWarning("VFX Map Test: No unit definitions found.");
            return;
        }

        Transform existingPanel = canvas.transform.Find("VFXMapTest_LeftPanel");
        if (existingPanel != null)
        {
            Destroy(existingPanel.gameObject);
        }

        GameObject panel = CreatePanel(canvas.transform);
        Transform unitRows = CreateUnitRows(panel.transform);
        Transform barbarianColumn = CreateFactionColumn(unitRows, "Barbarians");
        Transform lizardColumn = CreateFactionColumn(unitRows, "Lizards");
        Transform golemColumn = CreateFactionColumn(unitRows, "Golems");
        GameObject unitPrefab = Resources.Load<GameObject>(UnitChangeResourcePath);
        if (unitPrefab == null)
        {
            Debug.LogWarning("VFX Map Test: TESTING_UNITCHANGE prefab not found.");
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            CreateUnitRow(GetFactionColumn(units[i], barbarianColumn, lizardColumn, golemColumn), unitPrefab, units[i], i);
        }
    }

    Canvas FindBattleCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            if (FindChildRecursive(canvases[i].transform, "Footer") != null ||
                FindChildRecursive(canvases[i].transform, "Turn_Displayer") != null)
            {
                return canvases[i];
            }
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("VFXMapTest_LeftPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        panel.transform.SetParent(parent, false);
        panel.layer = parent.gameObject.layer;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500f, 800f);

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.04f, 0.045f, 0.05f, 0.88f);
        image.raycastTarget = true;

        HorizontalLayoutGroup layout = panel.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 120, 8);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        return panel;
    }

    Transform CreateUnitRows(Transform parent)
    {
        GameObject unitRows = new GameObject("UnitRows", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        unitRows.transform.SetParent(parent, false);
        unitRows.layer = parent.gameObject.layer;

        HorizontalLayoutGroup layout = unitRows.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 150f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        LayoutElement element = unitRows.GetComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.flexibleHeight = 1f;

        return unitRows.transform;
    }

    Transform CreateFactionColumn(Transform parent, string name)
    {
        GameObject column = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        column.transform.SetParent(parent, false);
        column.layer = parent.gameObject.layer;

        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement element = column.GetComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.flexibleHeight = 1f;

        return column.transform;
    }

    Transform GetFactionColumn(
        DataMapper.UnitDefinition unit,
        Transform barbarianColumn,
        Transform lizardColumn,
        Transform golemColumn)
    {
        int factionId = unit == null ? UnitFactionResolver.UnknownFactionId : unit.FactionId;
        if (factionId == UnitFactionResolver.LizardFactionId)
        {
            return lizardColumn;
        }

        if (factionId == UnitFactionResolver.GolemElementalFactionId)
        {
            return golemColumn;
        }

        return barbarianColumn;
    }

    void CreateUnitRow(Transform parent, GameObject unitPrefab, DataMapper.UnitDefinition unit, int index)
    {
        GameObject row = Instantiate(unitPrefab, parent);
        row.name = string.Format("{0:00}_{1}", index + 1, unit.Name);
        row.SetActive(true);
        row.layer = parent.gameObject.layer;

        RectTransform rect = row.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(0f, 30f);
            rect.localScale = new Vector3(0.7f, 0.7f, 1f);
        }

        LayoutElement element = row.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = row.AddComponent<LayoutElement>();
        }

        element.preferredHeight = 30f;
        element.minHeight = 30f;
        element.flexibleHeight = 0f;

        StackRepresentation stack = row.GetComponent<StackRepresentation>();
        if (stack != null)
        {
            stack.DisplayInfo(BuildStackInfo(unit, 1));
            HideStackNumbers(stack);
        }

        Image image = row.GetComponent<Image>();
        if (image == null)
        {
            image = row.AddComponent<Image>();
            image.color = new Color(0.13f, 0.1f, 0.07f, 0.98f);
        }

        image.enabled = true;
        image.raycastTarget = true;

        Button button = row.GetComponent<Button>();
        if (button == null)
        {
            button = row.AddComponent<Button>();
        }

        button.targetGraphic = image;
        int capturedIndex = index;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate { ChangeActiveUnitByIndex(capturedIndex); });

        CreateRowLabel(row.transform, unit, index);
    }

    void HideStackNumbers(StackRepresentation stack)
    {
        if (stack == null)
        {
            return;
        }

        ClearText(stack.Count);
        ClearText(stack.StackCost);
        ClearText(stack.StackValue);
        ClearText(stack.Lost);
        ClearText(stack.Level);
    }

    void ClearText(TMP_Text text)
    {
        if (text != null)
        {
            text.text = string.Empty;
        }
    }

    void CreateRowLabel(Transform parent, DataMapper.UnitDefinition unit, int index)
    {
        GameObject labelObject = new GameObject("VFXMapTest_Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        labelObject.layer = parent.gameObject.layer;

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(66f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.fontSize = 13f;
        text.color = Color.white;
        text.text = string.Format("{0}. T{1} F{2} {3}", index + 1, TierNumber(unit.Tier), unit.FactionId, unit.Name);
    }

    void ChangeActiveUnitByIndex(int index)
    {
        if (index < 0 || index >= units.Count)
        {
            Debug.LogWarning("VFX Map Test: Unit index outside range 1-" + units.Count.ToString() + ".");
            return;
        }

        ChangeActiveUnit(units[index]);
    }

    void ChangeActiveUnit(DataMapper.UnitDefinition unit)
    {
        TosterHexUnit active = ResolveActiveUnit();
        if (active == null)
        {
            Debug.LogWarning("VFX Map Test: Active unit not found.");
            return;
        }

        int amount = active.Amount;
        bool wasSelected = active.isSelected;
        ApplyUnitDefinition(active, unit);
        active.SetAmount(amount);
        active.isSelected = wasSelected;
        active.isRange = false;
        active.cooldowns = BuildEmptyCooldowns(active.skillstrings);
        active.ListOfAutocasts = new List<string>(TosterHexUnit.AutocastTurnOrder);
        SetUnitVisualPrefab(active);
        ReplaceActiveUnitView(active);
        RefreshTurnQueue();
    }

    TosterHexUnit ResolveActiveUnit()
    {
        MouseControler mouse = FindObjectOfType<MouseControler>();
        if (mouse != null && mouse.getSelectedToster() != null)
        {
            return mouse.getSelectedToster();
        }

        HexMap hexMap = FindObjectOfType<HexMap>();
        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (hexMap == null || turnManager == null || hexMap.Teams == null || hexMap.Teams.Count < 2)
        {
            return null;
        }

        return turnManager.AskWhosTurn();
    }

    void ReplaceActiveUnitView(TosterHexUnit active)
    {
        if (active == null || active.Hex == null || active.TosterPrefab == null)
        {
            return;
        }

        TosterView oldView = active.tosterView;
        Transform parent = active.Hex.MyHex == null ? null : active.Hex.MyHex.transform;
        if (oldView != null)
        {
            active.OnTosterMoved -= oldView.OnTosterMoved;
            parent = oldView.transform.parent;
            Destroy(oldView.gameObject);
        }

        GameObject newViewObject = Instantiate(active.TosterPrefab, active.Hex.Position(), Quaternion.identity, parent);
        TosterView newView = newViewObject.GetComponent<TosterView>();
        if (newView == null)
        {
            newView = newViewObject.AddComponent<TosterView>();
        }

        active.OnTosterMoved += newView.OnTosterMoved;
        active.tosterView = newView;
        active.ApplyTeamVisualFacing();
        active.SetTextAmount();
    }

    void RefreshTurnQueue()
    {
        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.GetTostersQueue();
        }
    }

    StackInfoData BuildStackInfo(DataMapper.UnitDefinition unit, int count)
    {
        UnitInfoData unitInfo = BuildUnitInfo(unit);
        return new StackInfoData(unit.Name, count, 0, 0, count * unit.Cost, unitInfo);
    }

    UnitInfoData BuildUnitInfo(DataMapper.UnitDefinition unit)
    {
        if (unit == null)
        {
            return null;
        }

        UnitStatsData stats = new UnitStatsData(
            unit.Attack,
            unit.Defense,
            unit.DamageMinimum,
            unit.DamageMaximum,
            Math.Max(0, unit.Speed - 1),
            unit.Initiative,
            unit.HP,
            unit.HP);

        List<SkillInfoData> skills = new List<SkillInfoData>();
        if (unit.SkillNames != null)
        {
            for (int i = 0; i < unit.SkillNames.Count; i++)
            {
                if (string.IsNullOrEmpty(unit.SkillNames[i]) == false)
                {
                    skills.Add(new SkillInfoData(unit.SkillNames[i], true));
                }
            }
        }

        return new UnitInfoData(unit.Name, unit.Name, unit.Tier, unit.Cost, unit.SpritePath, stats, skills);
    }

    List<int> BuildEmptyCooldowns(List<string> skillIds)
    {
        List<int> cooldowns = new List<int>();
        int count = skillIds == null ? 0 : skillIds.Count;
        for (int i = 0; i < count; i++)
        {
            cooldowns.Add(0);
        }

        return cooldowns;
    }

    void LoadSortedUnits()
    {
        units.Clear();
        List<DataMapper.UnitDefinition> loadedUnits = DataMapper.Instance.GetAllUnits();
        loadedUnits.Sort(CompareUnits);
        units.AddRange(loadedUnits);
    }

    void EnsureTeams(HexMap hexMap)
    {
        if (hexMap.Teams == null)
        {
            hexMap.Teams = new List<TeamClass>();
        }

        while (hexMap.Teams.Count < 2)
        {
            hexMap.Teams.Add(new TeamClass());
        }
    }

    void ClearGeneratedTeams(HexMap hexMap)
    {
        if (hexMap.Teams == null)
        {
            return;
        }

        for (int teamIndex = 0; teamIndex < hexMap.Teams.Count; teamIndex++)
        {
            TeamClass team = hexMap.Teams[teamIndex];
            if (team == null)
            {
                continue;
            }

            List<TosterHexUnit> snapshot = new List<TosterHexUnit>(team.Tosters);
            for (int unitIndex = 0; unitIndex < snapshot.Count; unitIndex++)
            {
                RemoveUnitFromMap(snapshot[unitIndex]);
            }

            team.Tosters.Clear();
            team.HexesUnderTeam.Clear();
        }
    }

    void RemoveUnitFromMap(TosterHexUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        if (unit.tosterView != null)
        {
            unit.OnTosterMoved -= unit.tosterView.OnTosterMoved;
            Destroy(unit.tosterView.gameObject);
            unit.tosterView = null;
        }

        if (unit.Hex != null)
        {
            unit.Hex.RemoveToster(unit);
        }
    }

    TosterHexUnit CreateUnit(string unitId, int amount, TeamClass team, bool isPlayerSide)
    {
        TosterHexUnit unit = new TosterHexUnit();
        unit.InitateType(unitId);
        unit.SetMyTeam(team);
        unit.teamN = isPlayerSide;
        unit.SetAmount(amount);
        unit.StartAutocast();
        SetUnitVisualPrefab(unit);
        team.AddNewUnit(unit);
        return unit;
    }

    TosterHexUnit CreateLocalEnemyUnit(TeamClass team)
    {
        TosterHexUnit unit = new TosterHexUnit();
        ApplyUnitDefinition(unit, GetLocalEnemyDefinition());
        unit.SetMyTeam(team);
        unit.teamN = false;
        unit.SetAmount(1);
        unit.StartAutocast();
        SetUnitVisualPrefab(unit);
        team.AddNewUnit(unit);
        return unit;
    }

    void ApplyUnitDefinition(TosterHexUnit unit, DataMapper.UnitDefinition definition)
    {
        if (unit == null || definition == null)
        {
            return;
        }

        unit.SetStats(
            definition.Name,
            definition.HP,
            definition.Attack,
            definition.Defense,
            definition.Initiative,
            definition.Speed,
            new List<string>(definition.SkillNames),
            definition.DamageMinimum,
            definition.DamageMaximum);
        unit.TosterSpriteName = definition.SpritePath;
    }

    void SetUnitVisualPrefab(TosterHexUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        unit.TosterPrefab = DataMapper.Instance.LoadUnitPrefab(unit.Name);
        if (unit.TosterPrefab == null)
        {
            unit.TosterPrefab = DataMapper.Instance.LoadUnitPrefab(FallbackVisualUnitId);
        }
    }

    DataMapper.UnitDefinition GetLocalEnemyDefinition()
    {
        UnitDefinitionAsset asset = GetLocalEnemyUnitAsset();
        return asset == null ? null : asset.ToUnitDefinition();
    }

    UnitDefinitionAsset GetLocalEnemyUnitAsset()
    {
        if (localEnemyUnitAsset == null)
        {
            localEnemyUnitAsset = Resources.Load<UnitDefinitionAsset>(LocalEnemyUnitResourcePath);
        }

        return localEnemyUnitAsset;
    }

    int CompareUnits(DataMapper.UnitDefinition left, DataMapper.UnitDefinition right)
    {
        int tierCompare = TierNumber(left.Tier).CompareTo(TierNumber(right.Tier));
        if (tierCompare != 0)
        {
            return tierCompare;
        }

        return string.Compare(left.Name, right.Name, StringComparison.Ordinal);
    }

    static int TierNumber(string tier)
    {
        if (string.IsNullOrEmpty(tier))
        {
            return 0;
        }

        int parsed;
        if (int.TryParse(tier, out parsed))
        {
            return parsed;
        }

        switch (tier.Trim().ToUpperInvariant())
        {
            case "I":
                return 1;
            case "II":
                return 2;
            case "III":
                return 3;
            case "IV":
                return 4;
            case "V":
                return 5;
            default:
                return 0;
        }
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == name)
            {
                return children[i];
            }
        }

        return null;
    }
}
