using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
 public  List<InventoryObjects> inventoryObjects;
    public int tCoins;
    public int aTokens;
    // Start is called before the first frame update
    void Start()
    {
        inventoryObjects = new List<InventoryObjects>();
    }

    public void GetInventoryFromServer()
    {
       
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
