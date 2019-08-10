using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TosterView : MonoBehaviour
{
    Vector3 oldPos;
    Vector3 newPos;
    Vector3 beforeJumpPos;
    Vector3 currentV;
    float smoothTime = 0.1f;
    float speed = 5f;

    HexMap_Highlight map;
    public bool AnimationIsPlaying = false;
    private void Start()
    {
        oldPos = newPos = this.transform.position;
        beforeJumpPos.y = newPos.y;
    }

    public void OnTosterMoved(HexClass oldHex, HexClass newHex)
    {
        // animate move
        
        oldPos = oldHex.Position();
        newPos = newHex.Position();


        newPos.y = this.transform.position.y;
        currentV = Vector3.zero;

        AnimationIsPlaying = true;
           // GameObject.FindObjectOfType<HexMap>().AnimationIsPlaying = true;

    }

    private void OnMouseOver()
    {
        
        
       newPos.y= 2;
      

    }

    private void OnMouseUp()
    {
         
    }
    private void OnMouseExit()
    {
        newPos.y = beforeJumpPos.y;
    }

    

    private void Update()
    {
         this.transform.position = Vector3.SmoothDamp(this.transform.position, newPos, ref currentV, smoothTime);
        //this.transform.position = Vector3.Lerp(this.transform.position, newPos,smoothTime);
        if (Vector3.Distance(this.transform.position, newPos) < 0.1f)
        {
            AnimationIsPlaying = false;
           // GameObject.FindObjectOfType<HexMap>().AnimationIsPlaying = false;
        }
        // this.transform.position = Vector3.SmoothDamp(this.transform.position, newPos, ref currentV, smoothTime);
    }
}
