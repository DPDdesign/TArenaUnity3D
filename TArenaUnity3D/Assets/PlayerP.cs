using UnityEngine;

public class PlayerP : MonoBehaviour
{
    public MouseControler mouseControler;

    void Start()
    {
        mouseControler = FindObjectOfType<MouseControler>();
    }

    public static void RefreshInstance(ref PlayerP player, PlayerP Prefab)
    {
        Quaternion rotation = Quaternion.identity;
        if (player != null)
        {
            rotation = player.transform.rotation;
            Destroy(player.gameObject);
        }

        player = Instantiate(Prefab, new Vector3(25, 3, 8), rotation);
    }
}
