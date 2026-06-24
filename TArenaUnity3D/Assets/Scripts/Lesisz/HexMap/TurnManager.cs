using System.Collections;
using System.Collections.Generic;
using TimeSpells;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    List<TeamClass> Teams;
    HexMap hexMap;
    MouseControler MController;
    public int Tura;
    public GameObject TurnBar;
    public List<TosterHexUnit> TostersQue;
    public List<TosterHexUnit> TostersQuep;
    public List<GameObject> QueImages;
    public GameObject pauza;
    const int QueueUnitPreviewWidth = 200;
    const int QueueTurnBarPreviewWidth = 50;
    const int QueueExtraTurnBarPreviewWidth = 100;
    const int QueuePreviewMaxWidth = QueueUnitPreviewWidth * 13 + QueueTurnBarPreviewWidth * 2;
    List<GameObject> spawnedQueueTurnBars = new List<GameObject>();
    bool resolvingNewTurnSequence;
    bool advanceRoundAfterNewTurnSequence;
    bool isDestroying;

    public bool IsResolvingNewTurnSequence
    {
        get { return resolvingNewTurnSequence; }
    }
   
    private void Start()
    {
        hexMap = FindObjectOfType<HexMap>();
        MController = FindObjectOfType<MouseControler>();
      Tura = 1;
      UpdateTurnNumberText();
    }

    private void OnDestroy()
    {
        isDestroying = true;
        resolvingNewTurnSequence = false;
        advanceRoundAfterNewTurnSequence = false;
    }


public void SetNewTurn()
{
    if (isDestroying)
    {
        return;
    }

    UpdateTurnNumberText();
}

void UpdateTurnNumberText()
{
    if (isDestroying)
    {
        return;
    }

    Transform turnNumberTransform = FindTurnNumberTransform();
    if (turnNumberTransform != null)
    {
        SetUiText(turnNumberTransform, Tura.ToString());
    }
}

Transform FindTurnNumberTransform()
{
    if (isDestroying)
    {
        return null;
    }

    Transform turnNumberTransform = null;

    turnNumberTransform = FindQueueChild(transform, "Turn_Number");

    if (turnNumberTransform == null)
    {
        GameObject turnNumberObject = GameObject.Find("Turn_Number");
        if (turnNumberObject != null)
        {
            turnNumberTransform = turnNumberObject.transform;
        }
    }

    return turnNumberTransform;
}

////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////




public void GetTostersQueue()
{
    Teams = hexMap.Teams;
    TostersQue = new List<TosterHexUnit>();
    TostersQuep = new List<TosterHexUnit>();

    ResetQueuePreview();

    if (Teams == null || Teams.Count < 2 || QueImages == null || QueImages.Count == 0)
    {
        return;
    }

    int queueSlotIndex = 0;
    int previewRound = Tura;
    int separatorIndex = 0;
    int previewWidth = 0;
    bool currentRound = true;
    bool previewIsFull = false;

    int turnBarPreviewCost = GetQueueTurnBarPreviewCost(separatorIndex);
    if (CanFitQueuePreviewElement(previewWidth, turnBarPreviewCost))
    {
        ShowQueueTurnBar(separatorIndex, previewRound, null);
        separatorIndex++;
        previewWidth += turnBarPreviewCost;
    }

    while (queueSlotIndex < QueImages.Count && previewIsFull == false)
    {
        TeamClass team1 = CreateQueuePreviewTeam(0);
        TeamClass team2 = CreateQueuePreviewTeam(1);
        bool roundHadUnit = false;

        while (queueSlotIndex < QueImages.Count)
        {
            TosterHexUnit nextToster = currentRound ? AskWhosTurnSimulator(team1, team2) : AskWhosTurnFutureRoundSimulator(team1, team2);
            if (nextToster == null)
            {
                break;
            }

            if (CanFitQueuePreviewElement(previewWidth, QueueUnitPreviewWidth) == false)
            {
                previewIsFull = true;
                break;
            }

            roundHadUnit = true;
            SetQueImages(QueImages[queueSlotIndex], nextToster);
            previewWidth += QueueUnitPreviewWidth;

            if (currentRound)
            {
                TostersQue.Add(nextToster);
            }
            else
            {
                TostersQuep.Add(nextToster);
            }

            RemoveQueuePreviewUnit(team1, team2, nextToster);
            queueSlotIndex++;
        }

        if (previewIsFull)
        {
            break;
        }

        if (roundHadUnit == false)
        {
            if (currentRound && HasAnyPreviewUnit())
            {
                currentRound = false;
                previewRound++;
                continue;
            }

            break;
        }

        if (queueSlotIndex < QueImages.Count)
        {
            turnBarPreviewCost = GetQueueTurnBarPreviewCost(separatorIndex);
            if (CanFitQueuePreviewElement(previewWidth, turnBarPreviewCost) == false)
            {
                break;
            }

            previewRound++;
            ShowQueueTurnBar(separatorIndex, previewRound, QueImages[queueSlotIndex - 1]);
            separatorIndex++;
            previewWidth += turnBarPreviewCost;
        }

        currentRound = false;
    }
}

