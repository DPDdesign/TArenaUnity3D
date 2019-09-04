using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryObjects
{
    uint TCprice;
    uint ATprice;
    string id;
    string Type;

   public InventoryObjects(string id, string type, uint tcost, uint acost )
    {
        this.id = id;
        this.Type = type;
        if (type=="ArmyBundle")
        {
            ATprice = acost;
        }
        if (type == "Unit")
        {
            TCprice = tcost;
        }
    }
    public InventoryObjects(string id, string type)
    {
        this.id = id;
        this.Type = type;
    }
    public string Id { get => id; set => id = value; }
    public string Type1 { get => Type; set => Type = value; }
    public uint ATprice1 { get => ATprice; set => ATprice = value; }
    public uint TCprice1 { get => TCprice; set => TCprice = value; }
}
