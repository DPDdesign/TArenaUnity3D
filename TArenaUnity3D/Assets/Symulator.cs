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
    }
    public void SetToster2(SimButtonCh.TosterStats s, string rightname)
    {
        Debug.LogError("right");
        right = new TosterHexUnit();
        right.InitateType(rightname);
    }
    public void LeftAttackRight()
    {
        Debug.LogError(left.Amount);
        Debug.LogError(right.Amount);
        left.Amount = Int32.Parse("0"+LeftAmount.text);
        right.Amount = Int32.Parse("0"+RightAmount.text);
        right.AttackMeS(left);
        LeftAmount.text = left.Amount.ToString();
        RightAmount.text = right.Amount.ToString();
        right.ResetCounterAttack();
        left.ResetCounterAttack();
        LeftHP.text = left.TempHP.ToString();
       RightHP.text = right.TempHP.ToString();
    }
    public void RightAttackLeft()
    {
        left.Amount = Int32.Parse("0" + LeftAmount.text);
        right.Amount = Int32.Parse("0" + RightAmount.text);
        left.AttackMeS(right);
        LeftAmount.text = left.Amount.ToString();
        RightAmount.text = right.Amount.ToString();
        right.ResetCounterAttack();
        left.ResetCounterAttack();
        LeftHP.text = left.TempHP.ToString();
        RightHP.text = right.TempHP.ToString();

    }

    internal void SaveCost(int cost)
    {
        throw new NotImplementedException();
    }

    internal void SaveUnit(string name)
    {
        throw new NotImplementedException();
    }
}
