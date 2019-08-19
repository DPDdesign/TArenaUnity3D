using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class TostersButtons : MonoBehaviour
{
    public List<MouseOverButton> Buttons;
    // Start is called before the first frame update
    void Start()
    {
        foreach (MouseOverButton b in Buttons)
        {
            //TODO: VALIDATE SCHEMA/XML
                TextAsset textAsset = (TextAsset)Resources.Load("data/Units");
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.LoadXml(textAsset.text);
                XmlNodeList nodes = xmldoc.SelectNodes("Units/Unit/Name");
                XmlNodeList costs = xmldoc.SelectNodes("Units/Unit/Cost");
                  int NumberOfNode = 0;
                bool found = false;
                int i = 0;
                foreach (XmlNode node in nodes)
                {
                    if (node.InnerText == b.Name && found == false)
                    {
                        found = true;
                        NumberOfNode = i;
                    }
                    i++;
                }
                nodes = xmldoc.SelectNodes("Units/Unit");
                //  
                if (found == true)
                {
                    XmlNodeList UnitNodes = nodes[NumberOfNode].ChildNodes;
                    XmlNodeList spells = UnitNodes[8].ChildNodes;

                    List<string> sp = new List<string>();
                    foreach (XmlNode s in spells)
                    {

                        sp.Add(s.InnerText);

                    }

                
                   b.tosterStats = new MouseOverButton.TosterStats(
                   
                        int.Parse(UnitNodes[1].InnerText),//hp
                        int.Parse(UnitNodes[2].InnerText),//att
                        int.Parse(UnitNodes[3].InnerText),//def
                        int.Parse(UnitNodes[4].InnerText),//Int
                        int.Parse(UnitNodes[5].InnerText),//speed                        
                        int.Parse(UnitNodes[6].InnerText),//dmgmin
                        int.Parse(UnitNodes[7].InnerText),//dmgmax
                        int.Parse(costs[NumberOfNode].InnerText),
                        sp
                        );

                
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
