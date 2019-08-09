using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseControler : MonoBehaviour
{
   

    HexMap hexMap;
    HexClass hexUnderMouse;
    public LayerMask LayerIDForHexTiles;



    delegate void UpdateFunc();
    UpdateFunc Update_CurrentFunc;

    Vector3 LastMousePosition;




    bool isDragginCamera = false;
    Vector3 LastMouseGroundPlanePosition;
    Vector3 cameraTargetOffset;

    // Start is called before the first frame update
    void Start()
    {
        Update_CurrentFunc = Update_DetectModeStart;
           hexMap = GameObject.FindObjectOfType<HexMap>();
    }

    void Update()
    {
    
        Update_CurrentFunc();
        LastMousePosition = Input.mousePosition;
        hexUnderMouse = MouseToHex();

        // Always do camera zooms (check for being over a scroll UI later)
        Update_ScrollZoom();


    }



    void Update_DetectModeStart()
    {
        if (Input.GetMouseButtonDown(1))
        {
            // Left Button went down - do nothing
        }
       else if (Input.GetMouseButton(1) && Input.mousePosition!=LastMousePosition) 
        {
            //when mouse is hold down and mouse moved = camera drag
            Update_CurrentFunc = Update_CameraDrag;
            LastMouseGroundPlanePosition = MouseToGroundPlane(Input.mousePosition);
        }
    }








    HexClass MouseToHex()
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        int layerMask = LayerIDForHexTiles.value;

        if (Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, layerMask))
        {
            // Something got hit
            Debug.Log( hitInfo.collider.name );

            // The collider is a child of the "correct" game object that we want.
            GameObject hexGO = hitInfo.rigidbody.gameObject;

            return hexMap.GetHexFromGameObject(hexGO);
        }

        //Debug.Log("Found nothing.");
        return null;
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
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, Camera.main.transform.position+ cameraTargetOffset, Time.deltaTime * 2f);
        cameraTargetOffset -= Camera.main.transform.position - lastCameraPosition;


        p = Camera.main.transform.position;
        if (p.y < minHeight) {
            p.y = minHeight;
        }
        if (p.y > maxHeight) {
            p.y = maxHeight;
        }
        Camera.main.transform.position = p;

        // Change camera angle
        Camera.main.transform.rotation = Quaternion.Euler (
            Mathf.Lerp (45, 65, Camera.main.transform.position.y / maxHeight),
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
        if (Input.GetMouseButtonUp(1))
        {
            Debug.Log("Cancelling camera drag.");
            CancelUpdateFunc();
            return;
        }

        // Right now, all we need are camera controls

        Vector3 hitPos = MouseToGroundPlane(Input.mousePosition);

        Vector3 diff = LastMouseGroundPlanePosition - hitPos;
        Camera.main.transform.Translate(diff, Space.World);

        LastMouseGroundPlanePosition = hitPos = MouseToGroundPlane(Input.mousePosition);



    }
    // Update is called once per frame




}
