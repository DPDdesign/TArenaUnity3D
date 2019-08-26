using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    List<TeamClass> Teams;
    HexMap hexMap;
    MouseControler MController;
   
    private void Start()
    {
        hexMap = FindObjectOfType<HexMap>();
        MController = FindObjectOfType<MouseControler>();
      
    }
    /// <summary>
    /// // TODO : CZEKAC NA NASTEPNA TURE DO KONCA ANIMACJI!!!
    /// </summary>
    /// <returns></returns>
    /// 


  public int isAnyoneAlive()
    {
        Teams = hexMap.Teams;
        if (Teams[0].IsMyTeamDEAD())
        {
            return 1;
        }

        if (Teams[1].IsMyTeamDEAD())
        {
            return 2;
        }
        return 0;
    }


    public void StartGame()
    {
        Teams = hexMap.Teams;
        Teams[0].NewTurn();
        Teams[1].NewTurn();
    }
    public TosterHexUnit AskWhosTurn()
    {

        Teams = hexMap.Teams;

        TosterHexUnit TeamRed = Teams[0].AskForUnit();
        TosterHexUnit TeamBlue = Teams[1].AskForUnit();

        if (TeamRed == TeamBlue)
        {
            Teams[0].NewTurn();
            Teams[1].NewTurn();
            return AskWhosTurn();
        }


      
        if (TeamRed == null) return TeamBlue;
        if (TeamBlue == null) return TeamRed;
       
        if (TeamBlue.GetIni() > TeamRed.GetIni() && TeamBlue.Waited != true || (TeamBlue.GetIni() <= TeamRed.GetIni() && TeamRed.Waited == true && TeamBlue.Waited != true))
        {
            return TeamBlue;
        }
        if (TeamRed.GetIni() > TeamBlue.GetIni() && TeamRed.Waited != true || (TeamRed.GetIni()<=TeamBlue.GetIni() && TeamBlue.Waited == true && TeamRed.Waited != true))
        {
            return TeamRed;
        }
        if (TeamRed.GetIni() == TeamBlue.GetIni() && TeamRed.Waited == false && TeamBlue.Waited == false)
        {
          
            if (TeamBlue.GetMS() > TeamRed.GetMS())
            {
                return TeamBlue;
            }
            if (TeamRed.GetMS() > TeamBlue.GetMS())
            {
                return TeamRed;
            }
            int t = 0;
            int o = 0;
            for (int i = 0; i < Teams[0].Tosters.Count; i++) { if (Teams[0].Tosters[i] == TeamRed) t = i; }
            for (int i = 0; i < Teams[1].Tosters.Count; i++) { if (Teams[1].Tosters[i] == TeamBlue) o = i; }
            if (t > o)
            {
                return TeamBlue;
            }
            else
            {
                return TeamRed;
            }
        }

        // zostały == lub wait
        if (TeamBlue.Waited == true && TeamRed.Waited == true)
        {
           
            if (TeamBlue.GetIni() < TeamRed.GetIni())
            {
                return TeamBlue;
            }
            if (TeamRed.GetIni() < TeamBlue.GetIni())
            {
                return TeamRed;
            }
          
            if (TeamRed.GetIni() == TeamBlue.GetIni())
            {
                if (TeamBlue.GetMS() < TeamRed.GetMS())
                {
                    return TeamBlue;
                }
                if (TeamRed.GetMS() < TeamBlue.GetMS())
                {
                    return TeamRed;
                }
                if (TeamRed.GetMS() == TeamBlue.GetMS())
                {
                    int t = 0;
                    int o = 0;
                    for (int i = 0; i < Teams[0].Tosters.Count; i++) { if (Teams[0].Tosters[i] == TeamRed) t = i; }
                    for (int i = 0; i < Teams[1].Tosters.Count; i++) { if (Teams[1].Tosters[i] == TeamBlue) o = i; }

                    if (o > t || o == t)
                    {
                        return TeamBlue;
                    }
                    else
                    {
                        return TeamRed;
                    }
                }
            }
        }
        Debug.LogError("TeamRED: " + TeamRed.Waited + " , " + TeamRed.GetIni());
        Debug.LogError("TeamBlue: " + TeamBlue.Waited + " , " + TeamBlue.GetIni());
        return null;

    }


}
    



