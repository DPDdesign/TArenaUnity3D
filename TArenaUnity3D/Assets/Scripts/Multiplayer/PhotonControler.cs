using UnityEngine;
using UnityEngine.UI;

public class PhotonControler : MonoBehaviour
{
    public string GameVersion = "pre-alpha";
    public Text ConnectState;
    public string Region = "eu";

    void Start()
    {
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("Local mode active.");
    }

    public void FixedUpdate()
    {
        if (ConnectState != null)
        {
            ConnectState.text = "Local";
        }
    }

    public void connecttogheter()
    {
    }
}
