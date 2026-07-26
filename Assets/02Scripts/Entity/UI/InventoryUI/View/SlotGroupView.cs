using System;
using System.Collections.Generic;
using UnityEngine;

public class SlotGroupView : MonoBehaviour
{
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private SlotViewBase _slotPrefab;

    public List<SlotViewBase> SlotViewList => _slotViewList;
    private readonly List<SlotViewBase> _slotViewList = new();

    public event Action OnRequestAddSlot;

    // SlotView 단일 생성
    // 로직 생성 성공 후 Presenter가 호출
    public SlotViewBase AddSlot()
    {
        if (_contentRoot == null || _slotPrefab == null)
            return null;

        SlotViewBase slotView = Instantiate(_slotPrefab, _contentRoot);

        slotView.Clear();
        _slotViewList.Add(slotView);

        return slotView;
    }

    // AddSlotBtn에서 호출 (AddSlot)
    public void RequestAddSlot()
    {
        OnRequestAddSlot?.Invoke();
    }
}
