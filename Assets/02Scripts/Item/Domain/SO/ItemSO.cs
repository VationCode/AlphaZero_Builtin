using UnityEngine;

public class ItemSO : ScriptableObject
{
    public int ID;

    public string Name;

    public EItemType ItemType;

    public Sprite Icon;

    public GameObject Prefab;

    public bool IsStackable;

    public int MaxStackCount;

    [TextArea]
    public string Description;
}
