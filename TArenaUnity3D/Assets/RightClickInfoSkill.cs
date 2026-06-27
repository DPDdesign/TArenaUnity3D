using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class RightClickInfoSkill : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    const string DebugPrefix = "[DEBUG-SKILLINFO]";

    [SerializeField] private string skillId;
    [SerializeField] private SkillInfoPresentation skillInfoPresentation;

    private void Awake()
    {
        ResolveSkillInfoPresentation();
        Debug.Log(DebugPrefix + " Awake object=" + GetHierarchyPath(transform) +
            " skillId=" + skillId +
            " presentation=" + GetPresentationPath());
    }

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
        ResolveSkillInfoPresentation();
        Debug.Log(DebugPrefix + " PointerDown object=" + GetHierarchyPath(transform) +
            " button=" + eventData.button +
            " skillId=" + skillId +
            " presentation=" + GetPresentationPath() +
            " raycast=" + GetRaycastObjectPath(eventData));

        if (eventData.button != PointerEventData.InputButton.Right ||
            skillInfoPresentation == null ||
            string.IsNullOrEmpty(skillId))
        {
            Debug.Log(DebugPrefix + " PointerDown ignored object=" + GetHierarchyPath(transform) +
                " isRight=" + (eventData.button == PointerEventData.InputButton.Right) +
                " hasPresentation=" + (skillInfoPresentation != null) +
                " hasSkillId=" + (string.IsNullOrEmpty(skillId) == false));
            return;
        }

        Debug.Log(DebugPrefix + " ShowSkill skillId=" + skillId +
            " presentation=" + GetPresentationPath());
        skillInfoPresentation.ShowSkill(skillId);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResolveSkillInfoPresentation();
        Debug.Log(DebugPrefix + " PointerUp object=" + GetHierarchyPath(transform) +
            " button=" + eventData.button +
            " skillId=" + skillId +
            " presentation=" + GetPresentationPath());

        if (eventData.button == PointerEventData.InputButton.Right && skillInfoPresentation != null)
        {
            skillInfoPresentation.Hide();
        }
    }

    private void ResolveSkillInfoPresentation()
    {
        SkillInfoPresentation localPresentation = GetLocalSkillInfoPresentation();
        if (localPresentation != null)
        {
            skillInfoPresentation = localPresentation;
            return;
        }

        SkillInfoPresentation slotPresentation = GetNearestSkillSlotPresentation();
        if (slotPresentation != null)
        {
            skillInfoPresentation = slotPresentation;
            return;
        }

        SkillInfoPresentation generatedPresentation = TryCreateNearestSkillSlotPresentation();
        if (generatedPresentation != null)
        {
            skillInfoPresentation = generatedPresentation;
            return;
        }

        if (skillInfoPresentation != null)
        {
            return;
        }
    }

    private SkillInfoPresentation GetLocalSkillInfoPresentation()
    {
        SkillInfoPresentation selfPresentation = GetComponent<SkillInfoPresentation>();
        if (selfPresentation != null)
        {
            return selfPresentation;
        }

        SkillInfoPresentation[] childPresentations = GetComponentsInChildren<SkillInfoPresentation>(true);
        if (childPresentations == null || childPresentations.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < childPresentations.Length; i++)
        {
            if (childPresentations[i] != null && childPresentations[i].transform.name == "SkillInfo")
            {
                return childPresentations[i];
            }
        }

        return childPresentations[0];
    }

    private SkillInfoPresentation GetNearestSkillSlotPresentation()
    {
        Transform slotRoot = FindNearestSkillSlotRoot();
        if (slotRoot == null)
        {
            return null;
        }

        SkillInfoPresentation[] slotPresentations = slotRoot.GetComponentsInChildren<SkillInfoPresentation>(true);
        if (slotPresentations == null || slotPresentations.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < slotPresentations.Length; i++)
        {
            if (slotPresentations[i] != null && slotPresentations[i].transform.name == "SkillInfo")
            {
                return slotPresentations[i];
            }
        }

        return slotPresentations[0];
    }

    private SkillInfoPresentation TryCreateNearestSkillSlotPresentation()
    {
        Transform slotRoot = FindNearestSkillSlotRoot();
        if (slotRoot == null)
        {
            return null;
        }

        Transform presentationRoot = FindPresentationRoot(slotRoot);
        if (presentationRoot == null)
        {
            return null;
        }

        TMP_Text skillNameText = FindDescendantComponentByName<TMP_Text>(presentationRoot, "NameS");
        TMP_Text skillTypeText = FindDescendantComponentByName<TMP_Text>(presentationRoot, "TYP");
        TMP_Text skillInfoText = FindDescendantComponentByName<TMP_Text>(presentationRoot, "INFO");
        Text legacySkillNameText = FindDescendantComponentByName<Text>(presentationRoot, "NameS");
        Text legacySkillTypeText = FindDescendantComponentByName<Text>(presentationRoot, "TYP");
        Text legacySkillInfoText = FindDescendantComponentByName<Text>(presentationRoot, "INFO");

        bool hasTmpTexts = skillNameText != null && skillTypeText != null && skillInfoText != null;
        bool hasLegacyTexts = legacySkillNameText != null && legacySkillTypeText != null && legacySkillInfoText != null;
        if (hasTmpTexts == false && hasLegacyTexts == false)
        {
            Debug.Log(DebugPrefix + " CreatePresentation failed root=" + GetHierarchyPath(presentationRoot) +
                " hasTmpName=" + (skillNameText != null) +
                " hasTmpType=" + (skillTypeText != null) +
                " hasTmpInfo=" + (skillInfoText != null) +
                " hasLegacyName=" + (legacySkillNameText != null) +
                " hasLegacyType=" + (legacySkillTypeText != null) +
                " hasLegacyInfo=" + (legacySkillInfoText != null));
            return null;
        }

        SkillInfoPresentation presentation = presentationRoot.GetComponent<SkillInfoPresentation>();
        if (presentation == null)
        {
            presentation = presentationRoot.gameObject.AddComponent<SkillInfoPresentation>();
        }

        Image skillIcon = FindDescendantComponentByName<Image>(presentationRoot, "SkillIcon");
        if (hasTmpTexts)
        {
            presentation.Configure(presentationRoot.gameObject, skillIcon, skillNameText, skillTypeText, skillInfoText);
            Debug.Log(DebugPrefix + " CreatePresentation TMP root=" + GetHierarchyPath(presentationRoot) +
                " nameText=" + GetHierarchyPath(skillNameText.transform) +
                " typeText=" + GetHierarchyPath(skillTypeText.transform) +
                " infoText=" + GetHierarchyPath(skillInfoText.transform));
        }
        else
        {
            presentation.ConfigureLegacy(presentationRoot.gameObject, skillIcon, legacySkillNameText, legacySkillTypeText, legacySkillInfoText);
            Debug.Log(DebugPrefix + " CreatePresentation Legacy root=" + GetHierarchyPath(presentationRoot) +
                " nameText=" + GetHierarchyPath(legacySkillNameText.transform) +
                " typeText=" + GetHierarchyPath(legacySkillTypeText.transform) +
                " infoText=" + GetHierarchyPath(legacySkillInfoText.transform));
        }

        return presentation;
    }

    private Transform FindPresentationRoot(Transform slotRoot)
    {
        Transform skillInfoRoot = FindDescendantByName(slotRoot, "SkillInfo");
        if (skillInfoRoot != null)
        {
            return skillInfoRoot;
        }

        Transform[] descendants = slotRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform candidate = descendants[i];
            if (candidate == null)
            {
                continue;
            }

            if (FindDescendantComponentByName<TMP_Text>(candidate, "NameS") != null &&
                FindDescendantComponentByName<TMP_Text>(candidate, "TYP") != null &&
                FindDescendantComponentByName<TMP_Text>(candidate, "INFO") != null)
            {
                return candidate;
            }
        }

        return null;
    }

    private Transform FindNearestSkillSlotRoot()
    {
        Transform current = transform;
        while (current != null)
        {
            if (IsSkillSlotRootName(current.name))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static bool IsSkillSlotRootName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string normalizedName = objectName.Trim();
        if (normalizedName == "SKILL" || normalizedName.StartsWith("SKILL ("))
        {
            return true;
        }

        if (normalizedName.Length > 5 && normalizedName.StartsWith("Skill"))
        {
            return char.IsDigit(normalizedName[5]);
        }

        return false;
    }

    private static Transform FindDescendantByName(Transform root, string descendantName)
    {
        if (root == null || string.IsNullOrEmpty(descendantName))
        {
            return null;
        }

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i] != null && descendants[i].name == descendantName)
            {
                return descendants[i];
            }
        }

        return null;
    }

    private static T FindDescendantComponentByName<T>(Transform root, string descendantName) where T : Component
    {
        Transform descendant = FindDescendantByName(root, descendantName);
        return descendant == null ? null : descendant.GetComponent<T>();
    }

    private string GetPresentationPath()
    {
        return skillInfoPresentation == null ? "<null>" : GetHierarchyPath(skillInfoPresentation.transform);
    }

    private static string GetRaycastObjectPath(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerCurrentRaycast.gameObject == null)
        {
            return "<null>";
        }

        return GetHierarchyPath(eventData.pointerCurrentRaycast.gameObject.transform);
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return "<null>";
        }

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
