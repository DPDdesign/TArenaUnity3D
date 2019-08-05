using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;

public class Generator : MonoBehaviour
{
    [System.Serializable]
    public class BuildG{
        public int hero;
        }
    public void whichbuttonisselected()
    {
        BuildG Build = new BuildG();
        Build.hero = 1;
        if ("Button_bialytoster" == EventSystem.current.currentSelectedGameObject.name)
            Build.hero = 1;
        if ("Button_czerwonytoster" == EventSystem.current.currentSelectedGameObject.name)
            Build.hero = 2;
        if ("Button_niebieskitoster" == EventSystem.current.currentSelectedGameObject.name)
            Build.hero = 3;
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/build.d";
        System.IO.FileStream stream = new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate);

        formatter.Serialize(stream, Build);
        stream.Close();

        Debug.Log(Build.hero);
    }

    
}
