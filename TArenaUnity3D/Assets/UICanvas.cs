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
                SkillButtons[i].interactable = true;
                SkillT[i].text = (i + 1).ToString() + "\n" + b;
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
