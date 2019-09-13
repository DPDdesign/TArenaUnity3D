using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;
using TimeSpells;

public class UICanvas : MonoBehaviour
{
    public List<Text> InfoTextsList;
    public List<Button> ActionButtons;
    public List<GameObject> SpellImages;
    public List<Button> SkillButtons;
    public List<Text> SkillT;
    public GameObject StatsPanel;
    public GameObject EndPanel;
    public Text EndText;
    public MouseControler MC;
    public List<Text> TypeT;
    public List<Text> Cooldowns;
    // Start is called before the first frame update
    void Start()
    {
        MC = FindObjectOfType<MouseControler>();
    }
    
    public void Disconnect()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect();
    }
    public void UpdateCHP(int chp)
    {
        
       
        InfoTextsList[1].text = chp.ToString();
    }

    public void UpdateAllStats(int mhp, int chp, int att, int def, int dmg , int ms , int INT, string N)
    {
        InfoTextsList[0].text = mhp.ToString();
        InfoTextsList[1].text = chp.ToString();
        InfoTextsList[2].text = att.ToString();
        InfoTextsList[3].text = def.ToString();
        InfoTextsList[4].text = dmg.ToString();
        InfoTextsList[5].text = ms.ToString();
        InfoTextsList[6].text = INT.ToString();
        InfoTextsList[7].text = N;
    }

public void GetSpellsOnToster(TosterHexUnit toster)
{

int i = 0;
    
     List<SpellOverTime> SpellsOnToster = new List<SpellOverTime>();
    SpellsOnToster.AddRange(toster.SpellsGoingOn);

foreach (GameObject gameobject in SpellImages)
{
gameobject.SetActive(false);
}

    foreach(SpellOverTime spell in SpellsOnToster)
    {
       Debug.Log(("Sprites/Skill_Icons/"+SpellsOnToster[i].nameofspell));
         SpellImages[i].GetComponent<Image>().sprite=  Resources.Load<Sprite>("Sprites/Skill_Icons/"+SpellsOnToster[i].nameofspell);
       SpellImages[i].SetActive(true);
       i++;

    }

}



    public void UpdateAllStats(TosterHexUnit toster)
    {
        InfoTextsList[0].text = toster.HP.ToString()+"("+toster.GetHP().ToString()+")";
        InfoTextsList[1].text = toster.TempHP.ToString();
        InfoTextsList[2].text = toster.Att.ToString() + "(" + toster.GetAtt().ToString() + ")";
        InfoTextsList[3].text = toster.Def.ToString() + "(" + toster.GetDef().ToString() + ")";
        InfoTextsList[4].text = toster.mindmg.ToString() + "(" + toster.GetMinDmg().ToString() + ")" + "-" + toster.maxdmg.ToString()  +"(" + toster.GetMaxDMG().ToString() + ")";
        InfoTextsList[5].text = toster.MovmentSpeed.ToString() + "(" + (toster.GetMS()-1).ToString() + ")";
        InfoTextsList[6].text = toster.Initiative.ToString() + "(" + toster.GetIni().ToString() + ")";
        InfoTextsList[7].text = toster.Name;
    }

    // Update is called once per frame
    public void UseSkill(int i)
    {

        SkillButtons[i].image.color= Color.grey;
    }
    public void UnUseSkill(int i)
    {
        if (TypeT[i].text!="Passive")
        SkillButtons[i].image.color = Color.white;
    }


    public string GetTypeOfSkill(string name) //XML DATA LOAD
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
                found = true;
                NumberOfNode = i;
            }
            i++;
        }
        nodes = xmldoc.SelectNodes("Skills/Skill");
        //  
        if (found == true)
        {
            return nodes[NumberOfNode].ChildNodes[1].InnerText;
        }
        else return "brak takiego skilla!";
    }


 
    void Update()
    {
        if (MC.activeButtons == true)
        {

            foreach (Button b in ActionButtons)
            {
                b.interactable = true;
                //    b.
            }
            int i = 0;
            foreach (string b in MC.SelectedToster.skillstrings)
            {
                SkillButtons[i].image.enabled = true;
                if (MC.SelectedToster.cooldowns[i] == 0)
                {
                    Cooldowns[i].gameObject.SetActive(false);
                    SkillButtons[i].interactable = true;
                    SkillT[i].text =b;
                    SkillButtons[i].image.sprite=  Resources.Load<Sprite>("Sprites/Skill_Icons/"+b);




                    if ("Passive" == GetTypeOfSkill(b))

                    {
                        SkillButtons[i].interactable = false;
                        SkillButtons[i].image.color = Color.grey;
                    }
                    else
                    {
                        SkillButtons[i].image.color = Color.white;
                    }
                }
                else
                {
                    SkillButtons[i].interactable = false;

                    SkillT[i].text = b;
                    SkillButtons[i].image.sprite = Resources.Load<Sprite>("Sprites/Skill_Icons/" + b);
                    Cooldowns[i].gameObject.SetActive(true);
                    Cooldowns[i].text = MC.SelectedToster.cooldowns[i].ToString();
                }
                i++;


            }

        }
        else
        {
            foreach (Button b in ActionButtons)
            {
                b.interactable = false;
            }
            int i = 0;
            foreach (Button b in SkillButtons)
            {
                b.interactable = false;
                b.image.enabled = false;
                SkillT[i].text = "";
                i++;
            }

        }
    }
}
