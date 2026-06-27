using System;
using System.Collections.Generic;
using UnityEngine;

public enum EUIType
{
    CategoryUI,
    InventoryUI,
    EquipmentUI,
    StateUI,
    CrossHairUI,
    OptionUI,
    ETC
}

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

public abstract class UIWindow : MonoBehaviour
{
    public Animation m_UIOpenAnim;

    public EUIType UIType;

    public EUIFlag CloseTargetUIs;

    public bool IsBlockCombat;
    public virtual void Open()
    {
        if (m_UIOpenAnim)
        {
            m_UIOpenAnim.Play();
        }

        this.gameObject.SetActive(true);
    }
    public virtual void Close() 
    {
        this.gameObject.SetActive(false);
    }
}
