using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RunMapNodeTypeIconCatalog", menuName = "TArena/Run Map/Node Type Icon Catalog")]
public class RunMapNodeTypeIconCatalog : ScriptableObject
{
    [SerializeField] private List<RunMapNodeTypeIconEntry> entries = new List<RunMapNodeTypeIconEntry>();

    public List<RunMapNodeTypeIconEntry> Entries
    {
        get { return entries; }
        set { entries = value ?? new List<RunMapNodeTypeIconEntry>(); }
    }

    public Sprite FindIcon(RunMapNodeType nodeType)
    {
        if (entries == null)
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RunMapNodeTypeIconEntry entry = entries[i];
            if (entry != null && entry.NodeType == nodeType)
            {
                return entry.Icon;
            }
        }

        return null;
    }
}

[Serializable]
public class RunMapNodeTypeIconEntry
{
    public RunMapNodeType NodeType;
    public Sprite Icon;

    public RunMapNodeTypeIconEntry(RunMapNodeType nodeType, Sprite icon)
    {
        NodeType = nodeType;
        Icon = icon;
    }
}
