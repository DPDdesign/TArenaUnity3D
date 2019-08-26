using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;


public class MultiControler : MonoBehaviour
{
    private string userEmail;
    private string userPassword;
    private string tosterName;
    public GameObject loginPanel;

    public void Start()
    {
        //PlayerPrefs.DeleteAll();
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "E66F5"; // Please change this value to your own titleId from PlayFab Game Manager
        }

        if (PlayerPrefs.HasKey("EMAIL"))
        {
            tosterName = PlayerPrefs.GetString("USERNAME");
            GameObject.Find("UserName").GetComponentInChildren<Text>().text = PlayerPrefs.GetString("USERNAME");
            userEmail = PlayerPrefs.GetString("EMAIL");
            GameObject.Find("Email").GetComponentInChildren<Text>().text = PlayerPrefs.GetString("EMAIL");
            var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword};
            PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
        }


    }

    public void OnLoginClick()
    {
        var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword};
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("WELCOME TO RETSOT");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        PlayerPrefs.SetString("USERNAME", tosterName);
        loginPanel.SetActive(false);
    }

    private void OnLoginFailure(PlayFabError error)
    {

        var RegisterRequest = new RegisterPlayFabUserRequest { Email = userEmail, Password = userPassword, Username = tosterName };
        PlayFabClientAPI.RegisterPlayFabUser(RegisterRequest, OnRegisterSuccess, OnRegisterFailure);

        Debug.LogError(error.GenerateErrorReport());
    }


    public void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        loginPanel.SetActive(false);
        Debug.Log("Congratulations, you made your Toster account!");
        PlayerPrefs.SetString("PASSWORD", userPassword);
        PlayerPrefs.SetString("USERNAME", tosterName);
        PlayerPrefs.SetString("EMAIL", userEmail);


    }

    private void OnRegisterFailure (PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
        PlayerPrefs.SetString("PASSWORD", userPassword);
        PlayerPrefs.SetString("USERNAME", tosterName);
        PlayerPrefs.SetString("EMAIL", userEmail);
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

 

}
