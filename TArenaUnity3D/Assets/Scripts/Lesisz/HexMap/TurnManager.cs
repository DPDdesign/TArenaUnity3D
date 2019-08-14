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
        if (TeamRed.Initiative == TeamBlue.Initiative)
        {
            if ((TeamBlue.Waited == false && TeamBlue.Waited == false) || (TeamBlue.Waited == true && TeamBlue.Waited == true))
            {
                // sprawdzamy kto ma  pierwszy kolejnosc - zaczyna ten kto wyzej sie zrespil, pierwszenstwo od lewo gora do prawo dol //
                int t = 0;
                int o = 0;
                for (int i = 0; i < Teams[0].Tosters.Count; i++) { if (Teams[0].Tosters[i] == TeamRed) t = i; }
                for (int i = 0; i < Teams[1].Tosters.Count; i++) { if (Teams[1].Tosters[i] == TeamBlue) o = i; }
                if (o == t)
                {

                    return TeamRed;
                }
                else return TeamBlue;



            }
            else
            if (TeamBlue.Waited == true)
            {
                return TeamRed;
            }
            else
            if (TeamRed.Waited == true)
            {
                return TeamBlue;
            }

        }
        if (TeamRed.Initiative > TeamBlue.Initiative && TeamRed.Waited == false)
        {
            return TeamRed;
        }
        else return TeamBlue;
    }


}
    



