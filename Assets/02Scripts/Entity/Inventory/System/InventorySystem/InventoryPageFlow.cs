using System;
using Unity.VisualScripting;
using UnityEngine;

public enum EInventoryPage
{
    Closed,
    Category,
    Weapon,
    Armor,
    Consumable,
    Material,
    Quest
}

public class InventoryPageFlow : MonoBehaviour
{
    public EInventoryPage CurrentPage => _currentPage;
    private EInventoryPage _currentPage;

    public event Action<EInventoryPage> HandlePageChanged;
    public event Action<bool> OnInventoryActiveChanged;

    private void Start()
    {
        _currentPage = EInventoryPage.Quest;
        ClosePage();
    }

    private void Update()
    {
    }

    public void OpenPage(int p_page)
    {
        if (!CanOpen((EInventoryPage)p_page)) return;

        ChangePage((EInventoryPage)p_page);
    }

    public bool CanOpen(EInventoryPage p_page)
    {
        // 닫혀있다가 오픈할때는 무조건 Category 페이지로 시작
        if (CurrentPage == EInventoryPage.Closed)
            return p_page == EInventoryPage.Category;

        if (CurrentPage == EInventoryPage.Category)
            return true;

        // Inventory에서는 Category로만 이동 가능
        return p_page == EInventoryPage.Category;
    }

    public void ChangePage(EInventoryPage p_page)
    {
        if (_currentPage == p_page) return;

        _currentPage = p_page;
        HandlePageChanged?.Invoke(_currentPage);
        OnInventoryActiveChanged?.Invoke(_currentPage != EInventoryPage.Closed);
    }

    public void ClosePage()
    {
        ChangePage(EInventoryPage.Closed);
    }
}
