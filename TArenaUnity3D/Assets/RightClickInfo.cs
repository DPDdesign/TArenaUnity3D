using System.Collections;
using System.Collections.Generic;
using System.Xml;
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
            //TODO: VALIDATE SCHEMA/XML
            TextAsset textAsset = (TextAsset)Resources.Load("data/skills");
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(textAsset.text);
            XmlNodeList nodes = xmldoc.SelectNodes("Skills/Skill/Name");
            int NumberOfNode = 0;
            bool found = false;
            int i = 0;
            foreach (XmlNode node in nodes)
            {

                if (node.InnerText == spell && found == false)
                {
                    Debug.LogError(node.InnerText);
                    found = true;
                    NumberOfNode = i;
                }
                i++;
            }
            nodes = xmldoc.SelectNodes("Skills/Skill");
            //  
            if (found == true)
            {
                SNameOfSkill[j].text = nodes[NumberOfNode].ChildNodes[0].InnerText;
                TypeSkill[j].text = nodes[NumberOfNode].ChildNodes[1].InnerText;
                InfoSkill[j].text = nodes[NumberOfNode].ChildNodes[2].InnerText;
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
