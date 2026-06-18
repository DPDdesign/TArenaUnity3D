using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StackRepresentation : MonoBehaviour
{
    [Header("Unit")]
    public Image Icon;
    public TMP_Text Tier;
    public GameObject TierStarsRoot;
    public TMP_Text Name;
    public TMP_Text Cost;

    [Header("Stack")]
    public TMP_Text Count;
    public TMP_Text StackCost;
    public TMP_Text StackValue;
    public TMP_Text Lost;
    public TMP_Text Level;

    [Header("Stats")]
    public TMP_Text Attack;
    public TMP_Text Defence;
    public TMP_Text Damage;
    public TMP_Text DamageMin;
    public TMP_Text DamageMax;
    public TMP_Text Movement;
    public TMP_Text Initiative;
    public TMP_Text Health;
    public TMP_Text CurrentHP;
    public TMP_Text MaxHP;
    public TMP_Text HPBarText;
    public Image HPBarFill;

    [Header("Skills")]
    public TMP_Text SkillsText;
    public List<Image> SkillIcons;
    public List<TMP_Text> SkillNames;

    [Header("Skill Info Panel")]
    public Image SkillInfoPanel;
    public Image SkillInfoIcon;
    public TMP_Text SkillInfoName;
    public TMP_Text SkillInfoType;
    public TMP_Text SkillInfoText;

    UnitRepresentation unitRepresentation;
    StackInfoData currentInfo;
    readonly List<string> currentSkillIds = new List<string>();
    readonly List<bool> currentSkillUnlocked = new List<bool>();

    public UnitRepresentation Unit
    {
        get { return ResolveUnitRepresentation(); }
    }

    void Awake()
    {
        BindSkillSlots();
        ResolveUnitRepresentation();
    }

    void OnEnable()
    {
        HideSkillInfo();
    }

    public void DisplayInfo(StackInfoData info)
    {
        currentInfo = info;
        bool hasInfo = info != null;
        gameObject.SetActive(hasInfo);
        if (!hasInfo)
        {
            return;
        }

        SetTextIfAvailable(Count, info.Count.ToString());
        SetTextIfAvailable(StackCost, info.StackCost.ToString());
        SetTextIfAvailable(StackValue, info.StackValue.ToString());
        SetTextIfAvailable(Lost, info.Lost > 0 ? info.Lost.ToString() : "");
        SetTextIfAvailable(Level, info.Level > 0 ? info.Level.ToString() : "");
        DisplayUnitInfo(info.Unit);

        UnitRepresentation unit = ResolveUnitRepresentation();
        if (unit != null)
        {
            unit.DisplayInfo(info.Unit);
        }
    }

    public void DisplayStackInfo(StackInfoData info)
    {
        DisplayInfo(info);
    }

    public StackInfoData GetCurrentInfo()
    {
        return currentInfo;
    }

    public void DisplayUnitInfo(UnitInfoData info)
    {
        if (info == null)
        {
            return;
        }

        SetTextIfAvailable(Name, info.DisplayName);
        DisplayTier(info.Tier);
        SetTextIfAvailable(Cost, info.Cost.ToString());
        SetIcon(Icon, DataMapper.Instance.LoadUnitSprite(info.SpriteReference));
        DisplayStats(info.Stats);
        DisplaySkills(info.Skills);
    }

    public void DisplayStats(UnitStatsData stats)
    {
        if (stats == null)
        {
            return;
        }

        SetTextIfAvailable(Attack, stats.Attack.ToString());
        SetTextIfAvailable(Defence, stats.Defence.ToString());
        SetTextIfAvailable(Damage, stats.Damage.ToString());
        SetTextIfAvailable(DamageMin, stats.DamageMin.ToString());
        SetTextIfAvailable(DamageMax, stats.DamageMax.ToString());
        SetTextIfAvailable(Movement, stats.Movement.ToString());
        SetTextIfAvailable(Initiative, stats.Initiative.ToString());
        SetTextIfAvailable(Health, stats.CurrentHealth.ToString() + " / " + stats.MaxHealth.ToString());
        SetTextIfAvailable(CurrentHP, stats.CurrentHealth.ToString());
        SetTextIfAvailable(MaxHP, stats.MaxHealth.ToString());
        SetTextIfAvailable(HPBarText, stats.CurrentHealth.ToString() + " / " + stats.MaxHealth.ToString());

        if (HPBarFill != null)
        {
            HPBarFill.type = Image.Type.Filled;
            HPBarFill.fillAmount = stats.MaxHealth > 0 ? Mathf.Clamp01((float)stats.CurrentHealth / stats.MaxHealth) : 0f;
        }
    }

    public void DisplaySkills(List<SkillInfoData> skills)
    {
        currentSkillIds.Clear();
        currentSkillUnlocked.Clear();

        if (skills != null)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] == null || string.IsNullOrEmpty(skills[i].SkillId))
                {
                    continue;
                }

                currentSkillIds.Add(skills[i].SkillId);
                currentSkillUnlocked.Add(skills[i].Unlocked);
            }
        }

        BindSkillSlots();
        SetTextIfAvailable(SkillsText, BuildSkillsText());

        int iconCount = SkillIcons == null ? 0 : SkillIcons.Count;
        for (int i = 0; i < iconCount; i++)
        {
            Image skillIcon = SkillIcons[i];
            bool hasSkill = i < currentSkillIds.Count;

            if (skillIcon != null)
            {
                skillIcon.gameObject.SetActive(hasSkill);
                skillIcon.enabled = hasSkill;
                SetIcon(skillIcon, hasSkill ? DataMapper.Instance.LoadSkillIcon(currentSkillIds[i]) : null);
                skillIcon.color = hasSkill && IsSkillUnlocked(i) ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }

            SetSkillName(i, hasSkill ? currentSkillIds[i] : "");
        }
    }

    public void ShowSkillInfo(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= currentSkillIds.Count)
        {
            return;
        }

        string skillId = currentSkillIds[skillIndex];
        DataMapper.SkillDefinition skill = DataMapper.Instance.FindSkill(skillId);
        if (skill == null)
        {
            return;
        }

        if (SkillInfoPanel != null)
        {
            SkillInfoPanel.gameObject.SetActive(true);
        }

        SetIcon(SkillInfoIcon, DataMapper.Instance.LoadSkillIcon(skillId));
        SetTextIfAvailable(SkillInfoName, skill.Name);
        SetTextIfAvailable(SkillInfoType, skill.Type);
        SetTextIfAvailable(SkillInfoText, skill.Info);
    }

    public void HideSkillInfo()
    {
        if (SkillInfoPanel != null)
        {
            SkillInfoPanel.gameObject.SetActive(false);
        }
    }

    UnitRepresentation ResolveUnitRepresentation()
    {
        if (unitRepresentation == null)
        {
            unitRepresentation = GetComponent<UnitRepresentation>();
        }

        if (unitRepresentation == null)
        {
            unitRepresentation = GetComponentInChildren<UnitRepresentation>(true);
        }

        return unitRepresentation;
    }

    void DisplayTier(string tier)
    {
        SetTextIfAvailable(Tier, tier);
        SetTierStars(ParseTier(tier));
    }

    void SetTierStars(int tier)
    {
        if (TierStarsRoot == null)
        {
            return;
        }

        int clampedTier = Mathf.Clamp(tier, 0, 5);
        for (int i = 0; i < TierStarsRoot.transform.childCount; i++)
        {
            TierStarsRoot.transform.GetChild(i).gameObject.SetActive(i < clampedTier && i < 5);
        }
    }

    int ParseTier(string tier)
    {
        if (string.IsNullOrEmpty(tier))
        {
            return 0;
        }

        int numericTier;
        if (int.TryParse(tier, out numericTier))
        {
            return numericTier;
        }

        string normalizedTier = tier.Trim().ToUpperInvariant();
        if (normalizedTier == "I")
        {
            return 1;
        }

        if (normalizedTier == "II")
        {
            return 2;
        }

        if (normalizedTier == "III")
        {
            return 3;
        }

        if (normalizedTier == "IV")
        {
            return 4;
        }

        if (normalizedTier == "V")
        {
            return 5;
        }

        return 0;
    }

    void BindSkillSlots()
    {
        if (SkillIcons == null)
        {
            return;
        }

        for (int i = 0; i < SkillIcons.Count; i++)
        {
            if (SkillIcons[i] == null)
            {
                continue;
            }

            StackRepresentationSkillSlot slot = SkillIcons[i].GetComponent<StackRepresentationSkillSlot>();
            if (slot == null)
            {
                slot = SkillIcons[i].gameObject.AddComponent<StackRepresentationSkillSlot>();
            }

            slot.Bind(this, i);
        }
    }

    bool IsSkillUnlocked(int index)
    {
        return index >= 0 && index < currentSkillUnlocked.Count && currentSkillUnlocked[index];
    }

    string BuildSkillsText()
    {
        if (currentSkillIds.Count == 0)
        {
            return "";
        }

        string result = "";
        for (int i = 0; i < currentSkillIds.Count; i++)
        {
            if (result.Length > 0)
            {
                result += " / ";
            }

            result += IsSkillUnlocked(i) ? currentSkillIds[i] : "[" + currentSkillIds[i] + "]";
        }

        return result;
    }

    void SetSkillName(int index, string value)
    {
        if (SkillNames == null || index < 0 || index >= SkillNames.Count || SkillNames[index] == null)
        {
            return;
        }

        SkillNames[index].gameObject.SetActive(string.IsNullOrEmpty(value) == false);
        SkillNames[index].text = value;
    }

    void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    void SetTextIfAvailable(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? "";
        }
    }
}
