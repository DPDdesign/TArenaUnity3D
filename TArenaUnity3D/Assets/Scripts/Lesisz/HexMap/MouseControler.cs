using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Xenu.Game;
public class MouseControler : MonoBehaviour
{
   

    HexMap hexMap;
    public static HexClass hexUnderMouse;
    public static HexClass hexLastUnderMouse;
    GameObject GOUnderMouse;
    GameObject GOLastUnderMouse=null;
    HexClass[] hexPath;
    public LayerMask LayerIDForHexTiles;
    public LayerMask LayerIDForPartTiles;
    public UnityOutlineManager outlineManager;
    public UnityOutlineManagerMainToster outlineManagerMainToster;
    // public Canvas canvas;
    public UICanvas canvas;
    delegate void UpdateFunc();
    UpdateFunc Update_CurrentFunc;

    Vector3 LastMousePosition;
    public bool shiftmode = false;
    LineRenderer lineRenderer;
    int MouseDragTreshold = 1;
    Vector3 TestGoUp;
    bool isDragginCamera = false;
    Vector3 LastMouseGroundPlanePosition;
    Vector3 cameraTargetOffset;
    TurnManager TM;

    public static TosterHexUnit SelectedToster = null;
    TosterHexUnit TempSelectedToster = null;
    TosterHexUnit TempOutlinedToster = null;
    TosterHexUnit TargetToster = null;
    void Start()
    {
        Update_CurrentFunc = Update_DetectModeStart;
           hexMap = GameObject.FindObjectOfType<HexMap>();
        hexPath = null;

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
        TM = FindObjectOfType<TurnManager>();

        if (SelectedToster!=null)
        {
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

        outlineManagerMainToster.ChangeObj(SelectedToster.tosterView.gameObject.GetComponentInChildren<Renderer>());      ///Odpowiadają za otoczke wybranego tostera
        outlineManagerMainToster.AddMainOutlineWithReset();

        hexUnderMouse = SelectedToster.Hex;
        SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
        Update_CurrentFunc = SelectTosterMovement;
        return;     
    }
    
  
    // TRYB RUCHU JEDNOSTKI
    void SelectTosterMovement()
    {
        Defense();
        Pathing();
        Outlining();
        shiftctrlmode();       
        ScrollLook();
        Wait();
        //Heal();
        CastSkill();

        if (Input.GetMouseButtonDown(1) && hexUnderMouse.Tosters.Count > 0)
        {
            Update_CurrentFunc = ShowInfo;
            ShowInfo();
        } //ShowStats     //hexUnderMouse.Highlight - Dostpeny HEX 
        if (Input.GetMouseButtonDown(0) && hexUnderMouse.Highlight && hexUnderMouse != SelectedToster.Hex && !SelectedToster.Team.HexesUnderTeam.Contains(hexUnderMouse))
        {
            //     Debug.LogError("test");
            if (hexUnderMouse.Tosters.Count > 0 && hexUnderMouse.Tosters[0].Team != SelectedToster.Team)
            {

                StartCoroutine(DoMoveAndAttack(hexUnderMouse.Tosters[0]));
            }
            else if (hexUnderMouse.Tosters.Count == 0)
            {

                StartCoroutine(DoMoves());
            }
           // CancelUpdateFunc();
            return;
        } //DoMove 
    }

    void Outlining()
    {
        if (hexUnderMouse != SelectedToster.Hex)
        {
            if (hexUnderMouse.Tosters.Count > 0)
            {
                if (TempOutlinedToster == null)
                {

                    TempOutlinedToster = hexUnderMouse.Tosters[0];
                    List<Renderer> Ren = new List<Renderer>();
                    Ren.Add(hexUnderMouse.Tosters[0].tosterView.gameObject.GetComponentInChildren<Renderer>());
                    outlineManager.ChangeObjects(Ren);
                }
                else if (TempOutlinedToster != hexUnderMouse.Tosters[0])
                {

                    outlineManager.RemoveAllButMain();
                    TempOutlinedToster = hexUnderMouse.Tosters[0];
                    List<Renderer> Ren = new List<Renderer>();
                    Ren.Add(hexUnderMouse.Tosters[0].tosterView.gameObject.GetComponentInChildren<Renderer>());
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
      //  shiftctrlmode();
       // ScrollLook();
    }
    IEnumerator DoMoves()
    {
        SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.MovmentSpeed);
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

    IEnumerator DoMoveAndAttack(TosterHexUnit toster)
    {
        TargetToster = hexUnderMouse.Tosters[0];
        var temp = MouseToPart();
        if (temp != null && temp.Highlight == true)
        {
            SelectedToster.move = true;
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.MovmentSpeed);

       
            //Debug.LogError("C: " + temp.C + "  R:" + temp.R);
         //   hexPath = SelectedToster.Pathing(temp);


            SelectedToster.Pathing_func(temp, false);
            SelectedToster.Moved = true;

            Update_CurrentFunc = BeforeNextTurn;
            StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
            yield return new WaitUntil(() => SelectedToster.tosterView.AnimationIsPlaying == false);
            toster.AttackMe(SelectedToster);
            CancelUpdateFunc();
            shiftmode = false;
        }
        // Debug.LogError(SelectedToster.tosterView.AnimationIsPlaying);
    }
    public void EndSkills()
    {
        SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.MovmentSpeed);
        SelectedToster.Moved = true;
        CancelUpdateFunc();
        shiftmode = false;
    }
    public static bool SkillState = true;
    void CastSkill()
    {
       
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
              SelectedToster.skills[0].CastSkill(); 
            if(!SkillState) { EndSkills(); }
            
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
                hexPath = SelectedToster.Pathing(hexUnderMouse);
                if (hexPath != null)
                {
                    foreach (HexClass h in hexPath)
                    {
                        TestGoUp = h.MyHex.transform.position;
                        TestGoUp.y = -0.1f;
                        h.MyHex.transform.position = TestGoUp;
                    }
                }
            }
            else if (hexUnderMouse.Tosters[0].Team!=SelectedToster.Team)
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
                            TestGoUp.y = -0.1f;
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
                if (TempSelectedToster != null) TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.MovmentSpeed, SelectedToster);
                CancelUpdateFunc();
            }
            else
            if (hexUnderMouse.Tosters.Count > 0)
            {
                if (TempSelectedToster != null)
                    if (hexUnderMouse.Tosters[0] != TempSelectedToster)
                    {

                        TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.MovmentSpeed, SelectedToster);
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
                    TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.MovmentSpeed, SelectedToster);
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
                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.MovmentSpeed);
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
            canvas.UpdateAllStats(t.HP, t.TempHP, t.Att, t.Def, 1, t.MovmentSpeed, t.Initiative, t.Name);
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
    
                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.MovmentSpeed);
                SelectedToster.Moved = true;
                SelectedToster.DefenceStance = true;
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
    HexClass MouseToHex()
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

        //Debug.Log("Found nothing.");
        return hexUnderMouse;
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
}
