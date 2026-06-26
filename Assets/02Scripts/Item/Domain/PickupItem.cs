using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [SerializeField]
    private int _itemId;

    public int ItemId => _itemId;
}
