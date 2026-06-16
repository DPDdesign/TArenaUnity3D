using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RightClickInfo : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
   public GameObject infopanel;
    public   Image infoImage;


    public List<Text> SNameOfSkill, InfoSkill, TypeSkill, NameOfSkill;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            infopanel.SetActive(true);
            SetPanelSkills(this.gameObject.GetComponent<MouseOverButton>().tosterStats.spells);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            infopanel.SetActive(false);
        }
    }
    public void SetPanelSkills(List<string> spells) //XML DATA LOAD
    {
        int j = 0;
        foreach (string spell in spells)
        {
            DataMapper.SkillDefinition skillDefinition = DataMapper.Instance.FindSkill(spell);
            if (skillDefinition != null)
            {
                SNameOfSkill[j].text = skillDefinition.Name;
                TypeSkill[j].text = skillDefinition.Type;
                InfoSkill[j].text = skillDefinition.Info;
            }
            j++;
        }
        Debug.LogError(j);
        if (j<3)
        {
            SNameOfSkill[j].text = "";
            TypeSkill[j].text = "";
            InfoSkill[j].text = "";
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
