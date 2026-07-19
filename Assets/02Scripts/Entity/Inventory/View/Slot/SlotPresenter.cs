using UnityEngine;

public class SlotPresenter : ISlotDragHandler
{
    private readonly SlotBase _slot;
    private readonly SlotBaseUI _view;
    private readonly ResourceLoadSystem _resourceLoader;
    private readonly SlotTransferSystem _transferSystem;

    public SlotPresenter(SlotBase p_slot, SlotBaseUI p_view, 
                         ResourceLoadSystem p_resourceLoader, SlotTransferSystem p_transferSystem)
    {
        _slot = p_slot;
        _view = p_view;
        _resourceLoader = p_resourceLoader;
        _transferSystem = p_transferSystem;

        _slot.Changed += OnSlotChanged;
        _view.BindDragHandler(this);

        Refresh();
    }

    // Item을 대상 Slot으로 이동한다.
    public bool TryMoveTo(ISlotDragHandler p_target)
    {
        if (p_target is not SlotPresenter targetPresenter || targetPresenter == this)
        {
            return false;
        }

        return _transferSystem.TryMove(_slot, targetPresenter._slot);
    }

    // Slot 변경 내용을 UI에 반영한다.
    private void OnSlotChanged(SlotBase p_slot)
    {
        Refresh();
    }

    // 현재 Slot 내용을 표시한다.
    public void Refresh()
    {
        if (_slot.IsEmpty)
        {
            _view.Clear();
            return;
        }

        Sprite icon = _resourceLoader.GetIcon(_slot.Item.ItemType, _slot.Item.IconKey);

        _view.Show(icon, _slot.Count);
    }
}
