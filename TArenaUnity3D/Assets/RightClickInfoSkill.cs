using System.Collections;
using System.Collections.Generic;
using System.Xml;
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
          //  infoImage.sprite = Resources.Load<Sprite>("Sprites/Info_Pages/" + NameOfSkill.text);
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
         
            if (node.InnerText == name && found == false)
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
            SNameOfSkill.text = nodes[NumberOfNode].ChildNodes[0].InnerText;
            TypeSkill.text = nodes[NumberOfNode].ChildNodes[1].InnerText;
            InfoSkill.text = nodes[NumberOfNode].ChildNodes[2].InnerText;
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
