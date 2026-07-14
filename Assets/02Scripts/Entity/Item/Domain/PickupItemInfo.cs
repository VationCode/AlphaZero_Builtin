using UnityEngine;

public class PickupItemInfo : MonoBehaviour
{
    [SerializeField]
    private int _itemId;
    [SerializeField]
    private int _count = 1;
    [SerializeField]
    private EItemType _itemType;

    public int ItemId => _itemId;
    public int Count => _count;
    public EItemType ItemType => _itemType;
}
