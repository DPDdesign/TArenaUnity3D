using cakeslice;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineM : MonoBehaviour
{
    public HexClass SHex, EHex, WHex;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetHexSelectedToster(HexClass hex)
    {
        SHex = hex;
        hex.MyHex.GetComponentInChildren<Outline>().eraseRenderer = false;
        hex.MyHex.GetComponentInChildren<Outline>().color = 1;

    }
    public void unSetHexSelectedToster()
    {
        if (SHex != null)
        {
            SHex.MyHex.GetComponentInChildren<Outline>().eraseRenderer = true;
            SHex.MyHex.GetComponentInChildren<Outline>().color = 0;
        }
    }
    public void SetHexEnemyToster(HexClass hex)
    {
        EHex = hex;
        hex.MyHex.GetComponentInChildren<Outline>().eraseRenderer = false;
        hex.MyHex.GetComponentInChildren<Outline>().color = 2;
    }
    public void unSetHexEnemyToster()
    {
        if (EHex != null&&SHex != EHex)
        {
            EHex.MyHex.GetComponentInChildren<Outline>().eraseRenderer = true;
            EHex.MyHex.GetComponentInChildren<Outline>().color = 0;
        }
    }
    public void SetHexWhiteToster(HexClass hex)
    {
        WHex = hex;
        hex.MyHex.GetComponentInChildren<Outline>().eraseRenderer = false;
        hex.MyHex.GetComponentInChildren<Outline>().color = 0;
    }
    public void unSetHexWhiteToster()
    {
        if (WHex != null && SHex!=WHex)
        {
            WHex.MyHex.GetComponentInChildren<Outline>().eraseRenderer = true;
            WHex.MyHex.GetComponentInChildren<Outline>().color = 0;
        }
    }
}
