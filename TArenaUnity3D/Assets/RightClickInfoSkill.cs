using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RightClickInfoSkill : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject infopanel;
    public Image infoImage;
    public Text NameOfSkill;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            infopanel.SetActive(true);
            infoImage.sprite = Resources.Load<Sprite>("Sprites/Info_Pages/" + NameOfSkill.text);
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
