using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillInfoPresentation : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image skillIcon;
    [SerializeField] private TMP_Text skillName;
    [SerializeField] private TMP_Text skillType;
    [SerializeField] private TMP_Text skillInfo;
    [SerializeField] private Text legacySkillName;
    [SerializeField] private Text legacySkillType;
    [SerializeField] private Text legacySkillInfo;
    [SerializeField] private bool hideOnStart = true;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (hideOnStart)
        {
            Hide();
        }
    }

    public void Configure(GameObject newPanelRoot, Image newSkillIcon, TMP_Text newSkillName, TMP_Text newSkillType, TMP_Text newSkillInfo)
    {
        panelRoot = newPanelRoot != null ? newPanelRoot : gameObject;
        skillIcon = newSkillIcon;
        skillName = newSkillName;
        skillType = newSkillType;
        skillInfo = newSkillInfo;
        legacySkillName = null;
        legacySkillType = null;
        legacySkillInfo = null;

        if (hideOnStart)
        {
            Hide();
        }
    }

    public void ConfigureLegacy(GameObject newPanelRoot, Image newSkillIcon, Text newSkillName, Text newSkillType, Text newSkillInfo)
    {
        panelRoot = newPanelRoot != null ? newPanelRoot : gameObject;
        skillIcon = newSkillIcon;
        skillName = null;
        skillType = null;
        skillInfo = null;
        legacySkillName = newSkillName;
        legacySkillType = newSkillType;
        legacySkillInfo = newSkillInfo;

        if (hideOnStart)
        {
            Hide();
        }
    }

    public void ShowSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || DataMapper.Instance == null)
        {
            Hide();
            return;
        }

        DataMapper.SkillDefinition skill = DataMapper.Instance.FindSkill(skillId);
        if (skill == null)
        {
            Hide();
            return;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        SetIcon(skillIcon, DataMapper.Instance.LoadSkillIcon(skillId));
        SetTextIfAvailable(skillName, skill.Name);
        SetTextIfAvailable(skillType, skill.Type);
        SetTextIfAvailable(skillInfo, skill.Info);
        SetTextIfAvailable(legacySkillName, skill.Name);
        SetTextIfAvailable(legacySkillType, skill.Type);
        SetTextIfAvailable(legacySkillInfo, skill.Info);
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private static void SetIcon(Image target, Sprite sprite)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private static void SetTextIfAvailable(TMP_Text target, string value)
    {
        if (target == null)
        {
            return;
        }

        target.text = value;
    }

    private static void SetTextIfAvailable(Text target, string value)
    {
        if (target == null)
        {
            return;
        }

        target.text = value;
    }
}
