using System;
using UnityEngine;

public enum EInventoryPage  // 순서 배치 지킬것
{
    Category,
    Weapon,
    Armor,
    Consumable,
    Material,
    Quest
}
namespace Alpha.UI.Inventory
{
    public class PlayerInventoryView : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private GameObject _windowRoot;

        [Header("Pages")]
        [SerializeField] private InventoryPageView[] _pages;

        private InventoryPageView _currentPage;

        public event Action<bool> OnRequestWindowActive;
        private void Awake()
        {
            _pages = GetComponentsInChildren<InventoryPageView>(true);

            ClosedAllPage();
            SetWindowActive(false);
        }

        // 키보드에 의한 인벤토리창 활성화 조작
        internal void SetWindowActive(bool p_isActive)
        {
            if (p_isActive)
            {
                _windowRoot.SetActive(true);
                OpenPage((int)EInventoryPage.Category);
                return;
            }

            CloseCurrentPage();
            _windowRoot.SetActive(false);
        }

        // 버튼에 의한 인벤토리창 조작
        public void OnRequestSetWindowActive(bool p_isActive)
        {
            OnRequestWindowActive?.Invoke(p_isActive);
        }

        // 각 페이지들
        public void OpenPage(int enumType)
        {
            InventoryPageView nextPage = _pages[enumType];

            if (nextPage == null || nextPage == _currentPage)
                return;

            CloseCurrentPage();

            _currentPage = nextPage;
            _currentPage.Open();
        }

        private void CloseCurrentPage()
        {
            if (_currentPage == null)
                return;

            _currentPage.Close();
            _currentPage = null;
        }

        private void ClosedAllPage()
        {
            foreach (InventoryPageView page in _pages)
            {
                if (page != null)
                    page.Close();
            }
            _currentPage = null;
        }

        // 페이지 조회
        public InventoryPageView GetPage(EInventoryPage p_pageType)
        {
            int pageIndex = (int)p_pageType;

            if (pageIndex < 0 || pageIndex >= _pages.Length)
            {
                Debug.LogError($"잘못된 Inventory Page입니다: {p_pageType}");
                return null;
            }

            return _pages[pageIndex];
        }
    }
}