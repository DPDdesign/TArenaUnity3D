using UnityEngine;
using UnityEngine.EventSystems;

public class RightClickInfoSkill : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private string skillId;
    [SerializeField] private SkillInfoPresentation skillInfoPresentation;

    public void Bind(string newSkillId, SkillInfoPresentation newSkillInfoPresentation)
    {
        skillId = newSkillId;
        skillInfoPresentation = newSkillInfoPresentation;
    }

    public void SetSkillId(string newSkillId)
    {
        skillId = newSkillId;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right ||
            skillInfoPresentation == null ||
            string.IsNullOrEmpty(skillId))
        {
            return;
        }

        skillInfoPresentation.ShowSkill(skillId);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && skillInfoPresentation != null)
        {
            skillInfoPresentation.Hide();
        }
    }
}
