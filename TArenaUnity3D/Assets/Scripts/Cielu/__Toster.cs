using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class __Toster : MonoBehaviour
{

    public class Toster
    {
        public int BaseHealthPoints;
        public int BaseDamage;
        public int BaseDefence;

        public Toster(int bhp, int bdmg, int bdef)
        {
            BaseHealthPoints = bhp;
            BaseDamage = bdmg;
            BaseDefence = bdef;
        }

    }

    public Toster Tank_Toster = new Toster(100,2,10);

    public Toster Dps_Toster = new Toster(50, 10, 2);

    public Toster Heal_Toster = new Toster(50, 1, 3);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
