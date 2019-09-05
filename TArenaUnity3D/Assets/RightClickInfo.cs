using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RightClickInfo : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
   public GameObject infopanel;
 public   Image infoImage;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            infopanel.SetActive(true);
            infoImage.sprite = Resources.Load<Sprite>("Sprites/Info_Pages/" + this.gameObject.name);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            infopanel.SetActive(false);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
