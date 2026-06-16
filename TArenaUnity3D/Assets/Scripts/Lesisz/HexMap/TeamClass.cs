using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class TeamClass
{
    public List<TosterHexUnit> Tosters;
    public List<HexClass> HexesUnderTeam;
   public int ThisTeamNO;
  public  PanelArmii.BuildG buildG;

   public  TeamClass()
    {
        Tosters = new List<TosterHexUnit>();
        HexesUnderTeam = new List<HexClass>();
     
        // ListOfAutocasts
    }

    public void WczytajPlik() // wczytaj plik zgodnie z reprezentacją buildu w PanelArmii - TODO: przenieść strukture/classe buildu do osobnego skryptu
    {
        string path = DataMapper.Instance.GetBuildFilePath(ThisTeamNO);
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
        //WczytajPlik();
        // plik wczytany do buildG //

  //  buildG = 
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
                nowytoster.StartAutocast();
                AddNewUnit(nowytoster);                         // Zapisz naszego tosta do TEGO teamu
                
            }
            i++;
        }


    }

    public TosterHexUnit AddNewUnit(string name, int amount)
    {
        TosterHexUnit nowytoster = new TosterHexUnit(); // + toster do teamu
        nowytoster.InitateType(name);                 // Znajdz tostera w xmlu -> wczytaj jego staty
        nowytoster.SetMyTeam(this);                     // Ustaw tosterowi do którego teamu należy
        nowytoster.SetAmount(amount);        // Wczytaj ilość jednostki z buildG
        nowytoster.StartAutocast();
        AddNewUnit(nowytoster);
        return nowytoster;// Zapisz naszego tosta do TEGO teamu
    }


    public void AddNewUnit(TosterHexUnit t)
    {
        Tosters.Add(t);
      
    }

    static bool CanReceiveTurn(TosterHexUnit t)
    {
        return t != null && t.isDead == false && t.Amount > 0;
    }

    public TosterHexUnit AskForUnit()
    {
        int Initiative = 0;
        TosterHexUnit T = null;
        foreach (TosterHexUnit t in Tosters)
        {

            if (CanReceiveTurn(t) && t.Moved==false&&t.GetIni()>Initiative)
            {

                if (t.Waited == false && t.Blinded==false)
                {
                    T = t;
                    Initiative = t.GetIni();
                }
            }
        }
        Initiative = 99;
        if (T == null)
            foreach (TosterHexUnit t in Tosters)
            {
            if (CanReceiveTurn(t) && t.Moved == false && t.GetIni() <= Initiative && t.Blinded==false)
            {
                T = t;
                Initiative = t.GetIni();
            }
        }
        return T;
    }



    public TosterHexUnit AskForUnitSimulator()
    {
        int Initiative = 0;
        TosterHexUnit T = null;
        foreach (TosterHexUnit t in Tosters)
        {

            if (CanReceiveTurn(t) && t.GetIni() > Initiative)
            {

                if (t.Blinded==false)
                {
                    T = t;
                    Initiative = t.GetIni();
                }
            }
        }
        Initiative = 99;
        if (T == null)
            foreach (TosterHexUnit t in Tosters)
            {
            if (CanReceiveTurn(t) && t.GetIni() <= Initiative && t.Blinded==false)
            {
                T = t;
                Initiative = t.GetIni();
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
    
            foreach (TosterHexUnit t in Tosters)
            {

                if (CanReceiveTurn(t))
                {
                    
                    t.CheckSpells();
                    if (CanReceiveTurn(t) == false)
                    {
                        continue;
                    }

                    ResetUnitForNewTurn(t);
                }
            }
        

    }

    public IEnumerator NewTurnSequence()
    {
        foreach (TosterHexUnit t in Tosters)
        {
            if (CanReceiveTurn(t))
            {
                yield return t.CheckSpellsSequence();
                if (CanReceiveTurn(t) == false)
                {
                    continue;
                }

                ResetUnitForNewTurn(t);
            }
        }
    }

    public void FinishNewTurnAfterSpellSequence()
    {
        foreach (TosterHexUnit t in Tosters)
        {
            if (CanReceiveTurn(t) == false)
            {
                continue;
            }

            t.FinishNewTurnSpellProcessing();
            if (CanReceiveTurn(t) == false)
            {
                continue;
            }

            ResetUnitForNewTurn(t);
        }
    }

    static void ResetUnitForNewTurn(TosterHexUnit t)
    {
        t.Moved = false;
        t.MovedThisTurn = false;
        t.UsedSkillThisTurn = false;
        t.ClearUsedSkillIdsThisTurn();
        t.CanMoveAfterSkillThisTurn = false;
        t.Waited = false;
        if (t.DefenceStance == true)
            t.SpecialDef -= 5;
        t.DefenceStance = false;
        t.ResetCounterAttack();
    }

    public bool IsMyTeamDEAD()
    {
        foreach (TosterHexUnit t in Tosters)
        {

            if (CanReceiveTurn(t))
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
        PrepareLoadedTosters(h, You);

        if (h.useLegacyMap)
        {
            GenerateLegacyTeam(h, You);
        }
        else
        {
            GenerateSizedTeam(h, You);
        }
    }

    void PrepareLoadedTosters(HexMap h, bool You)
    {
        ///Tutaj tosty są już wczytane do swojej drużyny
        ///
        foreach (TosterHexUnit Tost in Tosters)
        {
            if (Tost != null)
            {
                Tost.teamN = You;
                Tost.SetTosterPrefab(h);
                Tost.SetTextAmount();
            }
        }
    }

    void GenerateLegacyTeam(HexMap h, bool You)
    {
        int i = 0;

        if (You == true)
        {
            if (Tosters.Count < 6)
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

                    Debug.Log(t.Name);
                    if (ktory == 3 && t != null)
                    {
                        h.GenerateToster(1, 7, t);
                        i--;
                    }
                    else
                    if (ktory == 4 && t != null)
                    {
                        h.GenerateToster(2, 5, t);
                      //  i--;
                    }
                    else
                    if (t != null)
                    {
                        h.GenerateToster(0 + i, 10 - 2 * i, t);

                    }
                    Debug.Log(t.Hex.C + "   " + t.Hex.R);
                    ktory++;
                    i++;
                }
            }
        }

        if (You == false)
        {
            if (Tosters.Count < 6)
            {
                foreach (TosterHexUnit t in Tosters)
                {
                    if (t != null)
                    {
                        h.GenerateToster(12 + i, 10 - 2 * i, t);

                    }
                    i++;
                }
            }
            else
            {
                int ktory = 1;
                foreach (TosterHexUnit t in Tosters)
                {
                    if (ktory == 3 && t != null)
                    {
                        h.GenerateToster(13, 7, t);
                        i--;
                    }
                    else
                    if (ktory == 4)
                    {
                        h.GenerateToster(14, 5, t);
                        //i--;
                    }
                    else
                    if (t != null)
                    {
                        h.GenerateToster(12 + i, 10 - 2 * i, t);

                    }
                    ktory++;
                    i++;
                }
            }
        }
    }

    void GenerateSizedTeam(HexMap h, bool You)
    {
        List<HexClass> spawnHexes = BuildSizedMapSpawnHexes(h, You);
        int spawnIndex = 0;

        foreach (TosterHexUnit t in Tosters)
        {
            if (t == null)
            {
                continue;
            }

            if (spawnIndex >= spawnHexes.Count)
            {
                Debug.LogWarning("No free spawn hex found for " + t.Name + " on sized map.");
                continue;
            }

            HexClass spawnHex = spawnHexes[spawnIndex];
            h.GenerateToster(spawnHex.C, spawnHex.R, t);
            spawnIndex++;
        }
    }

    List<int> GetSizedMapSpawnColumns(HexMap h, bool You)
    {
        List<int> columns = new List<int>();
        int length = Mathf.Max(1, h.CurrentLength);

        if (You)
        {
            for (int column = 0; column < length; column++)
            {
                columns.Add(column);
            }
        }
        else
        {
            for (int column = length - 1; column >= 0; column--)
            {
                columns.Add(column);
            }
        }

        return columns;
    }

    List<HexClass> BuildSizedMapSpawnHexes(HexMap h, bool You)
    {
        List<HexClass> spawnHexes = new List<HexClass>();
        List<int> spawnColumns = GetSizedMapSpawnColumns(h, You);

        for (int columnIndex = 0; columnIndex < spawnColumns.Count; columnIndex++)
        {
            int column = spawnColumns[columnIndex];
            AddSizedMapSpawnRows(h, column, true, spawnHexes);
            AddSizedMapSpawnRows(h, column, false, spawnHexes);
        }

        return spawnHexes;
    }

    void AddSizedMapSpawnRows(HexMap h, int column, bool everySecondRowOnly, List<HexClass> spawnHexes)
    {
        for (int row = h.CurrentWidth - 1; row >= 0; row--)
        {
            if (everySecondRowOnly != IsPrimarySizedMapSpawnRow(h, row))
            {
                continue;
            }

            HexClass hex = h.GetHexAt(column, row);
            if (hex != null)
            {
                spawnHexes.Add(hex);
            }
        }
    }

    bool IsPrimarySizedMapSpawnRow(HexMap h, int row)
    {
        return row % 2 == (h.CurrentWidth - 1) % 2;
    }

}
