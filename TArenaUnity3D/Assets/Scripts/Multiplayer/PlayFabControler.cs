using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayFabControler : MonoBehaviour
{
    public static PlayFabControler PFC;
    public static int[] ExpRequiredForLevel = { 1, 10, 50, 150, 330, 460, 790, 940, 1280, 1700 };

    private string userEmail;
    private string userPassword;
    public string tosterName = "Local Player";
    public GameObject loginPanel;
    public GameObject mainMenuPanel;
    public GameObject UserNamePanel;
    public GameObject ConfirmRegister;
    public Text LogText;

    public int Wins;
    public int Losses;
    public int Experience;
    public int Level = 1;
    public int RankPoints;
    public List<InventoryObjects> inventoryObjects = new List<InventoryObjects>();
    public List<InventoryObjects> storeObjects = new List<InventoryObjects>();
    public List<string> ownedBundles = new List<string>();
    public int tCoins = 999999;
    public int aTokens = 999999;
    public string UserName = "Local Player";

    public static PlayFabControler EnsureInstance()
    {
        if (PFC != null)
        {
            PFC.EnsureLocalData();
            return PFC;
        }

        PlayFabControler existing = FindObjectOfType<PlayFabControler>();
        if (existing != null)
        {
            existing.RegisterSingleton();
            existing.EnsureLocalData();
            return existing;
        }

        GameObject go = new GameObject("LocalPlayFabControler");
        PlayFabControler created = go.AddComponent<PlayFabControler>();
        created.RegisterSingleton();
        created.EnsureLocalData();
        return created;
    }

    private void Awake()
    {
        RegisterSingleton();
        LoadSavedIdentity();
        EnsureLocalData();
    }

    private void OnEnable()
    {
        RegisterSingleton();
        EnsureLocalData();
    }

    private void RegisterSingleton()
    {
        if (PFC == null)
        {
            PFC = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (PFC != this)
        {
            Destroy(gameObject);
        }
    }

    private void LoadSavedIdentity()
    {
        if (PlayerPrefs.HasKey("USERNAME"))
        {
            tosterName = PlayerPrefs.GetString("USERNAME");
            UserName = tosterName;
        }

        if (PlayerPrefs.HasKey("EMAIL"))
        {
            userEmail = PlayerPrefs.GetString("EMAIL");
        }

        if (PlayerPrefs.HasKey("PASSWORD"))
        {
            userPassword = PlayerPrefs.GetString("PASSWORD");
        }
    }

    public void OnLoginClick()
    {
        SaveIdentity();
        GetStats();
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

        SceneManager.LoadScene("MainMenu_Scene");
    }

    public void RequestRegister()
    {
        if (UserNamePanel != null)
        {
            UserNamePanel.SetActive(true);
        }

        if (ConfirmRegister != null)
        {
            ConfirmRegister.SetActive(true);
        }
    }

    public void OnRegisterClick()
    {
        SaveIdentity();
        EnsureLocalData();

        if (LogText != null)
        {
            LogText.text = "Local profile ready.";
            LogText.color = Color.magenta;
        }

        if (UserNamePanel != null)
        {
            UserNamePanel.SetActive(false);
        }

        if (ConfirmRegister != null)
        {
            ConfirmRegister.SetActive(false);
        }
    }

    public void GetPhoton()
    {
    }

    public void GetuserEmail(string emailIn)
    {
        userEmail = emailIn;
    }

    public void GetuserPassword(string passwordIn)
    {
        userPassword = passwordIn;
    }

    public void GetuserName(string usernameIn)
    {
        tosterName = usernameIn;
        UserName = usernameIn;
    }

    public void StartCloudWipeAccount()
    {
        Wins = 0;
        Losses = 0;
        Experience = 0;
        RankPoints = 0;
        Level = 1;
    }

    public void StartCloudSetWin()
    {
        SetStats(1, 0, 10);
    }

    public void StartCloudSetLoss()
    {
        SetStats(0, 1, 3);
    }

    public void SetStats(int win, int lost, int exp)
    {
        Wins += win;
        Losses += lost;
        Experience += exp;
        CheckLevel();
    }

    public void GetStats()
    {
        EnsureLocalData();
        GetUserName();
    }

    public void GetUserName()
    {
        if (string.IsNullOrEmpty(UserName))
        {
            UserName = string.IsNullOrEmpty(tosterName) ? "Local Player" : tosterName;
        }
    }

    public void GetCatalog()
    {
        EnsureLocalData();
    }

    public bool BuyItem(string iD, string cost, string VC, Shop shop)
    {
        int parsedCost;
        int.TryParse(cost, out parsedCost);

        if (VC == "AT")
        {
            aTokens = Mathf.Max(0, aTokens - parsedCost);
        }
        else
        {
            tCoins = Mathf.Max(0, tCoins - parsedCost);
        }

        AddInventoryItem(iD, VC == "AT" ? "ArmyBundle" : "Unit");
        if (shop != null)
        {
            shop.Reload();
        }

        return true;
    }

    public bool BuyBundle(string iD, string cost, string VC)
    {
        return BuyItem(iD, cost, VC, null);
    }

    public void GetInventory()
    {
        EnsureLocalData();
    }

    public void GetInventory(Shop shop)
    {
        EnsureLocalData();
        if (shop != null)
        {
            shop.Reload();
        }
    }

    public IEnumerator GetInventoryI()
    {
        EnsureLocalData();
        yield break;
    }

    public bool CheckIfPlayerGotBundle(string bundle)
    {
        EnsureLocalData();
        return true;
    }

    private void SaveIdentity()
    {
        if (!string.IsNullOrEmpty(userEmail))
        {
            PlayerPrefs.SetString("EMAIL", userEmail);
        }

        if (!string.IsNullOrEmpty(userPassword))
        {
            PlayerPrefs.SetString("PASSWORD", userPassword);
        }

        if (!string.IsNullOrEmpty(tosterName))
        {
            PlayerPrefs.SetString("USERNAME", tosterName);
            UserName = tosterName;
        }
    }

    private void CheckLevel()
    {
        while (Level < ExpRequiredForLevel.Length - 1 && Experience >= ExpRequiredForLevel[Level])
        {
            Level++;
        }
    }

    private void EnsureLocalData()
    {
        if (Level <= 0)
        {
            Level = 1;
        }

        if (tCoins <= 0)
        {
            tCoins = 999999;
        }

        if (aTokens <= 0)
        {
            aTokens = 999999;
        }

        EnsureBundles();
        EnsureUnitsFromResources();
    }

    private void EnsureBundles()
    {
        string[] bundles = { "Barbarians", "Lizards", "Golems", "Shadows" };
        foreach (string bundle in bundles)
        {
            if (!ownedBundles.Contains(bundle))
            {
                ownedBundles.Add(bundle);
            }

            AddStoreItem(bundle, "ArmyBundle", 0, 0);
            AddInventoryItem(bundle, "ArmyBundle");
        }
    }

    private void EnsureUnitsFromResources()
    {
        TextAsset unitData = Resources.Load<TextAsset>("Data/Units");
        if (unitData == null)
        {
            return;
        }

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(unitData.text);
        XmlNodeList units = doc.SelectNodes("Units/Unit");
        foreach (XmlNode unit in units)
        {
            string id = ReadChildText(unit, "Name");
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            uint cost = 0;
            uint.TryParse(ReadChildText(unit, "Cost"), out cost);
            AddStoreItem(id, "Unit", cost, 0);
            AddInventoryItem(id, "Unit");
        }
    }

    private static string ReadChildText(XmlNode node, string childName)
    {
        XmlNode child = node.SelectSingleNode(childName);
        return child == null ? string.Empty : child.InnerText;
    }

    private void AddStoreItem(string id, string type, uint tcost, uint acost)
    {
        if (storeObjects.Exists(item => item.Id == id && item.Type1 == type))
        {
            return;
        }

        storeObjects.Add(new InventoryObjects(id, type, tcost, acost));
    }

    private void AddInventoryItem(string id, string type)
    {
        if (inventoryObjects.Exists(item => item.Id == id && item.Type1 == type))
        {
            return;
        }

        inventoryObjects.Add(new InventoryObjects(id, type));
    }
}