bool CanFitQueuePreviewElement(int currentWidth, int elementWidth)
{
    return currentWidth + elementWidth <= QueuePreviewMaxWidth;
}

int GetQueueTurnBarPreviewCost(int separatorIndex)
{
    if (separatorIndex < 2)
    {
        return QueueTurnBarPreviewWidth;
    }

    return QueueTurnBarPreviewWidth + QueueExtraTurnBarPreviewWidth;
}

void ResetQueuePreview()
{
    if (QueImages != null)
    {
        foreach (GameObject queueImage in QueImages)
        {
            if (queueImage != null)
            {
                queueImage.SetActive(false);
            }
        }
    }

    foreach (GameObject turnBar in spawnedQueueTurnBars)
    {
        if (turnBar != null)
        {
            turnBar.SetActive(false);
            Destroy(turnBar);
        }
    }

    spawnedQueueTurnBars.Clear();
}

TeamClass CreateQueuePreviewTeam(int teamIndex)
{
    TeamClass previewTeam = new TeamClass();
    if (Teams != null && teamIndex >= 0 && teamIndex < Teams.Count && Teams[teamIndex] != null)
    {
        previewTeam.Tosters.AddRange(Teams[teamIndex].Tosters);
    }

    return previewTeam;
}

void RemoveQueuePreviewUnit(TeamClass team1, TeamClass team2, TosterHexUnit toster)
{
    if (team1.Tosters.Remove(toster) == false)
    {
        team2.Tosters.Remove(toster);
    }
}

bool HasAnyPreviewUnit()
{
    if (Teams == null)
    {
        return false;
    }

    foreach (TeamClass team in Teams)
    {
        if (team == null)
        {
            continue;
        }

        foreach (TosterHexUnit toster in team.Tosters)
        {
            if (CanReceiveTurn(toster))
            {
                return true;
            }
        }
    }

    return false;
}

TosterHexUnit AskWhosTurnFutureRoundSimulator(TeamClass team1, TeamClass team2)
{
    TosterHexUnit TeamRed = team1.AskForUnitSimulator();
    TosterHexUnit TeamBlue = team2.AskForUnitSimulator();

    if (TeamRed == TeamBlue)
    {
        return null;
    }

    if (TeamRed == null) return TeamBlue;
    if (TeamBlue == null) return TeamRed;

    if (TeamBlue.GetIni() > TeamRed.GetIni())
    {
        return TeamBlue;
    }

    if (TeamRed.GetIni() > TeamBlue.GetIni())
    {
        return TeamRed;
    }

    if (TeamBlue.GetMS() > TeamRed.GetMS())
    {
        return TeamBlue;
    }

    if (TeamRed.GetMS() > TeamBlue.GetMS())
    {
        return TeamRed;
    }

    int redIndex = 0;
    int blueIndex = 0;
    for (int i = 0; i < Teams[0].Tosters.Count; i++)
    {
        if (Teams[0].Tosters[i] == TeamRed)
        {
            redIndex = i;
        }
    }

    for (int i = 0; i < Teams[1].Tosters.Count; i++)
    {
        if (Teams[1].Tosters[i] == TeamBlue)
        {
            blueIndex = i;
        }
    }

    if (redIndex > blueIndex)
    {
        return TeamBlue;
    }

    return TeamRed;
}

