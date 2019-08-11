using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class TeamClass
{
    List<TosterHexUnit> Tosters;
    int ThisTeamNO;

   public  TeamClass()
    {
        Tosters = new List<TosterHexUnit>();
    }

    public void WczytajPlik()
    {
        string path = Application.persistentDataPath + "/build" + ThisTeamNO + ".d";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.OpenRead(path);
            PanelArmii.BuildG buildG = (PanelArmii.BuildG)formatter.Deserialize(file);
            file.Close();
      //      Tosters.Add(buildG.NazwaBohatera); 
        }
    }

  


    public void CreateTeamFromFile()
    {
        WczytajPlik();
    }

    public void AddNewUnit(TosterHexUnit t)
    {
        Tosters.Add(t);
    }

    public TosterHexUnit AskForUnit()
    {
        int Initiative = 0;
        TosterHexUnit T = null;
        foreach (TosterHexUnit t in Tosters)
        {

            if (t.Moved==false&&t.Initiative>Initiative)
            {
                T = t;
                Initiative = t.Initiative;
            }
        }

        return T;
    }

    
    public void DidMove(TosterHexUnit t)
    {
        t.Moved = true;
    }


    public void NewTurn()
    {
        bool AllMoved=true;
        foreach (TosterHexUnit t in Tosters)
        {

            if (t.Moved == false)
            {
                AllMoved = false;


            }
        }

        if (AllMoved == true)
        {
            foreach (TosterHexUnit t in Tosters)
            {

                if (t.Moved == false)
                {
                    t.Moved = true;


                }
            }
        }

    }
    public void GenerateTeam(HexMap h, int TeamNO)
    {
      
        ThisTeamNO = TeamNO;
        CreateTeamFromFile();
        foreach (TosterHexUnit t in Tosters)
        {

            h.GenerateToster(2, 5, PlayerPrefs.GetInt("LewyToster"));
        }
    }


}
