using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class PlayFabControler : MonoBehaviour
{


public static PlayFabControler PFC; 

private void OnEnable()
{
    if(PlayFabControler.PFC==null)
    {
        PlayFabControler.PFC=this;
    }
    else 
    {
        if(PlayFabControler.PFC !=this)
        {Destroy(this.gameObject);}

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
            GameObject.Find("UserName").GetComponentInChildren<Text>().text = PlayerPrefs.GetString("USERNAME");
            userEmail = PlayerPrefs.GetString("EMAIL");
            GameObject.Find("Email").GetComponentInChildren<Text>().text = PlayerPrefs.GetString("EMAIL");
            //var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword};
            //PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
        }


    }

    public void OnLoginClick()
    {
        var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword};
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }


    private void OnLoginSuccess(LoginResult result)
    {

        Debug.Log("WELCOME TO RETSOT POBIERAM NAZWĘ PHOTON");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        PlayerPrefs.SetString("USERNAME", tosterName);
        GetStats();
        loginPanel.SetActive(false);
       SceneManager.LoadScene("MainMenu_Scene");
    _playFabPlayerIdCache = result.PlayFabId;
       GetPhoton();
        
    }


 public void GetPhoton(){
        
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
    private void AuthenticateWithPhoton(GetPhotonAuthenticationTokenResult obj) {
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
        Debug.Log("Sprobuj ponownie!");
        //var RegisterRequest = new RegisterPlayFabUserRequest { Email = userEmail, Password = userPassword, Username = tosterName };
        //PlayFabClientAPI.RegisterPlayFabUser(RegisterRequest, OnRegisterSuccess, OnRegisterFailure);
        //GameObject.Find("LogIn").GetComponentInChildren<Text>().text ="Register";
        Debug.LogError(error.GenerateErrorReport());
    }

    public void OnRegisterClick()
    {
        var RegisterRequest = new RegisterPlayFabUserRequest { Email = userEmail, Password = userPassword, Username = tosterName };
        PlayFabClientAPI.RegisterPlayFabUser(RegisterRequest, OnRegisterSuccess, OnRegisterFailure);
    }

    public void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        loginPanel.SetActive(false);
      
        PlayerPrefs.SetString("PASSWORD", userPassword);
        PlayerPrefs.SetString("USERNAME", tosterName);
        PlayerPrefs.SetString("EMAIL", userEmail);
        SetNewUserStats();

    }

    private void SetNewUserStats()
    {
              PlayFabClientAPI.UpdatePlayerStatistics( new UpdatePlayerStatisticsRequest {
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

    private void OnRegisterFailure (PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

   private void OnPlayFabError(PlayFabError obj) {
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

    public void GetuserName (string usernameIn)
    {
        tosterName = usernameIn;
    }


#endregion login

#region PlayerStats
public int Wins;
public int Loses;
public int Experience;
public int Level;
public float WinRatio;

public void SetStats(int win, int lost, int exp){


        Wins += win;
        Loses += lost;
        Experience +=exp;

        PlayFabClientAPI.UpdatePlayerStatistics( new UpdatePlayerStatisticsRequest {
            // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
            Statistics = new List<StatisticUpdate> {
                new StatisticUpdate { StatisticName = "Wins", Value = Wins}, 
                new StatisticUpdate { StatisticName = "Loses", Value = Loses},
                new StatisticUpdate { StatisticName = "Experience", Value = Experience},
                new StatisticUpdate {StatisticName = "WinRatio", Value = Wins/Loses},
            }
        },
        result => {  Debug.Log("User statistics updated"); },
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
}

void OnGetStats(GetPlayerStatisticsResult result)
{
    Debug.Log("Received the following Statistics:");
    foreach (var eachStat in result.Statistics)
    {
        Debug.Log("Statistic (" + eachStat.StatisticName + "): " + eachStat.Value);
        switch(eachStat.StatisticName)
        {
            case "Wins":
            Wins = eachStat.Value;
            break;

            case "Loses":
            Loses = eachStat.Value;
            break;

            case "Experience":
            Experience = eachStat.Value;
            break;

            case "Level":
            Level = eachStat.Value;
            break;

            case "WinRatio":
            WinRatio = eachStat.Value;
            break;
        }
    }
}

int[] ExpRequiredForLevel = {1,10,50,150,330,460,790,940,1280,1700};
void CheckLevel()
{
    if (Experience >= ExpRequiredForLevel[Level]){
         PlayFabClientAPI.UpdatePlayerStatistics( new UpdatePlayerStatisticsRequest {
            // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
            Statistics = new List<StatisticUpdate> {
                new StatisticUpdate { StatisticName = "Level", Value = Level + 1 }, 
            }
        },
        result => { Level +=1; Debug.Log("Level Up! New level: " + Level); },
        error => { Debug.LogError(error.GenerateErrorReport()); });
                
    }

}


#endregion PlayerStats




}
