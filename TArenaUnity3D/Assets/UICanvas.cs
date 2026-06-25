using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TimeSpells;
using TMPro;

[System.Serializable]
public class UnitStatsTextRefs
{
    public TMP_Text Name;
    public TMP_Text MaxHP;
    public TMP_Text CurrentHP;
    public TMP_Text HPBar_Text;
    public GameObject HPBar;
    public TMP_Text Attack;
    public TMP_Text Defence;
    public TMP_Text Damage;
    public TMP_Text Movement;
    public TMP_Text Initiative;
    public TMP_Text Count;
}

public class UICanvas : MonoBehaviour
{
    [Header("Stats Panel TMP")]
    public UnitStatsTextRefs InfoStatsTexts;

    [Header("Footer TMP")]
    public UnitStatsTextRefs FooterStatsTexts;

    [Header("Action UI")]
    public List<Button> ActionButtons;
    public List<GameObject> SpellImages;
    public List<Button> SkillButtons;
    public bool ShowSkillName = false;
    public List<TMP_Text> SkillT;
    public List<Image> CooldownFillImages;
    public Color CooldownFillColor = new Color(0.25f, 0.25f, 0.25f, 0.65f);
    public Image CurrentUnitPortrait;
    public GameObject StatsPanel;
    public GameObject EndPanel;
    public TMP_Text EndText;
    public MouseControler MC;
    public List<TMP_Text> Cooldowns;
    string currentUnitPortraitSpriteName;
    // Start is called before the first frame update
    void Start()
    {
        MC = FindObjectOfType<MouseControler>();
    }
    
    public void Disconnect()
    {
        LocalGameSession.ForceLocalMode();
        SceneManager.LoadScene("MainMenu_Scene");
    }
    public void UpdateCHP(int chp)
    {
        if (InfoStatsTexts == null)
        {
            return;
        }

        SetTextIfAvailable(InfoStatsTexts.CurrentHP, chp.ToString());
        int maxHP = MC != null && MC.SelectedToster != null ? MC.SelectedToster.GetHP() : chp;
        SetTextIfAvailable(InfoStatsTexts.HPBar_Text, chp.ToString() + " / " + maxHP.ToString());
        SetHPBarFill(InfoStatsTexts.HPBar, chp, maxHP);
    }

    public void UpdateAllStats(int mhp, int chp, int att, int def, int dmg , int ms , int INT, string N)
    {
        if (InfoStatsTexts == null)
        {
            return;
        }

        SetTextIfAvailable(InfoStatsTexts.MaxHP, mhp.ToString());
        SetTextIfAvailable(InfoStatsTexts.CurrentHP, chp.ToString());
        SetTextIfAvailable(InfoStatsTexts.HPBar_Text, chp.ToString() + " / " + mhp.ToString());
        SetHPBarFill(InfoStatsTexts.HPBar, chp, mhp);
        SetTextIfAvailable(InfoStatsTexts.Attack, att.ToString());
        SetTextIfAvailable(InfoStatsTexts.Defence, def.ToString());
        SetTextIfAvailable(InfoStatsTexts.Damage, dmg.ToString());
        SetTextIfAvailable(InfoStatsTexts.Movement, ms.ToString());
        SetTextIfAvailable(InfoStatsTexts.Initiative, INT.ToString());
        SetTextIfAvailable(InfoStatsTexts.Name, N);
    }

    public void GetSpellsOnToster(TosterHexUnit toster)
    {

        int i = 0;

        List<SpellOverTime> SpellsOnToster = new List<SpellOverTime>();
        SpellsOnToster.AddRange(toster.SpellsGoingOn);

        foreach (GameObject gameobject in SpellImages)
        {
            gameobject.SetActive(false);
        }

        foreach (SpellOverTime spell in SpellsOnToster)
        {
            Debug.Log(SpellsOnToster[i].nameofspell);
            SpellImages[i].GetComponent<Image>().sprite = DataMapper.Instance.LoadSkillIcon(SpellsOnToster[i].nameofspell);
            SpellImages[i].SetActive(true);
            i++;

        }

    }



    public void UpdateAllStats(TosterHexUnit toster)
    {
        SetStatsTexts(InfoStatsTexts, toster);
    }

    public void UpdateFooterStats(TosterHexUnit toster)
    {
        SetStatsTexts(FooterStatsTexts, toster);
    }

    public void UpdateCurrentUnitPortrait(TosterHexUnit toster)
    {
        if (CurrentUnitPortrait == null || toster == null)
        {
            return;
        }

        if (currentUnitPortraitSpriteName == toster.TosterSpriteName)
        {
            return;
        }

        currentUnitPortraitSpriteName = toster.TosterSpriteName;
        CurrentUnitPortrait.sprite = DataMapper.Instance.LoadUnitSprite(currentUnitPortraitSpriteName);
        CurrentUnitPortrait.enabled = CurrentUnitPortrait.sprite != null;
    }

