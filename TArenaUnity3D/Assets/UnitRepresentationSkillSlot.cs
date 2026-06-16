using UnityEngine;
using UnityEngine.EventSystems;

public class UnitRepresentationSkillSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    UnitRepresentation unitRepresentation;
    int skillIndex;

    public void Bind(UnitRepresentation newUnitRepresentation, int newSkillIndex)
    {
        unitRepresentation = newUnitRepresentation;
        skillIndex = newSkillIndex;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && unitRepresentation != null)
        {
            unitRepresentation.ShowSkillInfo(skillIndex);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && unitRepresentation != null)
        {
            unitRepresentation.HideSkillInfo();
        }
    }
}