void ShowQueueTurnBar(int separatorIndex, int roundNumber, GameObject previousQueueImage)
{
    GameObject turnBar = GetQueueTurnBar(separatorIndex);
    if (turnBar == null)
    {
        return;
    }

    Transform queueParent = previousQueueImage != null ? previousQueueImage.transform.parent : FindQueueParent();
    if (queueParent != null && turnBar.transform.parent != queueParent)
    {
        turnBar.transform.SetParent(queueParent, false);
    }

    turnBar.SetActive(true);
    SetQueueElementPreferredWidth(turnBar, QueueTurnBarPreviewWidth);
    SetTurnBarNumber(turnBar, roundNumber);
    if (previousQueueImage != null)
    {
        turnBar.transform.SetSiblingIndex(previousQueueImage.transform.GetSiblingIndex() + 1);
    }
    else if (QueImages != null && QueImages.Count > 0 && QueImages[0] != null)
    {
        turnBar.transform.SetSiblingIndex(QueImages[0].transform.GetSiblingIndex());
    }
}

void SetQueueElementPreferredWidth(GameObject queueElement, float preferredWidth)
{
    LayoutElement layoutElement = queueElement.GetComponent<LayoutElement>();
    if (layoutElement == null)
    {
        layoutElement = queueElement.AddComponent<LayoutElement>();
    }

    layoutElement.preferredWidth = preferredWidth;
    layoutElement.flexibleWidth = 0f;
}

Transform FindQueueParent()
{
    if (QueImages != null)
    {
        foreach (GameObject queueImage in QueImages)
        {
            if (queueImage != null)
            {
                return queueImage.transform.parent;
            }
        }
    }

    if (TurnBar != null)
    {
        return TurnBar.transform.parent;
    }

    return null;
}

GameObject GetQueueTurnBar(int separatorIndex)
{
    if (TurnBar == null)
    {
        return null;
    }

    while (spawnedQueueTurnBars.Count <= separatorIndex)
    {
        GameObject newTurnBar = Instantiate(TurnBar);
        newTurnBar.name = "Turn_Bar_Preview_" + (spawnedQueueTurnBars.Count + 1).ToString();
        spawnedQueueTurnBars.Add(newTurnBar);
    }

    return spawnedQueueTurnBars[separatorIndex];
}

void SetTurnBarNumber(GameObject turnBar, int roundNumber)
{
    Transform turnNumberTransform = FindQueueChild(turnBar.transform, "Turn_Number");
    if (turnNumberTransform == null)
    {
        turnNumberTransform = FindQueueChild(turnBar.transform, "TQAmount");
    }
    if (turnNumberTransform == null)
    {
        turnNumberTransform = FindQueueChild(turnBar.transform, "TEXT");
    }

    if (turnNumberTransform == null)
    {
        return;
    }

    SetUiText(turnNumberTransform, roundNumber.ToString());
}




void SetQueImages(GameObject parentobject, TosterHexUnit toster)
{
parentobject.SetActive(true);
    SetQueueElementPreferredWidth(parentobject, QueueUnitPreviewWidth);

    SetQueuePlayerBars(parentobject.transform, toster.teamN);

    Transform spriteTransform = FindQueueChild(parentobject.transform, "TQSprite");
    if (spriteTransform != null)
    {
        Image spriteImage = spriteTransform.GetComponent<Image>();
        if (spriteImage != null)
        {
            spriteImage.sprite = DataMapper.Instance.LoadUnitSprite(toster.TosterSpriteName);
        }
    }

    Transform amountTransform = FindQueueChild(parentobject.transform, "TQAmount");
    if (amountTransform != null)
    {
        SetUiText(amountTransform, toster.Amount.ToString());
    }

}

void SetUiText(Transform textTransform, string value)
{
    TMP_Text tmpText = textTransform.GetComponent<TMP_Text>();
    if (tmpText != null)
    {
        tmpText.text = value;
        return;
    }

    Text legacyText = textTransform.GetComponent<Text>();
    if (legacyText != null)
    {
        legacyText.text = value;
    }
}

Transform FindQueueChild(Transform parent, string childName)
{
    Transform directChild = parent.Find(childName);
    if (directChild != null)
    {
        return directChild;
    }

    Transform[] children = parent.GetComponentsInChildren<Transform>(true);
    foreach (Transform child in children)
    {
        if (child.name == childName)
        {
            return child;
        }
    }

    return null;
}

