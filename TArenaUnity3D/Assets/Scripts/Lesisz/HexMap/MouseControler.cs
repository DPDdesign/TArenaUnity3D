using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Xenu.Game;
public class MouseControler : LocalNetworkBehaviour
{

    public OutlineM outlineM;
    HexMap hexMap;
    public HexClass hexUnderMouse;
    public CastManager castManager;
    public HexClass hexLastUnderMouse;
    GameObject GOUnderMouse;
    GameObject GOLastUnderMouse = null;
    HexClass[] hexPath;
    public LayerMask LayerIDForHexTiles;
    public LayerMask LayerIDForPartTiles;
    public MonoBehaviour outlineManager;
    public MonoBehaviour outlineManagerMainToster;
    // public Canvas canvas;
    public UICanvas canvas;
    delegate void UpdateFunc();
    UpdateFunc Update_CurrentFunc;
    public bool isAiOn = true;
    public bool isAiTurn = true;
    Vector3 LastMousePosition;
    public bool shiftmode = false;
    LineRenderer lineRenderer;
    int MouseDragTreshold = 1;
    Vector3 TestGoUp;
    bool isDragginCamera = false;
    Vector3 LastMouseGroundPlanePosition;
    Vector3 cameraTargetOffset;
    TurnManager TM;
    public int SelectedSpellid = 0;
    int selectedSkillCooldown = 1;
    string selectedSkillId = "";
    bool selectedSkillConsumesTurn = false;
    bool selectedSkillAllowsMoveAfterUse = false;
    bool selectedSkillCompletionRequested = false;
    public TosterHexUnit SelectedToster = null;
    TosterHexUnit TempSelectedToster = null;
    TosterHexUnit TempOutlinedToster = null;
    TosterHexUnit TargetToster = null;
    public bool activeButtons = false;
    public bool isMulti = true;
    public MostStupidAIEver AI;
    public Camera c;
    public bool SYNC = false;
    bool runBattleResultReported = false;

    public int STC, STR;
    public int GetSelectedSpellID()
    {
        return SelectedSpellid;
    }
    public HexClass getHexUnderMouse()
    {
        return hexUnderMouse;
    }
    public TosterHexUnit getSelectedToster()
    {
        return SelectedToster;
    }
    void Start()
    {
        PlayFabControler.EnsureInstance().GetStats();

        if (PlayerPrefs.GetInt("AI") == 0)
        {
            isAiOn = false;
        }
        else isAiOn = true;

        isMulti = LocalGameSession.ShouldRunNetworkGameplay;
        SYNC = true;
        TM = FindObjectOfType<TurnManager>();
        BattleActionLifecycle.EnsureInstance();

        Update_CurrentFunc = Update_DetectModeStart;
        hexMap = GameObject.FindObjectOfType<HexMap>();
        hexPath = null;
   
        Debug.Log("Camera");
    }

    internal IEnumerator DoMoveAndAttackWithoutCheck(HexClass hex, TosterHexUnit tosterHexUnit)
    {
        throw new NotImplementedException();
    }

    /// // TODO : CZEKAC NA NASTEPNA TURE DO KONCA ANIMACJI!!!  - > ZOBACZ NA:     StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
    public bool isCamera()
    {
        if (c != null)
        {
            return true;
        }
        else
        {
            c = FindObjectOfType<Camera>();
            return false;

        }
    }

    void Update()
    {
        
        if (!isCamera() || !hexMap.isCreated) return;
        Update_CurrentFunc();

        LastMousePosition = Input.mousePosition;

        hexUnderMouse = MouseToHex();
        // MouseToPart();
        hexLastUnderMouse = hexUnderMouse;
        GOLastUnderMouse = GOUnderMouse;
    }

    internal IEnumerator DoMovesPath(List<HexClass> hexmaxpath)
    {
          throw new NotImplementedException();

    }

    void Update_DetectModeStart()
    {
        activeButtons = false;
        if (BattleActionLifecycle.IsActionBlocking)
        {
            return;
        }

        hexMap.unHighlightAroundHex(hexMap.GetHexAt(5, 5), 20);


        if (SelectedToster != null)
        {
            //Debug.LogError(SelectedToster.Name);
            //outlineManagerMainToster.RemoveOutline();
            outlineM.unSetHexSelectedToster();
        }   

        if (TM.isAnyoneAlive() == 2)
        {
            canvas.EndPanel.SetActive(true);
            canvas.EndText.text = "Left Player Win! ";
            ReportRunBattleResult(true);
            if (isMulti)
            {
                PlayFabControler.EnsureInstance().GetPhoton();
                if (LocalGameSession.IsMasterClient)
                {
                    PlayFabControler.EnsureInstance().StartCloudSetWin();
                }
                else
                {
                    PlayFabControler.EnsureInstance().StartCloudSetLoss();
                }
            }
        }
        else if (TM.isAnyoneAlive() == 1)
        {
            canvas.EndPanel.SetActive(true);
            canvas.EndText.text = "Right Player Win! ";
            ReportRunBattleResult(false);
            if (isMulti)
            {
                PlayFabControler.EnsureInstance().GetPhoton();
                if (LocalGameSession.IsMasterClient)
                {
                    PlayFabControler.EnsureInstance().StartCloudSetLoss();
                }
                else
                {
                    PlayFabControler.EnsureInstance().StartCloudSetWin();
                }
            }
        }

        SelectedToster = TM.AskWhosTurn();
        if (SelectedToster == null)
        {
            return;
        }

        TM.GetTostersQueue();
        SelectedToster.isSelected = true;
        if (isAiOn==true)
        {
            if (SelectedToster.Team == hexMap.Teams[1])
            {
                
                AI.AskAIwhattodo();
                return;
            }
        }
  

        hexUnderMouse = SelectedToster.Hex;
        if (isMulti && SYNC==false)
        {
          
            Update_CurrentFunc = WaitForSyncc;
            return;
        }
 

        outlineM.SetHexSelectedToster(SelectedToster.Hex);

        if (isMulti == true)
        {
            if (SelectedToster.Team == hexMap.Teams[1] && LocalGameSession.IsMasterClient)
            {
                Update_CurrentFunc = WaitinForYourTurn;
                return;
            }
            else
                if (SelectedToster.Team == hexMap.Teams[0] && !LocalGameSession.IsMasterClient)
            {
                Update_CurrentFunc = WaitinForYourTurn;
                return;
            }
        }
      
        if (isMulti && SYNC==false)
        {
          //  return;
        }
        if (isMulti) { SYNC = false; };
           hexMap.unHighlightAroundHex(hexMap.GetHexAt(5, 5), 20);
        //Debug.LogError(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>().bounds);
        // outlineManagerMainToster.ChangeObj(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>());      ///Odpowiadają za otoczke wybranego tostera
        // outlineManagerMainToster.ChangeObj(hexMap.GetObjectFromHex(SelectedToster.Hex).GetComponentInChildren<Renderer>());//SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>());
        // outlineManagerMainToster.AddMainOutlineWithReset();
        outlineM.SetHexSelectedToster(SelectedToster.Hex);


        if (SelectedToster.MovedThisTurn == false)
        {
            SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
        }
        Update_CurrentFunc = SelectTosterMovement;
        return;
    }
    [PunRPC]

    void SetSync()
    {
        SYNC = true;
    }

    [PunRPC]

    void WaitForSync()
    {
        photonView.RPC("SendSync", RpcTarget.Others, new object[] { });
     
    }
    [PunRPC]

    void SendSync(int c, int r)
    {
        if (SelectedToster != null)
        {
            STC = c;
            STR = r;

        }
    }


    void WaitForSyncc()
    {
        SYNC = false;
        photonView.RPC("SendSync", RpcTarget.Others, new object[] { SelectedToster.C, SelectedToster.R });
        if (STC == SelectedToster.C && STR == SelectedToster.R)
        {
            SYNC = true;
            Update_CurrentFunc = Update_DetectModeStart;
            return;
        }
    }
    IEnumerator AskSync()
    {
        yield return new WaitUntil(() => (SelectedToster.C == STC && SelectedToster.R == STR));
    }

