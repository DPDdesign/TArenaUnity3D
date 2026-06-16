using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CastManager : LocalNetworkBehaviour
{
    public MouseControler mouseControler;
  public  int cooldown = 0;
    TosterHexUnit tempToster;
    TosterHexUnit ST;
    public bool RangeSelectingenemy = false;
    public bool Rangeselectingfriend = false;
    public bool unselectaround = false;
    public bool RangeisAoE = false;
    public bool MeleeisAoE = false;
    public bool MeleeisAoEOnlyRadius = false; 
    public bool isInProgress = false;
    public bool SelfCast = false;
    public bool EndAfter = false;
    public bool Global = false;
    public bool SingleTarget = false;
    public bool isTurn = false;
    public bool canUseAfterMove = false;
    public bool canMoveAfterSkill = false;
    public int aoeradius = 0;
    public int MeleeisAoEbetweenRadiusInt = 0;
    public bool rush = false;
    public bool isAvailable = true;
    public bool SlashTarget = false;
    public bool isMove = false;
    [NonSerialized] public bool ActionInputBlockedByCommittedSkill = false;
    public List<GameObject> Projectiles;
    GameObject bullet;
    HexClass hexum;
    public HexClass tempHex;
    bool committedSpellStarted = false;
    void Start()
    {
        mouseControler = FindObjectOfType<MouseControler>();
    }
    public void startSpell(string spellID, HexClass hex)
    {
        hexum = hex;
        committedSpellStarted = true;

        Type type = this.GetType();
        MethodInfo method = type.GetMethod(spellID);
        method.Invoke(this, null);

    }
    public void getMode(string spellID, TosterHexUnit ST)
    {
        this.ST = ST;
        committedSpellStarted = false;
        cooldown = 1;
        canUseAfterMove = CanUseSkillAfterMove(spellID);
        canMoveAfterSkill = CanMoveAfterSkill(spellID);
        Type type = this.GetType();
        MethodInfo method = type.GetMethod(spellID + "M");
        method.Invoke(this, null);
    }

    public bool CanUseSkillAfterMove(string spellID)
    {
        DataMapper.SkillDefinition skillDefinition = DataMapper.Instance.FindSkill(spellID);
        return skillDefinition != null && skillDefinition.HasFlag("AM");
    }

    public bool CanMoveAfterSkill(string spellID)
    {
        DataMapper.SkillDefinition skillDefinition = DataMapper.Instance.FindSkill(spellID);
        return skillDefinition != null && skillDefinition.HasFlag("NI");
    }

    public void SetFalse()

    {
        SelectedT().TextToSend = "";
        SelectedT().TextToSend += SelectedT().Name + " użył skilla " + SelectedT().skillstrings[mouseControler.SelectedSpellid] + ".";
        Chat.chat.SendSkillUseMessage(SelectedT(), SelectedT().skillstrings[mouseControler.SelectedSpellid]);
        isMove = false;
        tempHex = null; 
         RangeSelectingenemy = false;
        Rangeselectingfriend = false;
        unselectaround = false;
        RangeisAoE = false;
        MeleeisAoE = false;
        MeleeisAoEOnlyRadius = false;
        SelfCast = false;
        aoeradius = 0;
        Global = false;
        SingleTarget = false;
        isTurn = false;
        MouseControler.SkillState = false;
        tempToster = null;
        cooldown = 0;
        canUseAfterMove = false;
        canMoveAfterSkill = false;
        rush = false;
        isAvailable = true;
        isInProgress = false;
        SlashTarget = false;
        ActionInputBlockedByCommittedSkill = false;
        if (committedSpellStarted && mouseControler != null)
        {
            committedSpellStarted = false;
            mouseControler.CompleteSelectedSkillLocally(SelectedT());
        }
    }
    public HexClass getHexUM()
    {
        return hexum;
    }
    public TosterHexUnit SelectedT()
    {
        return ST;
    }

    void PlaySequencedCasterEffect(string skillId)
    {
        SkillPresentationManager.PlaySequencedCasterEffect(skillId, SelectedT(), GetSelectedSkillAnimationState());
    }

    void PlaySequencedHexEffect(string skillId, HexClass targetHex)
    {
        SkillPresentationManager.PlaySequencedHexEffect(skillId, SelectedT(), targetHex, GetSelectedSkillAnimationState());
    }

    void PlaySequencedHexEffectWithResults(string skillId, HexClass targetHex, List<FrontendResultReveal> reveals)
    {
        SkillPresentationManager.PlaySequencedHexEffectWithReveals(skillId, SelectedT(), targetHex, reveals, GetSelectedSkillAnimationState());
    }

    void PlaySequencedUnitEffect(string skillId, TosterHexUnit target)
    {
        SkillPresentationManager.PlaySequencedUnitEffect(skillId, SelectedT(), target, GetSelectedSkillAnimationState());
    }

    void PlaySequencedResults(string skillId, List<FrontendResultReveal> reveals)
    {
        SkillPresentationManager.PlaySequencedInstantHits(skillId, SelectedT(), reveals, GetSelectedSkillAnimationState());
    }

    void PlaySequencedProjectilesToUnits(string skillId, List<FrontendResultReveal> reveals)
    {
        SkillPresentationManager.PlaySequencedProjectileHitsToUnits(skillId, SelectedT(), reveals, GetSelectedSkillAnimationState());
    }

    void PlaySequencedProjectileHexImpactThenResults(string skillId, HexClass targetHex, List<FrontendResultReveal> reveals, Action afterImpact)
    {
        SkillPresentationManager.PlaySequencedProjectileHexImpactThenReveals(
            skillId,
            SelectedT(),
            targetHex,
            reveals,
            GetSelectedSkillAnimationState(),
            afterImpact);
    }

    void PlaySequencedHexCastUnitImpactThenResults(
        string skillId,
        HexClass castHex,
        TosterHexUnit impactUnit,
        List<FrontendResultReveal> reveals,
        Action afterImpact)
    {
        SkillPresentationManager.PlaySequencedHexCastUnitImpactThenReveals(
            skillId,
            SelectedT(),
            castHex,
            impactUnit,
            reveals,
            GetSelectedSkillAnimationState(),
            afterImpact);
    }

    List<FrontendResultReveal> SingleReveal(FrontendResultReveal reveal)
    {
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        if (reveal != null)
        {
            reveals.Add(reveal);
        }

        return reveals;
    }

    void AddReveal(List<FrontendResultReveal> reveals, FrontendResultReveal reveal)
    {
        if (reveal != null)
        {
            reveals.Add(reveal);
        }
    }

    void SetUnitVisualVisibility(TosterHexUnit unit, bool visible)
    {
        if (unit == null || unit.tosterView == null)
        {
            return;
        }

        Renderer[] renderers = unit.tosterView.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }
    }

    FrontendResultReveal BuildStatusReveal(TosterHexUnit target)
    {
        if (target == null)
        {
            return null;
        }

        return target.BuildStatusFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill);
    }

    FrontendResultReveal BuildHealReveal(TosterHexUnit target)
    {
        if (target == null)
        {
            return null;
        }

        return target.BuildHealFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill);
    }

    IEnumerator MoveAndPlayHexEffect(HexClass moveHex, TosterHexUnit mover, string skillId, HexClass impactHex)
    {
        if (mouseControler != null && moveHex != null && mover != null)
        {
            yield return StartCoroutine(mouseControler.DoMoves(moveHex, mover));
        }

        PlaySequencedHexEffect(skillId, impactHex);
        SetFalse();
    }

    IEnumerator MoveWithoutMovedAndPlayResults(HexClass moveHex, TosterHexUnit mover, string skillId, List<FrontendResultReveal> reveals)
    {
        if (mouseControler != null && moveHex != null && mover != null)
        {
            yield return StartCoroutine(mouseControler.DoMovesWithoutMoved(moveHex, mover));
        }

        PlaySequencedResults(skillId, reveals);
        SetFalse();
    }

    IEnumerator MoveAndPlayResults(HexClass moveHex, TosterHexUnit mover, string skillId, List<FrontendResultReveal> reveals)
    {
        if (mouseControler != null && moveHex != null && mover != null)
        {
            yield return StartCoroutine(mouseControler.DoMoves(moveHex, mover));
        }

        PlaySequencedResults(skillId, reveals);
        SetFalse();
    }

    IEnumerator MoveAttackAndPlayHexEffect(HexClass moveHex, TosterHexUnit target, TosterHexUnit mover, string skillId, HexClass impactHex)
    {
        if (mouseControler != null && moveHex != null && mover != null)
        {
            yield return StartCoroutine(mouseControler.DoMoveAndAttackWithoutCheck(moveHex, target, mover));
        }

        PlaySequencedHexEffect(skillId, impactHex);
        SetFalse();
    }

    void StartCommittedSkillCoroutine(IEnumerator routine)
    {
        ActionInputBlockedByCommittedSkill = true;
        StartCoroutine(routine);
    }

    IEnumerator RushMoveAttackAndPlayHexEffect(HexClass moveHex, TosterHexUnit target, TosterHexUnit mover, string skillId, HexClass impactHex)
    {
        SkillPresentationManager.PlayCastSfxOnly(skillId);

        if (mouseControler != null && moveHex != null && mover != null)
        {
            mover.SetMovementAnimationOverride("run");
            try
            {
                yield return StartCoroutine(mouseControler.DoMoves(moveHex, mover));
            }
            finally
            {
                mover.ClearMovementAnimationOverride();
            }
        }

        SkillPresentationManager.PlaySequencedHexEffectWithoutCastSfx(skillId, mover, impactHex, GetSelectedSkillAnimationState());

        if (target != null && mover != null && mover.Hex == moveHex)
        {
            yield return StartCoroutine(target.AttackMeSequence(mover));
        }

        SetFalse();
        if (mouseControler != null && mover != null)
        {
            mouseControler.TryCompleteSkillAction(mover);
        }
    }

    string GetSelectedSkillAnimationState()
    {
        if (mouseControler == null)
        {
            return null;
        }

        return "skill" + (mouseControler.SelectedSpellid + 1);
    }


    public int[] getHexUnderMouseCoordinates()
    {
        int x = getHexUM().C;
        int y = getHexUM().R;
        int[] coords = { x, y };
        return coords;
    }

    public HexClass getHexNextToMouse(int x, int y)
    {

        int[] test = getHexUnderMouseCoordinates();
        return getHexUM().hexMap.GetHexAt(test[0] + x, test[1] + y);

    }

    // SelectedT().Team.HexesUnderTeam.Contains(getHexUT())
    // getSelectedToster().Hex
    // getHexUnderMouse()

    #region Barbarian skills:
    #region Rusher skills (T1)
    #region Skill 1 - Chope

    public void Chope() // kręci się dookoła i zadaje 40% obrażen wszystkim jednostkom, traci kontratak
    {
        List<HexClass> hexarea = new List<HexClass>(SelectedT().Hex.hexMap.GetHexesWithinRadiusOf(SelectedT().Hex, aoeradius));
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        foreach (HexClass t in hexarea)
        {
         
            if (t != null)
            {
                if (t.Tosters.Count > 0)
                {
                    
                    if (t.Tosters.Count > 0 && !t.Tosters.Contains(SelectedT()))
                    {
                       
                        TosterHexUnit target = t.Tosters[0];
                        AddReveal(reveals, target.DealMeDMGForFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill));
                        
                    }

                }
            }
            else { Debug.Log("No Tosters Hit"); }
        }
        SelectedT().SpecialDMGModificator = 0;
 
        PlaySequencedResults("Chope", reveals);
        SetFalse();
    }
    public void ChopeM()
    {
        isTurn = true;
        unselectaround = true;
        aoeradius = 1;
        MeleeisAoE = true;
    }

    #endregion
    #region Skill 2 - Rush
    public void Rush() //Biegnie przed siebie atakuje pierwszego napotkanego przeciwnika ??? +1 do ataku, traci 8 % jednostek, nie może zostać użyte poniżej 30.
    {

        Debug.Log("this");
        if (getHexUM() != null && getHexUM().Highlight == true)
        {
            
            rrush(getHexUM().C, getHexUM().R);
        }

    }

    [PunRPC]
    public void rrush(int i, int j)
    {

        getHexUM().hexMap.unHighlight(5, 5, 20);
        if (getHexUM().Tosters.Count > 0)
        {

            SelectedT().AddNewTimeSpell(1, null, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Rush", true);
            HexClass temp;

            if (SelectedT().teamN == true)
            {
                temp = getHexUM().hexMap.GetHexAt(getHexUM().C - 1, getHexUM().R);
            }
            else
            {


                temp = getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R);

            }
            StartCommittedSkillCoroutine(RushMoveAttackAndPlayHexEffect(temp, getHexUM().Tosters[0], SelectedT(), "Rush", getHexUM()));
        }
        else
        {

            StartCommittedSkillCoroutine(RushMoveAttackAndPlayHexEffect(getHexUM(), null, SelectedT(), "Rush", getHexUM()));
        }
    }
    public void RushM()
    {
        isTurn = true;
        unselectaround = true;
        rush = true;
    }

    #endregion
    #endregion
    #region Thrower skills (T2)
    #region Range_Stance //TODO: CIELU
    public void Range_Stance_Barb()
    {

    }
    public void Range_Stance_BarbM()
    {
        PlaySequencedCasterEffect("Range_Stance_Barb");
        if (SelectedT().isRange = !SelectedT().isRange)
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 20;
            SelectedT().SpecialResistance = 20;
     //       SelectedT().skillstrings[1] = "";
            SelectedT().skillstrings[mouseControler.SelectedSpellid] = "Melee_Stance_Barb";
        }
        else
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 0;
            SelectedT().SpecialResistance = 0;
        }
        isTurn = false;
        SetFalse();


    }
    public void Melee_Stance_Barb()
    {

    }
    public void Melee_Stance_BarbM()
    {
        PlaySequencedCasterEffect("Melee_Stance_Barb");
        if (SelectedT().isRange = !SelectedT().isRange)
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 20;
            SelectedT().SpecialResistance = 20;
        }
        else
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 0;
            SelectedT().SpecialResistance = 0;
            SelectedT().skillstrings[mouseControler.SelectedSpellid] = "Range_Stance_Barb";
        }
        isTurn = false;
        SetFalse();
       
    }
    #endregion
    #region Double_Throw 
    TosterHexUnit[] doubleThrowTargets = new TosterHexUnit[2];
    short doubleThrowTargetCounter = 0;

    public void Double_Throw()
    {
        if (doubleThrowTargetCounter < 2 && getHexUM() != SelectedT().Hex && !SelectedT().Team.HexesUnderTeam.Contains(getHexUM()) && getHexUM().Tosters.Count > 0)
        {
            isInProgress = true;
            doubleThrowTargets[doubleThrowTargetCounter] = getHexUM().Tosters[0];
            Debug.Log("Wybrałem: " + doubleThrowTargets[doubleThrowTargetCounter].Name);
            doubleThrowTargetCounter++;
        }


        if (doubleThrowTargetCounter == 2)
        {
            SelectedT().SpecialDMGModificator += 60;
            List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
            AddReveal(reveals, doubleThrowTargets[0].ShootMEForFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill));
            AddReveal(reveals, doubleThrowTargets[1].ShootMEForFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill));
            PlaySequencedProjectilesToUnits("Double_Throw", reveals);
            SelectedT().SpecialDMGModificator -= 60;
            doubleThrowTargetCounter = 0;
            SetFalse();
        }

       
    }

    public void Double_ThrowM()
    {
        isTurn = true;
        unselectaround = true;
        RangeSelectingenemy = true;

        if (false)
        {
            isTurn = false;
            SetFalse();
            Chat.chat.SendMessageToChat("Nie jesteś w trybie Range", Msg.MessageType.Info);
        }

        isTurn = true;
        unselectaround = true;
        RangeSelectingenemy = true;
    }

  
    #endregion

    #region Axe_Rain 
    public void Axe_Rain()
    {
        Debug.LogError("działam");
        List<HexClass> hexarea = new List<HexClass>(getHexUM().hexMap.GetHexesWithinRadiusOf(getHexUM(), aoeradius));
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();

        foreach (HexClass t in hexarea)
        {
            if (t != SelectedT().Hex && t!=null)
            {
                if (t == getHexUM() && t.Tosters.Count>0)
                {
                    SelectedT().SpecialDMGModificator = 0;
                    AddReveal(reveals, t.Tosters[0].DealMeDMGForFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill));
                    SelectedT().SpecialDMGModificator = 0;
                }
                else
                if (t != null)
                    if (t.Tosters.Count > 0)
                    {
                        SelectedT().SpecialDMGModificator = 50;
                        AddReveal(reveals, t.Tosters[0].DealMeDMGForFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill));
                        SelectedT().SpecialDMGModificator = 0;
                    }
            }
        }
        PlaySequencedProjectilesToUnits("Axe_Rain", reveals);
        mouseControler.SetCD(SelectedT());
        SetFalse();
    }
    public void Axe_RainM()
    {

        unselectaround = true;
        aoeradius = 1;
        cooldown = 2;
        RangeisAoE = true;
        isTurn = true;
    }
    #endregion
    #endregion
    #region Axeman (T3)
    #region Slash 

    public void Slash()
    {
        if (getHexUM().Highlight == true && SlashTarget == true&&hexum!=tempHex)
        {
            isTurn = true;
          
            slash();
          
        }
        if (isMove == true&& hexum.Highlight && SelectedT().IsPathAvaible(hexum) && (hexum.Tosters.Count==0 || hexum.Tosters.Contains(SelectedT())))
        {
          
            tempHex = getHexUM();
            SlashTarget = true;
            isMove = false;
            unselectaround = true;
            SelectedT().Hex.hexMap.unHighlight(SelectedT().Hex.C, SelectedT().Hex.R, SelectedT().GetMS());
        }

      
    }
    [PunRPC]

    public void slash()
    {

        StartCommittedSkillCoroutine(SlashApproachAndCast());
    }

    IEnumerator SlashApproachAndCast()
    {
        HexClass[] hexarray = getHexUM().hexMap.GetHexesWithinRadiusOf(getHexUM(), 1);
        List<TosterHexUnit> targets = new List<TosterHexUnit>();


        Debug.LogError(SelectedT().Name);


        foreach (HexClass h in hexarray)
        {

            if (h!=null && h.Highlight == true)
            {
                if (h.Tosters.Count > 0 && h.Tosters[0] != SelectedT())
                {
                    targets.Add(h.Tosters[0]);
                }
            }
        }

        yield return StartCoroutine(mouseControler.DoMoves(tempHex, SelectedT()));

        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        foreach (TosterHexUnit target in targets)
        {
            if (target == null || target.isDead)
            {
                continue;
            }

            SelectedT().SpecialDMGModificator = 60;
            AddReveal(reveals, target.DealMeDMGForFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill));
            SelectedT().SpecialDMGModificator = 0;
        }

        PlaySequencedResults("Slash", reveals);
        SetFalse();
    }
    public void SlashM()
    {

        cooldown = 2;
        isMove = true;
        
       // SlashTarget = true;
       // isTurn = true;

    }
    #endregion
    #region Hate 

    public void Hate() 
    {
        if (getHexUM().Highlight==true)
        {/*
            SelectedT().AddNewTimeSpell(2, getHexUM().Tosters[0], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0, "Hate", false);
            getHexUM().Tosters[0].AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0, "Hate", false);
            mouseControler.SetCD();
            SetFalse();
            */
            hate();
        }
    }

    [PunRPC]

    public void hate()
    {
        SelectedT().AddNewTimeSpell(2, getHexUM().Tosters[0], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Hate", false);
        getHexUM().Tosters[0].AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Hate", false);
        PlaySequencedResults("Hate", SingleReveal(BuildStatusReveal(getHexUM().Tosters[0])));
        Chat.chat.SendTargetedSkillMessage(SelectedT(), "Hate", getHexUM().Tosters[0]);
        mouseControler.SetCD(SelectedT());
        SetFalse();
    }

    public void HateM()
    {
        isTurn = false;
        cooldown = 2;
        unselectaround = true;
        RangeSelectingenemy = true;
    }
    #endregion
    #region Cold_Blood

    public void Cold_Blood() //PASSIVE = AUTOCAST
    {

    }
    public void Cold_BloodM()
    {
        Debug.Log("Ta umiejętność jest pasywna");
        SetFalse();
    }
    #endregion
    #endregion
    #region Heavy Hitter (T4)
    #region Insult

    public void Insult()
    {
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        if (SelectedT().Team == getHexUM().hexMap.Teams[0])
        {
            foreach (TosterHexUnit tost in getHexUM().hexMap.Teams[1].Tosters)
            {
                
                tost.AddNewTimeSpell(2, tost, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0,0, "Insult", false);
                AddReveal(reveals, BuildStatusReveal(tost));
            }
        }
        else
        {
            foreach (TosterHexUnit tost in getHexUM().hexMap.Teams[0].Tosters)
            {

                tost.AddNewTimeSpell(2, tost, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0,0, "Insult", false);
                AddReveal(reveals, BuildStatusReveal(tost));
            }
        }
        PlaySequencedResults("Insult", reveals);
        mouseControler.SetCD(SelectedT());
        SetFalse();
    }
    public void InsultM()
    {
        cooldown = 4;
        isTurn = false;
        unselectaround = true;
        RangeSelectingenemy = true;
    }
    #endregion
    #region Rage  //Cielu

    public void Rage()
    {
        if (getHexUM() == SelectedT().Hex)
        {
            SelectedT().AddNewTimeSpell(2, SelectedT(), 0, SelectedT().GetDef()/2, -SelectedT().GetDef(), 0, 0, 0, 0, 0, 0, 0, 0, 0, "Rage", false);
            PlaySequencedResults("Rage", SingleReveal(BuildStatusReveal(SelectedT())));
            mouseControler.SetCD(SelectedT());
            SetFalse();
        }
    }
    public void RageM()
    {
        cooldown = 3;
        isTurn = false;
        unselectaround = true;
        SelfCast = true;
    }
    #endregion
    #region Massochism 

    public void Massochism() //PASSIVE = AUTOCAST
    {
    //  SelectedT().AddNewTimeSpell(1, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Massochism", false);
    }
    public void MassochismM()
    {
        Debug.Log("Ta umiejętność jest pasywna");
        SetFalse();
    }
    #endregion
    #endregion
    #endregion
    #region Lizards skills:
    #region Trapper  skills (T1) Trapy...
    #region Skill 1 - Range_Stance

    public void Range_Stance_Lizard() 
    {
     
    }
    public void Range_Stance_LizardM()
    {
        PlaySequencedCasterEffect("Range_Stance_Lizard");
        if (SelectedT().isRange = !SelectedT().isRange)
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 20;
            SelectedT().SpecialResistance = 20;
            SelectedT().skillstrings[mouseControler.SelectedSpellid] = "Melee_Stance_Lizard";
        }
        else
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 0;
            SelectedT().SpecialResistance = 0;
        }
        isTurn = false;
        SetFalse();
    }
    public void Melee_Stance_Lizard()
    {

    }
    public void Melee_Stance_LizardM()
    {
        PlaySequencedCasterEffect("Melee_Stance_Lizard");
        if (SelectedT().isRange = !SelectedT().isRange)
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 20;
            SelectedT().SpecialResistance = 20;
        }
        else
        {
            Debug.Log(SelectedT().isRange);
            SelectedT().SpecialDMGModificator = 0;
            SelectedT().SpecialResistance = 0;
            SelectedT().skillstrings[mouseControler.SelectedSpellid] = "Range_Stance_Lizard";
        }
        isTurn = false;
        SetFalse();

    }
    #endregion
    #region Skill 2 - Spike_trap
    public void Spike_Trap() 
    {
        if (getHexUM()!=null)
        {
            getHexUM().AddTrap("Spike_Trap",999, SelectedT());
            PlaySequencedHexEffect("Spike_Trap", getHexUM());
            mouseControler.SetCD(SelectedT());
            SetFalse();
            
        }


    }
    public void Spike_TrapM()
    {
        unselectaround = true;
        Global = true;
        SingleTarget = true;
        isTurn = true;
        cooldown = 3;
    }

    #endregion
    #region Skill 3 - Rope_trap
    public void Rope_Trap() 
    {

        if (getHexUM() != null)
        {
            getHexUM().AddTrap("Rope_Trap",999, SelectedT());
            PlaySequencedHexEffect("Rope_Trap", getHexUM());
            mouseControler.SetCD(SelectedT());
            SetFalse();

        }
    }
    public void Rope_TrapM()
    {
        unselectaround = true;
        Global = true;
        SingleTarget = true;
        isTurn = true;
        cooldown = 2;
    }

    #endregion
    #endregion
    #region Healer  skills (T2)
    #region Tough_Skin 
    public void Tough_Skin()
    {
        if(getHexUM().Highlight==true)
        {
            tough_Skin();
        }
        
    }

    [PunRPC]

    public void tough_Skin()
    {
        TosterHexUnit target = getHexUM().Tosters[0];
        if (getHexUM().Tosters[0].Name == "Tank" || getHexUM().Tosters[0].Name == "Healer" || getHexUM().Tosters[0].Name == "Specialist" || getHexUM().Tosters[0].Name == "Trapper")
        {
            target.AddNewTimeSpell(2, target, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25, "Tough_Skin", false);
        }
        else
        target.AddNewTimeSpell(2, target, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, "Tough_Skin", false);

        PlaySequencedResults("Tough_Skin", SingleReveal(BuildStatusReveal(target)));

        Chat.chat.SendTargetedSkillMessage(SelectedT(), "Tough_Skin", getHexUM().Tosters[0]);

        mouseControler.SetCD(SelectedT());
        SetFalse();
    }
    public void Tough_SkinM()
    {
            cooldown = 2;
            unselectaround = true;
            Rangeselectingfriend = true;
            SelfCast = true;
        isTurn = true;
    }
    #endregion
    #region Defence_Ritual 
    public void Defence_Ritual()
    {
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        if (SelectedT().Team == getHexUM().hexMap.Teams[0])
        {
            foreach (TosterHexUnit tost in getHexUM().hexMap.Teams[0].Tosters)
            {
                if (tost.Name == "Tank" || tost.Name == "Healer" || tost.Name == "Specialist" || tost.Name == "Trapper")
                {
                    tost.AddNewTimeSpell(2, tost, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Defence_Ritual", false);
                }
                else
                    tost.AddNewTimeSpell(2, tost, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Defence_Ritual", false);
                AddReveal(reveals, BuildStatusReveal(tost));
            }
        }
        else
        {
            foreach (TosterHexUnit tost in getHexUM().hexMap.Teams[1].Tosters)
            {

                if (tost.Name == "Tank" || tost.Name == "Healer" || tost.Name == "Specialist" || tost.Name == "Trapper")
                {
                    tost.AddNewTimeSpell(2, tost, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Defence_Ritual", false);
                }
                else
                    tost.AddNewTimeSpell(2, tost, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Defence_Ritual", false);
                AddReveal(reveals, BuildStatusReveal(tost));
            }
        }
        PlaySequencedResults("Defence_Ritual", reveals);
        mouseControler.SetCD(SelectedT());
        SetFalse();
    }
    public void Defence_RitualM()
    {
        cooldown = 3;
        unselectaround = true;
        Rangeselectingfriend = true;
        SelfCast = true;
        isTurn = true;
    }

    #endregion

    #region Cleanse  //TODO: 
    public void Cleanse()
    {
        //List<string> SpellsToRemove = new List<string>(new string[] { "Slow", "Insult" });
        if (getHexUM().Tosters.Count > 0 && getHexUM().Highlight == true)
        {
            photonView.RPC("cleanse", RpcTarget.All, new object[] { });
        }
    }

    [PunRPC]

    public void cleanse()
    {
        List<string> SpellsToRemove = new List<string>(new string[] { "Slow", "Insult" });
        foreach (string s in SpellsToRemove)
        {
            TimeSpells.SpellOverTime spell = getHexUM().Tosters[0].AskForSpell(s);

            if (spell != null)
            {
                Debug.Log(spell.nameofspell);
                getHexUM().Tosters[0].SetOver(spell);
                SelectedT().AddNewTimeSpell(spell);


            }
        }
    }
    public void CleanseM()
    {


        unselectaround = true;
        Rangeselectingfriend = true;
        isTurn = true;
        /*
         *
         * 
         *  może być różnie, narazie dla Ciela
         * 
         */
    }
    #endregion
    #endregion
    #region Specialist  (T3)
    #region Force_Pull  //Cielu





    public void Force_Pull()
    {
        if (getHexUM().Tosters.Count > 0 && MeleeisAoE == false)
        {
            tempToster = getHexUM().Tosters[0];


            SingleTarget = true;
            RangeSelectingenemy = false;
            Rangeselectingfriend = false;
            MeleeisAoE = true;
            aoeradius = 2;
            mouseControler.CastSkillOnlyBooleans(SelectedT());
        }
        else
     if (MeleeisAoE == true && SingleTarget == true && tempToster != null)
        {
            if (getHexUM() != tempToster.Hex && getHexUM().Tosters.Count == 0)
            {

                HexClass hextomove = getHexUM();
                if (hextomove.Highlight == true)
                {
                    TosterHexUnit pulledToster = tempToster;
                    PlaySequencedHexCastUnitImpactThenResults(
                        "Force_Pull",
                        hextomove,
                        pulledToster,
                        SingleReveal(pulledToster.BuildStatusFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill)),
                        () => pulledToster.TeleportToHex(hextomove));

                    SetFalse();
                }
            }
        }
    }
    public void Force_PullM()
    {
        unselectaround = true;
     
        Rangeselectingfriend = true;
        isTurn = true;
        tempToster = new TosterHexUnit();
    }
    #endregion
    #region Stone_Stance 

    public void Stone_Stance()
    {
        SelectedT().AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, -SelectedT().CounterAttacks, 0,100, "Stone_Stance", false);
        SelectedT().CounterAttackAvaible = false;
        PlaySequencedResults("Stone_Stance", SingleReveal(BuildStatusReveal(SelectedT())));
        mouseControler.SetCD(SelectedT());
        SetFalse();

    }
    public void Stone_StanceM()
    {
        cooldown = 5;
        isTurn = true;
        SelfCast = true;
        unselectaround = true;
    }
    #endregion
    #region Brak_Weny //Lesisz

    public void Brak_Weny()
    {

    }
    public void Brak_WenyM()
    {

    }
    #endregion
    #endregion
    #region Tank  (T4)
    #region Toxic_Fume  

    public void Toxic_Fume()
    {


        SelectedT().AddNewTimeSpell(2, SelectedT(), 0, 0, 0, -SelectedT().GetMS(), 0, 0, 0, 0, 0, 2, 0, 0, "Toxic_Fume", false);
        List<HexClass> hexarea = new List<HexClass>(SelectedT().Hex.hexMap.GetHexesWithinRadiusOf(hexum, aoeradius));
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        foreach (HexClass t in hexarea)
        {

            if (t != null)
            {
                if (t.Tosters.Count > 0)
                {

                    if (t.Tosters.Count > 0 && !t.Tosters.Contains(SelectedT()))
                    {
                        TosterHexUnit target = t.Tosters[0];
                        target.AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Taunt", false);
                        AddReveal(reveals, BuildStatusReveal(target));
                    }

                }
            }
            else { Debug.Log("No Tosters Hit"); }
        }
        StartCommittedSkillCoroutine(MoveWithoutMovedAndPlayResults(hexum, SelectedT(), "Toxic_Fume", reveals));
        mouseControler.SetCD(SelectedT());

        //    SetFalse();
    }
    public void Toxic_FumeM()
    {
        isMove = true;
        isTurn = true;
        cooldown = 2;
        aoeradius = 1;
    }

    

    #endregion
    #region Shapeshift   //Cielu

    public void Shapeshift()
    {
        int temp = SelectedT().MovmentSpeed;
        SelectedT().MovmentSpeed = SelectedT().Initiative;
        SelectedT().Initiative = temp;
        PlaySequencedCasterEffect("Shapeshift");
        SetFalse();

    }
    public void ShapeshiftM()
    {
        SelfCast = true;
        isTurn = false;
        unselectaround = true;
    }
    #endregion
    #region Long_Lick

    public void Long_Lick()
    {

        if (getHexUM().Tosters.Count > 0 && getHexUM().Highlight == true && !getHexUM().Tosters.Contains(SelectedT()) && getHexUM().Tosters[0].Team != SelectedT().Team)
        {
            Debug.Log("Tsoter");
            hexum = getHexUM();
            
            long_Lick();

        }

    }
    [PunRPC]

    public void long_Lick()
    {

      /*  TosterHexUnit t = SelectedT();

                if (hexum.hexMap.GetHexAt((SelectedT().Hex.C + hexum.C) / 2, (SelectedT().Hex.R + hexum.R) / 2).Tosters.Count == 0)
                {
            hexum.Tosters[0].AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Taunt", false);
            hexum.Tosters[0].SetHex(getHexUM().hexMap.GetHexAt((SelectedT().Hex.C + getHexUM().C) / 2, (SelectedT().Hex.R + getHexUM().R) / 2));
                    Animator d = SelectedT().tosterView.GetComponentInChildren<Animator>();
                    if (d != null)
                    {
                        // Debug.Log(mouseControler.SelectedSpellid-1);
                        d.Play("skill" + (mouseControler.SelectedSpellid + 1));

                    }
                    SetFalse();
                    return;
                }
        Debug.Log("Tsoter");*/
        HexClass[] hexes = hexum.hexMap.GetHexesWithinRadiusOf(SelectedT().Hex, 1);

        foreach (HexClass h in hexes)
        {

            if (h != null && h.Tosters.Count == 0)
            {
                TosterHexUnit target = hexum.Tosters[0];
                target.AddNewTimeSpell(2, SelectedT(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Taunt", false);
                PlaySequencedHexCastUnitImpactThenResults(
                    "Long_Lick",
                    h,
                    target,
                    SingleReveal(BuildStatusReveal(target)),
                    () => target.TeleportToHex(h));
                SetFalse();
                return;
            }

        }
        Debug.Log("All Hexes full!!");
        SetFalse();
    }


    public void Long_LickM()
    {
        unselectaround = true;
        MeleeisAoEOnlyRadius = true;
        aoeradius = 3;
        isTurn = true;

    }
    #endregion
    #endregion

    #endregion



    #region Mage/Golems skills:
    #region Wisp  skills (T1) 
    #region Skill 1 - Blind_by_light    

    public void Blind_by_light()
    {
        /*
        int myhp = (SelectedT().Amount - 1) * SelectedT().GetHP() + SelectedT().TempHP; 

        foreach ( TosterHexUnit toster in SelectedT().Hex.hexMap.Teams[0].Tosters)
        {
            int targethp = (toster.Amount - 1) * toster.GetHP() + toster.TempHP;
            if (myhp > targethp)
            { 
                if(toster!=SelectedT())
                toster.AddNewTimeSpell(2, toster, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 , "Blind", false);
            }
        }
        foreach (TosterHexUnit toster in SelectedT().Hex.hexMap.Teams[1].Tosters)
        {
            int targethp = (toster.Amount - 1) * toster.GetHP() + toster.TempHP;
            if (myhp > targethp)
            {
                if (toster != SelectedT())
                    toster.AddNewTimeSpell(2, toster, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Blind", false);
            }
        }
        */
        if (hexum!=null && hexum.Tosters.Count>0)
        {
            hexum.Tosters[0].AddNewTimeSpell(2, hexum.Tosters[0], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Blind", false);
            PlaySequencedResults("Blind_by_light", SingleReveal(BuildStatusReveal(hexum.Tosters[0])));
            mouseControler.SetCD(SelectedT());
            SetFalse();

        }




    }
    public void Blind_by_lightM()
    {
        isTurn = true;
        cooldown =3;
        unselectaround = true;
        RangeSelectingenemy = true;

    }

    #endregion
    #region Skill 2 - Unstoppable_Light
    public void Unstoppable_Light()
    {
    


    } //Passive
    public void Unstoppable_LightM()
    {
        Debug.Log("Ta umiejętność jest pasywna");
        SetFalse();
    }

    #endregion

    #endregion
    #region StoneGolem  skills (T2)
    #region Stone_Throw 
    public void Stone_Throw()
    {
        if (getHexUM().Tosters.Count > 0)
        {
            int newamount = SelectedT().Amount / 2;
          
            HexClass[] hexes = getHexUM().hexMap.GetHexesWithinRadiusOf(getHexUM(), 1);
            foreach (HexClass h in hexes)
            {

                if (h != null && h.Tosters.Count == 0 && h!=getHexUM())
                {

                    TosterHexUnit newunit = SelectedT().Team.AddNewUnit(SelectedT().Name, newamount);
                    List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
                    SelectedT().Amount = SelectedT().Amount - newamount;
                    SelectedT().SetTextAmount();
                    AddReveal(reveals, getHexUM().Tosters[0].DealMeDMGDefForFrontendReveal(10, SelectedT(),true, FrontendResultRevealSource.Skill));
                    newunit.teamN = SelectedT().teamN;
                    newunit.SetTosterPrefab(getHexUM().hexMap);
                    newunit.SetTextAmount();
                    getHexUM().hexMap.GenerateToster(h.C, h.R, newunit);
                    SetUnitVisualVisibility(newunit, false);
                    AddReveal(reveals, newunit.DealMeDMGDefForFrontendReveal(12, SelectedT(),true, FrontendResultRevealSource.Skill));
                    newunit.skillstrings.Remove("Stone_Throw");
                    newunit.Moved = true;
                    PlaySequencedProjectileHexImpactThenResults("Stone_Throw", getHexUM(), reveals, () => SetUnitVisualVisibility(newunit, true));
                    mouseControler.SetCD(SelectedT());
                    SetFalse();

                    return;
                }

            }
        }
        else
        {
            int newamount = SelectedT().Amount / 2;
            TosterHexUnit newunit = SelectedT().Team.AddNewUnit(SelectedT().Name, newamount);
            List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
            SelectedT().Amount = SelectedT().Amount - newamount;
            SelectedT().SetTextAmount();
            newunit.teamN = SelectedT().teamN;
            newunit.SetTosterPrefab(getHexUM().hexMap);
            newunit.SetTextAmount();
            getHexUM().hexMap.GenerateToster(getHexUM().C, getHexUM().R, newunit);
            SetUnitVisualVisibility(newunit, false);
            AddReveal(reveals, newunit.DealMeDMGDefForFrontendReveal(12, SelectedT(),true, FrontendResultRevealSource.Skill));
            newunit.Moved = true;
            PlaySequencedProjectileHexImpactThenResults("Stone_Throw", getHexUM(), reveals, () => SetUnitVisualVisibility(newunit, true));
            mouseControler.SetCD(SelectedT());
            SetFalse();
        }
    }

    [PunRPC]

    public void stone_Throw()
    {
    }
    public void Stone_ThrowM()
    {

        if (SelectedT().Amount==1)
        {
            Debug.LogError("Nie można użyc tej umiejętności, za mało golemów");
            SetFalse();
        }
        isTurn = true;
        cooldown = 3;
        unselectaround = true;
        Global = true;
        SingleTarget = true;
    }
    #endregion
    #region Defence_Ritual 
    public void Stone_Skin() //passive
    {
   
    }
    public void Stone_SkinM()
    {
        Debug.Log("Ta umiejętność jest pasywna");
        isTurn = false;
        SetFalse();
    }

    #endregion


    #endregion
    #region FireElemental  (T3)
    #region Fire_Movement  //Cielu





    public void Fire_Movement() //passive
    {
    }
    public void Fire_MovementM()
    {
        isTurn = false;
        SetFalse();
    }
    #endregion
    #region Fire_ball 

    public void Fire_Ball()
    {
        Debug.LogError("działam");
        List<HexClass> hexarea = new List<HexClass>(getHexUM().hexMap.GetHexesWithinRadiusOf(getHexUM(), aoeradius));
        List<FrontendResultReveal> hitReveals = new List<FrontendResultReveal>();
        SelectedT().SpecialDMGModificator = 40;
        foreach (HexClass t in hexarea)
        {
            if (t != null)
                if (t.Tosters.Count > 0)
                {
                    TosterHexUnit target = t.Tosters[0];
                    hitReveals.Add(target.ShootMEForFrontendReveal(SelectedT(), FrontendResultRevealSource.Skill));
                }
        }
        Debug.Log("C: " + getHexUM().C + "  R: " + getHexUM().R);
        SkillPresentationManager.PlaySequencedProjectileHits("Fire_Ball", SelectedT(), getHexUM(), hitReveals, GetSelectedSkillAnimationState());
        SelectedT().SpecialDMGModificator = 0;
        mouseControler.SetCD(SelectedT());
        SetFalse();
    }
    public void Fire_BallM()
    {
        cooldown = 2;
        unselectaround = true;
        aoeradius = 1;
        RangeisAoE = true;
        isTurn = true;
    }


    #endregion
    #region Fire_Skin //Lesisz

    public void Fire_Skin()
    {

    }
    public void Fire_SkinM()
    {
        isTurn = false;
        SetFalse();
    }
    #endregion
    #endregion
    #region FleshGolem  (T4)
    #region Heavy_Fists  

    public void Heavy_Fists()
    {
        if (getHexUM().Highlight == true && SlashTarget == true && hexum != tempHex)
        {
            MeleeisAoE = true;
            unselectaround = true;
            aoeradius = 1;
            isTurn = true;
            List<TosterHexUnit> tosterstoattack = new List<TosterHexUnit>();
            int tC, tR;
            TosterHexUnit t = SelectedT();
            tC = tempHex.C - getHexUM().Tosters[0].Hex.C;
            tR = tempHex.R - getHexUM().Tosters[0].Hex.R;
            Debug.Log("tC: " + tC);
            Debug.Log("tR: " + tR);
            if (tC == 0 && tR == 1)
            {
                if (isHexA(0, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R).Tosters[0]));
                }
                if (isHexA(-1, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C - 1, getHexUM().R).Tosters[0]));
                }
                if (isHexA(0, -1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R - 1).Tosters[0]));
                }
                if (isHexA(1, -1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R - 1).Tosters[0]));
                }
                t.SetVisualFacingYaw(120f);
            }
            if (tC == 0 && tR == -1)
            {
                if (isHexA(0, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R).Tosters[0]));
                }
                if (isHexA(1, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R).Tosters[0]));
                }
                if (isHexA(0, 1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R + 1).Tosters[0]));
                }
                if (isHexA(-1, 1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C - 1, getHexUM().R + 1).Tosters[0]));
                }
                t.SetVisualFacingYaw(-60f);
            }
            if (tC == -1 && tR == 1)
            {
                if (isHexA(0, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R).Tosters[0]));
                }
                if (isHexA(1, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R).Tosters[0]));
                }
                if (isHexA(0, -1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R - 1).Tosters[0]));
                }
                if (isHexA(1, -1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R - 1).Tosters[0]));
                }
                t.SetVisualFacingYaw(60f);

            }
            if (tC == 1 && tR == -1)
            {
                if (isHexA(0, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R).Tosters[0]));
                }
                if (isHexA(0, 1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R + 1).Tosters[0]));
                }
                if (isHexA(-1, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C - 1, getHexUM().R).Tosters[0]));
                }
                if (isHexA(-1, 1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C - 1, getHexUM().R + 1).Tosters[0]));
                }

                t.SetVisualFacingYaw(-120f);

            }
            if (tC == 1 && tR == 0)
            {
                if (isHexA(0, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R).Tosters[0]));
                }
                if (isHexA(1, 0))
                {

                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R).Tosters[0]));
                }
                if (isHexA(0, -1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R - 1).Tosters[0]));
                }
                if (isHexA(-1, 1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C - 1, getHexUM().R + 1).Tosters[0]));
                }
                t.SetVisualFacingYaw(180f);
            }
            if (tC == -1 && tR == 0)
            {
                if (isHexA(0, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R).Tosters[0]));
                }
                if (isHexA(1, 0))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R).Tosters[0]));
                }
                if (isHexA(0, 1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C, getHexUM().R + 1).Tosters[0]));
                }
                if (isHexA(1, -1))
                {
                    tosterstoattack.Add((getHexUM().hexMap.GetHexAt(getHexUM().C + 1, getHexUM().R - 1).Tosters[0]));
                }
                t.SetVisualFacingYaw(0f);
            }



            StartCommittedSkillCoroutine(HeavyFistsApproachAndCast(tempHex, SelectedT(), tosterstoattack, GetSelectedSkillAnimationState()));
            return;

        }
        if (isMove == true && hexum.Highlight && SelectedT().IsPathAvaible(hexum) && (hexum.Tosters.Count == 0 || hexum.Tosters.Contains(SelectedT())))
        {

            tempHex = getHexUM();
            SlashTarget = true;
            isMove = false;
            unselectaround = true;
            SelectedT().Hex.hexMap.unHighlight(SelectedT().Hex.C, SelectedT().Hex.R, SelectedT().GetMS());
        }
    }







    IEnumerator HeavyFistsApproachAndCast(
        HexClass approachHex,
        TosterHexUnit caster,
        List<TosterHexUnit> targets,
        string animationState)
    {
        if (approachHex == null || caster == null)
        {
            SetFalse();
            yield break;
        }

        yield return StartCoroutine(mouseControler.DoMovesST(approachHex, caster));

        if (caster.GetHP() > 20) caster.SpecialHP -= 20;
        else caster.SpecialHP = -caster.HP + 1;

        List<FrontendResultReveal> hitReveals = new List<FrontendResultReveal>();
        foreach (TosterHexUnit target in targets)
        {
            if (target == null || target == caster || target.isDead)
            {
                continue;
            }

            try
            {
                caster.SpecialDMGModificator = -30;
                hitReveals.Add(target.DealMeDMGForFrontendReveal(caster, FrontendResultRevealSource.Skill));
            }
            finally
            {
                caster.SpecialDMGModificator = 0;
            }
        }

        SkillPresentationManager.PlaySequencedInstantHits("Heavy_Fists", caster, hitReveals, animationState);
        SetFalse();
    }

    [PunRPC]

    public void heavy_fists(int c, int r, int sc, int sr)
    {
       TosterHexUnit user =  hexum.hexMap.GetHexAt(sc, sr).Tosters[0];
        if (user.GetHP() > 20) user.SpecialHP -= 20;
        else user.SpecialHP = -user.HP + 1;
        StartCoroutine(mouseControler.DoMovesST(hexum.hexMap.GetHexAt(c, r), user));
     //   mouseControler.photonView.RPC("StartCoroutineDoMovesST", RpcTarget.Others, new object[] { c, r, sc, sr });
        SetFalse();
        //   SelectedT().SpecialDMGModificator = 0;

    }


    public bool isHexA(int i , int j)
    {
        if (getHexUM().hexMap.GetHexAt(getHexUM().C + i, getHexUM().R + j) != null && getHexUM().hexMap.GetHexAt(getHexUM().C + i, getHexUM().R + j).Tosters.Count > 0)
        {
            return true;
        }
        return false;
    }
    public void Heavy_FistsM()
    {
        isMove = true;

    }

    
    #endregion
    #region Shapeshift   //Cielu

    public void Terrifying_Presence()
    {
      
    }
    public void Terrifying_PresenceM()
    {
       isTurn = false;
        SetFalse();
    }
    #endregion
    #region Rotting

    public void Rotting()
    {
   
    }
    public void RottingM()
    {
        isTurn = false;
        SetFalse();

    }
    #endregion
    #endregion

    #endregion

    #region UNIT STRUCUTRE
    /*
      
    /////////*** FRACTION_NAME *** //////////// 


            #region Unit_Name


                    #region Unit_Name_Skill1
    
                        public void Unit_Name_Skill1()
                        {
                        
                        }

                        public void Unit_Name_Skill1M()
                        {

                        }
    
                    #endregion

                 #region Unit_Name_Skill2
    
                        public void Unit_Name_Skill2()
                        {
                        
                        }

                        public void Unit_Name_Skill2M()
                        {

                        }
    
                    #endregion

                     #region Unit_Name_Skill3
    
                        public void Unit_Name_Skill3()
                        {
                        
                        }

                        public void Unit_Name_Skill3M()
                        {

                        }
    
                    #endregion

                     #region Unit_Name_Skill4
    
                        public void Unit_Name_Skill4()
                        {
                        
                        }

                        public void Unit_Name_Skill4M()
                        {

                        }
    
                    #endregion

            #endregion

    */
    #endregion

    /////////*** Barbarians *** //////////// 
    #region Barbarians

    #endregion




}
