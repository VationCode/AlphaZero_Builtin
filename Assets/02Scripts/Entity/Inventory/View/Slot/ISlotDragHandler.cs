using UnityEngine;

public interface ISlotDragHandler
{
    bool TryMoveTo(ISlotDragHandler p_target);
}
