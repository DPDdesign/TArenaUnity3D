using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using UnityEngine.UI;
public class PhotonControler : Photon.PunBehaviour 
{
    public string GameVersion;
    public Text ConnectState;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void FixedUpdate()
    {
        ConnectState.text = PhotonNetwork.connectionStateDetailed.ToString();
    }
    public void connecttogheter()
    {
        
        PhotonNetwork.ConnectUsingSettings(GameVersion);
    }
}
