using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MouseOverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public class TosterStats
    {
          public int hp=0;
        public int att = 0;
        public int def = 0;
        public int Int = 0;
        public int speed = 0;
        public int DmgMin = 0;
        public int DmgMax = 0;
        public int Cost = 0;
        public List<string> spells;

        public TosterStats(int hp, int att, int def, int @int, int speed, int dmgMin, int dmgMax, int cost, List<string> spells)
        {
            this.hp = hp;
            this.att = att;
            this.def = def;
            Int = @int;
            this.speed = speed;
            DmgMin = dmgMin;
            DmgMax = dmgMax;
            Cost = cost;
            this.spells = spells;
        }

   
    }
    public GameObject StatsPanel;
    public List<Text> StatsText;
     public TosterStats tosterStats;
    public string Name;

 public   Generator g;
   public void SendToGenerator()
    {
        g.SaveCost(tosterStats.Cost);
        g.SaveUnit(Name);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      
    }
  

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.LogError("done");
        StatsText[0].text = Name;
        StatsText[1].text = tosterStats.hp.ToString();
        StatsText[2].text = tosterStats.att.ToString();
        StatsText[3].text = tosterStats.def.ToString();
        StatsText[4].text = tosterStats.DmgMin.ToString() + " - " + tosterStats.DmgMax.ToString();
        StatsText[5].text = tosterStats.speed.ToString();
        StatsText[6].text = tosterStats.Int.ToString();
        StatsText[7].text = tosterStats.Cost.ToString();
        string temp = "";
        foreach (string s in tosterStats.spells)
        {
            temp += s + "\n";
        }


        StatsText[8].text = temp;
        StatsPanel.SetActive(true);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StatsPanel.SetActive(false);
    }
}
