using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICanvas : MonoBehaviour
{
    public List<Text> InfoTextsList;
    public List<Button> ActionButtons;

    public List<Button> SkillButtons;
    public List<Text> SkillT;
    public GameObject StatsPanel;
    public GameObject EndPanel;
    public Text EndText;
    public MouseControler MC;
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
                if (MC.SelectedToster.cooldowns[i] == 0)
                {
                    SkillButtons[i].interactable = true;
                    SkillT[i].text = (i + 1).ToString() + "\n" + b;
                }
                else
                {
                    SkillButtons[i].interactable = false;
                    SkillT[i].text = "Wait: " + MC.SelectedToster.cooldowns[i].ToString() + " Turns";
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
                SkillT[i].text = "";
                i++;
            }

        }
    }
}
