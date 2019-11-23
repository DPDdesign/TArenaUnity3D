using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MostStupidAIEver : MonoBehaviour
{

    public MouseControler MC;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public void AskAIwhattodo()
    {

        MC.getSelectedToster();

        //      List<HexClass> hexarea = new List<HexClass>(mouseControler.getHexUnderMouse().hexMap.GetHexesWithinRadiusOf(mouseControler.getHexUnderMouse(), aoeradius);
        List<HexClass> EnemyHexes = MC.GetEnemy();
        HexClass tempCel = null;
        foreach (HexClass h in EnemyHexes)
        {

            if (h.Tosters[0].isDead == false)
            {/*
                for (int i = 0; i < 6; i++)
                {
                    if (i == 0) { tempCel = h.hexMap.GetHexAt(h.C, h.R + 1); }
                    if (i == 1) { tempCel = h.hexMap.GetHexAt(h.C+1, h.R); }
                    if (i == 2) { tempCel = h.hexMap.GetHexAt(h.C+1, h.R - 1); }
                    if (i == 3) { tempCel = h.hexMap.GetHexAt(h.C, h.R - 1); }
                    if (i == 4) { tempCel = h.hexMap.GetHexAt(h.C-1, h.R ); }
                    if (i == 5) { tempCel = h.hexMap.GetHexAt(h.C-1, h.R + 1); }
                    */
             //  h.hexMap.GetHexesWithinRadiusOf(h, 2);
                List<HexClass> hexarea = new List<HexClass>(h.hexMap.GetHexesWithinRadiusOf(h, 1));
                hexarea.Remove(h);
                foreach (HexClass hex in hexarea)
                {
                    if (hex != null && hex.Tosters.Count == 0)
                    {

                          //tempCel = h.hexMap.GetHexAt(h.C, h.R - 1);

                        if (MC.getSelectedToster().IsPathAvaible(hex))
                        {
                            MC.StartCoroutine(MC.DoMoveAndAttackWithoutCheck(hex, h.Tosters[0],MC.getSelectedToster()));
                            return;
                        }
                    }
                }

            }
        }
        int count = 999;
        int hNo = 0;
        int tempi = 0;
        List<HexClass> hexpath = new List<HexClass>();
        List<HexClass> hexmaxpath = new List<HexClass>();
        foreach (HexClass h in EnemyHexes)
        {

            if (h.Tosters[0].isDead == false)
            {
                hexpath = new List<HexClass>(MC.getSelectedToster().Pathing(h, true));
                if (hexpath.Count < count)
                {
                    count = hexpath.Count;
                    hNo = tempi;
                }
            }
            tempi++;
        }

        hexpath = new List<HexClass>(MC.getSelectedToster().Pathing(EnemyHexes[hNo], true));
        hexmaxpath = new List<HexClass>();
        for (int i = 0; i < MC.getSelectedToster().GetMS(); i++)
            hexmaxpath.Add(hexpath[i]);

        StartCoroutine(MC.DoMovesST(hexmaxpath[hexmaxpath.Count-1],MC.getSelectedToster()));
        return;


    }
    #region ANALIZA
    TosterHexUnit TosterWithLeastHP(List<TosterHexUnit> tosters)
    {
        TosterHexUnit target = null;
        foreach (TosterHexUnit t in tosters)
        {
            if (target == null)
            {
                target = t;
            }
            else if ((t.Amount - 1) * t.GetHP() + t.TempHP < (target.Amount - 1) * target.GetHP() + target.TempHP)
            {
                target = t;
            }
        }
        return target;
    }

    /// Sortuje malejąco jednostki przeciwnika względem zadawanego przez nie damage jednostce.
    List<TosterHexUnit> ListOfDamageFromPlayer(List<TosterHexUnit> tosters, TosterHexUnit ai)
    {
        List<TosterHexUnit> target = new List<TosterHexUnit>();

        foreach (TosterHexUnit t in tosters)
        {
            // pierwszy toster
            if (target.Count == 0)
            {
                target.Add(t);
            }

            // Jezeli zadaje wiecej - daj go na poczatek
            else if (ai.CalculateDamageBetweenTosters(t, ai, 0) > ai.CalculateDamageBetweenTosters(target[0], ai, 0))
            {
                target.Insert(0, t);
            }

            // Jezeli zadaje mniej - sortuj
            else
            {
                bool sorting = true;
                int i = target.Count - 1;
                while (sorting)
                {
                    if (ai.CalculateDamageBetweenTosters(t, ai, 0) <= ai.CalculateDamageBetweenTosters(target[i], ai, 0))
                    {
                        target.Insert(i + 1, t);
                        sorting = false;
                    }
                    i--;
                }
            }
        }
        return target;
    }

    double MaxDamageToGet(List<TosterHexUnit> tosters, TosterHexUnit ai)
    {
        double i = ai.CalculateDamageBetweenTosters(ListOfDamageFromPlayer(tosters, ai)[0], ai, 0);
        return i;
    }

    /// Sortuje malejąco jednostki przeciwnika względem otrzymane przez nie damage od jednostki.
    List<TosterHexUnit> ListOfDamageToPlayer(List<TosterHexUnit> tosters, TosterHexUnit ai) //Tworzy liste To
    {
        List<TosterHexUnit> target = new List<TosterHexUnit>();

        foreach (TosterHexUnit t in tosters)
        {
            // pierwszy toster
            if (target.Count == 0)
            {
                target.Add(t);
            }

            // Jezeli otrzyma wiecej - daj go na poczatek
            else if (ai.CalculateDamageBetweenTosters(ai, t, 0) > ai.CalculateDamageBetweenTosters(ai, target[0], 0))
            {
                target.Insert(0, t);
            }

            // Jezeli otrzyma mniej - sortuj
            else
            {
                bool sorting = true;
                int i = target.Count - 1;
                while (sorting)
                {
                    if (ai.CalculateDamageBetweenTosters(ai, t, 0) <= ai.CalculateDamageBetweenTosters(ai, target[i], 0))
                    {
                        target.Insert(i + 1, t);
                        sorting = false;
                    }
                    i--;
                }
            }
        }
        return target;
    }

    double MaxDamageToDeal(List<TosterHexUnit> tosters, TosterHexUnit ai)
    {
        double i = ai.CalculateDamageBetweenTosters(ai, ListOfDamageToPlayer(tosters, ai)[0], 0);
        return i;
    }

    /// Wylicza wartosc oszczedzonych obrazen
    List<TosterHexUnit> ListOfTradeValues(List<TosterHexUnit> tosters, TosterHexUnit ai)
    {

        Debug.Log("**********************************************************");
        Debug.Log(ai.Name + "  MOWI: OBLICZAM WARTOSCI TRADE OF");
        List<TosterHexUnit> target = new List<TosterHexUnit>();

        // 1. POLICZ ILE KAZDY TOSTER ZADA CI DMG
        // 2. POLICZ ILE DMG ZADASZ TEMU TOSTEROWI
        // 2.1 POLICZ ILE ZGINIE TOSTERÓW W WYNIKU ATAKU
        // 3. POLICZ ILE ZADZADZĄ CI DMG PO TWOIM ATAKU
        // 4. OBLICZ RÓŻNICĘ MIĘDZY 4 a 1

        // 1.
        List<TosterHexUnit> PDamageList = ListOfDamageFromPlayer(tosters, ai);

        List<int> NewQuantieties = new List<int>();
        int i = 0;
        double damagedifftemp = 0;
        double damagediff;

        foreach (TosterHexUnit toster in PDamageList)
        {

            double damage = ai.CalculateDamageBetweenTosters(toster, ai, 0);
            Debug.Log(toster.Name + " zada mi " + damage);


            // ZJEBANE BO ZLE LICZY ILOSC
            double cdamage = ai.CalculateDamageBetweenTosters(ai, toster, 0);
            Debug.Log("Zadam " + cdamage + " " + toster.Name);
            NewQuantieties.Add(toster.Amount - Mathf.FloorToInt((float)cdamage / toster.GetHP()));


            double damage2 = ai.CalculateDamageBetweenTostersWithQ(toster, ai, 0, NewQuantieties[i]);
            i++;
            Debug.Log("Teraz " + toster.Name + " Zada mi " + damage2.ToString());
            damagediff = damage - damage2;

            Debug.Log("dzieki temu oszczedze " + damagediff);

            if (target.Count == 0)
            {
                target.Add(toster);
                damagedifftemp = damagediff;
            }

            else if (damagediff > damagedifftemp)
            {
                target.Insert(0, toster);
                damagedifftemp = damagediff;
            }

            // ZJEBANE BO NIE SORTUJE :X
            else
            {
                target.Add(toster);
            }

        }
        //Amount = Mathf.FloorToInt(newhp / GetHP());
        Debug.Log("**********************************************************");
        Debug.Log(ai.Name + "  WYCIAGA WNIOSEK: Powinienem focusowac" + target[0].Name);
        return target;
    }
    #endregion





}



