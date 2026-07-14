using UnityEngine;

public class SlotPresenter : ISlotDragHandler
{
    private readonly SlotBase _slot;
    private readonly SlotBaseUI _view;
    private readonly ResourceLoadSystem _resourceLoader;
    private readonly SlotTransferSystem _transferSystem;

    public SlotPresenter
        (SlotBase p_slot, SlotBaseUI p_view, ResourceLoadSystem p_resourceLoader, SlotTransferSystem p_transferSystem)
    {
        _slot = p_slot;
        _view = p_view;
        _resourceLoader = p_resourceLoader;
        _transferSystem = p_transferSystem;

        _view.BindDragHandler(this);
    }
    public bool TryMoveTo(ISlotDragHandler p_target)
    {
        SlotPresenter targetPresenter = p_target as SlotPresenter;

        if (targetPresenter == null || targetPresenter == this)
            return false;

        bool isMoved = _transferSystem.TryMove(
            _slot,
            targetPresenter._slot);

        if (!isMoved)
            return false;

        Refresh();
        targetPresenter.Refresh();

        return true;
    }

    public void Refresh()
    {
        if (_slot.IsEmpty)
        {
            _view.Clear();
            return;
        }

        Sprite icon =
            _resourceLoader.GetIcon(_slot.Item.IconKey);

        _view.Show(icon, _slot.Count);
    }
}
