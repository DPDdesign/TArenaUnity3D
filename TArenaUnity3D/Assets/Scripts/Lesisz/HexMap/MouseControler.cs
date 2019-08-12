using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MouseControler : MonoBehaviour
{
   

    HexMap hexMap;
    HexClass hexUnderMouse;
    HexClass hexLastUnderMouse;
    GameObject GOUnderMouse;
    GameObject GOLastUnderMouse=null;
    HexClass[] hexPath;
    public LayerMask LayerIDForHexTiles;

   // public Canvas canvas;
    public UICanvas canvas;
    delegate void UpdateFunc();
    UpdateFunc Update_CurrentFunc;

    Vector3 LastMousePosition;
    bool shiftmode = false;
    LineRenderer lineRenderer;
    int MouseDragTreshold = 1;
    Vector3 TestGoUp;
    bool isDragginCamera = false;
    Vector3 LastMouseGroundPlanePosition;
    Vector3 cameraTargetOffset;
    TurnManager TM;

    TosterHexUnit SelectedToster = null;
    TosterHexUnit TempSelectedToster = null;
    
    void Start()
    {
        Update_CurrentFunc = Update_DetectModeStart;
           hexMap = GameObject.FindObjectOfType<HexMap>();
        hexPath = null;
    }


    /// <summary>
    /// // TODO : CZEKAC NA NASTEPNA TURE DO KONCA ANIMACJI!!!  - > ZOBACZ NA:     StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
    /// </summary>
    /// <returns></returns>
    void Update()
    {   
        Update_CurrentFunc();
        
        LastMousePosition = Input.mousePosition;

        hexUnderMouse = MouseToHex();

        hexLastUnderMouse = hexUnderMouse;
        GOLastUnderMouse = GOUnderMouse;
    }



    void Update_DetectModeStart()
    {
        TM = FindObjectOfType<TurnManager>();
        SelectedToster = TM.AskWhosTurn();
        hexUnderMouse = SelectedToster.Hex;
        SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
        if (!shiftmode)
            Update_CurrentFunc = SelectTosterMovement;
        else Update_CurrentFunc = CheckTosterMovement;
         /// OLD - NOT USED - FUNKCJA NIGDY NIE SCHODZI PONIŻEJ TEJ LINII///     
        if (Input.GetMouseButtonDown(2))
        {
          
        }  
        else if (Input.GetMouseButtonDown(1) )
        {
           TosterHexUnit[] tosters =   hexUnderMouse.tosters();

            if (tosters.Length>0)
            {

                SelectedToster = tosters[0];
                SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
                Update_CurrentFunc = CheckTosterMovement;
            }

        }
        else if (Input.GetMouseButton(0) && Vector3.Distance(Input.mousePosition, LastMousePosition) > MouseDragTreshold)
        {

        }
        else if (Input.GetMouseButtonDown(0))
        {
            TosterHexUnit[] tosters = hexUnderMouse.tosters();

            if (tosters.Length > 0)
            {
                Debug.LogError( tosters[0].MovmentSpeed);
                SelectedToster = tosters[0];
                SelectedToster.Hex.hexMap.HighlightWithPath(SelectedToster);
                Update_CurrentFunc = SelectTosterMovement;
            }

        }
        else if (Input.GetMouseButton(2) &&
            Vector3.Distance(Input.mousePosition, LastMousePosition) > MouseDragTreshold) 
        {
            //when mouse is hold down and mouse moved = camera drag
            Update_CurrentFunc = Update_CameraDrag;
            LastMouseGroundPlanePosition = MouseToGroundPlane(Input.mousePosition);
            Update_CameraDrag();
        }
        else if (Input.GetKey("z"))     {
            Debug.LogError(MouseToHex().Tosters.Count);
        }
        /// ^ ^ ^ ^ ^ ^ ^ ^ ///    
    }

    // SHIFT/CTRL MODE ON -> TRYB SPRAWDZANIA RUCHU INNYCH JEDNOSTEK
    void CheckTosterMovement()
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
        }
        if (Input.GetMouseButtonDown(0) && hexUnderMouse.Highlight && hexUnderMouse != SelectedToster.Hex && !SelectedToster.Team.HexesUnderTeam.Contains(hexUnderMouse))
        {
            SelectedToster.move = true;
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.MovmentSpeed);
            SelectedToster.Pathing_func(hexUnderMouse);
            StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
            SelectedToster.Moved = true;
            CancelUpdateFunc();
            return;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.LeftControl))
        {
            shiftmode = false;
            if (TempSelectedToster!=null) TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.MovmentSpeed, SelectedToster);
            CancelUpdateFunc();
        } else
        if (hexUnderMouse.Tosters.Count > 0)
        {
            if (TempSelectedToster != null)
                if (hexUnderMouse.Tosters[0]!= TempSelectedToster )
            {
                TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.MovmentSpeed, SelectedToster);
            }
            hexUnderMouse.Tosters[0].Hex.hexMap.CheckWithPath(hexUnderMouse.Tosters[0]);
            TempSelectedToster = hexUnderMouse.Tosters[0];
        }
        else
        {
            if (TempSelectedToster!=null)
            TempSelectedToster.Hex.hexMap.unCheckAround(TempSelectedToster.Hex.C, TempSelectedToster.Hex.R, TempSelectedToster.MovmentSpeed, SelectedToster);
        }
         if (Input.GetMouseButtonDown(1) && hexUnderMouse.Tosters.Count > 0)
        {
            Update_CurrentFunc = ShowInfo;
            ShowInfo();
        }
    }

    // TRYB RUCHU JEDNOSTKI
    void SelectTosterMovement()
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
        }     
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.LeftControl)))
        {
            shiftmode = true;
            Update_CurrentFunc = CheckTosterMovement;
        }        
   if (Input.GetMouseButtonDown(0) && hexUnderMouse.Highlight && hexUnderMouse != SelectedToster.Hex &&  !SelectedToster.Team.HexesUnderTeam.Contains(hexUnderMouse)) 
        {
            SelectedToster.move = true;
            SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.MovmentSpeed);
            SelectedToster.Pathing_func(hexUnderMouse);
            StartCoroutine(hexMap.DoUnitMoves(SelectedToster));
            SelectedToster.Moved = true;
            CancelUpdateFunc();
            return;
        }
        else if (Input.GetMouseButton(2) &&
            Vector3.Distance(Input.mousePosition, LastMousePosition) > MouseDragTreshold)
        {
            //when mouse is hold down and mouse moved = camera drag
            Update_CurrentFunc = Update_CameraDrag;
            LastMouseGroundPlanePosition = MouseToGroundPlane(Input.mousePosition);
            Update_CameraDrag();
        }
        else if (Input.GetKeyUp(KeyCode.N))
        {
            if (SelectedToster.Waited == false)
            {
                SelectedToster.Hex.hexMap.unHighlight(SelectedToster.Hex.C, SelectedToster.Hex.R, SelectedToster.MovmentSpeed);
                SelectedToster.Waited = true;
                CancelUpdateFunc();
            }         
        }
   else if (Input.GetMouseButtonDown(1) && hexUnderMouse.Tosters.Count>0)
        {
            Update_CurrentFunc = ShowInfo;
            ShowInfo();
        }
    }
    


    void ShowInfo()
    {
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
