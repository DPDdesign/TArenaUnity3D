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

    [System.Serializable]
    public class BuildG
    {
        public int hero;
    }
    void Start()
    {
        



    }
    public void sprawdz()
    {

     
        // Iterate through the array of 'btn' and add them to the 'buttons' list


        string path = Application.persistentDataPath + "/build.d";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.OpenRead(path);
            BuildG buildG = (BuildG)formatter.Deserialize(file);

            file.Close();
            Debug.Log(buildG.hero);
            if (buildG.hero == 1)
            {
                buttons[0].image.color = Color.red;
        
            }
            
        }
        else
        {
            Debug.Log("nie ma pliku");
        }
    }
    private void OnEnable()
    {
        sprawdz();
    }


    public void SaveBuild()
    { 

        BuildG Build = new BuildG();

        if (PlayerPrefs.HasKey("which"))
        {
            Build.hero = PlayerPrefs.GetInt("which");
            BinaryFormatter formatter = new BinaryFormatter();
            string path = Application.persistentDataPath + "/build.d";
            FileStream file = File.Create(path);
            formatter.Serialize(file, Build);
            file.Close();

            Debug.Log(Build.hero);
        }
    }
    // Update is called once per frame
    void Update()
    {

    }
}