    void Taunted()
    {
        Debug.Log("HOW DARE YOU!?");
        if (!SelectedToster.whoTauntedMe.isDead)
        {
            photonView.RPC("StartCoroutineDoMoveAndAttackWithoutCheck", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.whoTauntedMe.Hex.C, SelectedToster.whoTauntedMe.Hex.R, SelectedToster.Hex.C, SelectedToster.Hex.R});
        }
        // photonView.RPC("StartCoroutineDoMoveAndAttackWithoutCheck", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.whoTauntedMe.Hex.C, SelectedToster.whoTauntedMe.Hex.R });
        else { SelectedToster.Taunt = false; CancelUpdateFunc(); return; }
            //StartCoroutine(DoMoveAndAttackWithoutCheck(SelectedToster.whoTauntedMe.Hex,SelectedToster.whoTauntedMe));
        }

    [PunRPC]
    void StartCoroutineDoMoveAndAttackWithoutCheck(int i, int k, int r, int f, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit actor = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        if (r == -5 && f == -5)
        {
            TryStartMoveAndAttackAction(hexMap.GetHexAt(i, k), null, actor);
        }
        else
        {
            Debug.LogError("i" + i + "k" + k + "r" + r + "f" + f);
            TryStartMoveAndAttackAction(hexMap.GetHexAt(i, k), hexMap.GetHexAt(r, f).Tosters[0], actor);
        }
    }

    [PunRPC]
    void StartCoroutineDoMoveAndAttackWithoutCheck2(int i, int k, int r, int f, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit actor = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        if (r == -5 && f == -5)
        {
            TryStartMoveAndAttackAction(hexMap.GetHexAt(i, k), null, actor, true);
        }
        else
        {
            Debug.LogError("i" + i + "k" + k + "r" + r + "f" + f);
            TryStartMoveAndAttackAction(hexMap.GetHexAt(i, k), hexMap.GetHexAt(r, f).Tosters[0], actor, true);
        }
    }
    // TRYB RUCHU JEDNOSTKI

    void WaitinForYourTurn()
    {
        activeButtons = false;
        if (BattleActionLifecycle.IsActionBlocking)
        {
            return;
        }

        hexMap.unHighlightAroundHex(hexMap.GetHexAt(5, 5), 20);
        Outlining();
        shiftctrlmode();
        ScrollLook();
        //Heal();
       
        if (Input.GetMouseButtonDown(1) && hexUnderMouse.Tosters.Count > 0)
        {
            Update_CurrentFunc = ShowInfo;
            ShowInfo();
        } //ShowStats     //hexUnderMouse.Highlight - Dostpeny HEX 

   


        if (SelectedToster != null)
        {
            //Debug.LogError(SelectedToster.Name);
           // outlineManagerMainToster.RemoveOutline();
            outlineM.unSetHexSelectedToster();
        }

        if (TM.isAnyoneAlive() == 2)
        {
            canvas.EndPanel.SetActive(true);
            canvas.EndText.text = "Left Player Win! ";
            ReportRunBattleResult(true);
        }
        else
        if (TM.isAnyoneAlive() == 1)
        {
            canvas.EndPanel.SetActive(true);
            canvas.EndText.text = "Right Player Win! ";
            ReportRunBattleResult(false);
        }

        SelectedToster = TM.AskWhosTurn();
        if (SelectedToster == null)
        {
            return;
        }

        SelectedToster.isSelected = true;
        outlineM.SetHexSelectedToster(SelectedToster.Hex);
        if (isAiOn == true)
        {
            if (SelectedToster.Team == hexMap.Teams[1])
            {

                AI.AskAIwhattodo();
                return;
            }
        }

        if (isMulti == true)
        {
            if (SelectedToster.Team == hexMap.Teams[1] && LocalGameSession.IsMasterClient)
            {

                return;
            }
            else
                if (SelectedToster.Team == hexMap.Teams[0] && !LocalGameSession.IsMasterClient)
            {
                return;
            }
        }
        hexMap.unHighlightAroundHex(hexMap.GetHexAt(5, 5), 20);
        //Debug.LogError(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>().bounds);
        // outlineManagerMainToster.ChangeObj(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>());      ///Odpowiadają za otoczke wybranego tostera


        hexUnderMouse = SelectedToster.Hex;

        if (SelectedToster.MovedThisTurn == false)
        {
            SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
        }
        Update_CurrentFunc = SelectTosterMovement;
        return;

    }
    void SelectTosterMovement()
    {
        if (BattleActionLifecycle.IsActionBlocking)
        {
            activeButtons = false;
            return;
        }

        activeButtons = true;
        if (SelectedToster.Taunt == true)
        {
            hexMap.unHighlightAroundHex(hexMap.GetHexAt(5, 5), 20);
            //   SelectedToster.whoTauntedMe.Hex.Highlight = true;
            hexMap.HighlightAroundHex(SelectedToster.Hex, 0);
            hexMap.HighlightAroundHex(SelectedToster.whoTauntedMe.Hex, 0);
            var h = SelectedToster.Pathing2(SelectedToster.whoTauntedMe.Hex);
            foreach(HexClass hex in h)
            {
                hexMap.HighlightAroundHex(hex, 0);
            }
            //  Update_CurrentFunc = Taunted;
            // return;
        }
        else
        {
            Defense();

            if (SelectedToster.isRange == true)
            {
                HighlightEnemy();
            }


            Wait();
            //Heal();
            CastSkill();
        }
        Pathing();
        ScrollLook();
        shiftctrlmode();
        Outlining();
   

        if (Input.GetMouseButtonDown(1) && hexUnderMouse.Tosters.Count > 0)
        {
            Update_CurrentFunc = ShowInfo;
            ShowInfo();
        } //ShowStats     //hexUnderMouse.Highlight - Dostpeny HEX 
        if (Input.GetMouseButtonDown(0) && hexUnderMouse.Highlight && hexUnderMouse != SelectedToster.Hex && !SelectedToster.Team.HexesUnderTeam.Contains(hexUnderMouse))
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            //     Debug.LogError("test");
            if (hexUnderMouse.Tosters.Count > 0 && hexUnderMouse.Tosters[0].Team != SelectedToster.Team)
            {
                if (SelectedToster.isRange == false)
                {
                    if (SelectedToster.IsPathAvaible(hexUnderMouse))
                    {
                        var temp = MouseToPart();
                        if (temp == null)
                        {
                            return;
                        }

                        DoMoveAndAttack(hexUnderMouse.Tosters[0], hexMap.GetHexAt(temp.C, temp.R), SelectedToster);
                       // photonView.RPC("StartCoroutineDoMoveAndAttack", RpcTarget.All, new object[] { hexUnderMouse.C, hexUnderMouse.R, temp.C, temp.R });
                    }                 //   StartCoroutineDoMoveAndAttack(hexUnderMouse.C, hexUnderMouse.R);
                     //   StartCoroutine(DoMoveAndAttack(hexUnderMouse.Tosters[0]));
                }
                else if (hexUnderMouse.Highlight == true)
                {
                    photonView.RPC("Shot", RpcTarget.All, new object[] { hexUnderMouse.C, hexUnderMouse.R , SelectedToster.Hex.C, SelectedToster.Hex.R});
                }
            }
            else if (hexUnderMouse.Tosters.Count == 0 && SelectedToster.Taunt==false)
            {

                Debug.Log(RpcTarget.All);
               photonView.RPC("StartCoroutineDoMoves", RpcTarget.All, new object[] { hexUnderMouse.C, hexUnderMouse.R, SelectedToster.Hex.C, SelectedToster.Hex.R });
            }
            // CancelUpdateFunc();
            return;
        } //DoMove 
    }
    [PunRPC]
    void Shot(int i, int k, int SelectedTosterC, int SelectedTosterR)
    {
       TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        TryStartBasicRangedAttackAction(hexMap.GetHexAt(i, k), ST);

    }

    [PunRPC]
    void JustDmg(int i, int k, int mod, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        Debug.LogError(i +"   " +k);
        Debug.LogError(hexMap);
  
          Debug.LogError(hexMap.GetHexAt(i, k).C + "   " + hexMap.GetHexAt(i, k).R);
        ST.SpecialDMGModificator = mod;
        if (hexMap.GetHexAt(i,k)!=null ? hexMap.GetHexAt(i, k).Tosters.Count > 0 : false)
        {
            hexMap.GetHexAt(i, k).Tosters[0].DealMeDMG(ST);
        }
        ST.SpecialDMGModificator = 0;
    }
    [PunRPC]
    void JustSetFalse()
    {
        castManager.SetFalse();
        
    }
    [PunRPC]
    void StartCoroutineDoMoveAndAttack(int i , int k, int r, int f)
    {
        // StartCoroutine(DoMoveAndAttack(hexMap.GetHexAt(i, k).Tosters[0], hexMap.GetHexAt(r, f)));
    
    }



    [PunRPC]
    void StartCoroutineDoMoves(int i, int k, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        Debug.Log("happen");
        TryStartMoveAction(hexMap.GetHexAt(i, k), ST);

    }

    [PunRPC]
    void StartCoroutineDoMovesST(int si, int sr ,int i, int k)
    {
        Debug.Log("happen");
        TryStartMoveAction(hexMap.GetHexAt(i, k), hexMap.GetHexAt(sr,sr).Tosters[0]);
         
    }
    [PunRPC]
    void StartCoroutineDoMovesWithoutMoved(int i, int k, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        Debug.Log("happen");
        TryRunLifecycleAction(
            ST,
            BattleActionLifecycleKind.Movement,
            "MoveWithoutMoved",
            null,
            () => DoMovesWithoutMoved(hexMap.GetHexAt(i, k), ST));

    }
    public void Outlining()
    {
        if (hexUnderMouse != SelectedToster.Hex)
        {
            if (hexUnderMouse.Tosters.Count > 0)
            {
                if (TempOutlinedToster == null)
                {

                    TempOutlinedToster = hexUnderMouse.Tosters[0];
                    List<Renderer> Ren = new List<Renderer>();
                  //  outlineManagerMainToster.ChangeObj(hexMap.GetObjectFromHex(SelectedToster.Hex).GetComponentInChildren<Renderer>());//SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>());

                    Ren.Add((hexMap.GetObjectFromHex(hexUnderMouse).GetComponentInChildren<Renderer>()));
                    //    outlineManager.ChangeObjects(Ren);
                    if(TempOutlinedToster.Team!=SelectedToster.Team)
                    outlineM.SetHexEnemyToster(hexUnderMouse);
                    else outlineM.SetHexWhiteToster(hexUnderMouse);
                }
                else if (TempOutlinedToster != hexUnderMouse.Tosters[0])
                {

                    SendOutlineMessage(outlineManager, "RemoveAllButMain");
                    TempOutlinedToster = hexUnderMouse.Tosters[0];
                    List<Renderer> Ren = new List<Renderer>(); Ren.Add((hexMap.GetObjectFromHex(hexUnderMouse).GetComponentInChildren<Renderer>()));

                    if (TempOutlinedToster.Team != SelectedToster.Team)
                        outlineM.SetHexEnemyToster(hexUnderMouse);
                    else outlineM.SetHexWhiteToster(hexUnderMouse);
                }
            }
            else if (TempOutlinedToster != null && hexUnderMouse.Tosters.Count == 0)
            {

                //  outlineManager.RemoveAllButMain();
                outlineM.unSetHexEnemyToster();
                outlineM.unSetHexWhiteToster();
                TempOutlinedToster = null;
            }
        }

    }
    void BeforeNextTurn()
    {
        activeButtons = false;
        //  shiftctrlmode();
        // ScrollLook();
    }

    bool TryRunLifecycleAction(
        TosterHexUnit actor,
        BattleActionLifecycleKind kind,
        string label,
        Action commit,
        Func<IEnumerator> actionBody)
    {
        activeButtons = false;
        return BattleActionLifecycle.EnsureInstance().TryRunAction(
            actor,
            kind,
            label,
            commit,
            actionBody,
            CompleteActionModeCleanup);
    }

    void CompleteActionModeCleanup()
    {
        activeButtons = false;
        shiftmode = false;
        CancelUpdateFunc();
    }

    public bool TryStartMoveAction(HexClass destinationHex, TosterHexUnit actor)
    {
        if (destinationHex == null || actor == null || actor.MovedThisTurn || (actor.UsedSkillThisTurn && actor.CanMoveAfterSkillThisTurn == false))
        {
            return false;
        }

        return TryRunLifecycleAction(
            actor,
            BattleActionLifecycleKind.Movement,
            "Move",
            () => CompleteMoveCommit(actor),
            () => DoMoves(destinationHex, actor));
    }

    void CompleteMoveCommit(TosterHexUnit actor)
    {
        actor.MovedThisTurn = true;
        if (actor.Waited || HasAvailableSkillAfterMove(actor) == false)
        {
            actor.Moved = true;
        }
    }

    public bool TryStartMoveAndAttackAction(HexClass moveHex, TosterHexUnit target, TosterHexUnit actor)
    {
        return TryStartMoveAndAttackAction(moveHex, target, actor, false);
    }

    public bool TryStartMoveAndAttackAction(HexClass moveHex, TosterHexUnit target, TosterHexUnit actor, bool ignoreObstacles)
    {
        if (moveHex == null || actor == null || actor.MovedThisTurn || (actor.UsedSkillThisTurn && actor.CanMoveAfterSkillThisTurn == false))
        {
            return false;
        }

        return TryRunLifecycleAction(
            actor,
            BattleActionLifecycleKind.MoveAndAttack,
            "MoveAndAttack",
            () => actor.Moved = true,
            () => ignoreObstacles
                ? DoMoveAndAttackWithoutCheck2(moveHex, target, actor)
                : DoMoveAndAttackWithoutCheck(moveHex, target, actor));
    }

    public bool TryStartBasicRangedAttackAction(HexClass targetHex, TosterHexUnit actor)
    {
        if (targetHex == null || actor == null || actor.MovedThisTurn || actor.UsedSkillThisTurn)
        {
            return false;
        }

        return TryRunLifecycleAction(
            actor,
            BattleActionLifecycleKind.BasicRangedAttack,
            "BasicRangedAttack",
            () => actor.Moved = true,
            () => BasicRangedAttackAction(targetHex, actor));
    }

    IEnumerator BasicRangedAttackAction(HexClass targetHex, TosterHexUnit actor)
    {
        if (targetHex != null && targetHex.Tosters.Count > 0)
        {
            targetHex.Tosters[0].ShootME(actor, true);
        }

        yield return null;
    }

    public bool TryStartWaitAction(TosterHexUnit actor)
    {
        if (actor == null || actor.MovedThisTurn || actor.UsedSkillThisTurn || actor.Waited)
        {
            return false;
        }

        return TryRunLifecycleAction(
            actor,
            BattleActionLifecycleKind.Wait,
            "Wait",
            () => actor.Waited = true,
            null);
    }

    public bool TryStartDefenseAction(TosterHexUnit actor)
    {
        if (actor == null || actor.MovedThisTurn || actor.UsedSkillThisTurn)
        {
            return false;
        }

        return TryRunLifecycleAction(
            actor,
            BattleActionLifecycleKind.Defense,
            "Defense",
            () =>
            {
                actor.Moved = true;
                actor.DefenceStance = true;
                actor.SpecialDef += 5;
            },
            () => DefenseAction(actor));
    }

    IEnumerator DefenseAction(TosterHexUnit actor)
    {
        if (actor != null && actor.tosterView != null)
        {
            yield return actor.tosterView.PlayAnimatorStateAndWaitForDefault("defense", 1.25f);
        }
    }

    public bool TryCompleteSkillAction(TosterHexUnit actor)
    {
        if (actor == null)
        {
            return false;
        }

        string skillId = string.IsNullOrEmpty(selectedSkillId) == false
            ? selectedSkillId
            : GetSkillIdAtSlot(actor, SelectedSpellid);

        return TryRunLifecycleAction(
            actor,
            BattleActionLifecycleKind.Skill,
            skillId,
            () => CompleteSkillCommit(actor, selectedSkillConsumesTurn),
            null);
    }

    public bool TryStartSkillAction(
        TosterHexUnit actor,
        int skillSlot,
        string skillId,
        HexClass targetHex,
        out string failureReason)
    {
        failureReason = string.Empty;
        if (actor == null)
        {
            failureReason = "Skill actor is missing.";
            return false;
        }

        if (castManager == null)
        {
            failureReason = "CastManager reference is missing.";
            return false;
        }

        if (BattleActionLifecycle.IsActionBlocking)
        {
            failureReason = "Battle action lifecycle is currently blocking.";
            return false;
        }

        if (IsSkillIndexValid(skillSlot, actor) == false)
        {
            failureReason = "Skill slot is invalid for the live actor.";
            return false;
        }

        string liveSkillId = actor.skillstrings[skillSlot];
        if (string.Equals(liveSkillId, skillId ?? string.Empty, StringComparison.Ordinal) == false)
        {
            failureReason = "Skill slot/id pair no longer matches the live actor.";
            return false;
        }

        if (CanStartSkill(skillSlot, actor) == false)
        {
            failureReason = "MouseControler rejected the skill before CastManager preparation.";
            return false;
        }

        if (castManager.HasSkillModeMethod(skillId) == false || castManager.HasSkillCastMethod(skillId) == false)
        {
            failureReason = "CastManager does not contain the required mode/cast methods for skill " + skillId + ".";
            return false;
        }

        SelectedToster = actor;
        if (actor.Hex != null)
        {
            hexUnderMouse = targetHex != null ? targetHex : actor.Hex;
        }

        if (TryPrepareSkillSelectionForLiveExecution(actor, skillSlot, skillId, out failureReason) == false)
        {
            return false;
        }

        if (selectedSkillCompletionRequested)
        {
            return true;
        }

        if (IsRepeatableToggleSkill(skillId))
        {
            castManager.CancelPreparedSkillWithoutCommit();
            failureReason = "Repeatable toggle skill did not complete during CastManager mode preparation.";
            return false;
        }

        if (castManager.isMove && castManager.rush == false)
        {
            castManager.CancelPreparedSkillWithoutCommit();
            failureReason = "Skill requires staged movement/target selection that is not represented by one AI skill intent yet.";
            return false;
        }

        HexClass executionHex;
        if (TryResolvePreparedSkillExecutionHex(actor, targetHex, out executionHex, out failureReason) == false)
        {
            castManager.CancelPreparedSkillWithoutCommit();
            return false;
        }

        hexUnderMouse = executionHex;
        ApplyPreparedSkillTargetHighlights(executionHex);

        try
        {
            castManager.startSpell(skillId, executionHex);
        }
        catch (Exception ex)
        {
            castManager.CancelPreparedSkillWithoutCommit();
            Debug.LogException(ex);
            failureReason = "CastManager threw while starting skill " + skillId + ".";
            return false;
        }

        if (selectedSkillCompletionRequested ||
            castManager.ActionInputBlockedByCommittedSkill ||
            BattleActionLifecycle.IsActionBlocking)
        {
            return true;
        }

        if (castManager.isInProgress)
        {
            castManager.CancelPreparedSkillWithoutCommit();
            failureReason = "Skill entered a multi-step CastManager state that cannot be completed from one AI skill intent.";
            return false;
        }

        castManager.CancelPreparedSkillWithoutCommit();
        failureReason = "CastManager did not complete or start a blocking skill action.";
        return false;
    }

    bool TryPrepareSkillSelectionForLiveExecution(
        TosterHexUnit actor,
        int skillSlot,
        string skillId,
        out string failureReason)
    {
        failureReason = string.Empty;
        SelectedSpellid = skillSlot;
        selectedSkillCompletionRequested = false;
        SkillState = true;
        if (canvas != null)
        {
            canvas.UseSkill(SelectedSpellid);
        }

        selectedSkillId = skillId;
        selectedSkillAllowsMoveAfterUse = castManager.CanMoveAfterSkill(skillId);
        selectedSkillConsumesTurn = selectedSkillAllowsMoveAfterUse == false;

        try
        {
            castManager.getMode(skillId, actor);
        }
        catch (Exception ex)
        {
            castManager.CancelPreparedSkillWithoutCommit();
            Debug.LogException(ex);
            failureReason = "CastManager threw while preparing skill mode for " + skillId + ".";
            return false;
        }

        selectedSkillCooldown = Mathf.Max(1, castManager.cooldown);
        if (castManager.isAvailable == false)
        {
            castManager.CancelPreparedSkillWithoutCommit();
            CancelUpdateFunc();
            failureReason = "CastManager marked skill " + skillId + " unavailable during mode preparation.";
            return false;
        }

        return true;
    }

    bool TryResolvePreparedSkillExecutionHex(
        TosterHexUnit actor,
        HexClass targetHex,
        out HexClass executionHex,
        out string failureReason)
    {
        executionHex = null;
        failureReason = string.Empty;

        if (actor == null)
        {
            failureReason = "Skill actor is missing.";
            return false;
        }

        if (castManager.SelfCast && targetHex == null)
        {
            executionHex = actor.Hex;
            return executionHex != null;
        }

        if (castManager.MeleeisAoE && targetHex == null)
        {
            executionHex = actor.Hex;
            return executionHex != null;
        }

        if (targetHex == null)
        {
            failureReason = "Skill requires an explicit target hex in the intent.";
            return false;
        }

        if (castManager.RangeSelectingenemy)
        {
            if (targetHex.Tosters == null || targetHex.Tosters.Count == 0 || targetHex.Tosters[0].Team == actor.Team)
            {
                failureReason = "Skill target is not a live enemy unit.";
                return false;
            }
        }

        if (castManager.Rangeselectingfriend)
        {
            if (targetHex.Tosters == null || targetHex.Tosters.Count == 0 || targetHex.Tosters[0].Team != actor.Team)
            {
                failureReason = "Skill target is not a live friendly unit.";
                return false;
            }
        }

        if (castManager.MeleeisAoE || castManager.MeleeisAoEOnlyRadius)
        {
            if (actor.Hex == null)
            {
                failureReason = "Melee skill actor has no live hex.";
                return false;
            }

            int radius = Mathf.Max(1, castManager.aoeradius);
            if (Mathf.Abs(targetHex.C - actor.Hex.C) > radius || Mathf.Abs(targetHex.R - actor.Hex.R) > radius)
            {
                failureReason = "Melee skill target is outside the prepared CastManager radius.";
                return false;
            }
        }

        executionHex = targetHex;
        return true;
    }

    void ApplyPreparedSkillTargetHighlights(HexClass executionHex)
    {
        if (executionHex == null || hexMap == null)
        {
            return;
        }

        if (castManager.rush)
        {
            HighlightLine();
            return;
        }

        if (castManager.RangeisAoE)
        {
            hexMap.HighlightAroundHex(executionHex, castManager.aoeradius);
        }

        if (castManager.MeleeisAoE)
        {
            hexMap.HighlightAroundHex(SelectedToster.Hex, castManager.aoeradius);
        }
    }

    void CompleteSkillCommit(TosterHexUnit actor, bool consumesTurn)
    {
        string skillId = string.IsNullOrEmpty(selectedSkillId) == false
            ? selectedSkillId
            : GetSkillIdAtSlot(actor, SelectedSpellid);

        if (IsRepeatableToggleSkill(skillId))
        {
            CleanupSelectedSkillVisualState(actor);
            return;
        }

        actor.UsedSkillThisTurn = true;
        actor.AddUsedSkillIdThisTurn(skillId);
        actor.CanMoveAfterSkillThisTurn = selectedSkillAllowsMoveAfterUse;
        ApplySelectedSkillCooldownIfNeeded(actor);
        CleanupSelectedSkillVisualState(actor);
        if (actor.MovedThisTurn || (consumesTurn && selectedSkillAllowsMoveAfterUse == false))
        {
            actor.Moved = true;
        }
    }

    void CleanupSelectedSkillVisualState(TosterHexUnit actor)
    {
        if (canvas != null)
        {
            canvas.UnUseSkill(SelectedSpellid);
        }

        if (hexMap != null && hexUnderMouse != null && castManager != null)
        {
            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 20);
        }

        if (actor != null && actor.Hex != null && actor.Hex.hexMap != null)
        {
            actor.Hex.hexMap.unHighlight(actor.Hex.C, actor.Hex.R, actor.GetMS());
        }
    }

 public   IEnumerator DoMoves(HexClass hex, TosterHexUnit SelectedToster)
    {
        RemoveMainOutline();
        outlineM.unSetHexSelectedToster();
        activeButtons = false;
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        SelectedToster.Pathing_func(hex, false);

        // Debug.LogError(SelectedToster.HexPathList.Count);
        yield return StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
        shiftmode = false;
        // CancelUpdateFunc();

    }

