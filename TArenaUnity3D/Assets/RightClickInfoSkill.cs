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
    public Text SNameOfSkill, InfoSkill, TypeSkill;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)// && NameOfSkill.text)// != SNameOfSkill.text)
        {
            infopanel.SetActive(true);
            SetPanelSkill(NameOfSkill.text);
          //  infoImage.sprite = DataMapper.Instance.LoadInfoPageSprite(NameOfSkill.text);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            infopanel.SetActive(false);
        }
    }
    public void SetPanelSkill(string name) //XML DATA LOAD
    {
        DataMapper.SkillDefinition skillDefinition = DataMapper.Instance.FindSkill(name);
        if (skillDefinition != null)
        {
            SNameOfSkill.text = skillDefinition.Name;
            TypeSkill.text = skillDefinition.Type;
            InfoSkill.text = skillDefinition.Info;
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
