using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryPageView : ViewBase
{
    [SerializeField] private Transform[] _contentRoots;
    [SerializeField] private SlotViewBase _slotPrefab;

    private readonly List<List<SlotViewBase>> _slotViewGroupList = new();

    public event Action<int> OnRequestAddSlot;

    public void RequestAddSlot(int p_groupIndex)
    {
        if (p_groupIndex < 0 || p_groupIndex >= _contentRoots.Length)
        {
            Debug.LogError($"{name}: 잘못된 슬롯 그룹입니다.");
            return;
        }

        // UI는 추가를 요청만 함
        OnRequestAddSlot?.Invoke(p_groupIndex);
    }
    private void EnsureSlotViewGroups()
    {
        while (_slotViewGroupList.Count < _contentRoots.Length)
            _slotViewGroupList.Add(new List<SlotViewBase>());
    }

    public IReadOnlyList<SlotViewBase> AddSlotView(int p_groupIndex, int p_count)
    {
        EnsureSlotViewGroups();

        // Content 위치와 생성 개수 검사
        if (p_groupIndex < 0 || p_groupIndex >= _contentRoots.Length || p_count <= 0)
        {
            return Array.Empty<SlotViewBase>();
        }

        Transform contentRoot = _contentRoots[p_groupIndex];

        if (contentRoot == null || _slotPrefab == null)
            return Array.Empty<SlotViewBase>();

        // 이번 호출에서 생성된 View만 보관
        List<SlotViewBase> createdViews = new();

        for (int i = 0; i < p_count; i++)
        {
            SlotViewBase slotView = Instantiate(_slotPrefab, contentRoot);

            slotView.Clear();

            // 해당 그룹의 전체 View 목록에 추가
            _slotViewGroupList[p_groupIndex].Add(slotView);

            // Installer에 반환할 신규 View 목록
            createdViews.Add(slotView);
        }

        return createdViews;
    }
}
