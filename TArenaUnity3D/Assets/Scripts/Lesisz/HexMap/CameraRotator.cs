using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotator : MonoBehaviour
{
    Vector3 cameraTargetOffset;
    public Transform cameraJig;
    public float rotateSpeed;
    public float speed;

    public float zoomSpeed = 5;
    public float minAngle = 51;
    public float maxAngle = 74;
    private float newAngle;
    public float zoomUpdateSpeed = 10;
    private float angle;
    private Vector3 GetBaseInput()


    { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        float s = 0.1f;
        Mathf.Clamp(p_Velocity.x, 8.48f, 33.22f);
        Mathf.Clamp(p_Velocity.z, 0.22f, 15.15f);
    
        if (Input.GetKey(KeyCode.UpArrow))
        {
            p_Velocity += this.transform.forward * Time.deltaTime * speed;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            p_Velocity -= this.transform.forward * Time.deltaTime * speed;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            p_Velocity -= this.transform.right * Time.deltaTime * speed;

        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            p_Velocity += this.transform.right * Time.deltaTime * speed;
        }




        return p_Velocity;
    }

    private void Start()
    {
        

        Mathf.Clamp(this.transform.position.x, 8.48f, 33.22f);
        Mathf.Clamp(this.transform.position.z, 0.22f, 15.15f);
        newAngle = Camera.main.transform.localEulerAngles.x;

    }
    void LateUpdate()
    {
        if (Input.GetKey(KeyCode.A))
        {
            transform.RotateAround(this.transform.position, Vector3.up, rotateSpeed * Time.deltaTime*0.5f);
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.RotateAround(this.transform.position, -Vector3.up, rotateSpeed * Time.deltaTime*0.5f);
        }
       
        if (Input.GetKey(KeyCode.W))
        {
            if (this.GetComponentInChildren<Camera>().fieldOfView > 25)
            {
                this.GetComponentInChildren<Camera>().fieldOfView -= (20 * Time.deltaTime);
                newAngle-=15*Time.deltaTime;
            }
           

        }
        if (Input.GetKey(KeyCode.S))
        {
            if (this.GetComponentInChildren<Camera>().fieldOfView < 45)
            {
                this.GetComponentInChildren<Camera>().fieldOfView += (20 * Time.deltaTime);
                newAngle +=15 * Time.deltaTime;
            }
            
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            if (this.GetComponentInChildren<Camera>().fieldOfView < 45)
            {
                this.GetComponentInChildren<Camera>().fieldOfView += (120 * Time.deltaTime);
            }
            
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            if (this.GetComponentInChildren<Camera>().fieldOfView > 25)
            {
                this.GetComponentInChildren<Camera>().fieldOfView -= (120 * Time.deltaTime);
            }
        }
 




        this.transform.position = this.transform.position + GetBaseInput();

        newAngle -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        newAngle = Mathf.Clamp(newAngle, minAngle, maxAngle);
        if (Input.GetKey(KeyCode.X))
        {
            Debug.LogError(newAngle);
        }   

        Camera.main.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(Camera.main.transform.localEulerAngles.x, newAngle, Time.deltaTime * zoomUpdateSpeed), 0, 0);



        if (this.transform.position.x>30)
        {
            Vector3 v = this.transform.position;
            v.x = 30;
            this.transform.position = v;
        }
        if (this.transform.position.x < 9.2900009f)
        {
            Vector3 v = this.transform.position;
            v.x = 9.2900009f;
            this.transform.position = v;
        }
        if (this.transform.position.z > 9f)
        {
            Vector3 v = this.transform.position;
            v.z = 9f;
            this.transform.position = v;
        }



        if (this.transform.position.z < -2.22f)
        {
            Vector3 v = this.transform.position;
            v.z = -2.22f;
            this.transform.position = v;
        }
    }



    void Update_ScrollZoom()
    {
        // Zoom to scrollwheel
        float scrollAmount = Input.GetAxis("Mouse ScrollWheel");
        float minHeight = 15;
        float maxHeight = 27;
        // Move camera towards hitPos
        Vector3 hitPos = this.transform.position;
        Vector3 dir = hitPos - Camera.main.transform.position;

        Vector3 p = Camera.main.transform.position;

        // Stop zooming out at a certain distance.
        // TODO: Maybe you should still slide around at 20 zoom?
        if (scrollAmount > 0 || p.y < (maxHeight - 0.1f) || p.z < 0)
        {
            cameraTargetOffset += dir * scrollAmount;
        }
        Vector3 lastCameraPosition = Camera.main.transform.position;
        Vector3 Cam = Camera.main.transform.position;
        Cam.y += cameraTargetOffset.y;
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, Camera.main.transform.position + cameraTargetOffset, Time.deltaTime);
        cameraTargetOffset -= Camera.main.transform.position - lastCameraPosition;


        p = Camera.main.transform.position;
        if (p.y < minHeight)
        {
            p.y = minHeight;
        }
        if (p.y > maxHeight)
        {
            p.y = maxHeight;
        }
        Camera.main.transform.position = p;
        
        // Change camera angle
        Camera.main.transform.rotation = Quaternion.Euler (
            Mathf.Lerp (45, 65, Camera.main.transform.position.y / maxHeight),
            Camera.main.transform.rotation.eulerAngles.y,
            Camera.main.transform.rotation.eulerAngles.z
        );
        /*
        Camera.main.transform.rotation = Quaternion.Euler(
    Mathf.Lerp(45, 65, Camera.main.transform.position.x / maxHeight),
    Camera.main.transform.rotation.eulerAngles.y,
    Camera.main.transform.rotation.eulerAngles.z
);*/
    }
}
