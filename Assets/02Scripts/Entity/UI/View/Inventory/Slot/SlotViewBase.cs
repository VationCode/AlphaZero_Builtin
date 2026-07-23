using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotViewBase : MonoBehaviour
{
    [SerializeField] private Image _itemIcon;
    [SerializeField] private TMP_Text _countText;

    private ResourceLoadSystem _resourceLoader;

    public void Bind(ResourceLoadSystem p_resourceLoader)
    {
        _resourceLoader = p_resourceLoader;
    }

    public void SetSlot(ItemDTO p_item, int p_count)
    {
        if (p_item == null)
        {
            Clear();
            return;
        }

        Sprite icon = _resourceLoader.GetIcon(p_item.ItemType, p_item.IconKey);

        _itemIcon.sprite = icon;
        _itemIcon.enabled = icon != null;

        _countText.text = p_count > 1? p_count.ToString() : string.Empty;
    }

    public void Clear()
    {
        _itemIcon.sprite = null;
        _itemIcon.enabled = false;
        _countText.text = string.Empty;
    }
}
