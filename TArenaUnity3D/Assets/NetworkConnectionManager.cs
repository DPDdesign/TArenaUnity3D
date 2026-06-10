using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkConnectionManager : MonoBehaviour
{
    public Button BtnConnectMaster;
    public Button BtnConnectRoom;
    public InputField InputField;
    public Toggle isMulti;
    public bool TriesToConnectToMaster;
    public bool TriesToConnectToRoom;
    public bool CustomGame;
    public string customGameString;

    public static NetworkConnectionManager NCM;

    void Start()
    {
        if (NCM == null)
        {
            NCM = this;
        }
        else if (NCM != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        TriesToConnectToMaster = false;
        TriesToConnectToRoom = false;
    }

    void Update()
    {
        if (BtnConnectMaster != null)
        {
            BtnConnectMaster.gameObject.SetActive(!TriesToConnectToMaster);
        }

        if (BtnConnectRoom != null)
        {
            BtnConnectRoom.gameObject.SetActive(false);
        }
    }

    public void OnClickConnectToMaster()
    {
        CustomGame = false;
        PlayerPrefs.SetString("CustomGameName", "");
        PlayerPrefs.SetInt("customGame", 0);
        LoadBattleScene();
    }

    public void OnClickConnectToMasterCustom(string custom)
    {
        customGameString = custom;
        CustomGame = true;
        PlayerPrefs.SetString("CustomGameName", customGameString);
        PlayerPrefs.SetInt("customGame", 1);
        LoadBattleScene();
    }

    public void OnClickConnectToRoom()
    {
        LoadBattleScene();
    }

    public void OnDisconnected(string cause)
    {
        TriesToConnectToMaster = false;
        TriesToConnectToRoom = false;
        Debug.Log(cause);
    }

    public void OnConnectedToMaster()
    {
        TriesToConnectToMaster = false;
        LoadBattleScene();
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        LoadBattleScene();
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        LoadBattleScene();
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log(message);
        TriesToConnectToRoom = false;
    }

    public void OnJoinedRoom()
    {
        TriesToConnectToRoom = false;
        LoadBattleScene();
    }

    public void OnCreatedRoom()
    {
        Debug.Log("Local room created");
    }

    private void LoadBattleScene()
    {
        TriesToConnectToMaster = false;
        TriesToConnectToRoom = false;
        SceneManager.LoadScene("TestArea2");
    }
}
