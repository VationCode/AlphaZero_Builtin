using Alpha.Player.Combat;
using TMPro;
using UnityEngine;

public class StateUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _locoTMP;
    [SerializeField] private TextMeshProUGUI _combatTMP;
    [SerializeField] private TextMeshProUGUI _viewTMP;

    public void ChangeLocoState(ELocomotionMode p_mode, ELocoStateType p_state)
    {
        _locoTMP.text = $"{p_mode} / {p_state}";
    }

    public void ChangeCombatState(ECombatStateType p_state)
    {
        _combatTMP.text = $"{p_state}";
    }

    public void ChangeViewType(ECameraViewType p_viewType)
    {
        _viewTMP.text = $"{p_viewType}";
    }
}
