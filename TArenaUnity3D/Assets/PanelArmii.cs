using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;

public class PanelArmii : MonoBehaviour
{
    // Start is called before the first frame update
    public List<Button> buttons;


    public class BuildG
    {
        public int hero;
    }
    void Start()
    {
        



    }
    public void sprawdz()
    {

        GameObject[] btn = GameObject.FindGameObjectsWithTag("herobutton");
     
        // Iterate through the array of 'btn' and add them to the 'buttons' list
        for (int i = 0; i < btn.Length; i++)
        {
            // Adding the current 'btn' to the 'buttons' list
            buttons.Add(btn[i].GetComponent<Button>());
        }

        string path = Application.persistentDataPath + "/build.d";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            BuildG buildG = new BuildG();
            buildG = formatter.Deserialize(stream) as BuildG;
            if (buildG.hero==0)
                buttons[0].GetComponent<Image>().color = Color.white;
            
        }
        else
        {
            Debug.Log("nie ma pliku");
        }
    }
    // Update is called once per frame
    void Update()
    {
      
    }
}
