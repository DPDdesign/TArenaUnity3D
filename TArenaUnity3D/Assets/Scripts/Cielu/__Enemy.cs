using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class __Enemy : MonoBehaviour
{

    public class Enemy
    {
        public static int BaseHealthPoints;
        public static int BaseDamage;
        public static int BaseDefence;
        public static int CurrentHealth;

        public Enemy(int bhp, int bdmg, int bdef)
        {
            BaseHealthPoints = bhp;
            BaseDamage = bdmg;
            BaseDefence = bdef;
            CurrentHealth = bhp;
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
