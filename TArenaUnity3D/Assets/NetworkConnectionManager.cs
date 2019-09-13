using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkConnectionManager : MonoBehaviourPunCallbacks
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
        if(NetworkConnectionManager.NCM==null)
        {
            NetworkConnectionManager.NCM=this;
        }
        else 
        {
            if(NetworkConnectionManager.NCM !=this)
            {Destroy(this.gameObject);}

        }
        DontDestroyOnLoad(this.gameObject);


        TriesToConnectToMaster = false;
        TriesToConnectToMaster = false;
    }


    void Update()
    {
        if(BtnConnectMaster!=null)
        BtnConnectMaster.gameObject.SetActive(!PhotonNetwork.IsConnected && !TriesToConnectToMaster);
        if(BtnConnectRoom !=null)
        BtnConnectRoom.gameObject.SetActive(PhotonNetwork.IsConnected && !TriesToConnectToMaster & !TriesToConnectToRoom);
    }
    public void OnClickConnectToMaster()
    {

        PhotonNetwork.OfflineMode = (PlayerPrefs.GetInt("Multi") == 0);
        
            CustomGame = false;
        Debug.LogError(isMulti.isOn);
        PhotonNetwork.NickName = "Toster";
 //       PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "v1";
        PlayerPrefs.SetString("CustomGameName", "");
        PlayerPrefs.SetInt("customGame", 0);
        if (PhotonNetwork.OfflineMode == false)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

    }
    public void OnClickConnectToMasterCustom(string custom)
    {
        Debug.Log(custom);
        customGameString = custom;
        CustomGame = true;
        PhotonNetwork.OfflineMode = (PlayerPrefs.GetInt("Multi") == 0);
        PhotonNetwork.NickName = "Toster";
        //       PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "v1";
        PlayerPrefs.SetString("CustomGameName",customGameString);
        PlayerPrefs.SetInt("customGame", 1);
        //   PhotonNetwork.ConnectToRegion
        if (PhotonNetwork.OfflineMode == false)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        TriesToConnectToMaster = true;
    }



    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        TriesToConnectToMaster = false;
        TriesToConnectToRoom = false;
        Debug.Log(cause);
    }

    public override void OnConnectedToMaster()
    {
        Debug.LogError("Connected to Master!");
        base.OnConnectedToMaster(); 
         TriesToConnectToMaster = false;
       
        OnClickConnectToRoom();
    }

 



    public void OnClickConnectToRoom()
    {
        if (!PhotonNetwork.IsConnected)
            return;
        Debug.Log(customGameString);
        TriesToConnectToRoom = true;
        //PhotonNetwork.CreateRoom("Peter's Game 1"); //Create a specific Room - Error: OnCreateRoomFailed
      // PhotonNetwork.JoinRoom("test");   //Join a specific Room   - Error: OnJoinRoomFailed  
                      //Join a random Room     - Error: OnJoinRandomRoomFailed  

        if (PlayerPrefs.GetInt("customGame")==0)
        {
            Debug.Log(customGameString);
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.JoinRoom(PlayerPrefs.GetString("CustomGameName"));
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);
        //no room available
        //create a room (null as a name means "does not matter")
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.LogError("Room not found, create room");
        string NameOfRoom = PlayFabControler.PFC.tosterName;
        if (PlayerPrefs.GetInt("customGame") == 1)
        {
            Debug.Log(PlayerPrefs.GetString("CustomGameName"));
            PhotonNetwork.CreateRoom(PlayerPrefs.GetString("CustomGameName"), new RoomOptions { MaxPlayers = 2 });
        }
        else
        PhotonNetwork.CreateRoom(NameOfRoom, new RoomOptions { MaxPlayers = 2 });

    }


    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.Log(message);
        base.OnCreateRoomFailed(returnCode, message);
        TriesToConnectToRoom = false;
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        TriesToConnectToRoom = false;
        Debug.LogError("Master: " + PhotonNetwork.IsMasterClient + " | Players In Room: " + PhotonNetwork.CurrentRoom.PlayerCount + " | RoomName: " + PhotonNetwork.CurrentRoom.Name);
        Debug.LogError("Room Found");
        SceneManager.LoadScene("TestArea2");
    }
    public override void OnCreatedRoom()
    {
        Debug.LogError("Room Created");
        base.OnCreatedRoom();
     
    }
}
