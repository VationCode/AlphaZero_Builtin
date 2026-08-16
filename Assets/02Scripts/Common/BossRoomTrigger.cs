using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using Alpha.Enemy.CrabBoss;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private CrabBossCore _crabBossCore;

    [Header("Intro")]
    [FormerlySerializedAs("_inputDisableDuration")]
    [SerializeField, Min(0.1f)]
    private float _introDuration = 5f;

    [SerializeField] private bool _skipIntro;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector _bossTimeline;

    [Header("Dolly Cam")]
    [SerializeField] private GameObject _dollyCam;
    [SerializeField] private Camera _mainCamera;

    [Header("Input")]
    [SerializeField] private AlphaInputSystem _inputSystem;

    private Coroutine _inputRestoreRoutine;
    private bool _hasTriggered;
    private bool _ownsInputLock;

    private void Awake()
    {
        _mainCamera ??= Camera.main;
    }

    private void OnTriggerEnter(Collider p_other)
    {
        if (!CanTrigger(p_other))
            return;

            if (!_crabBossCore.BeginEncounter(p_other.gameObject, _skipIntro))
            return;

        _hasTriggered = true;

        // Intro 스킵 시 Timeline과 입력 제한 없이 바로 전투를 시작한다.
        if (_skipIntro)
            return;

        _dollyCam.SetActive(true);
        DisableInput();
        PlayTimeline();

        _inputRestoreRoutine = StartCoroutine(RestoreAfterDelay());
    }

    // Player가 처음 진입했을 때만 Timeline을 실행한다.
    private bool CanTrigger(Collider p_other)
    {
        bool hasValidIntro =
            _skipIntro ||
            (_bossTimeline != null && _bossTimeline.playableAsset != null);

        return !_hasTriggered &&
            p_other.gameObject.layer ==
            LayerMask.NameToLayer("Player") &&
            _crabBossCore != null &&
            hasValidIntro;
    }

    // 보스 등장 Timeline을 처음부터 재생한다.
    private void PlayTimeline()
    {
        _bossTimeline.time = 0d;
        _bossTimeline.Play();
    }

    // 전체 입력 시스템을 일시적으로 비활성화한다.
    private void DisableInput()
    {
        if (_inputSystem == null || !_inputSystem.enabled)
            return;

        _inputSystem.enabled = false;
        _ownsInputLock = true;
    }

    private IEnumerator RestoreAfterDelay()
    {
        yield return new WaitForSeconds(_introDuration);

        if (_dollyCam != null)
            _dollyCam.SetActive(false);

        // DollyCamera가 Main Camera 제어를 해제할 때까지 기다린다.
        yield return null;

        ResetMainCameraLocalPosition();

        // Intro 종료 후 첫 전투 준비 상태인 Chase로 전환한다.
        _crabBossCore?.CompleteIntro();

        EnableInput();
        _inputRestoreRoutine = null;
    }

    private void ResetMainCameraLocalPosition()
    {
        if (_mainCamera == null)
            return;

        _mainCamera.transform.localPosition = Vector3.zero;
        _mainCamera.transform.localRotation = Quaternion.identity;
    }

    private void EnableInput()
    {
        if (_inputSystem == null || !_ownsInputLock)
            return;

        _inputSystem.enabled = true;
        _ownsInputLock = false;
    }

    // Coroutine 중단으로 입력이 계속 잠기는 상황을 방지한다.
    private void OnDisable()
    {
        if (_inputRestoreRoutine != null)
        {
            StopCoroutine(_inputRestoreRoutine);
            _inputRestoreRoutine = null;
        }

        if (_dollyCam != null)
            _dollyCam.SetActive(false);

        ResetMainCameraLocalPosition();
        EnableInput();
    }
}
