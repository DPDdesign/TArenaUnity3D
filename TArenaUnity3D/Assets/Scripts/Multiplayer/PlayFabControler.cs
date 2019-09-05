using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System;

public class PlayFabControler : MonoBehaviour
{


    public static PlayFabControler PFC;

    private void OnEnable()
    {
        if (PlayFabControler.PFC == null)
        {
            PlayFabControler.PFC = this;
        }
        else
        {
            if (PlayFabControler.PFC != this)
            { Destroy(this.gameObject); }

        }
        DontDestroyOnLoad(this.gameObject);
    }

    #region login
    private string userEmail;
    private string userPassword;
    public string tosterName;
    public GameObject loginPanel;
    public GameObject mainMenuPanel;
    private string _playFabPlayerIdCache;
    private bool isLogged;
    public GameObject UserNamePanel;
    public GameObject ConfirmRegister;
    public Text LogText;

    public void Awake()
    {



        //PlayerPrefs.DeleteAll();
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "E66F5"; // Please change this value to your own titleId from PlayFab Game Manager
        }


        else if (PlayerPrefs.HasKey("EMAIL"))
        {
            tosterName = PlayerPrefs.GetString("USERNAME");
            userPassword = PlayerPrefs.GetString("PASSWORD");
           // GameObject.Find("UserName").GetComponentInChildren<Text>().text = PlayerPrefs.GetString("USERNAME");
            userEmail = PlayerPrefs.GetString("EMAIL");
            GameObject.Find("Email").GetComponentInChildren<Text>().text = PlayerPrefs.GetString("EMAIL");
            //var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword};
            //PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
        }


    }

    public void OnLoginClick()
    {
        var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }


    private void OnLoginSuccess(LoginResult result)
    {

        Debug.Log("WELCOME TO RETSOT POBIERAM NAZWĘ PHOTON");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
       // PlayerPrefs.SetString("USERNAME", tosterName);
        GetStats();
        loginPanel.SetActive(false);
        SceneManager.LoadScene("MainMenu_Scene");
        _playFabPlayerIdCache = result.PlayFabId;
        GetPhoton();

    }


    public void GetPhoton()
    {

        PlayFabClientAPI.GetPhotonAuthenticationToken(new GetPhotonAuthenticationTokenRequest()
        {
            PhotonApplicationId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime
        }, AuthenticateWithPhoton, OnPlayFabError);
    }
    /*
     * Step 3
     * This is the final and the simplest step. We create new AuthenticationValues instance.
     * This class describes how to authenticate a players inside Photon environment.
     */
    private void AuthenticateWithPhoton(GetPhotonAuthenticationTokenResult obj)
    {
        Debug.Log("Photon token acquired: " + obj.PhotonCustomAuthenticationToken + "  Authentication complete.");

        //We set AuthType to custom, meaning we bring our own, PlayFab authentication procedure.
        var customAuth = new AuthenticationValues { AuthType = CustomAuthenticationType.Custom };

        //We add "username" parameter. Do not let it confuse you: PlayFab is expecting this parameter to contain player PlayFab ID (!) and not username.
        customAuth.AddAuthParameter("username", _playFabPlayerIdCache);    // expected by PlayFab custom auth service

        //We add "token" parameter. PlayFab expects it to contain Photon Authentication Token issues to your during previous step.
        customAuth.AddAuthParameter("token", obj.PhotonCustomAuthenticationToken);

        //We finally tell Photon to use this authentication parameters throughout the entire application.
        PhotonNetwork.AuthValues = customAuth;
    }


    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
        // Recognize and handle the error
        LogText.color = Color.red;
        switch (error.Error)
        {

            case PlayFabErrorCode.InvalidTitleId:
                LogText.text = "Zły nickname!";

                // Handle invalid title id error
                break;
            case PlayFabErrorCode.AccountNotFound:
                // Handle account not found error
                LogText.text = "Nie znaleziono konta!";
                break;
            case PlayFabErrorCode.InvalidEmailOrPassword:
                // Handle invalid email or password error
                LogText.text = "Błędny email lub hasło!";
                break;
            case PlayFabErrorCode.RequestViewConstraintParamsNotAllowed:
                // Handle not allowed view params error
                break;
            case PlayFabErrorCode.InvalidEmailAddress:
                LogText.text = "Błędny email! ";
                break;
            default:
                // Handle unexpected error
                LogText.text = "Spróbuj ponownie!";
                Debug.Log("Sprobuj ponownie!");
                break;
                //   LogText.text = error.ErrorDetails[0];//.GenerateErrorReport();
        }
       
        //var RegisterRequest = new RegisterPlayFabUserRequest { Email = userEmail, Password = userPassword, Username = tosterName };
        //PlayFabClientAPI.RegisterPlayFabUser(RegisterRequest, OnRegisterSuccess, OnRegisterFailure);
        //GameObject.Find("LogIn").GetComponentInChildren<Text>().text ="Register";
       
    }

    public void RequestRegister()
    {
        UserNamePanel.SetActive(true);
        ConfirmRegister.SetActive(true);
    }

    public void OnRegisterClick()
    {
        var RegisterRequest = new RegisterPlayFabUserRequest { Email = userEmail, Password = userPassword, Username = tosterName };
        PlayFabClientAPI.RegisterPlayFabUser(RegisterRequest, OnRegisterSuccess, OnRegisterFailure);
    }

    public void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
      //  loginPanel.SetActive(false);

        PlayerPrefs.SetString("PASSWORD", userPassword);
        PlayerPrefs.SetString("USERNAME", tosterName);
        PlayerPrefs.SetString("EMAIL", userEmail);
        LogText.text = "Congratulations, you made your Retsot account!";
        LogText.color = Color.magenta;
        SetNewUserStats();
        UserNamePanel.SetActive(false);
        ConfirmRegister.SetActive(false);

    }

    private void SetNewUserStats()
    {
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
            Statistics = new List<StatisticUpdate> {
                new StatisticUpdate { StatisticName = "Wins", Value = 0},
                new StatisticUpdate { StatisticName = "Loses", Value = 0},
                new StatisticUpdate { StatisticName = "Experience", Value = 0},
                new StatisticUpdate { StatisticName = "Level", Value = 1},
                new StatisticUpdate { StatisticName = "WinRatio", Value = 1},
            }
        },
  result => { Debug.Log("Congratulations, you made your Toster account!"); },
  error => { Debug.LogError(error.GenerateErrorReport()); });

    }

    private void OnRegisterFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    private void OnPlayFabError(PlayFabError obj)
    {
        Debug.LogError(obj.GenerateErrorReport());
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
    }


    #endregion login

    #region PlayerStats
    public int Wins;
    public int Losses;
    public int Experience;
    public int Level;
    public int RankPoints;
    public List<InventoryObjects> inventoryObjects;
    public List<InventoryObjects> storeObjects;
    public List<string> ownedBundles;
    public int tCoins;
    public int aTokens;

    public void SetStats(int win, int lost, int exp)
    {


        Wins += win;
        Losses += lost;
        Experience += exp;

        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
            Statistics = new List<StatisticUpdate> {
                new StatisticUpdate { StatisticName = "Wins", Value = Wins},
                new StatisticUpdate { StatisticName = "Loses", Value = Losses},
                new StatisticUpdate { StatisticName = "Experience", Value = Experience},
                new StatisticUpdate {StatisticName = "WinRatio", Value = Wins/(Losses+Wins)},
            }
        },
        result => { Debug.Log("User statistics updated"); },
        error => { Debug.LogError(error.GenerateErrorReport()); });

        CheckLevel();

    }

    public void GetStats()
    {
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            OnGetStats,
            error => Debug.LogError(error.GenerateErrorReport())
        );

        GetUserName();
    }

    void OnGetStats(GetPlayerStatisticsResult result)
    {
        Debug.Log("Received the following Statistics:");
        foreach (var eachStat in result.Statistics)
        {
            Debug.Log("Statistic (" + eachStat.StatisticName + "): " + eachStat.Value);
            switch (eachStat.StatisticName)
            {
                case "Wins":
                    Wins = eachStat.Value;
                    break;

                case "Loses":
                    Losses = eachStat.Value;
                    break;

                case "Experience":
                    Experience = eachStat.Value;
                    break;

                case "Level":
                    Level = eachStat.Value;
                    break;

                case "WinRatio":
                    RankPoints = eachStat.Value;
                    break;

            }
        }
    }

    public static int[] ExpRequiredForLevel = { 1, 10, 50, 150, 330, 460, 790, 940, 1280, 1700 };
    void CheckLevel()
    {
        if (Experience >= ExpRequiredForLevel[Level])
        {
            PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
            {
                // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
                Statistics = new List<StatisticUpdate> {
                new StatisticUpdate { StatisticName = "Level", Value = Level + 1 },
            }
            },
           result => { Level += 1; Debug.Log("Level Up! New level: " + Level); },
           error => { Debug.LogError(error.GenerateErrorReport()); });

        }

    }

    public string UserName;

    public void GetUserName()
    {

        var request2 = new GetAccountInfoRequest
        {
            Email = PFC.userEmail
        };

        PlayFabClientAPI.GetAccountInfo(request2, result =>

        {
            UserName =  result.AccountInfo.Username;

        }, resultCallback => { Debug.LogError("error"); }, null, null);


    }




    #endregion PlayerStats



    #region Store
    public void GetCatalog()
    {

        var request2 = new GetCatalogItemsRequest
        {
            CatalogVersion = "AllThings"
        };

        PlayFabClientAPI.GetCatalogItems(request2, result =>

        {
            storeObjects = new List<InventoryObjects>();
            foreach (CatalogItem catalogItem in result.Catalog)
            {

                InventoryObjects iO = new InventoryObjects(catalogItem.ItemId, catalogItem.ItemClass, catalogItem.VirtualCurrencyPrices["TC"], catalogItem.VirtualCurrencyPrices["AT"]);
                storeObjects.Add(iO);
                Debug.Log(iO.Id);
            }

        }, resultCallback => { Debug.LogError("error"); }, null, null);


    }


    public bool BuyItem(string iD, string cost, string VC)
    {
        var request = new PurchaseItemRequest
        {
            CatalogVersion = "AllThings",
            ItemId = iD,
            Price = Int32.Parse(cost),
            VirtualCurrency = VC
        };
        PlayFabClientAPI.PurchaseItem(request, result => { Debug.LogError("Kupiłeś " + iD); PlayFabControler.PFC.GetInventory(); }, result => { Debug.LogError("Nie udało się kupić " + iD); });

        return true;
    }
    IEnumerator Example()
    {
        print(Time.time);
        yield return new WaitForSeconds(5);
        print(Time.time);
    }

    public bool BuyBundle(string iD, string cost, string VC)
    {

        var request = new PurchaseItemRequest
        {
            CatalogVersion = "AllThings",
            ItemId = iD,
            Price = Int32.Parse(cost),
            VirtualCurrency = VC
        };
        PlayFabClientAPI.PurchaseItem(request, result => { Debug.LogError("Kupiłeś " + iD); PlayFabControler.PFC.GetInventory();}, result => { Debug.LogError("Nie udało się kupić " + iD); });

        return true;
    }
    public void GetInventory()
    {

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            inventoryObjects = new List<InventoryObjects>();

            foreach (ItemInstance itemInstance in result.Inventory)
            {
                InventoryObjects iO = new InventoryObjects(itemInstance.ItemId, itemInstance.ItemClass);
                inventoryObjects.Add(iO);
                Debug.LogError(iO.Id);
                
            }
            result.VirtualCurrency.TryGetValue("TC", out tCoins);
            result.VirtualCurrency.TryGetValue("AT", out aTokens);
            Debug.Log("Wczytano dane użytkownika");

        }, resultCallback => { Debug.LogError("error"); }, null, null);
    }



  public  IEnumerator GetInventoryI()
    {
        bool toster = false;
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            inventoryObjects = new List<InventoryObjects>();

            foreach (ItemInstance itemInstance in result.Inventory)
            {
                InventoryObjects iO = new InventoryObjects(itemInstance.ItemId, itemInstance.ItemClass);
                inventoryObjects.Add(iO);

            }
            result.VirtualCurrency.TryGetValue("TC", out tCoins);
            result.VirtualCurrency.TryGetValue("AT", out aTokens);
            Debug.Log("Wczytano dane użytkownika");
        }, resultCallback => { Debug.LogError("error"); toster = true; }, null, null);
        while (toster == false)
        {
            yield return new WaitForSeconds(5);
        }
      
    }
    public bool CheckIfPlayerGotBundle(string bundle)
    {
        bool toster = true;
        var request2 = new GetCatalogItemsRequest
        {
            CatalogVersion = "AllThings"
        };

        PlayFabClientAPI.GetCatalogItems(request2, result =>

        {
            storeObjects = new List<InventoryObjects>();
            foreach (CatalogItem catalogItem in result.Catalog)
            {
                if (catalogItem.ItemClass == "ArmyBundle" && catalogItem.ItemId == bundle)
                {

                    foreach (string bundleInfo in catalogItem.Bundle.BundledItems)
                    {
                        foreach (InventoryObjects inventoryObjects in PlayFabControler.PFC.inventoryObjects)
                        {
                            if (inventoryObjects.Id == bundleInfo)
                            {
                                toster = false;
                            }
                        }
                    }

                }
            }

        }, resultCallback => { Debug.LogError("error"); }, null, null);
        return toster;

    }
    #endregion Store

}