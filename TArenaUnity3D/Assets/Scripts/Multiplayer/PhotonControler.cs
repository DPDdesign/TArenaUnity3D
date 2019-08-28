using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
using UnityEngine.UI;
public class PhotonControler : MonoBehaviourPunCallbacks
{
    public string GameVersion = "pre-alpha";
    public Text ConnectState;
    public string Region = "eu";

    // Start is called before the first frame update
    void Start()
    {
        ServerSettings settings = PhotonNetwork.PhotonServerSettings;
        settings.AppSettings.FixedRegion = Region;

        PhotonNetwork.ConnectUsingSettings();
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("We are now connected to photon!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void FixedUpdate()
    {
     // ConnectState.text = PhotonNetwork.connectionStateDetailed.ToString();
    }
    public void connecttogheter()
    {
        
      //PhotonNetwork.ConnectUsingSettings(GameVersion);
    }
}
