using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkConne22ctionManager : MonoBehaviourPunCallbacks
{
    public Button BtnConnectMaster;
    public Button BtnConnectRoom;


    public bool TriesToConnectToMaster;
    public bool TriesToConnectToRoom;


    void Start()
    {
        TriesToConnectToMaster = false;
        TriesToConnectToMaster = false;
    }


    void Update()
    {
        BtnConnectMaster.gameObject.SetActive(!PhotonNetwork.IsConnected && !TriesToConnectToMaster);
        BtnConnectRoom.gameObject.SetActive(PhotonNetwork.IsConnected && !TriesToConnectToMaster & !TriesToConnectToRoom);
    }
    public void OnClickConnectToMaster()
    {
        PhotonNetwork.OfflineMode = false;
        PhotonNetwork.NickName = "PlayerName";
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "v2";
        
        PhotonNetwork.ConnectUsingSettings();
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
        base.OnConnectedToMaster();
        TriesToConnectToMaster = false;
        Debug.Log("Connected to Master!");
    }



    public void OnClickConnectToRoom()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        TriesToConnectToRoom = true;
        //PhotonNetwork.CreateRoom("Peter's Game 1"); //Create a specific Room - Error: OnCreateRoomFailed
        //PhotonNetwork.JoinRoom("Peter's Game 1");   //Join a specific Room   - Error: OnJoinRoomFailed  
        PhotonNetwork.JoinRandomRoom();               //Join a random Room     - Error: OnJoinRandomRoomFailed  
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);
        //no room available
        //create a room (null as a name means "does not matter")
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 20 });
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("OnCreateRoomFailed");
        base.OnCreateRoomFailed(returnCode, message);
        Debug.Log(message);
        base.OnCreateRoomFailed(returnCode, message);
        TriesToConnectToRoom = false;
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        TriesToConnectToRoom = false;
        Debug.Log("Master: " + PhotonNetwork.IsMasterClient + " | Players In Room: " + PhotonNetwork.CurrentRoom.PlayerCount + " | RoomName: " + PhotonNetwork.CurrentRoom.Name);
       // SceneManager.LoadScene("TestArea");
    }
}
