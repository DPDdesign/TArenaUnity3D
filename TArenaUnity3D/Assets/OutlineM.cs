using cakeslice;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineM : MonoBehaviour
{
    public HexClass SHex, EHex, WHex;

    void Start()
    {
    }

    void Update()
    {
    }

    public void SetHexSelectedToster(HexClass hex)
    {
        if (SHex != null && SHex != hex)
        {
            DisableOutline(SHex);
        }

        SHex = hex;
        EnableOutline(hex, 1);
    }

    public void unSetHexSelectedToster()
    {
        if (SHex != null)
        {
            DisableOutline(SHex);
            SHex = null;
        }
    }

    public void SetHexEnemyToster(HexClass hex)
    {
        if (EHex != null && EHex != hex && EHex != SHex)
        {
            DisableOutline(EHex);
        }

        EHex = hex;
        EnableOutline(hex, 2);
    }

    public void unSetHexEnemyToster()
    {
        if (EHex != null && SHex != EHex)
        {
            DisableOutline(EHex);
        }

        EHex = null;
    }

    public void SetHexWhiteToster(HexClass hex)
    {
        if (WHex != null && WHex != hex && WHex != SHex)
        {
            DisableOutline(WHex);
        }

        WHex = hex;
        EnableOutline(hex, 0);
    }

    public void unSetHexWhiteToster()
    {
        if (WHex != null && SHex != WHex)
        {
            DisableOutline(WHex);
        }

        WHex = null;
    }

    static void EnableOutline(HexClass hex, int color)
    {
        if (hex == null || hex.MyHex == null)
        {
            return;
        }

        Outline outline = hex.MyHex.GetComponentInChildren<Outline>();
        if (outline == null)
        {
            return;
        }

        outline.eraseRenderer = false;
        outline.color = color;
    }

    static void DisableOutline(HexClass hex)
    {
        if (hex == null || hex.MyHex == null)
        {
            return;
        }

        Outline outline = hex.MyHex.GetComponentInChildren<Outline>();
        if (outline == null)
        {
            return;
        }

        outline.eraseRenderer = true;
        outline.color = 0;
    }
}
