using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MostStupidAIEver : MonoBehaviour
{
    public MouseControler MC;
    TacticalAIAsyncTurnIntegrator asyncTurnIntegrator;
    Coroutine activeAiRoutine;
    bool battleNotReadyWarningShown;
    int battleReadyObservedFrame = -1;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDisable()
    {
        if (activeAiRoutine != null)
        {
            StopCoroutine(activeAiRoutine);
            activeAiRoutine = null;
        }
    }


    public void AskAIwhattodo()
    {
        if (activeAiRoutine != null)
        {
            return;
        }

        if (IsBattleReadyForAI() == false)
        {
            if (battleNotReadyWarningShown == false)
            {
                battleNotReadyWarningShown = true;
                Debug.LogWarning("[TacticalAI] waiting-for-ready-battle-state");
            }

            return;
        }

        battleNotReadyWarningShown = false;
        EnsureAsyncTurnIntegrator();
        activeAiRoutine = StartCoroutine(RunAsyncTacticalAI());
    }

    IEnumerator RunAsyncTacticalAI()
    {
        TacticalAILiveTurnIntegrationResult result;
        if (asyncTurnIntegrator.TryBeginTurn(out result) == false)
        {
            HandleAsyncTurnResult(result);
            activeAiRoutine = null;
            yield break;
        }

        while (asyncTurnIntegrator.TryCompleteTurn(out result) == false)
        {
            yield return null;
        }

        HandleAsyncTurnResult(result);
        activeAiRoutine = null;
    }

    void HandleAsyncTurnResult(TacticalAILiveTurnIntegrationResult result)
    {
        if (result != null && result.Started)
        {
            return;
        }

        Debug.LogWarning(TacticalAILiveTurnIntegrator.BuildFallbackLog(
            result != null ? result.ActorUnitId : string.Empty,
            result != null ? result.Plan : null,
            result != null ? result.ExecutionResult : null,
            result != null ? result.FallbackReason : "UnknownAsyncFailure"));
        Debug.LogError("[TacticalAI] no shared-rule AI action was started; legacy fallback is disabled for PRD049 shared execution.");
    }

    bool IsBattleReadyForAI()
    {
        bool ready = MC != null &&
            MC.getSelectedToster() != null &&
            HexMap.Instance != null &&
            HexMap.Instance.IsBattleReadyForTacticalActions &&
            BattleActionLifecycle.IsActionBlocking == false;

        if (ready == false)
        {
            battleReadyObservedFrame = -1;
            return false;
        }

        if (battleReadyObservedFrame < 0)
        {
            battleReadyObservedFrame = Time.frameCount;
            return false;
        }

        return Time.frameCount - battleReadyObservedFrame >= 2;
    }

    void EnsureAsyncTurnIntegrator()
    {
        if (asyncTurnIntegrator != null)
        {
            return;
        }

        asyncTurnIntegrator = TacticalAIAsyncTurnIntegrator.CreateFromScene(
            TacticalAIProfileCatalog.LoadNormalProfileAsset());
    }

    void RunLegacyFallbackAI()
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
                            MC.TryStartMoveAndAttackAction(hex, h.Tosters[0], MC.getSelectedToster());
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

        MC.TryStartMoveAction(hexmaxpath[hexmaxpath.Count-1], MC.getSelectedToster());
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
            else if (PredictCommittedCombatDamage(t, ai, "legacy-ai-incoming") > PredictCommittedCombatDamage(target[0], ai, "legacy-ai-incoming"))
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
                    if (PredictCommittedCombatDamage(t, ai, "legacy-ai-incoming") <= PredictCommittedCombatDamage(target[i], ai, "legacy-ai-incoming"))
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
        double i = PredictCommittedCombatDamage(ListOfDamageFromPlayer(tosters, ai)[0], ai, "legacy-ai-incoming");
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
            else if (PredictCommittedCombatDamage(ai, t, "legacy-ai-outgoing") > PredictCommittedCombatDamage(ai, target[0], "legacy-ai-outgoing"))
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
                    if (PredictCommittedCombatDamage(ai, t, "legacy-ai-outgoing") <= PredictCommittedCombatDamage(ai, target[i], "legacy-ai-outgoing"))
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
        double i = PredictCommittedCombatDamage(ai, ListOfDamageToPlayer(tosters, ai)[0], "legacy-ai-outgoing");
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

            double damage = PredictCommittedCombatDamage(toster, ai, "legacy-ai-incoming");
            Debug.Log(toster.Name + " zada mi " + damage);


            // ZJEBANE BO ZLE LICZY ILOSC
            double cdamage = PredictCommittedCombatDamage(ai, toster, "legacy-ai-outgoing");
            Debug.Log("Zadam " + cdamage + " " + toster.Name);
            NewQuantieties.Add(toster.Amount - Mathf.FloorToInt((float)cdamage / toster.GetHP()));


            double damage2 = PredictCommittedCombatDamage(toster, ai, "legacy-ai-incoming-after-trade");
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

    double PredictCommittedCombatDamage(TosterHexUnit attacker, TosterHexUnit defender, string rollPurpose)
    {
        int damage;
        string error;
        if (LiveCombatDamageResolver.TryCalculateCommittedDamage(
            attacker,
            defender,
            rollPurpose,
            1.0,
            out damage,
            out error))
        {
            return damage;
        }

        Debug.LogWarning("[TacticalAI] legacy heuristic damage prediction failed: " + error);
        return 0;
    }
    #endregion





}



