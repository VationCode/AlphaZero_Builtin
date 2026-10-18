using Alpha.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace Alpha.Enemy.Boss
{
    // 플레이어의 보스 구역 진입을 한 번만 받아 인트로 연출과 카메라 복귀를 조정한다.
    [RequireComponent(typeof(Collider))]
    public sealed class CrabBossCinematicFlow : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private GameObject _dollyCamera;
        [SerializeField] private CinemachineBrain _cameraBrain;
        [SerializeField] private Camera _gameplayCamera;
        [SerializeField] private Rigidbody _bossBody;
        [SerializeField] private BossStateFlow _bossStateFlow;
        [SerializeField] private BossLocomotionModule _bossLocomotion;
        [SerializeField] private Transform _startPos;
        [SerializeField] private AnimationClip _emergenceClip;
        [SerializeField, Min(0f)] private float _emergenceStartTime = 4f;

        private CrabBossAnimationView _bossAnimation;
        private Vector3 _gameplayCameraLocalPosition;
        private Quaternion _gameplayCameraLocalRotation;
        private float _gameplayFieldOfView;
        private double _jumpEndTime;
        private bool _bossGravity;
        private bool _bossDetectCollisions;
        private bool _bossStateFlowEnabled;
        private bool _bossHeld;
        private bool _jumpStarted;
        private bool _jumpCompleted;
        private bool _hasPlayed;
        private bool _isPlaying;

        private void Awake()
        {
            if (_bossBody == null || _bossStateFlow == null ||
                _bossLocomotion == null || _startPos == null ||
                _director == null || _emergenceClip == null ||
                _emergenceClip.length <= 0f)
            {
                Debug.LogError("CrabBossCinematicFlow: 보스 인트로 참조 또는 Cinematic1 클립이 누락되었습니다.", this);
                enabled = false;
                return;
            }

            _bossAnimation = _bossBody.GetComponentInChildren<CrabBossAnimationView>(true);
            if (_bossAnimation == null)
            {
                Debug.LogError("CrabBossCinematicFlow: 보스 애니메이션 View가 누락되었습니다.", this);
                enabled = false;
                return;
            }

            _jumpEndTime = _emergenceStartTime + _emergenceClip.length;

            // 연출 전에는 아래쪽 보스가 중력으로 떨어지거나 전투를 시작하지 않는다.
            _bossGravity = _bossBody.useGravity;
            _bossDetectCollisions = _bossBody.detectCollisions;
            _bossStateFlowEnabled = _bossStateFlow.enabled;
            _bossBody.linearVelocity = Vector3.zero;
            _bossBody.useGravity = false;
            _bossBody.detectCollisions = false;
            _bossStateFlow.enabled = false;
            _bossHeld = true;
        }

        private void OnEnable()
        {
            if (_director != null)
                _director.stopped += HandleDirectorStopped;
        }

        private void OnDisable()
        {
            if (_director != null)
                _director.stopped -= HandleDirectorStopped;

            if (_isPlaying)
            {
                if (_director != null)
                    _director.Stop();
                FinishCinematic();
            }
            else if (_bossHeld)
            {
                ReleaseBoss();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasPlayed || !_bossHeld ||
                other.GetComponentInParent<PlayerCore>() == null)
                return;

            if (_director == null || _director.playableAsset == null ||
                _dollyCamera == null || _cameraBrain == null || _gameplayCamera == null)
            {
                Debug.LogError("CrabBossCinematicFlow: 시네마틱 참조가 누락되었습니다.", this);
                return;
            }

            _hasPlayed = true;
            _isPlaying = true;
            _gameplayCameraLocalPosition = _gameplayCamera.transform.localPosition;
            _gameplayCameraLocalRotation = _gameplayCamera.transform.localRotation;
            _gameplayFieldOfView = _gameplayCamera.fieldOfView;

            _dollyCamera.SetActive(true);
            _cameraBrain.enabled = true;
            _director.time = 0d;
            _director.Play();
        }

        private void Update()
        {
            if (!_isPlaying || _jumpStarted || _director.time < _emergenceStartTime)
                return;

            // 페이드가 사라진 순간 애니메이션과 몸체 이동을 함께 시작한다.
            if (!_bossAnimation.PlayCinematic1())
            {
                Debug.LogError("CrabBossCinematicFlow: Cinematic1 Animator State를 재생할 수 없습니다.", this);
                _director.Stop();
                return;
            }

            _bossLocomotion.BeginJumpTo(_startPos.position);
            _jumpStarted = true;
        }

        private void FixedUpdate()
        {
            if (!_isPlaying || !_jumpStarted || _jumpCompleted)
                return;

            float progress = Mathf.Clamp01((float)((_director.time - _emergenceStartTime) /
                (_jumpEndTime - _emergenceStartTime)));
            _bossLocomotion.AdvanceJump(progress);
            if (progress < 1f)
                return;

            _bossLocomotion.CompleteJump();
            _jumpCompleted = true;
        }

        private void HandleDirectorStopped(PlayableDirector director)
        {
            if (director == _director)
                FinishCinematic();
        }

        private void FinishCinematic()
        {
            if (!_isPlaying)
                return;

            ReleaseBoss();
            RestoreGameplayCamera();
        }

        // 전투 시작 위치를 확정한 뒤 물리와 보스 판단을 다시 활성화한다.
        private void ReleaseBoss()
        {
            if (!_bossHeld)
                return;

            _bossHeld = false;
            if (_bossBody == null || _startPos == null)
                return;

            if (_jumpStarted && !_jumpCompleted && _bossLocomotion != null)
                _bossLocomotion.CompleteJump();

            _bossBody.position = _startPos.position;
            _bossBody.rotation = _startPos.rotation;
            _bossBody.linearVelocity = Vector3.zero;
            _bossBody.detectCollisions = _bossDetectCollisions;
            _bossBody.useGravity = _bossGravity;
            if (_bossStateFlow != null)
            {
                _bossStateFlow.SetHomePose(_startPos);
                _bossStateFlow.enabled = _bossStateFlowEnabled;
            }
        }

        // Brain이 기록한 카메라 위치를 제거하고 Player 카메라 Rig의 위치로 되돌린다.
        private void RestoreGameplayCamera()
        {
            if (!_isPlaying)
                return;

            _isPlaying = false;
            if (_cameraBrain != null)
                _cameraBrain.enabled = false;
            if (_dollyCamera != null)
                _dollyCamera.SetActive(false);
            if (_gameplayCamera != null)
            {
                _gameplayCamera.transform.localPosition = _gameplayCameraLocalPosition;
                _gameplayCamera.transform.localRotation = _gameplayCameraLocalRotation;
                _gameplayCamera.fieldOfView = _gameplayFieldOfView;
            }
        }
    }
}
