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
        Debug.LogWarning("Left Attacks");
        beforeA();
        right.AttackMeS(left);

        afterA();

    }
    public void RightAttackLeft()
    {
        Debug.LogWarning("Right Attacks");
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
        while (Int32.Parse(LeftAmount.text) > 0 && Int32.Parse(RightAmount.text) > 0)
        {
            LeftAttackRight();
            if(Int32.Parse(LeftAmount.text) <= 0 || Int32.Parse(RightAmount.text) <= 0)
            {
                return;
            }
            RightAttackLeft();
        }
        
    }
}
