using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public PlayerCore _playerCore;
    private Dictionary<Type, UIWindow> _uiDict;

    private UIWindow _currentUI;
    public bool IsCombatBlocked
    {
        get
        {
            foreach (UIWindow ui in _uiDict.Values)
            {
                if (!ui.gameObject.activeSelf)
                    continue;

                if (ui.IsBlockCombat)
                    return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        _uiDict = new();
    }

    public void Bind(PlayerCore _core)
    {
        _playerCore = _core;
    }

    private void Start()
    {
        foreach (Transform child in transform)
        {
            UIWindow ui = child.GetComponent<UIWindow>();

            if (ui == null)
                continue;

            Register(ui); ;
        }

        AllClose();

        Open<CrossHairUI>();
        Open<StateUI>();
    }

    // 등록
    public void Register(UIWindow p_ui)
    {
        _uiDict[p_ui.GetType()] = p_ui;
    }

    // 해당 UI 받아서 필요한 기능 있거나 연동되어야할 일이 있을 때 사용(BaseUI를 상속받은)
    // var inventoryUI = _uiManager.Get<InventoryUI>(); 이런식으로
    public T Get<T>() where T : UIWindow
    {
        if (_uiDict.TryGetValue(typeof(T), out var ui))
            return (T)ui;

        return null;
    }

    public void Open<T>() where T : UIWindow
    {
        UIWindow target = Get<T>();

        if (_currentUI != null)
            _currentUI.Close();

        _uiDict[typeof(T)].Open();
    }

    public void Close<T>() where T : UIWindow
    {
        _uiDict[typeof(T)].Close();
    }

    public void AllClose()
    {
        foreach(var ui in _uiDict)
        {
            ui.Value.Close();
        }
    }



}
