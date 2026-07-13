using System.Collections.Generic;
using UnityEngine;

public class InventoryBase : MonoBehaviour
{
    public IReadOnlyList<SlotBase> Slots => _SlotList;
    protected List<SlotBase> _SlotList = new();
}
