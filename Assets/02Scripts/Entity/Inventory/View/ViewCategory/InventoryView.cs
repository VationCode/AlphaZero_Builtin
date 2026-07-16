using System.Collections.Generic;
using UnityEngine;
using System;

public enum EInventoryView
{
    Closed,
    Category,
    Weapon,
    Armor,
    Consumable,
    Material,
    Quest
}
public class InventoryView : MonoBehaviour
{
    [SerializeField] private ViewBase _category;
    [SerializeField] private ViewBase _weaponInventory;
    [SerializeField] private ViewBase _armorInventory;
    [SerializeField] private ViewBase _consumableInventory;
    [SerializeField] private ViewBase _materialInventory;
    [SerializeField] private ViewBase _questInventory;

    private Dictionary<EInventoryView, ViewBase> _viewDict;

    public EInventoryView CurrentView { get; private set; }
    public bool IsOpen => CurrentView != EInventoryView.Closed;

    public WeaponInventoryView WeaponView =>
        _weaponInventory as WeaponInventoryView;

    public ArmorInventoryView ArmorView =>
        _armorInventory as ArmorInventoryView;

    public ItemInventoryView ConsumableView =>
    _consumableInventory as ItemInventoryView;

    public ItemInventoryView MaterialView =>
        _materialInventory as ItemInventoryView;

    public ItemInventoryView QuestView =>
        _questInventory as ItemInventoryView;

    public event Action<bool> OpenStateChanged;
    private void Awake()
    {
        _viewDict = new Dictionary<EInventoryView, ViewBase>
        {
            { EInventoryView.Category, _category },
            { EInventoryView.Weapon, _weaponInventory },
            { EInventoryView.Armor, _armorInventory },
            { EInventoryView.Consumable, _consumableInventory },
            { EInventoryView.Material, _materialInventory },
            { EInventoryView.Quest, _questInventory }
        };

        CurrentView = EInventoryView.Closed;
        ApplyView();
    }

    public void OpenView(int p_view)
    {
        EInventoryView page = (EInventoryView)p_view;

        if (!CanOpen(page))
            return;

        ChangeView(page);
    }

    public void CloseView()
    {
        ChangeView(EInventoryView.Closed);
    }

    private bool CanOpen(EInventoryView p_view)
    {
        if (CurrentView == EInventoryView.Closed)
            return p_view == EInventoryView.Category;

        if (CurrentView == EInventoryView.Category)
            return p_view != EInventoryView.Closed;

        return p_view == EInventoryView.Category;
    }

    private void ChangeView(EInventoryView p_view)
    {
        if (CurrentView == p_view)
            return;

        bool wasOpen = IsOpen;

        CurrentView = p_view;
        ApplyView();

        bool isOpen = IsOpen;

        // 카테고리 간 이동에서는 호출하지 않고,
        // 실제 열기 또는 닫기에서만 호출한다.
        if (wasOpen != isOpen)
        {
            OpenStateChanged?.Invoke(isOpen);
        }
    }

    private void ApplyView()
    {
        foreach (var view in _viewDict)
        {
            bool isOpen = view.Key == CurrentView;

            view.Value.gameObject.SetActive(isOpen);

            if (isOpen)
                view.Value.Open();
            else
                view.Value.Close();
        }
    }
}
