using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;

public class RayCast : MonoBehaviour
{
  
    void Update()
    {
        float turnSpeed = 45.0f;
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
                if (hit.transform != null)
                {
                 //   Debug.Log("Hit " + hit.transform.gameObject.name);
                   // targett.hp++;

                    //  hit.transform.Rotate(Vector3.up * turnSpeed);
                }
        }
    }
}