using System;
using System.Collections.Generic;
using UnityEngine;

// EUIType 관련 선택 값을 정의한다.
public enum EUIType
{
    None,
    InventoryUI,
    EquipmentUI,
    StateUI,
    CrossHairUI,
    OptionUI,
    ETC
}

// EUIFlag 관련 선택 값을 정의한다.
[Flags]
public enum EUIFlag
{
    None = 0,
    CategoryUI = 1 << 0,
    InventoryUI = 1 << 1,
    EquipmentUI = 1 << 2,
    StateUI = 1 << 3,
    CrossHairUI = 1 << 4,
    OptionUI = 1 << 5,
    ETC = 1 << 6
}

// UI 종류와 함께 열림 Animation·전투 차단 정보를 보유하는 공통 Window이다.
public abstract class UIWindow : MonoBehaviour
{
    public Animation m_UIOpenAnim;

    public EUIType UIType;

    public EUIFlag CloseTargetUIs;

    public bool IsBlockCombat;
    // 선택적 열림 Animation을 재생하고 Window를 활성화한다.
    public virtual void Open()
    {
        if (m_UIOpenAnim)
        {
            m_UIOpenAnim.Play();
        }

        this.gameObject.SetActive(true);
    }
    // Window GameObject를 비활성화한다.
    public virtual void Close() 
    {
        this.gameObject.SetActive(false);
    }
}