void SetQueuePlayerBars(Transform parent, bool isFirstPlayer)
{
    Transform[] children = parent.GetComponentsInChildren<Transform>(true);
    foreach (Transform child in children)
    {
        if (child.name.StartsWith("Red_Player"))
        {
            child.gameObject.SetActive(isFirstPlayer);
        }
        else if (child.name.StartsWith("Blue_Player"))
        {
            child.gameObject.SetActive(!isFirstPlayer);
        }
    }
}



  public int isAnyoneAlive()
    {
        Teams = hexMap.Teams;
        if (Teams[0].IsMyTeamDEAD())
        {
            return 1;
        }

        if (Teams[1].IsMyTeamDEAD())
        {
            return 2;
        }
        return 0;
    }


    public void StartGame()
    {
        Teams = hexMap.Teams;
        TryStartNewTurnSequence(false);

    }



    public TosterHexUnit AskWhosTurn()
    {
        if (resolvingNewTurnSequence || BattleActionLifecycle.IsActionBlocking)
        {
            return null;
        }

        Teams = hexMap.Teams;

        TosterHexUnit TeamRed = Teams[0].AskForUnit();
        TosterHexUnit TeamBlue = Teams[1].AskForUnit();

        if (TeamRed == TeamBlue)
        {
            TryStartNewTurnSequence(true);
            return null;
        }

      
        if (TeamRed == null) return TeamBlue;
        if (TeamBlue == null) return TeamRed;
       
        if (TeamBlue.GetIni() > TeamRed.GetIni() && TeamBlue.Waited != true || (TeamBlue.GetIni() <= TeamRed.GetIni() && TeamRed.Waited == true && TeamBlue.Waited != true))
        {
            return TeamBlue;
        }
        if (TeamRed.GetIni() > TeamBlue.GetIni() && TeamRed.Waited != true|| (TeamRed.GetIni()<=TeamBlue.GetIni() && TeamBlue.Waited == true && TeamRed.Waited != true))
        {
            return TeamRed;
        }
        if (TeamRed.GetIni() == TeamBlue.GetIni() && TeamRed.Waited == false && TeamBlue.Waited == false)
        {
          
            if (TeamBlue.GetMS() > TeamRed.GetMS())
            {
                return TeamBlue;
            }
            if (TeamRed.GetMS() > TeamBlue.GetMS())
            {
                return TeamRed;
            }
            int t = 0;
            int o = 0;
            for (int i = 0; i < Teams[0].Tosters.Count; i++) { if (Teams[0].Tosters[i] == TeamRed) t = i; }
            for (int i = 0; i < Teams[1].Tosters.Count; i++) { if (Teams[1].Tosters[i] == TeamBlue) o = i; }
            if (t > o)
            {
                return TeamBlue;
            }
            else
            {
                return TeamRed;
            }
        }

        // zostały == lub wait
        if (TeamBlue.Waited == true && TeamRed.Waited == true)
        {
           
            if (TeamBlue.GetIni() < TeamRed.GetIni())
            {
                return TeamBlue;
            }
            if (TeamRed.GetIni() < TeamBlue.GetIni())
            {
                return TeamRed;
            }
          
            if (TeamRed.GetIni() == TeamBlue.GetIni())
            {
                if (TeamBlue.GetMS() < TeamRed.GetMS())
                {
                    return TeamBlue;
                }
                if (TeamRed.GetMS() < TeamBlue.GetMS())
                {
                    return TeamRed;
                }
                if (TeamRed.GetMS() == TeamBlue.GetMS())
                {
                    int t = 0;
                    int o = 0;
                    for (int i = 0; i < Teams[0].Tosters.Count; i++) { if (Teams[0].Tosters[i] == TeamRed) t = i; }
                    for (int i = 0; i < Teams[1].Tosters.Count; i++) { if (Teams[1].Tosters[i] == TeamBlue) o = i; }

                    if (o > t || o == t)
                    {
                        return TeamBlue;
                    }
                    else
                    {
                        return TeamRed;
                    }
                }
            }
        }
        Debug.LogError("TeamRED: " + TeamRed.Waited + " , " + TeamRed.GetIni());
        Debug.LogError("TeamBlue: " + TeamBlue.Waited + " , " + TeamBlue.GetIni());
        return null;

    }

    bool TryStartNewTurnSequence(bool advanceRoundCounter)
    {
        if (resolvingNewTurnSequence)
        {
            return false;
        }

        resolvingNewTurnSequence = true;
        advanceRoundAfterNewTurnSequence = advanceRoundCounter;

        bool started = BattleActionLifecycle.EnsureInstance().TryRunAction(
            null,
            BattleActionLifecycleKind.Automatic,
            "NewTurn",
            null,
            () => RunNewTurnSequence(),
            CompleteNewTurnSequence,
            0f);

        if (started == false)
        {
            resolvingNewTurnSequence = false;
            advanceRoundAfterNewTurnSequence = false;
        }

        return started;
    }

    IEnumerator RunNewTurnSequence()
    {
        if (isDestroying || hexMap == null)
        {
            yield break;
        }

        Teams = hexMap.Teams;
        if (Teams == null || Teams.Count < 2)
        {
            yield break;
        }

        List<TosterHexUnit> turnUnits = BuildNewTurnUnitQueue();
        yield return ResolveNewTurnSpellsByGroups(turnUnits);

        if (isDestroying || hexMap == null || Teams == null || Teams.Count < 2)
        {
            yield break;
        }

        Teams[0].FinishNewTurnAfterSpellSequence();
        Teams[1].FinishNewTurnAfterSpellSequence();

        hexMap.DoTurn();
    }

    void CompleteNewTurnSequence()
    {
        if (isDestroying)
        {
            advanceRoundAfterNewTurnSequence = false;
            resolvingNewTurnSequence = false;
            return;
        }

        if (advanceRoundAfterNewTurnSequence)
        {
            Tura++;
            SetNewTurn();
        }

        advanceRoundAfterNewTurnSequence = false;
        resolvingNewTurnSequence = false;
    }

    List<TosterHexUnit> BuildNewTurnUnitQueue()
    {
        List<TosterHexUnit> units = new List<TosterHexUnit>();
        if (Teams == null || Teams.Count < 2)
        {
            return units;
        }

        units.AddRange(Teams[0].Tosters);
        units.AddRange(Teams[1].Tosters);
        return units;
    }

    IEnumerator ResolveNewTurnSpellsByGroups(List<TosterHexUnit> units)
    {
        List<string> spellOrder = BuildNewTurnSpellOrder(units);
        for (int i = 0; i < spellOrder.Count; i++)
        {
            List<SpellOverTime> spells = CollectNewTurnSpells(units, spellOrder[i]);
            if (spells.Count == 0)
            {
                continue;
            }

            for (int spellIndex = 0; spellIndex < spells.Count; spellIndex++)
            {
                SpellOverTime spell = spells[spellIndex];
                if (spell != null && spell.me != null)
                {
                    spell.me.ResolveNewTurnSpell(spell, false);
                }
            }

            yield return SkillPresentationManager.WaitForBlockingPresentation(TosterHexUnit.PassiveResolveMaxWaitSeconds);
        }
    }

    List<string> BuildNewTurnSpellOrder(List<TosterHexUnit> units)
    {
        List<string> spellOrder = new List<string>();
        for (int i = 0; i < TosterHexUnit.AutocastTurnOrder.Length; i++)
        {
            AddSpellNameIfMissing(spellOrder, TosterHexUnit.AutocastTurnOrder[i]);
        }

        for (int unitIndex = 0; unitIndex < units.Count; unitIndex++)
        {
            TosterHexUnit unit = units[unitIndex];
            if (unit == null || unit.SpellsGoingOn == null)
            {
                continue;
            }

            for (int spellIndex = 0; spellIndex < unit.SpellsGoingOn.Count; spellIndex++)
            {
                SpellOverTime spell = unit.SpellsGoingOn[spellIndex];
                if (spell != null)
                {
                    AddSpellNameIfMissing(spellOrder, spell.nameofspell);
                }
            }
        }

        return spellOrder;
    }

    void AddSpellNameIfMissing(List<string> spellOrder, string spellName)
    {
        if (string.IsNullOrEmpty(spellName) || spellOrder.Contains(spellName))
        {
            return;
        }

        spellOrder.Add(spellName);
    }

    List<SpellOverTime> CollectNewTurnSpells(List<TosterHexUnit> units, string spellName)
    {
        List<SpellOverTime> spells = new List<SpellOverTime>();
        if (string.IsNullOrEmpty(spellName))
        {
            return spells;
        }

        for (int unitIndex = 0; unitIndex < units.Count; unitIndex++)
        {
            TosterHexUnit unit = units[unitIndex];
            if (CanReceiveTurn(unit) == false || unit.SpellsGoingOn == null)
            {
                continue;
            }

            List<SpellOverTime> unitSpells = new List<SpellOverTime>(unit.SpellsGoingOn);
            for (int spellIndex = 0; spellIndex < unitSpells.Count; spellIndex++)
            {
                SpellOverTime spell = unitSpells[spellIndex];
                if (spell != null && spell.nameofspell == spellName && unit.SpellsGoingOn.Contains(spell))
                {
                    spells.Add(spell);
                }
            }
        }

        return spells;
    }

    bool CanReceiveTurn(TosterHexUnit t)
    {
        return t != null && t.isDead == false && t.Amount > 0;
    }



 public TosterHexUnit AskWhosTurnSimulator(TeamClass team1, TeamClass team2)
    {

       

        TosterHexUnit TeamRed = team1.AskForUnit();
        TosterHexUnit TeamBlue = team2.AskForUnit();

        if (TeamRed == TeamBlue)
        {
          return null;
        }

      
        if (TeamRed == null) return TeamBlue;
        if (TeamBlue == null) return TeamRed;
       
        if (TeamBlue.GetIni() > TeamRed.GetIni() && TeamBlue.Waited != true || (TeamBlue.GetIni() <= TeamRed.GetIni() && TeamRed.Waited == true && TeamBlue.Waited != true))
        {
            return TeamBlue;
        }
        if (TeamRed.GetIni() > TeamBlue.GetIni() && TeamRed.Waited != true || (TeamRed.GetIni()<=TeamBlue.GetIni() && TeamBlue.Waited == true && TeamRed.Waited != true))
        {
            return TeamRed;
        }
        if (TeamRed.GetIni() == TeamBlue.GetIni() && TeamRed.Waited == false && TeamBlue.Waited == false)
        {
          
            if (TeamBlue.GetMS() > TeamRed.GetMS())
            {
                return TeamBlue;
            }
            if (TeamRed.GetMS() > TeamBlue.GetMS())
            {
                return TeamRed;
            }
            int t = 0;
            int o = 0;
            for (int i = 0; i < Teams[0].Tosters.Count; i++) { if (Teams[0].Tosters[i] == TeamRed) t = i; }
            for (int i = 0; i < Teams[1].Tosters.Count; i++) { if (Teams[1].Tosters[i] == TeamBlue) o = i; }
            if (t > o)
            {
                return TeamBlue;
            }
            else
            {
                return TeamRed;
            }
        }

        // zostały == lub wait
        if (TeamBlue.Waited == true && TeamRed.Waited == true)
        {
           
            if (TeamBlue.GetIni() < TeamRed.GetIni())
            {
                return TeamBlue;
            }
            if (TeamRed.GetIni() < TeamBlue.GetIni())
            {
                return TeamRed;
            }
          
            if (TeamRed.GetIni() == TeamBlue.GetIni())
            {
                if (TeamBlue.GetMS() < TeamRed.GetMS())
                {
                    return TeamBlue;
                }
                if (TeamRed.GetMS() < TeamBlue.GetMS())
                {
                    return TeamRed;
                }
                if (TeamRed.GetMS() == TeamBlue.GetMS())
                {
                    int t = 0;
                    int o = 0;
                    for (int i = 0; i < Teams[0].Tosters.Count; i++) { if (Teams[0].Tosters[i] == TeamRed) t = i; }
                    for (int i = 0; i < Teams[1].Tosters.Count; i++) { if (Teams[1].Tosters[i] == TeamBlue) o = i; }

                    if (o > t || o == t)
                    {
                        return TeamBlue;
                    }
                    else
                    {
                        return TeamRed;
                    }
                }
            }
        }
        Debug.LogError("TeamRED: " + TeamRed.Waited + " , " + TeamRed.GetIni());
        Debug.LogError("TeamBlue: " + TeamBlue.Waited + " , " + TeamBlue.GetIni());
        return null;

    }

public TosterHexUnit AskWhosTurnSimulatorS(TeamClass team1, TeamClass team2)
    {
        return AskWhosTurnFutureRoundSimulator(team1, team2);
    }


}
    



