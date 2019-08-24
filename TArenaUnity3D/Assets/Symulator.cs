using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Symulator : MonoBehaviour
{

   public SimButtonCh.TosterStats Toster1;
   public SimButtonCh.TosterStats Toster2;
    public TosterHexUnit left;
    public TosterHexUnit right;
    public InputField LeftAmount;
    public InputField RightAmount;
    public Text LeftHP;
    public Text RightHP;
    public List<InputField> SpecialS;
    public List<Text> dmgtexts;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SetToster1(SimButtonCh.TosterStats s, string leftname)
    {
        Debug.LogError("left");
        left = new TosterHexUnit();
        left.InitateType(leftname);
        LeftAmount.text = left.Amount.ToString();
        left.ResetCounterAttack();
        LeftHP.text = left.TempHP.ToString();
    }
    public void SetToster2(SimButtonCh.TosterStats s, string rightname)
    {
        Debug.LogError("right");
        right = new TosterHexUnit();
        right.InitateType(rightname);
        RightAmount.text = right.Amount.ToString();
        right.ResetCounterAttack();


        RightHP.text = right.TempHP.ToString();
    }
    public void LeftAttackRight()
    {
        //Debug.LogWarning("Left Attacks");
        beforeA();
        right.AttackMeS(left);

        afterA();

    }
    public void RightAttackLeft()
    {
       // Debug.LogWarning("Right Attacks");
        beforeA();
        left.AttackMeS(right);

       
        afterA();

    }
    public void LeftAttackRightNoC()
    {
        Debug.LogWarning("Left Attacks");
        beforeA();
        right.DealMeDMGS(left);
        afterA();

    }
    public void RightAttackLeftNoC()
    {
        Debug.LogWarning("Right Attacks");
        beforeA();

        left.DealMeDMGS(right);
        afterA();

    }

    public void beforeA()
    {
        left.Amount = Int32.Parse("0" + LeftAmount.text);
        right.Amount = Int32.Parse("0" + RightAmount.text);
        SetSpecialS();
    }

    public void afterA()
    {
        if (left != null)
        {
            LeftAmount.text = left.Amount.ToString();
            left.ResetCounterAttack();
            LeftHP.text = left.TempHP.ToString();
        }
        if (right != null)
        {
            RightAmount.text = right.Amount.ToString();
            right.ResetCounterAttack();


            RightHP.text = right.TempHP.ToString();
        }
    }



    public void SetSpecialS()
    {
  
        left.SpecialHP = Int32.Parse(SpecialS[0].text + "0") / 10;
        left.SpecialAtt = Int32.Parse( SpecialS[1].text + "0") / 10;
        left.SpecialDef = Int32.Parse( SpecialS[2].text + "0") / 10;
        left.SpecialminDMG = Int32.Parse(SpecialS[3].text + "0") / 10;
        left.SpecialmaxDMG = Int32.Parse(SpecialS[4].text + "0") / 10;

        right.SpecialHP = Int32.Parse( SpecialS[5].text + "0") / 10;
        right.SpecialAtt = Int32.Parse( SpecialS[6].text + "0") / 10;
        right.SpecialDef = Int32.Parse( SpecialS[7].text + "0") / 10;
        right.SpecialminDMG = Int32.Parse(SpecialS[8].text + "0") / 10;
        
        right.SpecialmaxDMG = Int32.Parse(SpecialS[9].text+"0") / 10;

    }
    internal void SaveCost(int cost)
    {
        throw new NotImplementedException();
    }

    internal void SaveUnit(string name)
    {
        throw new NotImplementedException();
    }


    public void ResetB()
    {
        foreach (InputField input in SpecialS)
        {
            input.text = "0";
        }
    }

    public void setDMG1()
    {
        SpecialS[3].text = (-Int32.Parse(dmgtexts[0].text + "0") / 10 +1).ToString();
        SpecialS[4].text = (-Int32.Parse(dmgtexts[1].text + "0") / 10 + 1).ToString();
        SpecialS[8].text = (-Int32.Parse(dmgtexts[2].text + "0") / 10 + 1).ToString();
        SpecialS[9].text = (-Int32.Parse(dmgtexts[3].text + "0") / 10 + 1).ToString();
  
    }


   public void HealL()
    {
        left.HealMe(1000);
        LeftHP.text = left.TempHP.ToString();
    }
    public void HealR()
    {
        right.HealMe(1000);
        RightHP.text = right.TempHP.ToString();
    }

public void LoopTilEnd()
    {
        int turn = 1;
        while (Int32.Parse(LeftAmount.text) > 0 && Int32.Parse(RightAmount.text) > 0)
        {
         
            LeftAttackRight();
            if(Int32.Parse(LeftAmount.text) <= 0 || Int32.Parse(RightAmount.text) <= 0)
            {
                Debug.LogError("Turns " + turn);
                return;
            }
            RightAttackLeft();
            turn++;
        }
        Debug.LogError("Turns " + turn);
    }
    public void LoopTilEnd2()
    {
        int turn = 1;
        while (Int32.Parse(LeftAmount.text) > 0 && Int32.Parse(RightAmount.text) > 0)
        {
         
            RightAttackLeft();
            if (Int32.Parse(LeftAmount.text) <= 0 || Int32.Parse(RightAmount.text) <= 0)
            {
                Debug.LogError("Turns " + turn);
                return;
            }
            LeftAttackRight();
           
            turn++;
        }
        Debug.LogError("Turns " + turn);
    }
    public void ResetStats(int startleft, int startright)
    {

        RightAmount.text = startright.ToString();
        LeftAmount.text = startleft.ToString();
        left.Amount = Int32.Parse("0" + LeftAmount.text);
        right.Amount = Int32.Parse("0" + RightAmount.text);
        SetSpecialS();
        HealL();
        HealR();

    }
    public void FindBALANSforlowarmy()
    {
        left.Amount = Int32.Parse("0" + LeftAmount.text);
        right.Amount = Int32.Parse("0" + RightAmount.text);
        SetSpecialS();
        int StartLeftAmount = left.Amount; int StartRightAmount = right.Amount;
        bool LeftOK = false;
        bool RightOK = false;


        while (LeftOK == false && RightOK == false)
        {
            ResetStats(StartLeftAmount, StartRightAmount);
            TryFightLeftRight();
            if (left.Amount > right.Amount)
            {
                LeftOK = true;
            }
            ResetStats(StartLeftAmount, StartRightAmount);
            TryFightRightLeft();
            if (right.Amount > left.Amount)
            {
                RightOK = true;
            }
            if(LeftOK==false)
            {
                StartLeftAmount++;
                RightOK = false;
            }
            else if(RightOK ==false)
            {
                StartRightAmount++;
                LeftOK = false;
            }
            Debug.LogError("Left: " + StartLeftAmount);
            Debug.LogError("Right: " + StartRightAmount);
        }
        Debug.Log("Perfect Left: " + StartLeftAmount);
        Debug.Log("Perfect Right: " + StartRightAmount);

    }




    public void FindOPTIMALBALANSforlowarmy()
    {

        left.Amount = Int32.Parse("0" + LeftAmount.text);
        right.Amount = Int32.Parse("0" + RightAmount.text);
        SetSpecialS();
        double StartLeftAmount = left.Amount; double StartRightAmount = right.Amount;
        int Iter = 1;
        double ladv = 0, padv = 0;
        double p = 0.2;
        bool SzukajPrawego = false;
        bool SzukajWGore = false;
        Debug.Log(Convert.ToInt32(StartRightAmount));
        ResetStats(Convert.ToInt32(StartLeftAmount), Convert.ToInt32(StartRightAmount));
        TryFightLeftRight();
        if (left.Amount < right.Amount)
        {
            SzukajPrawego = true;
            SzukajWGore = false;
        }
        else
        {
            ladv = left.Amount / StartLeftAmount;
        }
        ResetStats(Convert.ToInt32(StartLeftAmount), Convert.ToInt32(StartRightAmount));
        TryFightRightLeft();
        if (left.Amount > right.Amount)
        {
            SzukajPrawego = true;
            SzukajWGore = true;
        }
        else
        {
            padv = right.Amount / StartRightAmount;
        }
        if (padv != 0 && ladv != 0) 
        if (ladv - padv > 0.10 && SzukajPrawego == false)
        {
            SzukajPrawego = true;
            SzukajWGore = true;
        }
        else if (ladv - padv > -0.10)
        {
            SzukajWGore = true; SzukajWGore = false;
        }
        else
        {
            SzukajPrawego = false;
        }
       // Debug.Log("padv " + padv);
      //  Debug.Log("ladv " + ladv);
        int i = 1;
        while (SzukajPrawego == true)
        {
            //    break;

          //  Debug.Log("Iter:" + Iter);
         
            if (Iter > 25) return;
            if (SzukajWGore == true)
            {
               // Debug.Log((1 + (i * p) / Iter));
                //Debug.Log(StartRightAmount * (1 + (i * p) / Iter));
                ResetStats(Convert.ToInt32(StartLeftAmount), Convert.ToInt32(StartRightAmount * (1 + (i * p) / Iter)));
                TryFightRightLeft();
                
                   /* if (left.Amount < right.Amount)
                {
                    i++;
                    continue;
                }*/
               
                    padv = Convert.ToDouble(right.Amount ) / Convert.ToInt32(StartRightAmount * (1 + (i * p) / Iter));
                    if (padv < 0.1)
                    {
                        i++;
                        continue;
                    }
                    else if (padv > 0.2)
                    {
                        Iter++;
                        SzukajWGore = false;
                        StartRightAmount = Convert.ToInt32(StartRightAmount * (1 + (i * p) / Iter));
                        i = 1;
                    }
                    else
                    {
                    StartRightAmount=Convert.ToInt32(StartRightAmount * (1 + (i * p) / Iter));
                 //   Debug.Log("Optymalna ilość: " + Convert.ToInt32(StartRightAmount * (1 + (i * p) / Iter)));
                        SzukajPrawego = false;
                    }
                
            }
            else if (SzukajWGore == false)
            {
             //   Debug.Log((1 - (i * p) / Iter));
              //  Debug.Log(StartRightAmount * (1 - (i * p) / Iter));
                ResetStats(Convert.ToInt32(StartLeftAmount), Convert.ToInt32(StartRightAmount * (1 - (i * p) / Iter)));
                TryFightRightLeft();
               /* if (left.Amount < right.Amount)
                {
                    i++;
                    continue;
                }*/
               
                    padv = Convert.ToDouble(right.Amount) / Convert.ToInt32(StartRightAmount * (1 - (i * p) / Iter));
                   // Debug.Log(padv);
                    if (padv < 0.05)
                    {
                //        Debug.Log(padv);
                        Iter++;
                        SzukajWGore = true;
                        StartRightAmount = Convert.ToInt32(StartRightAmount * (1 - (i * p) / Iter));
                        i = 1;
                     
                    }
                    else if (padv > 0.1)
                    {
                        Debug.Log(padv);
                        i++;
                        continue;
                    }
                    else
                {
                    StartRightAmount = Convert.ToInt32(StartRightAmount * (1 - (i * p) / Iter));
                //    Debug.Log("Optymalna ilość: " + Convert.ToInt32(StartRightAmount * (1 - (i * p) / Iter)));
                        SzukajPrawego = false;
                    }
                
            }

        }
        Debug.LogError("Iter:" + Iter);

        Debug.Log("Perfect Left: " + StartLeftAmount);
        Debug.Log("Perfect Right: " + StartRightAmount);




    }




    public int LookForMiddle(int left1, int middle1)
    {
        double Fadvantage = 0, Sadvantage = 0;
        ResetStats(left1, Convert.ToInt32(middle1*0.95));
        TryFightLeftRight();
        Fadvantage = right.Amount / Convert.ToInt32(middle1 * 0.95);
        ResetStats(left1, Convert.ToInt32(middle1 * 1.05));
        TryFightLeftRight();
        Sadvantage = right.Amount / Convert.ToInt32(middle1 * 1.05);
        if (Fadvantage > Sadvantage && Fadvantage > 0)
        {

        }
        if(Fadvantage<Sadvantage)
        {

        }
        Debug.LogError("ERROR");
        return 0;
    }


    public void FindBALANSformediumarmy()
    {
        left.Amount = Int32.Parse("0" + LeftAmount.text);
        right.Amount = Int32.Parse("0" + RightAmount.text);
        SetSpecialS();
        int StartLeftAmount = 10*left.Amount; int StartRightAmount = 10*right.Amount;
        bool LeftOK = false;
        bool RightOK = false;


        while (LeftOK == false && RightOK == false)
        {
            ResetStats(StartLeftAmount, StartRightAmount);
            TryFightLeftRight();
            if (left.Amount > right.Amount)
            {
                LeftOK = true;
            }
            ResetStats(StartLeftAmount, StartRightAmount);
            TryFightRightLeft();
            if (right.Amount > left.Amount)
            {
                RightOK = true;
            }
            if (LeftOK == false)
            {
                StartLeftAmount++;
                RightOK = false;
            }
            else if (RightOK == false)
            {
                StartRightAmount++;
                LeftOK = false;
            }
        }

    }

    public void FindBALANSforBigarmy()
    {
        left.Amount = Int32.Parse("0" + LeftAmount.text);
        right.Amount = Int32.Parse("0" + RightAmount.text);
        SetSpecialS();
        int StartLeftAmount = 100 * left.Amount; int StartRightAmount = 100 * right.Amount;
        bool LeftOK = false;
        bool RightOK = false;


        while (LeftOK == false && RightOK == false)
        {
            ResetStats(StartLeftAmount, StartRightAmount);
            TryFightLeftRight();
            if (left.Amount > right.Amount)
            {
                LeftOK = true;
            }
            ResetStats(StartLeftAmount, StartRightAmount);
            TryFightRightLeft();
            if (right.Amount > left.Amount)
            {
                RightOK = true;
            }
            if (LeftOK == false)
            {
                StartLeftAmount++;
                RightOK = false;
            }
            else if (RightOK == false)
            {
                StartRightAmount++;
                LeftOK = false;
            }
        }

    }




    public void TryFightLeftRight()
    {
        int turn = 1;
        while (Int32.Parse(LeftAmount.text) > 0 && Int32.Parse(RightAmount.text) > 0)
        {
            Debug.LogError("Turn " + turn);
            LeftAttackRight();
            if (Int32.Parse(LeftAmount.text) <= 0 || Int32.Parse(RightAmount.text) <= 0)
            {
                return;
            }
            RightAttackLeft();
            turn++;
        }
    }


    public void TryFightRightLeft()
    {
        int turn = 1;
        while (Int32.Parse(LeftAmount.text) > 0 && Int32.Parse(RightAmount.text) > 0)
        {
          //  Debug.LogError("Turn " + turn);
            RightAttackLeft();
            if (Int32.Parse(LeftAmount.text) <= 0 || Int32.Parse(RightAmount.text) <= 0)
            {
                return;
            }
            LeftAttackRight();
           
            turn++;
        }
    }

}
