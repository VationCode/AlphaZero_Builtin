using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Alpha.Boss
{
    // Boss Cinematic의 Animator, Timeline, Camera 표현 생명주기를 관리한다.
    [DisallowMultipleComponent]
    public sealed class BossCinematicView : MonoBehaviour
    {
        private const string BossAnimationTrackName =
            "Crab Cinematic Animation";

        [SerializeField]
        private PlayableDirector _director;

        [SerializeField]
        private CinemachineCamera _cinematicCamera;

        [SerializeField]
        private BossAnimationView _bossAnimationView;

        [SerializeField]
        private TrackAsset _bossAnimationTrack;

        private CinemachineBrain _brain;
        private Transform _lookAtTarget;
        private Action<EBossCinematicPlayResult> _completionCallback;
        private EBossCinematicPlayResult _requestedResult;

        private CameraTarget _previousCameraTarget;
        private DirectorUpdateMode _previousUpdateMode;
        private Vector3 _previousCameraLocalPosition;
        private Quaternion _previousCameraLocalRotation;
        private Transform _controlledObject;
        private Vector3 _previousControlledObjectLocalPosition;
        private Quaternion _previousControlledObjectLocalRotation;
        private bool _previousBrainIgnoreTimeScale;
        private UnityEngine.Object _previousBossAnimationBinding;
        private bool _hasCachedPlaybackState;

        public bool IsPlaying { get; private set; }
        public bool IsConfigured =>
            string.IsNullOrEmpty(ConfigurationIssue);

        public string ConfigurationIssue
        {
            get
            {
                ResolveReferences();

                if (_director == null)
                    return "PlayableDirector가 없습니다.";
                if (_director.playableAsset == null)
                    return "Cinematic Timeline이 없습니다.";
                if (_cinematicCamera == null)
                    return "CinemachineCamera가 없습니다.";
                if (_bossAnimationView == null)
                    return "BossAnimationView가 없습니다.";
                if (_bossAnimationView.Animator == null)
                    return "Boss Animator가 없습니다.";
                if (_bossAnimationTrack == null)
                    return $"{BossAnimationTrackName} Track을 찾지 못했습니다.";
                if (_lookAtTarget == null)
                    return "Boss LookAt Target이 없습니다.";
                if (_brain == null)
                    return "CinemachineBrain을 찾지 못했습니다.";

                return string.Empty;
            }
        }

        private void Awake()
        {
            ResolveReferences();
            SetCinematicCameraActive(false);
        }

        public bool BindBossAnimation(
            BossAnimationView p_animationView,
            Transform p_lookAtTarget)
        {
            _bossAnimationView = p_animationView;
            _lookAtTarget = p_lookAtTarget;
            return _bossAnimationView != null &&
                   _lookAtTarget != null;
        }

        public bool BindCamera(CinemachineBrain p_brain)
        {
            ResolveReferences();
            _brain = p_brain;
            return _brain != null;
        }

        public bool TryPlay(
            Action<EBossCinematicPlayResult> p_onCompleted)
        {
            if (IsPlaying || !IsConfigured)
                return false;

            CachePlaybackState();

            if (!_bossAnimationView.BeginCinematic())
            {
                RestorePlaybackState();
                return false;
            }

            _director.SetGenericBinding(
                _bossAnimationTrack,
                _bossAnimationView.Animator);

            ApplyCameraTarget();
            SubscribeDirector();

            _requestedResult = EBossCinematicPlayResult.Completed;
            _completionCallback = p_onCompleted;
            _director.timeUpdateMode =
                DirectorUpdateMode.UnscaledGameTime;
            _director.time = 0d;
            IsPlaying = true;
            SetCinematicCameraActive(true);
            _director.Play();
            return true;
        }

        public bool Skip()
        {
            if (!IsPlaying)
                return false;

            _requestedResult = EBossCinematicPlayResult.Skipped;
            double duration = _director.duration;

            if (duration > 0d &&
                !double.IsNaN(duration) &&
                !double.IsInfinity(duration))
            {
                _director.time = duration;
                _director.Evaluate();
            }

            StopDirectorAndComplete();
            return true;
        }

        public bool Cancel()
        {
            if (!IsPlaying)
                return false;

            _requestedResult = EBossCinematicPlayResult.Cancelled;
            StopDirectorAndComplete();
            return true;
        }

        private void ApplyCameraTarget()
        {
            CameraTarget target = _cinematicCamera.Target;
            target.LookAtTarget = _lookAtTarget;
            target.CustomLookAtTarget = true;
            _cinematicCamera.Target = target;
            _brain.IgnoreTimeScale = true;
        }

        private void CachePlaybackState()
        {
            _previousCameraTarget = _cinematicCamera.Target;
            _previousUpdateMode = _director.timeUpdateMode;

            Transform cameraTransform = _cinematicCamera.transform;
            _previousCameraLocalPosition =
                cameraTransform.localPosition;
            _previousCameraLocalRotation =
                cameraTransform.localRotation;

            _previousBrainIgnoreTimeScale = _brain.IgnoreTimeScale;
            _previousBossAnimationBinding =
                _director.GetGenericBinding(_bossAnimationTrack);
            _controlledObject = _brain.ControlledObject != null
                ? _brain.ControlledObject.transform
                : null;

            if (_controlledObject != null)
            {
                _previousControlledObjectLocalPosition =
                    _controlledObject.localPosition;
                _previousControlledObjectLocalRotation =
                    _controlledObject.localRotation;
            }

            _hasCachedPlaybackState = true;
        }

        private void StopDirectorAndComplete()
        {
            _director.Stop();

            if (IsPlaying)
                CompletePlayback();
        }

        private void HandleDirectorStopped(PlayableDirector p_director)
        {
            if (IsPlaying && ReferenceEquals(_director, p_director))
                CompletePlayback();
        }

        private void CompletePlayback()
        {
            Action<EBossCinematicPlayResult> callback =
                _completionCallback;
            EBossCinematicPlayResult result = _requestedResult;

            IsPlaying = false;
            _completionCallback = null;
            _bossAnimationView?.EndCinematic();
            RestorePlaybackState();

            // 기본 카메라 복구가 끝난 뒤 Flow가 전투를 시작한다.
            callback?.Invoke(result);
        }

        private void RestorePlaybackState()
        {
            if (!_hasCachedPlaybackState)
                return;

            SetCinematicCameraActive(false);

            if (_cinematicCamera != null)
            {
                _cinematicCamera.Target = _previousCameraTarget;
                _cinematicCamera.transform.SetLocalPositionAndRotation(
                    _previousCameraLocalPosition,
                    _previousCameraLocalRotation);
            }

            if (_director != null)
            {
                _director.timeUpdateMode = _previousUpdateMode;

                if (_previousBossAnimationBinding != null)
                {
                    _director.SetGenericBinding(
                        _bossAnimationTrack,
                        _previousBossAnimationBinding);
                }
                else
                {
                    _director.ClearGenericBinding(
                        _bossAnimationTrack);
                }
            }

            if (_controlledObject != null)
            {
                _controlledObject.SetLocalPositionAndRotation(
                    _previousControlledObjectLocalPosition,
                    _previousControlledObjectLocalRotation);
            }

            if (_brain != null)
            {
                _brain.IgnoreTimeScale =
                    _previousBrainIgnoreTimeScale;
                _brain.ResetState();
            }

            _controlledObject = null;
            _previousBossAnimationBinding = null;
            _hasCachedPlaybackState = false;
        }

        private void SubscribeDirector()
        {
            _director.stopped -= HandleDirectorStopped;
            _director.stopped += HandleDirectorStopped;
        }

        private void UnsubscribeDirector()
        {
            if (_director != null)
                _director.stopped -= HandleDirectorStopped;
        }

        private void SetCinematicCameraActive(bool p_isActive)
        {
            if (_cinematicCamera != null &&
                _cinematicCamera.gameObject.activeSelf != p_isActive)
            {
                _cinematicCamera.gameObject.SetActive(p_isActive);
            }
        }

        private void ResolveReferences()
        {
            _director ??= GetComponent<PlayableDirector>();
            _cinematicCamera ??=
                GetComponentInChildren<CinemachineCamera>(true);

            if (_brain == null)
            {
                Camera mainCamera = Camera.main;
                _brain = mainCamera != null
                    ? mainCamera.GetComponent<CinemachineBrain>()
                    : null;
                _brain ??=
                    FindFirstObjectByType<CinemachineBrain>(
                        FindObjectsInactive.Include);
            }

            if (_bossAnimationTrack != null ||
                _director?.playableAsset is not TimelineAsset timeline)
            {
                return;
            }

            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                if (track is AnimationTrack &&
                    string.Equals(
                        track.name,
                        BossAnimationTrackName,
                        StringComparison.Ordinal))
                {
                    _bossAnimationTrack = track;
                    break;
                }
            }
        }

        private void OnDisable()
        {
            Cancel();
            UnsubscribeDirector();
        }

        private void OnDestroy()
        {
            Cancel();
            UnsubscribeDirector();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
