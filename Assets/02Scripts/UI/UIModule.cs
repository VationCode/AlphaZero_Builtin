using TMPro;
using UnityEngine;

public class UIModule : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _locoTMP;
    [SerializeField] private TextMeshProUGUI _combatTMP;
    [SerializeField] private TextMeshProUGUI _viewTMP;
    public void ChangeLocoText(string p_text)
    {
        _locoTMP.text = p_text;
    }

    public void ChangeCombatText(string p_text)
    {
        _combatTMP.text = p_text;
    }

    public void ChangeViewText(string p_text)
    {
        _viewTMP.text = p_text;
    }
}
