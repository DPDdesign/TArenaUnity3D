using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MostStupidAIEver : MonoBehaviour
{

    public MouseControler MC;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public void AskAIwhattodo()
    {
        
        MC.getSelectedToster();

        //      List<HexClass> hexarea = new List<HexClass>(mouseControler.getHexUnderMouse().hexMap.GetHexesWithinRadiusOf(mouseControler.getHexUnderMouse(), aoeradius);
        List<HexClass> EnemyHexes = MC.GetEnemy();
        HexClass tempCel = null;
        foreach (HexClass h in EnemyHexes)
        {

            if (h.Tosters[0].isDead == false)
            {/*
                for (int i = 0; i < 6; i++)
                {
                    if (i == 0) { tempCel = h.hexMap.GetHexAt(h.C, h.R + 1); }
                    if (i == 1) { tempCel = h.hexMap.GetHexAt(h.C+1, h.R); }
                    if (i == 2) { tempCel = h.hexMap.GetHexAt(h.C+1, h.R - 1); }
                    if (i == 3) { tempCel = h.hexMap.GetHexAt(h.C, h.R - 1); }
                    if (i == 4) { tempCel = h.hexMap.GetHexAt(h.C-1, h.R ); }
                    if (i == 5) { tempCel = h.hexMap.GetHexAt(h.C-1, h.R + 1); }
                    */
             //  h.hexMap.GetHexesWithinRadiusOf(h, 2);
                List<HexClass> hexarea = new List<HexClass>(h.hexMap.GetHexesWithinRadiusOf(h, 1));
                hexarea.Remove(h);
                foreach (HexClass hex in hexarea)
                {
                    if (hex != null && hex.Tosters.Count==0)
                    {
                        
                        //  tempCel = h.hexMap.GetHexAt(h.C, h.R - 1);
                      
                        if (MC.getSelectedToster().IsPathAvaible(hex))
                        {
                           
                            MC.StartCoroutine(MC.DoMoveAndAttackWithoutCheck(hex, h.Tosters[0]));
                            return;
                        }
                    }
                }
                
            }
        }
        int count = 999;
        int hNo = 0;
        int tempi = 0;
        List<HexClass> hexpath = new List<HexClass>(); 
        List<HexClass> hexmaxpath = new List<HexClass>();
        foreach (HexClass h in EnemyHexes)
        {
           
            if (h.Tosters[0].isDead == false)
            {
                 hexpath = new List<HexClass>(MC.getSelectedToster().Pathing(h, true));
                if (hexpath.Count<count)
                {
                    count = hexpath.Count;
                    hNo = tempi;
                }
            }
            tempi++;
      }

        hexpath = new List<HexClass>(MC.getSelectedToster().Pathing(EnemyHexes[hNo], true));
        hexmaxpath = new List<HexClass>();
        for (int i = 0; i < MC.getSelectedToster().MovmentSpeed; i++)
            hexmaxpath.Add(hexpath[i]);
     
        StartCoroutine(MC.DoMovesPath(hexmaxpath));
        return;


    }
}