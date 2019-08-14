using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class TeamClass
{
    public List<TosterHexUnit> Tosters;
    public List<HexClass> HexesUnderTeam;
    int ThisTeamNO;
    PanelArmii.BuildG buildG;
   public  TeamClass()
    {
        Tosters = new List<TosterHexUnit>();
        HexesUnderTeam = new List<HexClass>();
    }

    public void WczytajPlik() // wczytaj plik zgodnie z reprezentacją buildu w PanelArmii - TODO: przenieść strukture/classe buildu do osobnego skryptu
    {
        string path = Application.persistentDataPath + "/build" + ThisTeamNO + ".d";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.OpenRead(path);
            buildG = (PanelArmii.BuildG)formatter.Deserialize(file);
            file.Close();
        }
    }

  


    public void CreateTeamFromFile()
    {
        WczytajPlik();
        // plik wczytany do buildG //
        int i = 0;
        foreach (string toster in buildG.Units) //Tosty są określone po ich nazwie // Ta funkcja ZAWSZE powinna wykonać się 7 razy
        {
            
            if (toster != "" && toster != null && toster != "Null")
            {
             //   Debug.LogError(toster);
                TosterHexUnit nowytoster = new TosterHexUnit(); // + toster do teamu
                nowytoster.InitateType(toster);                 // Znajdz tostera w xmlu -> wczytaj jego staty
                nowytoster.SetMyTeam(this);                     // Ustaw tosterowi do którego teamu należy
                nowytoster.SetAmount(buildG.NoUnits[i]);        // Wczytaj ilość jednostki z buildG
                AddNewUnit(nowytoster);                         // Zapisz naszego tosta do TEGO teamu
                
            }
            i++;
        }


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

                if (t.Waited == false)
                {
                    T = t;
                    Initiative = t.Initiative;
                }
            }
        }
        if (T == null)
            foreach (TosterHexUnit t in Tosters)
            {
            if (t.Moved == false && t.Initiative > Initiative)
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

                if (t.Moved == true && t.isDead==false)
                {
                    t.Moved = false;
                    t.Waited = false;
                    t.DefenceStance = false;
                    t.CounterAttackAvaible = true;
                }
            }
        }

    }

    public bool IsMyTeamDEAD()
    {
        foreach (TosterHexUnit t in Tosters)
        {

            if (t.isDead == false)
            {
                return false;


            }
        }

        return true;
    }


    /// <summary>
    /// TODO:
    /// 
    /// Poprawić Spawn point - tak aby każdy toster pojawiał się zawsze w wyznaczonym punkcie z menu/pliku
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// </summary>
    public void GenerateTeam(HexMap h, int TeamNO, bool You) /// h -> MAPA /// TeamNO -> numer pliku /// You: True = lewy gracz | False = prawy gracz ///
    {
       
        ThisTeamNO = TeamNO;
        CreateTeamFromFile();
        int i = 0;
        ///Tutaj tosty są już wczytane do swojej drużyny
        ///
        foreach (TosterHexUnit Tost in Tosters)
        {
            if (Tost != null)
            {
                Tost.SetTosterPrefab(h);
                Tost.SetTextAmount();
            }
        }
        if (You == true)
        {
          
            if (Tosters.Count <6)
            {
                foreach (TosterHexUnit t in Tosters)
                {
                    if (t != null)
                    {
                        h.GenerateToster(0 + i, 10 - 2 * i, t);
                    
                    }
                    i++;
                }
            }
            else
            {
                int ktory = 1;
                foreach (TosterHexUnit t in Tosters)
                {
                    if (ktory == 4 && t != null)
                    {
                        h.GenerateToster(2, 5, t);
                        i--;
                    }
                    else
                    if (t != null)
                    {
                        h.GenerateToster(0 + i, 10 - 2 * i, t);

                    }
                    ktory++;
                    i++;
                }
            }
        }
        if (You ==false)
        {
            if (Tosters.Count < 6)
            {
                foreach (TosterHexUnit t in Tosters)
                {
                    if (t != null)
                    {
                        h.GenerateToster(14 + i, 10 - 2 * i, t);

                    }
                    i++;
                }
            }
            else
            {
                int ktory = 1;
                foreach (TosterHexUnit t in Tosters)
                {
                    if (ktory == 4)
                    {
                        h.GenerateToster(16, 5, t);
                        i--;
                    }
                    else
                    if (t != null)
                    {
                        h.GenerateToster(14 + i, 10 - 2 * i, t);

                    }
                    ktory++;
                    i++;
                }
            }
        }
    }


}
