
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimButtonCh : MonoBehaviour
{
    public class TosterStats
    {
        public int hp = 0;
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
    public List<Text> StatsText1;
    public List<Text> StatsText2;
    public TosterStats tosterStats;
    public string Name;

    public Symulator g;

    public string Toster;
    public void SendToSymulator()
    {
        if (Toster == "left")
        {
            g.SetToster1(tosterStats, Name);

        }
        if (Toster == "right")
            g.SetToster2(tosterStats, Name);


        StatsText1[0].text = Name;
        StatsText1[1].text = tosterStats.hp.ToString();
        StatsText1[2].text = tosterStats.att.ToString();
        StatsText1[3].text = tosterStats.def.ToString();
        StatsText1[4].text = tosterStats.DmgMin.ToString();
        StatsText1[9].text = tosterStats.DmgMax.ToString();
        StatsText1[5].text = tosterStats.speed.ToString();
        StatsText1[6].text = tosterStats.Int.ToString();
        StatsText1[7].text = tosterStats.Cost.ToString();
        string temp = "";
        foreach (string s in tosterStats.spells)
        {
            temp += s + "\n";
        }


        StatsText1[8].text = temp;


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
