using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameMotion : MonoBehaviour
{
    Vector3 PosCam;
    Quaternion Q;
    Vector3 currentV;

    private void Start()
    {
        PosCam = this.transform.position;
        Q = this.transform.rotation;
        currentV = Vector3.zero;
    }



    void CheckIfCameraMoved()
    {
        if (PosCam != this.transform.position)
        {
            // SOMETHING moved the camera.

            Vector3 test = this.transform.position;
            if (this.transform.position.x > PosCam.x + 5)
                test.x= PosCam.x + 5;
            if (this.transform.position.x < PosCam.x - 5)
                test.x = PosCam.x - 5;
            if (this.transform.position.z > PosCam.z + 1)
                test.z = PosCam.z + 1;
            if (this.transform.position.z < PosCam.z - 1) 
                test.z = PosCam.z - 1;
            if (this.transform.position.y > PosCam.y + 3)
                test.y = PosCam.y + 3;
            if (this.transform.position.y < PosCam.y - 10)
                test.y = PosCam.y - 10;

    
            if (this.transform.position.y == PosCam.y)
                test = PosCam;
            if (this.transform.position.x == PosCam.x)
                test = PosCam;
            if (this.transform.position.z == PosCam.z)
                test = PosCam;
            this.transform.position = Vector3.SmoothDamp(this.transform.position, test, ref currentV, 0.5f) ;

        }
    }
    private void Update()
    {
        CheckIfCameraMoved();
    }
}