    void SetStatsTexts(UnitStatsTextRefs texts, TosterHexUnit toster)
    {
        if (texts == null || toster == null)
        {
            return;
        }

        SetTextIfAvailable(texts.Name, toster.Name);
        SetTextIfAvailable(texts.MaxHP, toster.GetHP().ToString());
        SetTextIfAvailable(texts.CurrentHP, toster.TempHP.ToString());
        SetTextIfAvailable(texts.HPBar_Text, toster.TempHP.ToString() + " / " + toster.GetHP().ToString());
        SetHPBarFill(texts.HPBar, toster.TempHP, toster.GetHP());
        SetTextIfAvailable(texts.Attack, toster.GetAtt().ToString());
        SetTextIfAvailable(texts.Defence, toster.GetDef().ToString());
        SetTextIfAvailable(texts.Damage, GetAverageDamageText(toster));
        SetTextIfAvailable(texts.Movement, (toster.GetMS() - 1).ToString());
        SetTextIfAvailable(texts.Initiative, toster.GetIni().ToString());
        SetTextIfAvailable(texts.Count, toster.Amount.ToString());
    }

    void SetTextIfAvailable(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
    }

    string GetAverageDamageText(TosterHexUnit toster)
    {
        return Mathf.CeilToInt((toster.GetMinDmg() + toster.GetMaxDMG()) / 2f).ToString();
    }

    void SetHPBarFill(GameObject hpBar, int currentHP, int maxHP)
    {
        Image fillImage = GetHPBarFillImage(hpBar);
        if (fillImage == null)
        {
            return;
        }

        fillImage.type = Image.Type.Filled;
        fillImage.fillAmount = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
    }

    Image GetHPBarFillImage(GameObject hpBar)
    {
        if (hpBar == null)
        {
            return null;
        }

        Transform fillTransform = hpBar.transform.Find("Fill");
        if (fillTransform != null)
        {
            Image fillImage = fillTransform.GetComponent<Image>();
            if (fillImage != null)
            {
                return fillImage;
            }
        }

        Image directImage = hpBar.GetComponent<Image>();
        if (directImage != null)
        {
            return directImage;
        }

        return hpBar.GetComponentInChildren<Image>();
    }
    // Update is called once per frame
    public void UseSkill(int i)
    {
        if (HasSkillButton(i) == false)
        {
            return;
        }

        SkillButtons[i].image.color= Color.grey;
    }
    public void UnUseSkill(int i)
    {
        if (HasSkillButton(i) == false)
        {
            return;
        }

        if (IsPassiveSkillSlot(i) == false)
        {
            SkillButtons[i].image.color = Color.white;
        }
    }

