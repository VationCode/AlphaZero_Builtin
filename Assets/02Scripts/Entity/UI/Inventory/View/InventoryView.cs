using System;
using UnityEngine;

public enum EInventoryPage  // 순서 배치 지킬것
{
    Category,
    Weapon,
    Armor,
    Consumable,
    Material,
    QuestItem
}
namespace Alpha.Inventory
{
    /// <summary>
    /// 입력에 따른 창 Toggle
    /// Page 열기·닫기
    /// Window 표시
    /// 활성 상태 이벤트 전달
    /// </summary>
    public class InventoryView : MonoBehaviour
    {
        private AlphaInputSystem _input;
        private InventoryPageView _currentPage;
        
        [Header("Window")]
        [SerializeField] private GameObject _windowRoot;

        [Header("Pages")]
        [SerializeField] private InventoryPageView[] _pages;
        public bool IsOpen { get; private set; }

        public event Action<bool> OnWindowActiveChanged;    // 창 On/Off

        private void Awake()
        {
            _pages = GetComponentsInChildren<InventoryPageView>(true);

            // 이벤트 없이 초기 화면만 닫기
            foreach (InventoryPageView page in _pages)
            {
                if (page != null)
                    page.Initialize();
            }

            ClosedAllPage();

            IsOpen = false;
            _windowRoot.SetActive(false);
        }

        public void Bind(AlphaInputSystem p_input)
        {
            _input = p_input;
        }

        private void Update()
        {
            if (_input != null && _input.IsInventory)
            {
                SetWindowActive(!IsOpen);
            }
        }

        // 키보드에 의한 인벤토리창 활성화 조작
        internal void SetWindowActive(bool p_isActive)
        {
            if (IsOpen == p_isActive)
                return;

            IsOpen = p_isActive;

            if (IsOpen)
            {
                _windowRoot.SetActive(true);
                OpenPage((int)EInventoryPage.Category);
            }
            else
            {
                CloseCurrentPage();
                _windowRoot.SetActive(false);
            }

            // Mouse, Combat 차단 등 외부 반응에 창 상태를 전달한다.
            OnWindowActiveChanged?.Invoke(IsOpen);
        }

        // UI 버튼에서 직접 호출한다.
        public void OnRequestSetWindowActive(bool p_isActive)
        {
            SetWindowActive(p_isActive);
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