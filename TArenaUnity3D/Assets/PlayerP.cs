using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerP : MonoBehaviourPun, IPunObservable
{
    //public CameraRotator cameraRotator;
    // Start is called before the first frame update
    public MouseControler mouseControler;

    void Start()
    {
        
        mouseControler = FindObjectOfType<MouseControler>();
    }
    
    private void Awake()
    {
       // if (!photonView.IsMine && GetComponent<CameraRotator>() != null)
       //     Destroy(GetComponent<CameraRotator>());
    }
    // Update is called once per frame

    public static void RefreshInstance(ref PlayerP player, PlayerP Prefab)
    {
        var position = Vector3.zero;
        var rotation = Quaternion.identity;
        if (player != null)
        {
            position = player.transform.position;
            rotation = player.transform.rotation;
            PhotonNetwork.Destroy(player.gameObject);
        }
        
        player = PhotonNetwork.Instantiate(Prefab.gameObject.name, new Vector3(25,3,8), rotation).GetComponent<PlayerP>();
    }
    void Update()
    {
        
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
       
    }
}