    public GameObject menu;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menu != null)
            {
                menu.SetActive(!menu.activeSelf);
            }
        }
        if (MC != null && MC.SelectedToster != null)
        {
            UpdateFooterStats(MC.SelectedToster);
            UpdateCurrentUnitPortrait(MC.SelectedToster);
        }
        bool hasSelectedToster = MC != null && MC.SelectedToster != null;
        bool canUseButtons = hasSelectedToster && MC.activeButtons == true;
        bool canUseActionButtons = canUseButtons && MC.SelectedToster.MovedThisTurn == false && MC.SelectedToster.UsedSkillThisTurn == false;

        if (hasSelectedToster)
        {

            if (ActionButtons != null)
            {
                foreach (Button b in ActionButtons)
                {
                    if (b != null)
                    {
                        b.interactable = canUseActionButtons;
                    }
                }
            }
            int i = 0;
            if (MC.SelectedToster.skillstrings != null)
            {
                foreach (string b in MC.SelectedToster.skillstrings)
                {
                    if (HasSkillButton(i) == false)
                    {
                        break;
                    }

                    SkillButtons[i].image.enabled = true;
                    int cooldown = GetCooldownValue(i);
                    if (cooldown == 0)
                    {
                        SetCooldownTextActive(i, false);
                        SetCooldownFill(i, 0f);
                        SkillButtons[i].interactable = canUseButtons;
                        SetSkillNameText(i, b);
                        SkillButtons[i].image.sprite = DataMapper.Instance.LoadSkillIcon(b);



                        if (canUseButtons == false || MC.CanUseSkillSlot(i) == false)

                        {
                            SkillButtons[i].interactable = false;
                            SkillButtons[i].image.color = Color.grey;
                        }
                        else
                        {
                            SkillButtons[i].image.color = Color.white;
                        }
                    }
                    else
                    {
                        SkillButtons[i].interactable = false;

                        SetSkillNameText(i, b);
                        SkillButtons[i].image.sprite = DataMapper.Instance.LoadSkillIcon(b);
                        SetCooldownText(i, cooldown.ToString());
                        SetCooldownFill(i, GetCooldownFillAmount(b, cooldown));
                    }
                    i++;


                }
            }

            ClearSkillButtonsFrom(i);

        }
        else
        {
            if (ActionButtons != null)
            {
                foreach (Button b in ActionButtons)
                {
                    if (b != null)
                    {
                        b.interactable = false;
                    }
                }
            }
            ClearSkillButtonsFrom(0);

        }
    }

    void SetSkillNameText(int index, string skillName)
    {
        SetRightClickSkillId(index, skillName);

        if (ShowSkillName == false || SkillT == null || index < 0 || index >= SkillT.Count || SkillT[index] == null)
        {
            return;
        }

        SkillT[index].text = skillName;
    }

    void SetRightClickSkillId(int index, string skillName)
    {
        if (SkillButtons == null || index < 0 || index >= SkillButtons.Count || SkillButtons[index] == null)
        {
            return;
        }

        RightClickInfoSkill rightClickInfoSkill = SkillButtons[index].GetComponent<RightClickInfoSkill>();
        if (rightClickInfoSkill != null)
        {
            rightClickInfoSkill.SetSkillId(skillName);
        }
    }

    bool HasSkillButton(int index)
    {
        return SkillButtons != null &&
            index >= 0 &&
            index < SkillButtons.Count &&
            SkillButtons[index] != null &&
            SkillButtons[index].image != null;
    }

    bool IsPassiveSkillSlot(int index)
    {
        string skillName = GetSelectedSkillName(index);
        if (string.IsNullOrEmpty(skillName))
        {
            return false;
        }

        DataMapper.SkillDefinition skillDefinition = DataMapper.Instance.FindSkill(skillName);
        return skillDefinition != null && skillDefinition.Type == "Passive";
    }

    string GetSelectedSkillName(int index)
    {
        if (MC == null ||
            MC.SelectedToster == null ||
            MC.SelectedToster.skillstrings == null ||
            index < 0 ||
            index >= MC.SelectedToster.skillstrings.Count)
        {
            return "";
        }

        return MC.SelectedToster.skillstrings[index];
    }

    int GetCooldownValue(int index)
    {
        if (MC == null ||
            MC.SelectedToster == null ||
            MC.SelectedToster.cooldowns == null ||
            index < 0 ||
            index >= MC.SelectedToster.cooldowns.Count)
        {
            return 0;
        }

        return MC.SelectedToster.cooldowns[index];
    }

    float GetCooldownFillAmount(string skillName, int currentCooldown)
    {
        if (currentCooldown <= 0)
        {
            return 0f;
        }

        int maxCooldown = GetSkillMaxCooldown(skillName);
        if (maxCooldown <= 0)
        {
            maxCooldown = currentCooldown;
        }

        return Mathf.Clamp01((float)currentCooldown / maxCooldown);
    }

    int GetSkillMaxCooldown(string skillName)
    {
        SkillDefinitionAsset skillAsset = DataMapper.Instance != null ? DataMapper.Instance.FindSkillAsset(skillName) : null;
        if (skillAsset != null && skillAsset.ActivationRule.cooldownTurns > 0)
        {
            return skillAsset.ActivationRule.cooldownTurns;
        }

        DataMapper.SkillDefinition skillDefinition = DataMapper.Instance.FindSkill(skillName);
        if (skillDefinition == null || string.IsNullOrEmpty(skillDefinition.Info))
        {
            return 0;
        }

        string info = skillDefinition.Info.ToUpperInvariant();
        int cooldownIndex = info.IndexOf("CD");
        if (cooldownIndex < 0)
        {
            return 0;
        }

        string digits = "";
        for (int i = cooldownIndex + 2; i < info.Length; i++)
        {
            if (char.IsDigit(info[i]))
            {
                digits += info[i];
            }
            else if (digits.Length > 0)
            {
                break;
            }
        }

        int cooldown;
        return int.TryParse(digits, out cooldown) ? cooldown : 0;
    }

    void SetCooldownText(int index, string value)
    {
        if (Cooldowns == null || index < 0 || index >= Cooldowns.Count || Cooldowns[index] == null)
        {
            return;
        }

        Cooldowns[index].gameObject.SetActive(true);
        Cooldowns[index].text = value;
    }

    void SetCooldownTextActive(int index, bool active)
    {
        if (Cooldowns == null || index < 0 || index >= Cooldowns.Count || Cooldowns[index] == null)
        {
            return;
        }

        Cooldowns[index].gameObject.SetActive(active);
    }

    void SetCooldownFill(int index, float fillAmount)
    {
        if (CooldownFillImages == null || index < 0 || index >= CooldownFillImages.Count || CooldownFillImages[index] == null)
        {
            return;
        }

        Image cooldownFill = CooldownFillImages[index];
        cooldownFill.gameObject.SetActive(fillAmount > 0f);
        cooldownFill.type = Image.Type.Filled;
        cooldownFill.fillAmount = fillAmount;
        cooldownFill.color = CooldownFillColor;
        cooldownFill.raycastTarget = false;
    }

    void ClearSkillButtonsFrom(int startIndex)
    {
        if (SkillButtons == null)
        {
            return;
        }

        for (int i = startIndex; i < SkillButtons.Count; i++)
        {
            if (SkillButtons[i] == null || SkillButtons[i].image == null)
            {
                continue;
            }

            SkillButtons[i].interactable = false;
            SkillButtons[i].image.enabled = false;
            if (ShowSkillName && SkillT != null && i < SkillT.Count && SkillT[i] != null)
            {
                SkillT[i].text = "";
            }
            SetRightClickSkillId(i, "");
            SetCooldownTextActive(i, false);
            SetCooldownFill(i, 0f);
        }
    }
}
