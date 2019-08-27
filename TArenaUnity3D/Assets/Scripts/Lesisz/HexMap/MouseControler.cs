using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Xenu.Game;
public class MouseControler : MonoBehaviour
{


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
    int SelectedSpellid = 0;
    public TosterHexUnit SelectedToster = null;
    TosterHexUnit TempSelectedToster = null;
    TosterHexUnit TempOutlinedToster = null;
    TosterHexUnit TargetToster = null;
    public bool activeButtons = false;

    public MostStupidAIEver AI;


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
        TM = FindObjectOfType<TurnManager>();

        Update_CurrentFunc = Update_DetectModeStart;
        hexMap = GameObject.FindObjectOfType<HexMap>();
        hexPath = null;
        if (PlayerPrefs.GetInt("AI") == 0)
        {
            isAiOn = false;
        }
        else isAiOn = true;
       
    }



    /// // TODO : CZEKAC NA NASTEPNA TURE DO KONCA ANIMACJI!!!  - > ZOBACZ NA:     StartCoroutine(hexMap.DoUnitMoves(SelectedToster));


    void Update()
    {
        Update_CurrentFunc();

        LastMousePosition = Input.mousePosition;

        hexUnderMouse = MouseToHex();
        // MouseToPart();
        hexLastUnderMouse = hexUnderMouse;
        GOLastUnderMouse = GOUnderMouse;
    }



    void Update_DetectModeStart()
    {
        activeButtons = false;
        hexMap.unHighlightAroundHex(hexMap.GetHexAt(5, 5), 20);



        if (SelectedToster != null)
        {
            //Debug.LogError(SelectedToster.Name);
            outlineManagerMainToster.RemoveOutline();
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
        if (isAiOn==true)
        {
            if (SelectedToster.Team == hexMap.Teams[1])
            {
                
                AI.AskAIwhattodo();
                return;
            }
        }
   
        //Debug.LogError(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>().bounds);
       // outlineManagerMainToster.ChangeObj(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>());      ///Odpowiadają za otoczke wybranego tostera
        outlineManagerMainToster.ChangeObj(hexMap.GetObjectFromHex(SelectedToster.Hex).GetComponentInChildren<Renderer>());//SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>());
        outlineManagerMainToster.AddMainOutlineWithReset();

        hexUnderMouse = SelectedToster.Hex;
        if (SelectedToster.Taunt==true)
        {
            Update_CurrentFunc = Taunted;
            return;
        }
        SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
        Update_CurrentFunc = SelectTosterMovement;
        return;
    }

    void Taunted()
    {
        Debug.Log("HOW DARE YOU!?");

        StartCoroutine(DoMoveAndAttackWithoutCheck(SelectedToster.whoTauntedMe.Hex,SelectedToster.whoTauntedMe));
    }
    // TRYB RUCHU JEDNOSTKI
    void SelectTosterMovement()
    {
        activeButtons = true;
        Defense();
        Pathing();
        Outlining();
        shiftctrlmode();
        ScrollLook();
        Wait();
        //Heal();
        CastSkill();
     if (SelectedToster.isRange == true)
        {
            HighlightEnemy();
        }

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
                        StartCoroutine(DoMoveAndAttack(hexUnderMouse.Tosters[0]));
                }
                else if (hexUnderMouse.Highlight == true)
                {
                    hexUnderMouse.Tosters[0].ShootME(SelectedToster);
                    SelectedToster.Moved = true;
                    CancelUpdateFunc();
                }
            }
            else if (hexUnderMouse.Tosters.Count == 0)
            {

                StartCoroutine(DoMoves());
            }
            // CancelUpdateFunc();
            return;
        } //DoMove 
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
                    outlineManager.ChangeObjects(Ren);
                }
                else if (TempOutlinedToster != hexUnderMouse.Tosters[0])
                {

                    outlineManager.RemoveAllButMain();
                    TempOutlinedToster = hexUnderMouse.Tosters[0];
                    List<Renderer> Ren = new List<Renderer>(); Ren.Add((hexMap.GetObjectFromHex(hexUnderMouse).GetComponentInChildren<Renderer>()));

                    outlineManager.ChangeObjects(Ren);
                }
            }
            else if (TempOutlinedToster != null && hexUnderMouse.Tosters.Count == 0)
            {

                outlineManager.RemoveAllButMain();
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
    IEnumerator DoMoves()
    {
        outlineManagerMainToster.RemoveOutline();
        activeButtons = false;
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
        SelectedToster.Pathing_func(hexUnderMouse, false);

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

    public IEnumerator DoMovesPath(List<HexClass> h)
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

    IEnumerator DoMoveAndAttack(TosterHexUnit toster)
    {

        var temp = MouseToPart();
        if (temp.Tosters.Count == 0)
        {
            if (temp != null && temp.Highlight == true)
            {

                outlineManagerMainToster.RemoveOutline();
                activeButtons = false;
                TargetToster = hexUnderMouse.Tosters[0];
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
                shiftmode = false;
            }
        }
        else if (temp.Tosters[0] == SelectedToster)
        {
            if (temp != null && temp.Highlight == true)
            {

                outlineManagerMainToster.RemoveOutline();
                activeButtons = false;
                TargetToster = hexUnderMouse.Tosters[0];
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
                shiftmode = false;
            }
        }
     
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
    }


  public  IEnumerator DoMoveAndAttackWithoutCheck(HexClass temp, TosterHexUnit toster)
    {

        outlineManagerMainToster.RemoveOutline();
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


            if (SelectedToster.Hex == temp) toster.AttackMe(SelectedToster);
        }
            CancelUpdateFunc();
            shiftmode = false;
        
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
    }



    public void EndSkills()
    {
        if (castManager.isTurn)
        {
            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 20);
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
            SelectedToster.Moved = true;
            CancelUpdateFunc();
            shiftmode = false;
        }
        else
        {
            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 20);
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
       
            CancelUpdateFunc();
            shiftmode = false;
        }
        return;
    }

    public void CancelSpellCasting()
    {
      if (castManager.isInProgress == false)
        {
            castManager.SetFalse();
            hexMap.unHighlightAroundHex(hexUnderMouse, castManager.aoeradius + 20);
            CancelUpdateFunc();
            return;
        }
    }
    public void SetCD()
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
            if ((hexUnderMouse.Tosters.Count != 0 && hexUnderMouse != SelectedToster.Hex) || hexUnderMouse.Tosters.Count == 0)
            {
                
                hexMap.unHighlightAroundHex(SelectedToster.Hex, 1);

                hexMap.HighlightSlash(SelectedToster, hexUnderMouse);//hexUnderMouse.Tosters[0]);

            }
            
    

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
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                castManager.startSpell(SelectedToster.skillstrings[SelectedSpellid]);

            }
        }


        if (Input.GetMouseButtonDown(1))
        {
            CancelSpellCasting();
        }
            

        
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
            for (int i = 1; i < 5- SelectedToster.Hex.C; i++)
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

    public void CastSkillBooleans(int SelectedSkill)
    {
        SelectedSpellid = SelectedSkill;
        SkillState = true;

        Debug.LogError((SelectedToster.skillstrings[SelectedSpellid]));
        castManager.getMode(SelectedToster.skillstrings[SelectedSpellid]);
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

        Update_CurrentFunc = SpellCasting;
    }
    public void CastSkillOnlyBooleans()
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

    public static bool SkillState = true;
    void CastSkill()
    {

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (SelectedToster.skillstrings.Count >= 1 && SelectedToster.cooldowns[0]==0)
            {
                CastSkillBooleans(0);
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (SelectedToster.skillstrings.Count >= 2 && SelectedToster.cooldowns[1] == 0)
            {
                CastSkillBooleans(1);
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (SelectedToster.skillstrings.Count >= 3 && SelectedToster.cooldowns[2] == 0)
            {
                CastSkillBooleans(2);
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (SelectedToster.skillstrings.Count >= 4 && SelectedToster.cooldowns[3] == 0)
            {
                CastSkillBooleans(3);
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
                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
                SelectedToster.Waited = true;
                CancelUpdateFunc();
            }
        }
    }
    
    void ShowInfo()
    {
        shiftctrlmode();
        Outlining();
        if (hexUnderMouse.Tosters.Count > 0)
        {
            TosterHexUnit t = hexUnderMouse.Tosters[0];
            canvas.UpdateAllStats(t);
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
            var d =SelectedToster.tosterView.GetComponentInChildren<Animator>();
            if (d != null)
            {
                Debug.Log(d);
                d.Play("Defense");

            }
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
                SelectedToster.Moved = true;
                SelectedToster.DefenceStance = true;
            SelectedToster.SpecialDef += 5;
            CancelUpdateFunc();
            
        }
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
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        int layerMask = LayerIDForHexTiles.value;
        if (Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, layerMask))
        {
            //   Debug.Log( hitInfo.collider.name ); -> WYSWIETL NA CO WSKAZUJE MYSZ
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
        Ray mouseRay = Camera.main.ScreenPointToRay(mousePos);
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
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
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
        Vector3 dir = hitPos - Camera.main.transform.position;
        
        Vector3 p = Camera.main.transform.position;

        // Stop zooming out at a certain distance.
        // TODO: Maybe you should still slide around at 20 zoom?
        if (scrollAmount > 0 || p.y < (maxHeight - 0.1f)) {
            cameraTargetOffset += dir * scrollAmount;
        }
        Vector3 lastCameraPosition = Camera.main.transform.position;
        Vector3 Cam = Camera.main.transform.position;
        Cam.y += cameraTargetOffset.y;
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, Camera.main.transform.position+ cameraTargetOffset, Time.deltaTime);
        cameraTargetOffset -= Camera.main.transform.position - lastCameraPosition;


        p = Camera.main.transform.position;
        if (p.y < minHeight) {
            p.y = minHeight;
        }
        if (p.y > maxHeight) {
            p.y = maxHeight;
        }
        Camera.main.transform.position = p;
        /*
        // Change camera angle
        Camera.main.transform.rotation = Quaternion.Euler (
            Mathf.Lerp (45, 65, Camera.main.transform.position.y / maxHeight),
            Camera.main.transform.rotation.eulerAngles.y,
            Camera.main.transform.rotation.eulerAngles.z
        );
        */
        Camera.main.transform.rotation = Quaternion.Euler(
    Mathf.Lerp(45, 65, Camera.main.transform.position.x / maxHeight),
    Camera.main.transform.rotation.eulerAngles.y,
    Camera.main.transform.rotation.eulerAngles.z
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
                    SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
                    SelectedToster.Waited = true;
                    CancelUpdateFunc();
                }
       
    }


   public  void DefenseB()
    {

                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.GetMS());
                SelectedToster.Moved = true;
                SelectedToster.DefenceStance = true;
                SelectedToster.SpecialDef += 5;
                CancelUpdateFunc();
  
    }

    public void CastSkill1B()
    {
        
        CancelSpellCasting();
        CastSkillBooleans(0);
    }
    public void CastSkill2B()
    {
        CancelSpellCasting();
        CastSkillBooleans(1);
    }
    public void CastSkill3B()
    {
        CancelSpellCasting();
        CastSkillBooleans(2);
    }
    public void CastSkill4B()
    {
        CancelSpellCasting();
        CastSkillBooleans(3);
    }
}
