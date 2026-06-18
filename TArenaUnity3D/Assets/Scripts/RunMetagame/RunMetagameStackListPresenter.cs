using System.Collections.Generic;
using UnityEngine;

public static class RunMetagameStackListPresenter
{
    public static void DisplayStackInfo(
        Transform rowsParent,
        GameObject rowPrefab,
        IList<StackInfoData> stacks,
        List<StackRepresentation> rowInstances)
    {
        if (rowInstances == null)
        {
            Debug.LogWarning("[RunMetagameStackListPresenter] Row instance list is missing.");
            return;
        }

        if (rowInstances.Count == 0)
        {
            ClearExistingRows(rowsParent);
        }

        int stackCount = stacks == null ? 0 : stacks.Count;
        EnsureRowCount(rowsParent, rowPrefab, stackCount, rowInstances);

        for (int i = 0; i < rowInstances.Count; i++)
        {
            StackRepresentation row = rowInstances[i];
            if (row == null)
            {
                continue;
            }

            StackInfoData info = i < stackCount ? stacks[i] : null;
            SetRowRootActive(rowsParent, row.transform, info != null);
            row.DisplayStackInfo(info);
        }
    }

    private static void ClearExistingRows(Transform rowsParent)
    {
        if (rowsParent == null)
        {
            return;
        }

        for (int i = rowsParent.childCount - 1; i >= 0; i--)
        {
            Transform child = rowsParent.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
        }
    }

    private static void EnsureRowCount(
        Transform rowsParent,
        GameObject rowPrefab,
        int requiredCount,
        List<StackRepresentation> rowInstances)
    {
        if (rowsParent == null || rowPrefab == null)
        {
            if (requiredCount > 0)
            {
                Debug.LogWarning("[RunMetagameStackListPresenter] Cannot instantiate army stack rows because rows parent or row prefab is not assigned.");
            }

            return;
        }

        while (rowInstances.Count < requiredCount)
        {
            Object instantiated = Object.Instantiate((Object)rowPrefab, rowsParent);
            GameObject rowObject = instantiated as GameObject;
            if (rowObject == null)
            {
                Debug.LogWarning("[RunMetagameStackListPresenter] Army stack row prefab reference did not instantiate as a GameObject.");
                rowInstances.Add(null);
                continue;
            }

            rowObject.name = "ArmyStackRow_" + (rowInstances.Count + 1).ToString("00");
            StackRepresentation row = rowObject.GetComponent<StackRepresentation>();
            if (row == null)
            {
                row = rowObject.GetComponentInChildren<StackRepresentation>(true);
            }

            if (row == null)
            {
                Debug.LogWarning("[RunMetagameStackListPresenter] Army stack row prefab does not contain StackRepresentation.");
                rowObject.SetActive(false);
            }

            rowInstances.Add(row);
        }
    }

    private static void SetRowRootActive(Transform rowsParent, Transform rowTransform, bool active)
    {
        if (rowsParent == null || rowTransform == null)
        {
            return;
        }

        Transform rowRoot = rowTransform;
        while (rowRoot.parent != null && rowRoot.parent != rowsParent)
        {
            rowRoot = rowRoot.parent;
        }

        if (rowRoot.parent == rowsParent)
        {
            rowRoot.gameObject.SetActive(active);
        }
    }
}
