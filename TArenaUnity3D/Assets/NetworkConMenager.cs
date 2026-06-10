using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkConne22ctionManager : MonoBehaviour
{
    public Button BtnConnectMaster;
    public Button BtnConnectRoom;
    public bool TriesToConnectToMaster;
    public bool TriesToConnectToRoom;

    void Start()
    {
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
        Debug.Log("Local mode selected.");
    }

    public void OnClickConnectToRoom()
    {
        LoadBattleScene();
    }

    public void OnJoinRandomFailed(short returnCode, string message)
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
        Debug.Log("Local room ready.");
    }

    private void LoadBattleScene()
    {
        SceneManager.LoadScene("TestArea2");
    }
}
