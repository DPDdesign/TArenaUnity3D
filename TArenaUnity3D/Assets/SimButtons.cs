using System.Collections;
using System.Collections.Generic;
using UnityEngine;





public class SimButtons : MonoBehaviour
{
    public List<SimButtonCh> Buttons;
    // Start is called before the first frame update
    void Start()
    {
        foreach (SimButtonCh b in Buttons)
        {
            DataMapper.UnitDefinition definition = DataMapper.Instance.FindUnit(b.Name);
            if (definition != null)
            {
                b.tosterStats = new SimButtonCh.TosterStats(
                     definition.HP,
                     definition.Attack,
                     definition.Defense,
                     definition.Initiative,
                     definition.Speed,
                     definition.DamageMinimum,
                     definition.DamageMaximum,
                     definition.Cost,
                     new List<string>(definition.SkillNames)
                     );
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
