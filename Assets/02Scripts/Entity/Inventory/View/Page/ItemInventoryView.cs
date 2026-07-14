using UnityEngine;

public class ItemInventoryView : ViewBase
{
    [SerializeField]
    private SlotBaseUI _slotPrefab;

    [SerializeField]
    private Transform _content;

    public SlotBaseUI CreateSlotView()
    {
        if (_slotPrefab == null || _content == null)
            return null;

        return Instantiate(_slotPrefab, _content);
    }
}
