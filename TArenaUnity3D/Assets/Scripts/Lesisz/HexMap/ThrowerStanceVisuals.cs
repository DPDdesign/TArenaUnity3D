using UnityEngine;

public class ThrowerStanceVisuals : MonoBehaviour
{
    [SerializeField] Transform leftOneHandAxe;
    [SerializeField] Transform rightOneHandAxe;
    [SerializeField] Transform meleeTwoHandAxe;

    void Awake()
    {
        SetRangedStance(true);
    }

    public void SetRangedStance(bool isRanged)
    {
        SetActive(leftOneHandAxe, isRanged);
        SetActive(rightOneHandAxe, isRanged);
        SetActive(meleeTwoHandAxe, !isRanged);
    }

    static void SetActive(Transform target, bool isActive)
    {
        if (target == null)
        {
            return;
        }

        target.gameObject.SetActive(isActive);
    }
}