public    IEnumerator DoMovesST(HexClass hex, TosterHexUnit ST)
    {
        RemoveMainOutline();
        outlineM.unSetHexSelectedToster();
        activeButtons = false;
        ST.move = true;
        ST.Hex.hexMap.unHighlight(ST.Hex.C, ST.Hex.R, ST.GetMS());
        ST.Pathing_func(hex, false);

        // Debug.LogError(SelectedToster.HexPathList.Count);
        yield return StartCoroutine(hexMap.DoUnitMoves(ST));
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
        shiftmode = false;
        // CancelUpdateFunc();

    }

    public IEnumerator DoMovesWithoutMoved(HexClass hex, TosterHexUnit SelectedToster)
    {
        RemoveMainOutline();
        outlineM.unSetHexSelectedToster();
        activeButtons = false;
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        SelectedToster.Pathing_func(hex, false);

        // Debug.LogError(SelectedToster.HexPathList.Count);
        yield return StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);

        shiftmode = false;
        // CancelUpdateFunc();

    }
    IEnumerator DoMovesWithoutEnd(HexClass hex, TosterHexUnit SelectedToster)
    {
        RemoveMainOutline();
        outlineM.unSetHexSelectedToster();
        activeButtons = false;
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        SelectedToster.Pathing_func(hex, false);

        // Debug.LogError(SelectedToster.HexPathList.Count);
        yield return StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
        shiftmode = false;
        // CancelUpdateFunc();

    }

    public IEnumerator DoMovesPath(List<HexClass> h, TosterHexUnit SelectedToster)
    {
        activeButtons = false;
        SelectedToster.SetHexPath(h.ToArray());
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
   

        // Debug.LogError(SelectedToster.HexPathList.Count);
        yield return StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
        shiftmode = false;
        // CancelUpdateFunc();

    }



    [PunRPC]
    void DoMoveAndAttackA(int i, int k, int r, int f, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        TryStartMoveAndAttackAction(hexMap.GetHexAt(r, f), hexMap.GetHexAt(i, k).Tosters[0], ST);

    }
    IEnumerator StartCoroutineDoMoveAndAttackA(int i, int k, int r, int f, TosterHexUnit SelectedToster)
    {
        RemoveMainOutline();
        activeButtons = false;
        TargetToster = hexMap.GetHexAt(i, k).Tosters[0];// hexUnderMouse.Tosters[0];
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());


        //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
        //   hexPath = SelectedToster.Pathing(temp);


        SelectedToster.Pathing_func(hexMap.GetHexAt(r, f), false);
        yield return StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        if (SelectedToster.Hex == hexMap.GetHexAt(r, f)) yield return StartCoroutine(hexMap.GetHexAt(i, k).Tosters[0].AttackMeSequence(SelectedToster));
        shiftmode = false;

    }

    [PunRPC]
    void DoMoveAndAttackB(int i, int k, int r, int f, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        TryStartMoveAndAttackAction(hexMap.GetHexAt(r, f), hexMap.GetHexAt(i, k).Tosters[0], ST);

    }


    IEnumerator StartCoroutineDoMoveAndAttackB(int i, int k, int r, int f, TosterHexUnit SelectedToster)
    {
        RemoveMainOutline();
        activeButtons = false;
        TargetToster = hexMap.GetHexAt(i, k).Tosters[0];//hexUnderMouse.Tosters[0];
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());


        //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
        //   hexPath = SelectedToster.Pathing(temp);


        SelectedToster.Pathing_func(hexMap.GetHexAt(r, f), false);
        yield return StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        if (SelectedToster.Hex == hexMap.GetHexAt(r, f)) yield return StartCoroutine(hexMap.GetHexAt(i, k).Tosters[0].AttackMeSequence(SelectedToster));
        shiftmode = false;
    }

    void DoMoveAndAttack(TosterHexUnit toster, HexClass temp, TosterHexUnit SelectedToster)
    {

        
        if (temp.Tosters.Count == 0)
        {
            if (temp != null && temp.Highlight == true)
            {
                photonView.RPC("DoMoveAndAttackA", RpcTarget.All, new object[] { toster.Hex.C, toster.Hex.R, temp.C, temp.R , SelectedToster.Hex.C, SelectedToster.Hex.R });
                /*
                RemoveMainOutline();
                activeButtons = false;
                TargetToster = toster;// hexUnderMouse.Tosters[0];
                SelectedToster.move = true;
                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());


                //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
                //   hexPath = SelectedToster.Pathing(temp);


                SelectedToster.Pathing_func(temp, false);
                SelectedToster.Moved = true;

                Update_CurrentFunc = BeforeNextTurn;
                StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
                yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
                if (SelectedToster.Hex == temp) toster.AttackMe(SelectedToster);
                CancelUpdateFunc();
                shiftmode = false;*/
            }
        }
        else if (temp.Tosters[0] == SelectedToster)
        {
            if (temp != null && temp.Highlight == true)
            {
                photonView.RPC("DoMoveAndAttackB", RpcTarget.All, new object[] { toster.Hex.C, toster.Hex.R, temp.C, temp.R , SelectedToster.Hex.C, SelectedToster.Hex.R });
                /*
                RemoveMainOutline();
                activeButtons = false;
                TargetToster = toster;//hexUnderMouse.Tosters[0];
                SelectedToster.move = true;
                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());


                //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
                //   hexPath = SelectedToster.Pathing(temp);


                SelectedToster.Pathing_func(temp, false);
                SelectedToster.Moved = true;

                Update_CurrentFunc = BeforeNextTurn;
                StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
                yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
                if (SelectedToster.Hex==temp) toster.AttackMe(SelectedToster);
                CancelUpdateFunc();
                shiftmode = false;*/
            }
        }
     
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
    }


    public IEnumerator DoMoveAndAttackWithoutCheck(HexClass temp, TosterHexUnit toster, TosterHexUnit SelectedToster)
    {

        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());


        //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
        //   hexPath = SelectedToster.Pathing(temp);


        SelectedToster.Pathing_func(temp, false);
        yield return StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        if (toster != null)
        {
            Debug.LogError(toster.Name);

            if (SelectedToster.Hex == temp) { yield return StartCoroutine(toster.AttackMeSequence(SelectedToster)); Debug.LogError(toster.Name); }
        }
        shiftmode = false;

        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
    }
    public IEnumerator DoMoveAndAttackWithoutCheck2(HexClass temp, TosterHexUnit toster, TosterHexUnit SelectedToster)
    {

        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());


        //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
        //   hexPath = SelectedToster.Pathing(temp);


        SelectedToster.Pathing_func(temp, true);
        SelectedToster.HexPathList.RemoveAt(SelectedToster.HexPathList.Count - 1);
        yield return StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        if (toster != null)
        {
            Debug.LogError(toster.Name);

            if (SelectedToster.Hex == temp) { yield return StartCoroutine(toster.AttackMeSequence(SelectedToster)); Debug.LogError(toster.Name); }
        }
        shiftmode = false;

        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
    }


    public void EndSkills()
    {
        if (selectedSkillCompletionRequested)
        {
            return;
        }

        photonView.RPC("EndSkillss", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });
    }

    [PunRPC]
    void EndSkillss(int SelectedTosterC, int SelectedTosterR)
    {
        if (selectedSkillCompletionRequested)
        {
            return;
        }

        TosterHexUnit ST = ResolveSkillCompletionActor(SelectedTosterC, SelectedTosterR);
        if (ST == null)
        {
            Debug.LogWarning("Could not resolve skill completion actor.");
            return;
        }

        CompleteSelectedSkillLocally(ST);
        return;
    }

    public bool CompleteSelectedSkillLocally(TosterHexUnit actor)
    {
        if (selectedSkillCompletionRequested)
        {
            return false;
        }

        selectedSkillCompletionRequested = true;
        return TryCompleteSkillAction(actor);
    }

    TosterHexUnit ResolveSkillCompletionActor(int selectedTosterC, int selectedTosterR)
    {
        HexClass actorHex = hexMap.GetHexAt(selectedTosterC, selectedTosterR);
        if (actorHex != null && actorHex.Tosters.Count > 0)
        {
            return actorHex.Tosters[0];
        }

        TosterHexUnit teamActor = ResolveSkillCompletionActorFromTeams(selectedTosterC, selectedTosterR);
        if (teamActor != null)
        {
            return teamActor;
        }

        if (SelectedToster != null &&
            SelectedToster.Hex != null &&
            SelectedToster.Hex.C == selectedTosterC &&
            SelectedToster.Hex.R == selectedTosterR)
        {
            return SelectedToster;
        }

        return null;
    }

    TosterHexUnit ResolveSkillCompletionActorFromTeams(int selectedTosterC, int selectedTosterR)
    {
        if (hexMap == null || hexMap.Teams == null)
        {
            return null;
        }

        foreach (TeamClass team in hexMap.Teams)
        {
            if (team == null || team.Tosters == null)
            {
                continue;
            }

            foreach (TosterHexUnit toster in team.Tosters)
            {
                if (toster != null &&
                    toster.Hex != null &&
                    toster.Hex.C == selectedTosterC &&
                    toster.Hex.R == selectedTosterR)
                {
                    return toster;
                }
            }
        }

        return null;
    }

    [PunRPC]
    void TeleportToster(int i , int j,int k, int t)
    {
        hexMap.GetHexAt(i, j).Tosters[0].TeleportToHex(hexMap.GetHexAt(k, t));  
    }

    public void CancelSpellCasting()
    {
      if (castManager.ActionInputBlockedByCommittedSkill)
        {
            return;
        }

      if (castManager.isInProgress == false)
        {
            canvas.UnUseSkill(SelectedSpellid);
            castManager.SetFalse();
            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 20);
            CancelUpdateFunc();
            return;
        }
    }
    public void SetCD(TosterHexUnit SelectedToster)
    {
        if (IsSkillIndexValid(SelectedSpellid, SelectedToster) == false)
        {
            return;
        }

        SelectedToster.cooldowns[SelectedSpellid] = Mathf.Max(1, selectedSkillCooldown);
        Debug.LogError(SelectedSpellid);
        Debug.LogError(SelectedToster.cooldowns[SelectedSpellid]);
    }

    bool IsSkillIndexValid(int skillIndex, TosterHexUnit actor)
    {
        return actor != null &&
            actor.skillstrings != null &&
            actor.cooldowns != null &&
            skillIndex >= 0 &&
            skillIndex < actor.skillstrings.Count &&
            skillIndex < actor.cooldowns.Count;
    }

    string GetSkillIdAtSlot(TosterHexUnit actor, int skillIndex)
    {
        if (actor == null || actor.skillstrings == null || skillIndex < 0 || skillIndex >= actor.skillstrings.Count)
        {
            return "Skill";
        }

        return actor.skillstrings[skillIndex];
    }

    bool CanStartSkill(int skillIndex, TosterHexUnit actor)
    {
        if (IsSkillIndexValid(skillIndex, actor) == false)
        {
            return false;
        }

        if (actor.cooldowns[skillIndex] > 0)
        {
            return false;
        }

        if (actor.Waited)
        {
            return false;
        }

        string skillId = actor.skillstrings[skillIndex];
        if (IsPassiveSkill(skillId))
        {
            return false;
        }

        if (IsRepeatableToggleSkill(skillId))
        {
            return true;
        }

        if (IsSkillAlreadyUsedThisTurn(actor, skillId))
        {
            return false;
        }

        bool canUseAfterMove = CanUseSkillAfterMove(skillId);
        if (actor.MovedThisTurn && canUseAfterMove == false)
        {
            return false;
        }

        return true;
    }

    public bool CanUseSkillSlot(int skillIndex)
    {
        return CanStartSkill(skillIndex, SelectedToster);
    }

    bool IsSkillAlreadyUsedThisTurn(TosterHexUnit actor, string skillId)
    {
        return actor != null &&
            IsRepeatableToggleSkill(skillId) == false &&
            actor.HasUsedSkillIdThisTurn(skillId);
    }

    bool IsRepeatableToggleSkill(string skillId)
    {
        return string.IsNullOrEmpty(skillId) == false &&
            (skillId.StartsWith("Melee_Stance", StringComparison.Ordinal) ||
             skillId.StartsWith("Range_Stance", StringComparison.Ordinal));
    }

    bool IsPassiveSkill(string skillId)
    {
        if (DataMapper.Instance == null)
        {
            return false;
        }

        DataMapper.SkillDefinition skillDefinition = DataMapper.Instance.FindSkill(skillId);
        return skillDefinition != null && skillDefinition.Type == "Passive";
    }

    public bool CanUseSkillAfterMove(string skillId)
    {
        return castManager != null && castManager.CanUseSkillAfterMove(skillId);
    }

    public bool CanMoveAfterSkill(string skillId)
    {
        return castManager != null && castManager.CanMoveAfterSkill(skillId);
    }

    bool HasAvailableSkillAfterMove(TosterHexUnit actor)
    {
        if (actor == null || actor.skillstrings == null || actor.cooldowns == null)
        {
            return false;
        }

        for (int i = 0; i < actor.skillstrings.Count && i < actor.cooldowns.Count; i++)
        {
            string skillId = actor.skillstrings[i];
            if (IsRepeatableToggleSkill(skillId))
            {
                continue;
            }

            if (CanStartSkill(i, actor))
            {
                return true;
            }
        }

        return false;
    }

    void ApplySelectedSkillCooldownIfNeeded(TosterHexUnit actor)
    {
        if (IsSkillIndexValid(SelectedSpellid, actor) == false)
        {
            return;
        }

        if (actor.cooldowns[SelectedSpellid] <= 0)
        {
            SetCD(actor);
        }
    }

    bool TryStartSelectedSkill(int skillIndex)
    {
        if (CanStartSkill(skillIndex, SelectedToster) == false)
        {
            return false;
        }

        photonView.RPC("CastSkillBooleanss", RpcTarget.All, new object[] { skillIndex, SelectedToster.Hex.C, SelectedToster.Hex.R });
        Update_CurrentFunc = SpellCasting;
        return true;
    }

    void SpellCasting()
    {
        if (BattleActionLifecycle.IsActionBlocking)
        {
            activeButtons = false;
            return;
        }

        if (castManager.ActionInputBlockedByCommittedSkill)
        {
            activeButtons = false;
            return;
        }

       
        Outlining();
        ScrollLook();


        if (castManager.SelfCast == true)
        {
            HighLightSelectedToster();
        }

        if (castManager.RangeisAoE == true)
        {
    

            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius+2);
            hexMap.HighlightAroundHex(hexUnderMouse, castManager.aoeradius);
           
        }

        if (castManager.SlashTarget == true)
        {
            if ((hexUnderMouse.Tosters.Count != 0 && hexUnderMouse != castManager.tempHex) || hexUnderMouse.Tosters.Count == 0)
            {
                
                hexMap.unHighlightAroundHex(castManager.tempHex, 1);

                hexMap.HighlightSlash(castManager.tempHex, hexUnderMouse);//hexUnderMouse.Tosters[0]);

            }
            
    

        }/*
        if (castManager.SlashTarget == true)
        {
            if ((hexUnderMouse.Tosters.Count != 0 && hexUnderMouse != SelectedToster.Hex) || hexUnderMouse.Tosters.Count == 0)
            {

                hexMap.unHighlightAroundHex(SelectedToster.Hex, 1);

                hexMap.HighlightSlash(SelectedToster, hexUnderMouse);//hexUnderMouse.Tosters[0]);

            }



        }*/
        if (castManager.isMove == true)
        {
            Pathing();
        }

        if (castManager.MeleeisAoE == true)
        {


            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 2);
            hexMap.HighlightAroundHex(getSelectedToster().Hex, castManager.aoeradius);

        }
        if (castManager.MeleeisAoEOnlyRadius == true)
        {


            // hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 2);
      
            hexMap.HighlightRadiusNoEmpty(getSelectedToster().Hex, castManager.aoeradius);

        }

        
        if (castManager.SingleTarget)
        {
            hexMap.DownHex(hexUnderMouse, 1);
            hexMap.UpHex(hexUnderMouse, 0);
        }
        if (castManager.rush==true)
        {
            HighlightLine();
        }
        if (SkillState == false)
        {

            EndSkills();
            return;
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {

                photonView.RPC("startSpell", RpcTarget.All, new object[] { hexUnderMouse.C,hexUnderMouse.R, SelectedToster.Hex.C, SelectedToster.Hex.R });
               // castManager.startSpell(SelectedToster.skillstrings[SelectedSpellid]);

            }
        }


        if (Input.GetMouseButtonDown(1))
        {
            CancelSpellCasting();
        }
            

        
    }

    [PunRPC]
    void startSpell(int i, int j, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];

        if (EventSystem.current.IsPointerOverGameObject())
            return;

  
    
        string skillId = string.IsNullOrEmpty(selectedSkillId) == false
            ? selectedSkillId
            : GetSkillIdAtSlot(ST, SelectedSpellid);

        castManager.startSpell(skillId,hexMap.GetHexAt(i,j));

    }


    public void HighlightLineOnlyLast()
    {
        List<HexClass> HexesToHighlight = new List<HexClass>();
        if (SelectedToster.teamN == true) // true znaczy ze jest z teamu po lewej stronie
        {
            for (int i = 1; i < 5 - SelectedToster.Hex.C; i++)
            {


                HexesToHighlight.Add(hexMap.GetHexAt(SelectedToster.Hex.C + i, SelectedToster.Hex.R));
                if (hexMap.GetHexAt(SelectedToster.Hex.C + i, SelectedToster.Hex.R).Tosters.Count > 0)
                {
                    i = 20;
                }
            }
            foreach (HexClass h in HexesToHighlight)
            {
                h.Highlight = true;
            }
        }
        if (SelectedToster.teamN == false) // true znaczy ze jest z teamu po lewej stronie
        {
            for (int i = 1; i < SelectedToster.Hex.C + 1; i++)
            {
                //  Debug.Log(SelectedToster.Hex.C - i);


                HexesToHighlight.Add(hexMap.GetHexAt(SelectedToster.Hex.C - i, SelectedToster.Hex.R));
                if (hexMap.GetHexAt(SelectedToster.Hex.C - i, SelectedToster.Hex.R) != null && hexMap.GetHexAt(SelectedToster.Hex.C - i, SelectedToster.Hex.R).Tosters.Count > 0)
                {

                    i = 20;
                }
            }
            foreach (HexClass h in HexesToHighlight)
            {
                h.Highlight = true;
            }
        }
        hexMap.UpdateHexVisuals();
    }
    public void HighlightLine()
    {
        List<HexClass> HexesToHighlight = new List<HexClass>();
        if (SelectedToster.teamN==true) // true znaczy ze jest z teamu po lewej stronie
        {
            for (int i = 1; i < 20- SelectedToster.Hex.C; i++)
            {

                if (hexMap.GetHexAt(SelectedToster.Hex.C + i, SelectedToster.Hex.R) != null)
                {
                    HexesToHighlight.Add(hexMap.GetHexAt(SelectedToster.Hex.C + i, SelectedToster.Hex.R));
                    if (hexMap.GetHexAt(SelectedToster.Hex.C + i, SelectedToster.Hex.R).Tosters.Count > 0)
                    {
                        i = 20;
                    }
                }
            }
            foreach( HexClass h in HexesToHighlight)
            {
                h.Highlight = true;
            }
        }
        if (SelectedToster.teamN == false) // true znaczy ze jest z teamu po lewej stronie
        {
            for (int i = 1; i < SelectedToster.Hex.C+1; i++)
            {
              //  Debug.Log(SelectedToster.Hex.C - i);
       
               
                    HexesToHighlight.Add(hexMap.GetHexAt(SelectedToster.Hex.C - i, SelectedToster.Hex.R));
                if (hexMap.GetHexAt(SelectedToster.Hex.C - i, SelectedToster.Hex.R) != null && hexMap.GetHexAt(SelectedToster.Hex.C - i, SelectedToster.Hex.R).Tosters.Count > 0)
                {

                    i = 20;
                }
            }
            foreach (HexClass h in HexesToHighlight)
            {
                if (h!=null)
                h.Highlight = true;
            }
        }
        hexMap.UpdateHexVisuals();
    }
    public void HighlightEnemy()
    {
        TeamClass team = new TeamClass();
        if (hexMap.Teams[0] == SelectedToster.Team) team = hexMap.Teams[1];
        else team = hexMap.Teams[0];

            foreach (HexClass h in team.HexesUnderTeam)
        {
            h.Highlight = true;
        }
        hexMap.UpdateHexVisuals();
    }

    public void HighLightSelectedToster()
    {
        HexClass h = SelectedToster.Hex;
        h.Highlight = true;
        hexMap.UpdateHexVisuals();
    }


    public void HighlightFriend()
    {
        TeamClass team = new TeamClass();
        if (hexMap.Teams[0] == SelectedToster.Team) team = hexMap.Teams[0];
        else team = hexMap.Teams[1];

        foreach (HexClass h in team.HexesUnderTeam)
        {
            h.Highlight = true;
        }
        hexMap.UpdateHexVisuals();
    }
    public void unHighlightFriend()
    {
        TeamClass team = new TeamClass();
        if (hexMap.Teams[0] == SelectedToster.Team) team = hexMap.Teams[0];
        else team = hexMap.Teams[1];

        foreach (HexClass h in team.HexesUnderTeam)
        {
            h.Highlight = false;
        }
        hexMap.UpdateHexVisuals();
    }

    public void unHighlightEnemy()
    {
        TeamClass team = new TeamClass();
        if (hexMap.Teams[0] == SelectedToster.Team) team = hexMap.Teams[1];
        else team = hexMap.Teams[0];

        foreach (HexClass h in team.HexesUnderTeam)
        {
            h.Highlight = false;
        }
        hexMap.UpdateHexVisuals();
    }

    public List<HexClass> GetEnemy()
    {
        if (hexMap.Teams[0] == SelectedToster.Team) return hexMap.Teams[1].HexesUnderTeam;
        else return hexMap.Teams[0].HexesUnderTeam;
    } 

    public void CastSkillBooleans(int SelectedSkill,TosterHexUnit SelectedToster)
    {
        if (CanStartSkill(SelectedSkill, SelectedToster) == false)
        {
            return;
        }

        SelectedSpellid = SelectedSkill;
        selectedSkillCompletionRequested = false;
        SkillState = true;
        canvas.UseSkill(SelectedSpellid);
        string skillId = SelectedToster.skillstrings[SelectedSpellid];
        selectedSkillId = skillId;
        Debug.LogError(skillId);
        selectedSkillAllowsMoveAfterUse = castManager.CanMoveAfterSkill(skillId);
        selectedSkillConsumesTurn = selectedSkillAllowsMoveAfterUse == false;
        castManager.getMode(SelectedToster.skillstrings[SelectedSpellid], SelectedToster);
        selectedSkillCooldown = Mathf.Max(1, castManager.cooldown);
        if (castManager.isAvailable == false)
        {
            Debug.LogError("Umiejetnosc niedostepna");
            castManager.SetFalse();
            CancelUpdateFunc();
            return;
        }
        if (castManager.unselectaround == true)
        {
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        }
        if (castManager.RangeSelectingenemy == true)
        {
            HighlightEnemy();
        }
        if (castManager.Rangeselectingfriend == true)
        {
            HighlightFriend();
        }
        if (castManager.Global==true)
        {
            hexMap.HighlightAroundHex(SelectedToster.Hex, 20);
        }

        if (castManager.SlashTarget == true)
        {
         //   hexMap.HighlightRadiusNoEmpty(getSelectedToster().Hex, 1);
        }

      
    }
    [PunRPC]
    void CastSkillBooleanss(int SelectedSkill, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];

        CastSkillBooleans(SelectedSkill, ST);

    }

    public void CastSkillOnlyBooleans(TosterHexUnit SelectedToster)
    {
        if (castManager.isAvailable == false)
        {
            Debug.LogError("Umiejetnosc niedostepna");
            castManager.SetFalse();
            CancelUpdateFunc();
            return;
        }

        if (castManager.unselectaround == true)
        {
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        }
        if (castManager.RangeSelectingenemy == true)
        {
            HighlightEnemy();
        }
        if (castManager.Rangeselectingfriend == true)
        {
            HighlightFriend();
        }
        if (castManager.Global == true)
        {
            hexMap.HighlightAroundHex(SelectedToster.Hex, 20);
        }
      
    }


    public void CastSkill(int i)
    {
        TryStartSelectedSkill(i);
    }


    public static bool SkillState = true;
    void CastSkill()
    {

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryStartSelectedSkill(0);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryStartSelectedSkill(1);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TryStartSelectedSkill(2);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TryStartSelectedSkill(3);
            return;
        }

        /*
      if (Input.GetKeyDown(KeyCode.Alpha2))
      {
          SelectedToster.skills[1].CastSkill(SelectedToster, SelectedToster);
          SelectedToster.Moved = true;
          CancelUpdateFunc();
          shiftmode = false;
      }

      else  if (Input.GetKeyDown(KeyCode.Alpha3))
      {
          SelectedToster.skills[2].CastSkill(SelectedToster, SelectedToster);
          SelectedToster.Moved = true;
          CancelUpdateFunc();
          shiftmode = false;
      }
      */


    }


    /*

    void Heal()
    {
        if (Input.GetKeyDown(KeyCode.H) && SelectedToster.Name=="TosterHEAL")
        {
            SelectedToster.HealMe(5);
        }

  
    }

    */

    void WaitForMove()
    {

    }

    void Pathing()
    {
        if (hexPath != null)
        {
            foreach (HexClass h in hexPath)
            {
                TestGoUp = h.MyHex.transform.position;
                TestGoUp.y = 0.0f;
                h.MyHex.transform.position = TestGoUp;
            }
        }
        if (hexUnderMouse != SelectedToster.Hex && hexUnderMouse.Highlight)
        {
            if (hexUnderMouse.Tosters.Count == 0)
            {
            //    List<Renderer> listofhexes = new List<Renderer>(); 
                hexPath = SelectedToster.Pathing(hexUnderMouse);
                if (hexPath != null)
                {
                    foreach (HexClass h in hexPath)
                    {
                     
                           TestGoUp = h.MyHex.transform.position;
                        TestGoUp.y = 0.2f;
                        h.MyHex.transform.position = TestGoUp;
                   //     listofhexes.Add(h.MyHex.GetComponent<Renderer>());
                    }
                }
               // outlineManagerMainToster.ChangeObjectss(listofhexes);
            }
            else if (hexUnderMouse.Tosters[0].Team!=SelectedToster.Team && SelectedToster.isRange==false)
            {
                TargetToster = hexUnderMouse.Tosters[0];
                var temp = MouseToPart();
                if (temp != null)
                {
                    //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
                    hexPath = SelectedToster.Pathing(temp);
                    if (hexPath != null)
                    {
                        foreach (HexClass h in hexPath)
                        {
                            TestGoUp = h.MyHex.transform.position;
                            TestGoUp.y = -0.2f;
                            h.MyHex.transform.position = TestGoUp;
                        }
                    }
                }
            }
        }
    }

    public virtual void shiftctrlmode()
    {
        if (shiftmode)
        {
            if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.LeftControl))
            {
                shiftmode = false;
                if (TempSelectedToster != null) TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.GetMS(), SelectedToster);
                CancelUpdateFunc();
            }
            else
            if (hexUnderMouse.Tosters.Count > 0)
            {
                if (TempSelectedToster != null)
                    if (hexUnderMouse.Tosters[0] != TempSelectedToster)
                    {

                        TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.GetMS(), SelectedToster);
                        TempSelectedToster = null;
                    }
                if (TempSelectedToster != hexUnderMouse.Tosters[0])
                {

                    hexUnderMouse.Tosters[0].Hex.hexMap.CheckWithPath(hexUnderMouse.Tosters[0]);
                    TempSelectedToster = hexUnderMouse.Tosters[0];
                }
            }
            else
            {
                if (TempSelectedToster != null)
                {
                    TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.GetMS(), SelectedToster);
                    TempSelectedToster = null;

                }
            }
        }
        else
        {
            if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.LeftControl)))
            {
                shiftmode = true;
                TempSelectedToster = null;
            }
        }
    }

    void ScrollLook()
    {
        if (Input.GetMouseButton(2) &&
           Vector3.Distance(Input.mousePosition, LastMousePosition) > MouseDragTreshold)
        {
            //when mouse is hold down and mouse moved = camera drag
            Update_CurrentFunc = Update_CameraDrag;
            LastMouseGroundPlanePosition = MouseToGroundPlane(Input.mousePosition);
            Update_CameraDrag();
        }
    }

    void Wait()
    {
        if (Input.GetKeyUp(KeyCode.N))
        {
            if (SelectedToster.MovedThisTurn || SelectedToster.UsedSkillThisTurn)
            {
                return;
            }

            if (SelectedToster.Waited == false)
            {
                SelectedToster.TextToSend = "";
                SelectedToster.TextToSend += SelectedToster.Name + " czeka.";
                Chat.chat.SendUnitActionMessage(SelectedToster, "czeka.");
                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
                photonView.RPC("Waitt", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });
            }
        }
    }

    [PunRPC]
    void Waitt(  int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        TryStartWaitAction(ST);

    }


    void ShowInfo()
    {
        shiftctrlmode();
        Outlining();
        if (hexUnderMouse.Tosters.Count > 0)
        {
            TosterHexUnit t = hexUnderMouse.Tosters[0];
            canvas.UpdateAllStats(t);
             canvas.GetSpellsOnToster(t);
            canvas.StatsPanel.SetActive(true);
          
        }
        if (Input.GetMouseButtonUp(1))
        {
            canvas.StatsPanel.SetActive(false);
            CancelUpdateFunc();
        }


    }


    void Defense()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (SelectedToster.MovedThisTurn || SelectedToster.UsedSkillThisTurn)
            {
                return;
            }

            SelectedToster.TextToSend = "";
            SelectedToster.TextToSend += SelectedToster.Name + " broni się.";
            Chat.chat.SendUnitActionMessage(SelectedToster, "broni się.");
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
            photonView.RPC("Defensee", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });
          
            
        }
    }


    [PunRPC]
    void Defensee(int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        TryStartDefenseAction(ST);

    }


    // SZUKAJ DROGI OD ZAZNACZONEGO HEXA - NOT USED
    void DrawPath(HexClass[] hexPath)
    {
        if (hexPath.Length==0)
        {           
            return;
        }
        Vector3[] ps = new Vector3[hexPath.Length];
        for( int i=0;i<hexPath.Length;i++)
        {
            GameObject GO= hexMap.GetObjectFromHex(hexPath[i]);

        }
    }

    // SPRAWDZ NA JAKI HEX CELUJE MYSZ (JEZELI W OGOLE) - ZWRACA TEN HEX ///////// TODO: ZAWSZE PODWYZSZAJ (ZAZNACZAJ) HEXA NA KTOREGO CELUJE MYSZ
    public HexClass MouseToHex()
    {
        
        Ray mouseRay = c.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        int layerMask = LayerIDForHexTiles.value;
        if (Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, layerMask))
        {
             //  Debug.Log( hitInfo.collider.name ); //-> WYSWIETL NA CO WSKAZUJE MYSZ
           GameObject hexGO = hitInfo.rigidbody.gameObject;
           HexClass hexUnderMouse1 = hexMap.GetHexFromGameObject(hexGO);

            GOUnderMouse = hexGO;
            return hexUnderMouse1;
            /*
            if (hexUnderMouse != hexLastUnderMouse)
            {

                TestGoUp = GOUnderMouse.transform.position;
                TestGoUp.y = 0.1f;
                GOUnderMouse.transform.position = TestGoUp;
                TestGoUp.y = 0.0f;
                if (GOLastUnderMouse != null) GOLastUnderMouse.transform.position = TestGoUp;
            }
            */
        }
         HexClass hex = new HexClass();
        //Debug.Log("Found nothing.");
        return hex;// hexUnderMouse;
    }
    Vector3 MouseToGroundPlane(Vector3 mousePos)
    {
        Ray mouseRay = c.ScreenPointToRay(mousePos);
        // What is the point at which the mouse ray intersects Y=0
        if (mouseRay.direction.y >= 0)
        {
       
            //Debug.LogError("Why is mouse pointing up?");
            return Vector3.zero;
        }
        float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
        return mouseRay.origin - (mouseRay.direction * rayLength);
    }

    HexClass MouseToPart()
    {
        Ray mouseRay = c.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        int layerMask = LayerIDForPartTiles.value;
        if (Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, layerMask))
        {
            //Debug.Log( hitInfo.collider.name );// -> WYSWIETL NA CO WSKAZUJE MYSZ
            GameObject hexGO = hitInfo.collider.gameObject;       
            if (hexUnderMouse.ListOfParts.Contains(hexGO))
            switch (hexGO.name)
            {
                case "0":
                     
                    return hexMap.GetHexAt(hexUnderMouse.C, hexUnderMouse.R+1);
                case "60":
                    return hexMap.GetHexAt(hexUnderMouse.C+1, hexUnderMouse.R);
                case "120":
                    return hexMap.GetHexAt(hexUnderMouse.C+1, hexUnderMouse.R -1);
                case "180":
                    return hexMap.GetHexAt(hexUnderMouse.C, hexUnderMouse.R - 1);
                case "240":
                    return hexMap.GetHexAt(hexUnderMouse.C-1, hexUnderMouse.R);
                case "300":
                    return hexMap.GetHexAt(hexUnderMouse.C-1, hexUnderMouse.R + 1);
            }
          
            return null;
        
        }

        //Debug.Log("Found nothing.");
        return null ;
    }


    // NOT USED - > AKTUALNE W CAMERA ROTATOR
    void Update_ScrollZoom()
    {
        // Zoom to scrollwheel
        float scrollAmount = Input.GetAxis ("Mouse ScrollWheel");
        float minHeight = 15;
        float maxHeight = 27;
        // Move camera towards hitPos
        Vector3 hitPos = MouseToGroundPlane(Input.mousePosition);
        Vector3 dir = hitPos - c.transform.position;
        
        Vector3 p = c.transform.position;

        // Stop zooming out at a certain distance.
        // TODO: Maybe you should still slide around at 20 zoom?
        if (scrollAmount > 0 || p.y < (maxHeight - 0.1f)) {
            cameraTargetOffset += dir * scrollAmount;
        }
        Vector3 lastCameraPosition = c.transform.position;
        Vector3 Cam = c.transform.position;
        Cam.y += cameraTargetOffset.y;
        c.transform.position = Vector3.Lerp(c.transform.position, c.transform.position+ cameraTargetOffset, Time.deltaTime);
        cameraTargetOffset -=c.transform.position - lastCameraPosition;


        p = c.transform.position;
        if (p.y < minHeight) {
            p.y = minHeight;
        }
        if (p.y > maxHeight) {
            p.y = maxHeight;
        }
        c.transform.position = p;
        /*
        // Change camera angle
        c.transform.rotation = Quaternion.Euler (
            Mathf.Lerp (45, 65, c.transform.position.y / maxHeight),
            c.transform.rotation.eulerAngles.y,
            c.transform.rotation.eulerAngles.z
        );
        */
        c.transform.rotation = Quaternion.Euler(
    Mathf.Lerp(45, 65,c.transform.position.x / maxHeight),
    c.transform.rotation.eulerAngles.y,
    c.transform.rotation.eulerAngles.z
);
    }



    public void CancelUpdateFunc()
    {
        Update_CurrentFunc = Update_DetectModeStart;

        // Also do cleanup of any UI stuff associated with modes.

    }

    private void RemoveMainOutline()
    {
        SendOutlineMessage(outlineManagerMainToster, "RemoveOutline");
    }

    private void SendOutlineMessage(MonoBehaviour target, string methodName)
    {
        if (target == null)
        {
            return;
        }

        target.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
    }

    void Update_CameraDrag()
    {
        if (Input.GetMouseButtonUp(2))
        {
            Debug.Log("Cancelling camera drag.");
            CancelUpdateFunc();
            return;
        }
        Vector3 hitPos = MouseToGroundPlane(Input.mousePosition);
        Vector3 diff = LastMouseGroundPlanePosition - hitPos;             
        CameraRotator cameraRotator = FindObjectOfType<CameraRotator>();
        cameraRotator.transform.Translate(diff, Space.World);
        cameraRotator.ClampPosition();
        LastMouseGroundPlanePosition = hitPos = MouseToGroundPlane(Input.mousePosition);
    }










    void ReportRunBattleResult(bool playerWon)
    {
        if (runBattleResultReported)
        {
            return;
        }

        if (RunBattleTacticalResultBridge.ReportBattleFinished(playerWon, hexMap))
        {
            runBattleResultReported = true;
        }
    }

    //////////////////////////////////UI FUNCTIONS
    ///

    public void WaitB()
    {
        if (SelectedToster.MovedThisTurn || SelectedToster.UsedSkillThisTurn)
        {
            return;
        }

        if (SelectedToster.Waited == false)
        {
            SelectedToster.TextToSend = "";
            SelectedToster.TextToSend += SelectedToster.Name + " czeka.";
            Chat.chat.SendUnitActionMessage(SelectedToster, "czeka.");
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
            photonView.RPC("Waitt", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });
        }

    }


   public  void DefenseB()
    {
        if (SelectedToster.MovedThisTurn || SelectedToster.UsedSkillThisTurn)
        {
            return;
        }

        SelectedToster.TextToSend = "";
        SelectedToster.TextToSend += SelectedToster.Name + " broni się.";
        Chat.chat.SendUnitActionMessage(SelectedToster, "broni się.");
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        photonView.RPC("Defensee", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });

    }

    public void CastSkill1B()
    {
        
        CancelSpellCasting();
        CastSkill(0);
    }
    public void CastSkill2B()
    {
        CancelSpellCasting();
        CastSkill(1);
    }
    public void CastSkill3B()
    {
        CancelSpellCasting();
        CastSkill(2);
    }
    public void CastSkill4B()
    {
        CancelSpellCasting();
        CastSkill(3);
    }
}
