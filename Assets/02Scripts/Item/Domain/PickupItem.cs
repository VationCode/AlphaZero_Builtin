using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [SerializeField]
    private int _itemId;
    [SerializeField]
    private EItemType _itemType;

    public int ItemId => _itemId;
    public EItemType ItemType => _itemType;
}
