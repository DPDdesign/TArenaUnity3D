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
}
