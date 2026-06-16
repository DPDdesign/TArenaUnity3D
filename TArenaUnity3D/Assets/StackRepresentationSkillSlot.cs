using UnityEngine;
using UnityEngine.EventSystems;

public class StackRepresentationSkillSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    StackRepresentation stackRepresentation;
    int skillIndex;

    public void Bind(StackRepresentation newStackRepresentation, int newSkillIndex)
    {
        stackRepresentation = newStackRepresentation;
        skillIndex = newSkillIndex;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && stackRepresentation != null)
        {
            stackRepresentation.ShowSkillInfo(skillIndex);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && stackRepresentation != null)
        {
            stackRepresentation.HideSkillInfo();
        }
    }
}
