using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Xenu.Game;
public class MouseControler : MonoBehaviourPunCallbacks
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
    public UnityOutlineManager outlineManager;
    public UnityOutlineManagerMainToster outlineManagerMainToster;
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
    public TosterHexUnit SelectedToster = null;
    TosterHexUnit TempSelectedToster = null;
    TosterHexUnit TempOutlinedToster = null;
    TosterHexUnit TargetToster = null;
    public bool activeButtons = false;
    public bool isMulti = true;
    public MostStupidAIEver AI;
    public Camera c;
    public bool SYNC = false;

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
        PlayFabControler.PFC.GetStats();

        if (PlayerPrefs.GetInt("AI") == 0)
        {
            isAiOn = false;
        }
        else isAiOn = true;

        if (PlayerPrefs.GetInt("Multi") == 0)
        {
            isMulti = false;
        }
        else isMulti = true;
        TM = FindObjectOfType<TurnManager>();

        Update_CurrentFunc = Update_DetectModeStart;
        hexMap = GameObject.FindObjectOfType<HexMap>();
        hexPath = null;
   
        Debug.Log("Camera");
    }

    internal IEnumerator DoMoveAndAttackWithoutCheck(HexClass hex, TosterHexUnit tosterHexUnit)
    {
        throw new NotImplementedException();
    }

    /*
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        canvas.EndPanel.SetActive(true);
        canvas.EndText.text = "Other Player Disconnected. WIN!";
        if (isMulti)
        {
            PlayFabControler.PFC.GetPhoton();

                PlayFabControler.PFC.StartCloudSetWin();
            

        }
    }
    */
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
            if (isMulti)
            {
                PlayFabControler.PFC.GetPhoton();
                if (PhotonNetwork.LocalPlayer.IsMasterClient)
                {
                    PlayFabControler.PFC.StartCloudSetWin();
                }
                else
                {
                    PlayFabControler.PFC.StartCloudSetLoss();
                }
            }
        }
        else if (TM.isAnyoneAlive() == 1)
        {
            canvas.EndPanel.SetActive(true);
            canvas.EndText.text = "Right Player Win! ";
            if (isMulti)
            {
                PlayFabControler.PFC.GetPhoton();
                if (PhotonNetwork.LocalPlayer.IsMasterClient)
                {
                    PlayFabControler.PFC.StartCloudSetLoss();
                }
                else
                {
                    PlayFabControler.PFC.StartCloudSetWin();
                }
            }
        }

        SelectedToster = TM.AskWhosTurn();
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
        if (SYNC==false)
        {
          
            Update_CurrentFunc = WaitForSyncc;
            return;
        }
 

        outlineM.SetHexSelectedToster(SelectedToster.Hex);

        if (isMulti == true)
        {
            if (SelectedToster.Team == hexMap.Teams[1] && PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                Update_CurrentFunc = WaitinForYourTurn;
                return;
            }
            else
                if (SelectedToster.Team == hexMap.Teams[0] && !PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                Update_CurrentFunc = WaitinForYourTurn;
                return;
            }
        }
      
        if (SYNC==false)
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


        SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
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
        if (r == -5 && f == -5)
        {
            StartCoroutine(DoMoveAndAttackWithoutCheck(hexMap.GetHexAt(i, k), null, hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0]));
        }
        else
        {
            Debug.LogError("i" + i + "k" + k + "r" + r + "f" + f);
            StartCoroutine(DoMoveAndAttackWithoutCheck(hexMap.GetHexAt(i, k), hexMap.GetHexAt(r, f).Tosters[0], hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0]));
        }
    }

    [PunRPC]
    void StartCoroutineDoMoveAndAttackWithoutCheck2(int i, int k, int r, int f, int SelectedTosterC, int SelectedTosterR)
    {
        if (r == -5 && f == -5)
        {
            StartCoroutine(DoMoveAndAttackWithoutCheck2(hexMap.GetHexAt(i, k), null, hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0]));
        }
        else
        {
            Debug.LogError("i" + i + "k" + k + "r" + r + "f" + f);
            StartCoroutine(DoMoveAndAttackWithoutCheck2(hexMap.GetHexAt(i, k), hexMap.GetHexAt(r, f).Tosters[0], hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0]));
        }
    }
    // TRYB RUCHU JEDNOSTKI

    void WaitinForYourTurn()
    {
        activeButtons = false;
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
        }
        else
        if (TM.isAnyoneAlive() == 1)
        {
            canvas.EndPanel.SetActive(true);
            canvas.EndText.text = "Right Player Win! ";
        }

        SelectedToster = TM.AskWhosTurn();
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
            if (SelectedToster.Team == hexMap.Teams[1] && PhotonNetwork.LocalPlayer.IsMasterClient)
            {

                return;
            }
            else
                if (SelectedToster.Team == hexMap.Teams[0] && !PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                return;
            }
        }
        hexMap.unHighlightAroundHex(hexMap.GetHexAt(5, 5), 20);
        //Debug.LogError(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>().bounds);
        // outlineManagerMainToster.ChangeObj(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>());      ///Odpowiadają za otoczke wybranego tostera


        hexUnderMouse = SelectedToster.Hex;

        SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
        Update_CurrentFunc = SelectTosterMovement;
        return;

    }
    void SelectTosterMovement()
    {
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
        hexMap.GetHexAt(i, k).Tosters[0].ShootME(ST, true);
        ST.Moved = true;
        CancelUpdateFunc();

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
        StartCoroutine(DoMoves(hexMap.GetHexAt(i, k), ST));

    }

    [PunRPC]
    void StartCoroutineDoMovesST(int si, int sr ,int i, int k)
    {
        Debug.Log("happen");
        StartCoroutine(DoMovesST(hexMap.GetHexAt(i, k), hexMap.GetHexAt(sr,sr).Tosters[0]));
         
    }
    [PunRPC]
    void StartCoroutineDoMovesWithoutMoved(int i, int k, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        Debug.Log("happen");
        StartCoroutine(DoMovesWithoutMoved(hexMap.GetHexAt(i, k), ST));

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

                    outlineManager.RemoveAllButMain();
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
    IEnumerator DoMoves(HexClass hex, TosterHexUnit SelectedToster)
    {
        outlineManagerMainToster.RemoveOutline();
        outlineM.unSetHexSelectedToster();
        activeButtons = false;
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        SelectedToster.Pathing_func(hex, false);

        // Debug.LogError(SelectedToster.HexPathList.Count);
        SelectedToster.Moved = true;
        Update_CurrentFunc = BeforeNextTurn;
        StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
        CancelUpdateFunc();
        shiftmode = false;
        // CancelUpdateFunc();

    }

public    IEnumerator DoMovesST(HexClass hex, TosterHexUnit ST)
    {
        outlineManagerMainToster.RemoveOutline();
        outlineM.unSetHexSelectedToster();
        activeButtons = false;
        ST.move = true;
        ST.Hex.hexMap.unHighlight(ST.Hex.C, ST.Hex.R, ST.GetMS());
        ST.Pathing_func(hex, false);

        // Debug.LogError(SelectedToster.HexPathList.Count);
        ST.Moved = true;
        Update_CurrentFunc = BeforeNextTurn;
        StartCoroutine(hexMap.DoUnitMoves(ST));
        yield return new WaitUntil(() => ST.tosterView.AnimationIsPlaying == false);
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
        CancelUpdateFunc();
        shiftmode = false;
        // CancelUpdateFunc();

    }

    IEnumerator DoMovesWithoutMoved(HexClass hex, TosterHexUnit SelectedToster)
    {
        outlineManagerMainToster.RemoveOutline();
        outlineM.unSetHexSelectedToster();
        activeButtons = false;
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        SelectedToster.Pathing_func(hex, false);

        // Debug.LogError(SelectedToster.HexPathList.Count);

        StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);

        shiftmode = false;
        // CancelUpdateFunc();

    }
    IEnumerator DoMovesWithoutEnd(HexClass hex, TosterHexUnit SelectedToster)
    {
        outlineManagerMainToster.RemoveOutline();
        outlineM.unSetHexSelectedToster();
        activeButtons = false;
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        SelectedToster.Pathing_func(hex, false);

        // Debug.LogError(SelectedToster.HexPathList.Count);
        SelectedToster.Moved = true;
        Update_CurrentFunc = BeforeNextTurn;
        StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
        CancelUpdateFunc();
        shiftmode = false;
        // CancelUpdateFunc();

    }

    public IEnumerator DoMovesPath(List<HexClass> h, TosterHexUnit SelectedToster)
    {
        activeButtons = false;
        SelectedToster.SetHexPath(h.ToArray());
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
   

        // Debug.LogError(SelectedToster.HexPathList.Count);
        SelectedToster.Moved = true;
        Update_CurrentFunc = BeforeNextTurn;
        StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
        CancelUpdateFunc();
        shiftmode = false;
        // CancelUpdateFunc();

    }



    [PunRPC]
    void DoMoveAndAttackA(int i, int k, int r, int f, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        StartCoroutine(StartCoroutineDoMoveAndAttackA(i, k, r, f,ST));

    }
    IEnumerator StartCoroutineDoMoveAndAttackA(int i, int k, int r, int f, TosterHexUnit SelectedToster)
    {
        outlineManagerMainToster.RemoveOutline();
        activeButtons = false;
        TargetToster = hexMap.GetHexAt(i, k).Tosters[0];// hexUnderMouse.Tosters[0];
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());


        //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
        //   hexPath = SelectedToster.Pathing(temp);


        SelectedToster.Pathing_func(hexMap.GetHexAt(r, f), false);
        SelectedToster.Moved = true;

        Update_CurrentFunc = BeforeNextTurn;
        StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
        if (SelectedToster.Hex == hexMap.GetHexAt(r, f)) hexMap.GetHexAt(i, k).Tosters[0].AttackMe(SelectedToster);
        CancelUpdateFunc();
        shiftmode = false;

    }

    [PunRPC]
    void DoMoveAndAttackB(int i, int k, int r, int f, int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        StartCoroutine(StartCoroutineDoMoveAndAttackB(i,k,r,f,ST));

    }


    IEnumerator StartCoroutineDoMoveAndAttackB(int i, int k, int r, int f, TosterHexUnit SelectedToster)
    {
        outlineManagerMainToster.RemoveOutline();
        activeButtons = false;
        TargetToster = hexMap.GetHexAt(i, k).Tosters[0];//hexUnderMouse.Tosters[0];
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());


        //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
        //   hexPath = SelectedToster.Pathing(temp);


        SelectedToster.Pathing_func(hexMap.GetHexAt(r, f), false);
        SelectedToster.Moved = true;

        Update_CurrentFunc = BeforeNextTurn;
        StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
        if (SelectedToster.Hex == hexMap.GetHexAt(r, f)) hexMap.GetHexAt(i, k).Tosters[0].AttackMe(SelectedToster);
        CancelUpdateFunc();
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
                outlineManagerMainToster.RemoveOutline();
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
                outlineManagerMainToster.RemoveOutline();
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
        SelectedToster.Moved = true;

        Update_CurrentFunc = BeforeNextTurn;
        StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
        if (toster != null)
        {
            Debug.LogError(toster.Name);

            if (SelectedToster.Hex == temp) { toster.AttackMe(SelectedToster); Debug.LogError(toster.Name); }
        }
        CancelUpdateFunc();
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
        SelectedToster.Moved = true;

        Update_CurrentFunc = BeforeNextTurn;
        StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
        yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
        if (toster != null)
        {
            Debug.LogError(toster.Name);

            if (SelectedToster.Hex == temp) { toster.AttackMe(SelectedToster); Debug.LogError(toster.Name); }
        }
        CancelUpdateFunc();
        shiftmode = false;

        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
    }


    public void EndSkills()
    {
        photonView.RPC("EndSkillss", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });
    }

    [PunRPC]
    void EndSkillss(int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];


        if (castManager.isTurn)
        {
            canvas.UnUseSkill(SelectedSpellid);
            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 20);
            ST.Hex.hexMap.unHighlight(ST.Hex.C, ST.Hex.R, ST.GetMS());
            ST.Moved = true;
            CancelUpdateFunc();
            shiftmode = false;
        }
        else
        {
            canvas.UnUseSkill(SelectedSpellid);
            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 20);
            ST.Hex.hexMap.unHighlight(ST.Hex.C, ST.Hex.R, ST.GetMS());

            CancelUpdateFunc();
            shiftmode = false;
        }
        return;
    }

    [PunRPC]
    void TeleportToster(int i , int j,int k, int t)
    {
        hexMap.GetHexAt(i, j).Tosters[0].TeleportToHex(hexMap.GetHexAt(k, t));  
    }

    public void CancelSpellCasting()
    {
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
        SelectedToster.cooldowns[SelectedSpellid] = castManager.cooldown;
        Debug.LogError(SelectedSpellid);
        Debug.LogError(SelectedToster.cooldowns[SelectedSpellid]);
    }
    void SpellCasting()
    {
       
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

  
    
        castManager.startSpell(ST.skillstrings[SelectedSpellid],hexMap.GetHexAt(i,j));

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
             
                
                HexesToHighlight.Add(hexMap.GetHexAt(SelectedToster.Hex.C + i, SelectedToster.Hex.R));
                if (hexMap.GetHexAt(SelectedToster.Hex.C + i, SelectedToster.Hex.R).Tosters.Count > 0)
                {
                    i = 20;
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
        SelectedSpellid = SelectedSkill;
        SkillState = true;
        canvas.UseSkill(SelectedSpellid);
        Debug.LogError((SelectedToster.skillstrings[SelectedSpellid]));
        castManager.getMode(SelectedToster.skillstrings[SelectedSpellid], SelectedToster);
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
        photonView.RPC("CastSkillBooleanss", RpcTarget.All, new object[] { i,SelectedToster.Hex.C,SelectedToster.Hex.R });
        Update_CurrentFunc = SpellCasting;
        return;
    }


    public static bool SkillState = true;
    void CastSkill()
    {

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (SelectedToster.skillstrings.Count >= 1 && SelectedToster.cooldowns[0]==0)
            {
                photonView.RPC("CastSkillBooleanss", RpcTarget.All, new object[] { 0, SelectedToster.Hex.C, SelectedToster.Hex.R });
                Update_CurrentFunc = SpellCasting;
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (SelectedToster.skillstrings.Count >= 2 && SelectedToster.cooldowns[1] == 0)
            {
                photonView.RPC("CastSkillBooleanss", RpcTarget.All, new object[] { 1, SelectedToster.Hex.C, SelectedToster.Hex.R });
                Update_CurrentFunc = SpellCasting;
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (SelectedToster.skillstrings.Count >= 3 && SelectedToster.cooldowns[2] == 0)
            {
                photonView.RPC("CastSkillBooleanss", RpcTarget.All, new object[] { 2, SelectedToster.Hex.C, SelectedToster.Hex.R });
                Update_CurrentFunc = SpellCasting;
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (SelectedToster.skillstrings.Count >= 4 && SelectedToster.cooldowns[3] == 0)
            {
                photonView.RPC("CastSkillBooleanss", RpcTarget.All, new object[] { 3, SelectedToster.Hex.C, SelectedToster.Hex.R });
                Update_CurrentFunc = SpellCasting;
                return;
            }
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
            if (SelectedToster.Waited == false)
            {
                SelectedToster.TextToSend = "";
                SelectedToster.TextToSend += SelectedToster.Name + " czeka.";
                if (SelectedToster.teamN == true)
                {
                    Chat.chat.SendMessageToChat(SelectedToster.TextToSend, Msg.MessageType.Master);
                }
                else
                {
                    Chat.chat.SendMessageToChat(SelectedToster.TextToSend, Msg.MessageType.Client);
                }
                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
                photonView.RPC("Waitt", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });
            }
        }
    }

    [PunRPC]
    void Waitt(  int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        ST.Waited = true;
        CancelUpdateFunc();

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
            SelectedToster.TextToSend = "";
            SelectedToster.TextToSend += SelectedToster.Name + " broni się.";
            if (SelectedToster.teamN == true)
            {
                Chat.chat.SendMessageToChat(SelectedToster.TextToSend, Msg.MessageType.Master);
            }
            else
            {
                Chat.chat.SendMessageToChat(SelectedToster.TextToSend, Msg.MessageType.Client);
            }
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
            photonView.RPC("Defensee", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });
          
            
        }
    }


    [PunRPC]
    void Defensee(int SelectedTosterC, int SelectedTosterR)
    {
        TosterHexUnit ST = hexMap.GetHexAt(SelectedTosterC, SelectedTosterR).Tosters[0];
        var d = ST.tosterView.GetComponentInChildren<Animator>();
        if (d != null)
        {
            Debug.Log(d);
            d.Play("Defense");

        }


        ST.Moved = true;
        ST.DefenceStance = true;
        ST.SpecialDef += 5;
        CancelUpdateFunc();

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
        FindObjectOfType<CameraRotator>().transform.Translate(diff, Space.World);
        LastMouseGroundPlanePosition = hitPos = MouseToGroundPlane(Input.mousePosition);
    }










    //////////////////////////////////UI FUNCTIONS
    ///

    public void WaitB()
    {

        if (SelectedToster.Waited == false)
        {
            SelectedToster.TextToSend = "";
            SelectedToster.TextToSend += SelectedToster.Name + " czeka.";
            if (SelectedToster.teamN == true)
            {
                Chat.chat.SendMessageToChat(SelectedToster.TextToSend, Msg.MessageType.Master);
            }
            else
            {
                Chat.chat.SendMessageToChat(SelectedToster.TextToSend, Msg.MessageType.Client);
            }
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
            photonView.RPC("Waitt", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });
        }

    }


   public  void DefenseB()
    {

        SelectedToster.TextToSend = "";
        SelectedToster.TextToSend += SelectedToster.Name + " broni się.";
        if (SelectedToster.teamN == true)
        {
            Chat.chat.SendMessageToChat(SelectedToster.TextToSend, Msg.MessageType.Master);
        }
        else
        {
            Chat.chat.SendMessageToChat(SelectedToster.TextToSend, Msg.MessageType.Client);
        }
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        photonView.RPC("Defensee", RpcTarget.All, new object[] { SelectedToster.Hex.C, SelectedToster.Hex.R });

    }

    public void CastSkill1B()
    {
        
        CancelSpellCasting();
        CastSkillBooleans(0 , SelectedToster);
    }
    public void CastSkill2B()
    {
        CancelSpellCasting();
        CastSkillBooleans(1, SelectedToster);
    }
    public void CastSkill3B()
    {
        CancelSpellCasting();
        CastSkillBooleans(2, SelectedToster);
    }
    public void CastSkill4B()
    {
        CancelSpellCasting();
        CastSkillBooleans(3, SelectedToster);
    }
}
