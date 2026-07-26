namespace Alpha.UI
{
    // View의 요청을 SlotBase.TryMoveTo()로 전달
    public class SlotDragHandler
    {
        private readonly SlotBase _slot;

        public SlotDragHandler(SlotBase p_slot)
        {
            _slot = p_slot;
        }

        public bool TryMoveTo(SlotDragHandler p_target)
        {
            return p_target is SlotDragHandler target && _slot.TryMoveTo(target._slot);
        }
    }
}