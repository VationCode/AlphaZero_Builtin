using System;
using Alpha.Enemy.Animation;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace Alpha.Enemy.View
{
    // CrabBoss/Intro가 소유한 Animator, Timeline, DollyCam 표현 생명주기를 관리한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class CrabBossIntroView : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector _director;

        [SerializeField]
        private CinemachineCamera _dollyCamera;

        [SerializeField]
        private Transform _lookAtTarget;

        [SerializeField]
        private EnemyAnimationView _bossAnimationView;

        private const string BossIntroStatePath = "Base Layer.Intro1";

        private CinemachineBrain _brain;
        private CinemachineVirtualCameraBase _previousVirtualCamera;
        private CameraTarget _previousCameraTarget;
        private DirectorUpdateMode _previousUpdateMode;
        private Vector3 _previousOutputCameraPosition;
        private Quaternion _previousOutputCameraRotation;
        private Vector3 _previousDollyLocalPosition;
        private Quaternion _previousDollyLocalRotation;
        private Action<bool> _onFinished;
        private bool _previousIgnoreTimeScale;
        private bool _hasOutputCameraPose;
        private bool _hasDollyCameraPose;
        private bool _hasCameraTarget;
        private bool _hasUpdateMode;
        private bool _hasBrainSetting;
        private bool _wasCancelled;

        public bool IsPlaying { get; private set; }
        public bool IsConfigured =>
            _director != null &&
            _director.playableAsset != null &&
            _dollyCamera != null &&
            _lookAtTarget != null &&
            _bossAnimationView != null;

        private void Awake()
        {
            _director ??= GetComponent<PlayableDirector>();
            ResolveBossAnimationView();
            SetDollyCameraActive(false);
        }

        private void OnEnable()
        {
            SubscribeDirector();
        }

        public void BindCamera(CinemachineBrain p_brain)
        {
            _brain = p_brain;
        }

        public bool TryPlay(Action<bool> p_onFinished)
        {
            if (IsPlaying || !IsConfigured)
                return false;

            // Timeline과 같은 Frame에 CrabBoss Animator의 Intro1부터 시작한다.
            if (!_bossAnimationView.PlayCinematic(BossIntroStatePath))
                return false;

            SubscribeDirector();
            _onFinished = p_onFinished;
            _wasCancelled = false;

            ApplyPlaybackView();
            _director.time = 0d;
            IsPlaying = true;
            _director.Play();
            return true;
        }

        public void Cancel()
        {
            if (!IsPlaying)
                return;

            _wasCancelled = true;
            _director.Stop();

            // stopped 이벤트가 발생하지 않는 Director 상태도 즉시 정리한다.
            if (IsPlaying)
                FinishPlayback();
        }

        private void ApplyPlaybackView()
        {
            _previousUpdateMode = _director.timeUpdateMode;
            _director.timeUpdateMode =
                DirectorUpdateMode.UnscaledGameTime;
            _hasUpdateMode = true;

            _previousCameraTarget = _dollyCamera.Target;
            CameraTarget target = _dollyCamera.Target;
            target.LookAtTarget = _lookAtTarget;
            target.CustomLookAtTarget = true;
            _dollyCamera.Target = target;
            _hasCameraTarget = true;

            Transform dollyTransform = _dollyCamera.transform;
            _previousDollyLocalPosition = dollyTransform.localPosition;
            _previousDollyLocalRotation = dollyTransform.localRotation;
            _hasDollyCameraPose = true;

            if (_brain != null)
            {
                _previousVirtualCamera =
                    _brain.ActiveVirtualCamera as
                        CinemachineVirtualCameraBase;

                Transform outputCamera = _brain.ControlledObject.transform;
                _previousOutputCameraPosition = outputCamera.position;
                _previousOutputCameraRotation = outputCamera.rotation;
                _hasOutputCameraPose = true;

                _previousIgnoreTimeScale = _brain.IgnoreTimeScale;
                _brain.IgnoreTimeScale = true;
                _hasBrainSetting = true;
            }

            SetDollyCameraActive(true);
        }

        private void HandleDirectorStopped(PlayableDirector p_director)
        {
            if (IsPlaying && ReferenceEquals(_director, p_director))
                FinishPlayback();
        }

        private void FinishPlayback()
        {
            bool wasCancelled = _wasCancelled;
            Action<bool> callback = _onFinished;

            IsPlaying = false;
            _wasCancelled = false;
            _onFinished = null;
            RestorePlaybackView();
            callback?.Invoke(wasCancelled);
        }

        private void RestorePlaybackView()
        {
            SetDollyCameraActive(false);

            if (_hasDollyCameraPose && _dollyCamera != null)
            {
                _dollyCamera.transform.SetLocalPositionAndRotation(
                    _previousDollyLocalPosition,
                    _previousDollyLocalRotation);
            }

            if (_hasCameraTarget && _dollyCamera != null)
                _dollyCamera.Target = _previousCameraTarget;

            if (_hasUpdateMode && _director != null)
                _director.timeUpdateMode = _previousUpdateMode;

            if (_brain != null)
            {
                if (_hasOutputCameraPose)
                {
                    // 기존 Virtual Camera 상태와 실제 Render Camera Pose를 함께 복구한다.
                    _previousVirtualCamera?.ForceCameraPosition(
                        _previousOutputCameraPosition,
                        _previousOutputCameraRotation);
                    _brain.ResetState();
                    _brain.ControlledObject.transform
                        .SetPositionAndRotation(
                            _previousOutputCameraPosition,
                            _previousOutputCameraRotation);
                }

                if (_hasBrainSetting)
                    _brain.IgnoreTimeScale = _previousIgnoreTimeScale;
            }

            _previousVirtualCamera = null;
            _hasOutputCameraPose = false;
            _hasDollyCameraPose = false;
            _hasCameraTarget = false;
            _hasUpdateMode = false;
            _hasBrainSetting = false;
        }

        private void SubscribeDirector()
        {
            if (_director == null)
                return;

            _director.stopped -= HandleDirectorStopped;
            _director.stopped += HandleDirectorStopped;
        }

        private void SetDollyCameraActive(bool p_isActive)
        {
            if (_dollyCamera != null &&
                _dollyCamera.gameObject.activeSelf != p_isActive)
            {
                _dollyCamera.gameObject.SetActive(p_isActive);
            }
        }

        private void ResolveBossAnimationView()
        {
            if (_bossAnimationView != null || transform.parent == null)
                return;

            _bossAnimationView = transform.parent
                .GetComponentInChildren<EnemyAnimationView>(true);
        }

        private void OnDisable()
        {
            if (_director != null)
                _director.stopped -= HandleDirectorStopped;

            if (!IsPlaying)
                return;

            _wasCancelled = true;
            FinishPlayback();
        }

        private void OnValidate()
        {
            _director ??= GetComponent<PlayableDirector>();
            _dollyCamera ??=
                GetComponentInChildren<CinemachineCamera>(true);
            ResolveBossAnimationView();
        }
    }
}
