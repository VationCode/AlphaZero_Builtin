using Alpha.Player.Combat;
using TMPro;
using UnityEngine;

// 현재 이동 Mode·전투 상태·Camera View 종류를 Text UI로 표시한다.
public class StateUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _locoTMP;
    [SerializeField] private TextMeshProUGUI _combatTMP;
    [SerializeField] private TextMeshProUGUI _viewTMP;

    // 이동 Mode와 세부 상태를 이동 상태 Text에 표시한다.
    public void ChangeLocoState(ELocomotionMode p_mode, ELocoStateType p_state)
    {
        _locoTMP.text = $"{p_mode} / {p_state}";
    }

    // 전투 상태를 전투 상태 Text에 표시한다.
    public void ChangeCombatState(ECombatStateType p_state)
    {
        _combatTMP.text = $"{p_state}";
    }

    // 현재 Camera View 종류를 View 상태 Text에 표시한다.
    public void ChangeViewType(ECameraViewType p_viewType)
    {
        _viewTMP.text = $"{p_viewType}";
    }
}
