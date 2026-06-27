using System;
using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;

public class InventoryUI : UIWindow
{
    private UIPanel _currentPanel;

    /*[SerializeField] private CategoryInventoryUI CategoryUI;
    [SerializeField] private WeaponInventoryUI WeaponUI;
    [SerializeField] private ArmorInventoryUI ArmorUI;
    [SerializeField] private PotionInventoryUI PotionUI;
    [SerializeField] private MaterialInventoryUI MaterialUI;
    [SerializeField] private QuestInventoryUI QuestUI;*/

    private Dictionary<Type, UIPanel> _uiDict;

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            UIPanel ui = child.GetComponent<UIPanel>();

            if (ui == null)
                continue;

            Register(ui);
        }
    }

    public void Register(UIPanel p_ui)
    {
        _uiDict[p_ui.GetType()] = p_ui;
    }

    // 해당 UI 받아서 필요한 기능 있거나 연동되어야할 일이 있을 때 사용(BaseUI를 상속받은)
    // var inventoryUI = _uiManager.Get<InventoryUI>(); 이런식으로
    public T Get<T>() where T : UIPanel
    {
        if (_uiDict.TryGetValue(typeof(T), out var ui))
            return (T)ui;

        return null;
    }

    public override void Open()
    {
        base.Open();
        Refresh();
    }

    private void Refresh()
    {
        AllClose();

        CategoryInventoryUI category = Get<CategoryInventoryUI>();
        category.Show();
    }
    public override void Close()
    {
        base.Close();
    }

    public void AllClose()
    {
        foreach(var ui in _uiDict)
        {
            ui.Value.Hide();
        }
    }
}
