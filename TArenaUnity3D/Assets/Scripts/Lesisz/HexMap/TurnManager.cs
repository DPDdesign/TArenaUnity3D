using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    List<TeamClass> Teams;
    HexMap hexMap;
    MouseControler MController;
    bool rotating = false;
    public int Tura;
    float duration =3f;
    public GameObject obracacz; 
    public List<Text> turns;
    public List<TosterHexUnit> TostersQue;
    public List<TosterHexUnit> TostersQuep;
    public List<GameObject> QueImages;
    public GameObject pauza;
   
    private void Start()
    {
        hexMap = FindObjectOfType<HexMap>();
        MController = FindObjectOfType<MouseControler>();
      Tura = 1;
    }


public void SetNewTurn()
{
    StartCoroutine(RotateElement(obracacz,new Vector3(0,0,-60f), duration));

}

   IEnumerator RotateElement(GameObject ElementToRotate, Vector3 EulerAngles, float Duration)
    {
            if(rotating)
            {
                yield break;
            }

            rotating = true;

            Vector3 newRot = ElementToRotate.transform.eulerAngles + EulerAngles;
            Vector3 currentRot = ElementToRotate.transform.eulerAngles;
        float counter = 0;
        while (counter <duration)
        {
            counter+=Time.deltaTime;
            ElementToRotate.transform.eulerAngles=Vector3.Lerp(currentRot, newRot,counter);
            yield return null;
        }
        var temp = turns[0];
        temp.text =  (Tura+5).ToString();
        turns.Remove(temp);
        turns.Add(temp);
        Tura++;
        rotating = false;
    }

////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////




public void GetTostersQueue()
{ int q = 0;
int j =0;
     Teams = hexMap.Teams;

    TostersQue = new List<TosterHexUnit>();
    TeamClass team1 = new TeamClass();
    TeamClass team2 = new TeamClass();
    team1.Tosters.AddRange(Teams[0].Tosters);
    team2.Tosters.AddRange(Teams[1].Tosters);
pauza.transform.SetSiblingIndex(team1.Tosters.Count+team2.Tosters.Count);
     TostersQuep = new List<TosterHexUnit>();
    TeamClass team1p = new TeamClass();
    TeamClass team2p = new TeamClass();
     team1p.Tosters.AddRange(Teams[0].Tosters);
    team2p.Tosters.AddRange(Teams[1].Tosters);


    foreach (GameObject objects in QueImages)
    {
        objects.gameObject.SetActive(false);
    }

    TosterHexUnit team1toster = team1.AskForUnit();
    TosterHexUnit team2toster = team2.AskForUnit();
    TosterHexUnit temptoster = null;

    do {
        temptoster = AskWhosTurnSimulator(team1,team2);
        if(temptoster!=null){
            TostersQue.Add(temptoster);
       
        if(temptoster.teamN) {team1.Tosters.Remove(temptoster); }
        else                 {team2.Tosters.Remove(temptoster); }

        SetQueImages(QueImages[q],temptoster);
        q++;}

    }while(temptoster!=null);

    TosterHexUnit team1tosterp = team1p.AskForUnitSimulator();
    TosterHexUnit team2tosterp = team2p.AskForUnitSimulator();
    TosterHexUnit temptosterp = null;

        do {
       
        temptosterp = AskWhosTurnSimulatorS(team1p,team2p);
          if(temptosterp!=null){

                    if(!TostersQue.Contains(temptosterp) || temptosterp.Waited){  
                        Debug.Log("TEST TOSTERA " + temptosterp.Name);
                        TostersQuep.Add(temptosterp);
                         SetQueImages(QueImages[pauza.transform.GetSiblingIndex()+1+j],temptosterp);
                        j++;
                    }

                      if(temptosterp.teamN) {team1p.Tosters.Remove(temptosterp);}
                        else                 {team2p.Tosters.Remove(temptosterp);}

          }
    }while(temptosterp!=null);
}




void SetQueImages(GameObject parentobject, TosterHexUnit toster)
{
parentobject.SetActive(true);


            if(toster.teamN) {parentobject.transform.Find("TQBorder").gameObject.GetComponent<Image>().color = Color.blue;}
            else             {parentobject.transform.Find("TQBorder").gameObject.GetComponent<Image>().color = Color.red;}
    
    parentobject.transform.Find("TQSprite").gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(toster.TosterSpriteName); 
    parentobject.transform.Find("TQAmount").gameObject.GetComponent<Text>().text = toster.Amount.ToString(); 

}



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

        hexMap.DoTurn();

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
            hexMap.DoTurn();
            Tura++;
            SetNewTurn();
         
            return AskWhosTurn();
        }

      
        if (TeamRed == null) return TeamBlue;
        if (TeamBlue == null) return TeamRed;
       
        if (TeamBlue.GetIni() > TeamRed.GetIni() && TeamBlue.Waited != true || (TeamBlue.GetIni() <= TeamRed.GetIni() && TeamRed.Waited == true && TeamBlue.Waited != true))
        {
            return TeamBlue;
        }
        if (TeamRed.GetIni() > TeamBlue.GetIni() && TeamRed.Waited != true|| (TeamRed.GetIni()<=TeamBlue.GetIni() && TeamBlue.Waited == true && TeamRed.Waited != true))
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



 public TosterHexUnit AskWhosTurnSimulator(TeamClass team1, TeamClass team2)
    {

       

        TosterHexUnit TeamRed = team1.AskForUnit();
        TosterHexUnit TeamBlue = team2.AskForUnit();

        if (TeamRed == TeamBlue)
        {
          return null;
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

public TosterHexUnit AskWhosTurnSimulatorS(TeamClass team1, TeamClass team2)
    {

       

        TosterHexUnit TeamRed = team1.AskForUnitSimulator();
        TosterHexUnit TeamBlue = team2.AskForUnitSimulator();

        if (TeamRed == TeamBlue)
        {
          return null;
        }

      
        if (TeamRed == null) return TeamBlue;
        if (TeamBlue == null) return TeamRed;
       
        if (TeamBlue.GetIni() > TeamRed.GetIni() || (TeamBlue.GetIni() <= TeamRed.GetIni()))
        {
            return TeamBlue;
        }
        if (TeamRed.GetIni() > TeamBlue.GetIni()  || (TeamRed.GetIni()<=TeamBlue.GetIni()))
        {
            return TeamRed;
        }
        if (TeamRed.GetIni() == TeamBlue.GetIni())
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
      
        Debug.LogError("TeamRED: " + TeamRed.Waited + " , " + TeamRed.GetIni());
        Debug.LogError("TeamBlue: " + TeamBlue.Waited + " , " + TeamBlue.GetIni());
        return null;

    }


}
    



