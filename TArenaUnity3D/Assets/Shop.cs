using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    public List<Button> Bundles, Units, Skins, Units2;
    public int Barbarians=0, Lizards=0, Golems=0, Shadows=0;
    public GameObject BuyMenu;
    public Text totalM, totalC, leftM, nameOfObject, Error;
    public string objToBuy, typeOfObject, totalCost, typeOfCurrency;
    public Button BuyButton;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnEnable()
    {
        Debug.Log("OnEnable");

        PlayFabControler.PFC.GetInventory(this);

    }
    public void Reload()
    {
        //StartCoroutine(PlayFabControler.PFC.GetInventoryI());
        LoadAll2();
        LoadAll();
    }
    public void LoadAll2()
    {
        foreach (InventoryObjects shopobj in PlayFabControler.PFC.storeObjects)
        {
            if (shopobj.Type1 == "Unit")
            {
                foreach (Button b in Units2)
                {
                    if (b.name == shopobj.Id)
                    {

                        Debug.Log("test");
                        //  Text[] texts = b.gameObject.GetComponentsInChildren<Text>();
                        //  texts[0].gameObject.SetActive(true);
                        bool inter = false;
                        foreach (InventoryObjects inventoryObjects in PlayFabControler.PFC.inventoryObjects)
                        {
                            if (inventoryObjects.Id == shopobj.Id)
                            {
                                // b.interactable = true;
                                inter = true;
                                //        texts[0].gameObject.SetActive(false);
                            }
                        }
                        b.interactable = inter;
                        //b.GetComponentInChildren<Text>().gameObject.SetActive(!inter);
                    }
                }

            }

        }


    }
    public void LoadAll()
    {
        foreach (InventoryObjects shopobj in PlayFabControler.PFC.storeObjects)
        {
            if (shopobj.Type1 == "ArmyBundle")
            {
                foreach (Button b in Bundles)
                {
                    if (b.name == shopobj.Id)
                    {
                        b.interactable = true;
                        Text[] texts = b.gameObject.GetComponentsInChildren<Text>();
                        foreach (Text t in texts)
                        {
                            if (t.name == "Cost")
                            {
                                t.text = shopobj.ATprice1.ToString();
                                if (shopobj.ATprice1 > PlayFabControler.PFC.aTokens)
                                {
                                    texts[0].text = "Too Expensive";
                                    b.interactable = false;
                                }
                            }
                        }

                    }
                }

                //bundle
            }
            else if (shopobj.Type1 == "Unit")
            {
                foreach (Button b in Units)
                {
                    if (b.name == shopobj.Id)
                    {
                        b.interactable = true;
                    
                        Text[] texts = b.gameObject.GetComponentsInChildren<Text>();
                        foreach (Text t in texts)
                        {
                            if (t.name == "Cost")
                            {
                                t.text = shopobj.TCprice1.ToString();
                                if (shopobj.TCprice1 > PlayFabControler.PFC.tCoins)
                                {
                                    texts[0].text = "Too Expensive";
                                    b.interactable = false;
                                }
                            }
                        }
                        foreach (InventoryObjects inventoryObjects in PlayFabControler.PFC.inventoryObjects)
                        {
                            if (inventoryObjects.Id == shopobj.Id)
                            {
                                texts[0].text = "OWNED";
                                b.interactable = false;
                           
                            }
                        }
                    }
                }

            }

        }

         
    }



   public void ShowBuyMenu(Button b)
    {
        BuyMenu.SetActive(true);
        objToBuy = b.name;
        nameOfObject.text = objToBuy;
        Text[] texts = b.gameObject.GetComponentsInChildren<Text>();
        foreach (Text t in texts)
        {
            if (t.name == "Cost")
            {
                totalCost = t.text;
                totalC.text = "-" + totalCost;
            }


            if(Bundles.Contains(b))

            {
                typeOfCurrency = "AT";
                typeOfObject = "ArmyBundle";

            }
            else
            {
                typeOfCurrency = "TC";
                typeOfObject = "Unit";
            }
        }
        if (typeOfObject == "ArmyBundle")
        {
            Debug.LogError("here");
           if (PlayFabControler.PFC.CheckIfPlayerGotBundle(objToBuy))
            {
                Error.gameObject.SetActive(false);
                BuyButton.interactable = true;
            }
           else
            {
                Error.gameObject.SetActive(true);
                BuyButton.interactable = false;
            }

            totalM.text = PlayFabControler.PFC.aTokens.ToString();
            leftM.text =( PlayFabControler.PFC.aTokens - System.Convert.ToInt32(totalCost)).ToString();
            
           

        } else
        if (typeOfObject == "Unit")
        {
            Error.gameObject.SetActive(false);
            BuyButton.interactable = true;
            totalM.text = PlayFabControler.PFC.tCoins.ToString();
            leftM.text = (PlayFabControler.PFC.tCoins - System.Convert.ToInt32(totalCost)).ToString();

        }

    }




    public void Buy()
    {
        if (PlayFabControler.PFC.BuyItem(objToBuy, totalCost, typeOfCurrency,this))
        {
           // Debug.Log("dasda");
         //   PlayFabControler.PFC.GetInventory(this);
        }
       
        BuyMenu.SetActive(false);
   
    }
    // Update is called once per frame
    void Update()
    {

    }
}
