using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class SlotBase : MonoBehaviour
{
    public ItemDataDTO ItemData;
    public Image Icon;
    public TextMeshProUGUI CountTMP;
    public int StackCount;
    public int Index;

    public virtual void SetItem(ItemDataDTO p_ItemData)
    {
        ItemData = p_ItemData;
        Sprite icon = ResourceDBLoadModule.Instance.GetIcon(ItemData.IconKey);
        Icon.sprite = icon;
        Icon.gameObject.SetActive(true);
    }

    public void SetCount()
    {
        StackCount++;
        CountTMP.text = StackCount.ToString();
    }

    public virtual void Clear()
    {
        ItemData = null;
        Icon.sprite = null;
        StackCount = 0;
        CountTMP.text = "";
        Icon.gameObject.SetActive(false);
    }
}
