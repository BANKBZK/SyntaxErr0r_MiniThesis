using UnityEngine;
using SyntaxError.Managers;
using SyntaxError.Inputs;

namespace SyntaxError.Player
{
    public class PlayerImmersion : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Transform _cameraHolder;
        [SerializeField] private InputManager _inputManager;

        [Header("Head Bob Settings")]
        [SerializeField] private bool _enableHeadBob = true;
        [SerializeField] private float _bobFrequency = 10.0f;
        [SerializeField] private float _bobAmplitude = 0.05f;
        [SerializeField] private float _sprintMultiplier = 1.3f;

        [Header("Crouch Camera Settings")]
        [Tooltip("ระยะที่กล้องจะลดระดับลงตอนย่อตัว")]
        [SerializeField] private float _crouchYOffset = -0.6f;
        [SerializeField] private float _crouchTransitionSpeed = 10f;

        [Header("Footstep Settings")]
        [SerializeField] private bool _enableFootsteps = true;
        [SerializeField] private string[] _footstepSounds;

        private float _baseYPos = 0; // ความสูงกล้องต้นฉบับ
        private float _defaultYPos = 0; // ความสูงเป้าหมายปัจจุบัน (เปลี่ยนไปมาตอนย่อ)
        private float _timer = 0;
        private bool _isStepPlayed = false;

        private void Start()
        {
            if (_controller == null) _controller = GetComponent<CharacterController>();
            if (_inputManager == null) _inputManager = GetComponent<InputManager>();

            if (_cameraHolder != null)
            {
                // จำความสูงกล้องแต่แรกไว้
                _baseYPos = _cameraHolder.localPosition.y;
                _defaultYPos = _baseYPos;
            }
        }

        private void Update()
        {
            HandleCrouchHeight();
            if (_enableHeadBob) HandleHeadBob();
        }

        private void HandleCrouchHeight()
        {
            if (_cameraHolder == null || _inputManager == null) return;

            // กำหนดเป้าหมาย: ถ้าย่อตัว ให้กล้องต่ำลง ถ้าไม่ย่อ ให้กลับมาที่เดิม
            float targetY = _inputManager.IsCrouching ? (_baseYPos + _crouchYOffset) : _baseYPos;

            // ค่อยๆ เลื่อนความสูงแบบสมูท
            _defaultYPos = Mathf.Lerp(_defaultYPos, targetY, Time.deltaTime * _crouchTransitionSpeed);
        }

        private void HandleHeadBob()
        {
            if (_controller == null || _cameraHolder == null) return;

            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z);
            float speed = horizontalVelocity.magnitude;
            bool hasInput = _inputManager != null && _inputManager.MoveInput.magnitude > 0.1f;

            if ((speed > 0.1f || hasInput) && _controller.isGrounded)
            {
                bool isActuallySprinting = speed > 4.0f;
                float currentFreq = isActuallySprinting ? _bobFrequency * _sprintMultiplier : _bobFrequency;
                float currentAmp = isActuallySprinting ? _bobAmplitude * _sprintMultiplier : _bobAmplitude;

                // ถ้าย่อตัวอยู่ ให้ลด Head Bob ลงครึ่งนึง (เดินนิ่งขึ้น)
                if (_inputManager.IsCrouching) currentAmp *= 0.5f;

                _timer += Time.deltaTime * currentFreq;
                float newY = _defaultYPos + Mathf.Sin(_timer) * currentAmp;
                _cameraHolder.localPosition = new Vector3(_cameraHolder.localPosition.x, newY, _cameraHolder.localPosition.z);

                // --- ระบบเสียงเท้า: ถ้าย่อตัวอยู่ จะไม่เล่นเสียงฝีเท้า (เงียบกริบ) ---
                if (_enableFootsteps && !_inputManager.IsCrouching)
                {
                    HandleFootsteps(Mathf.Sin(_timer));
                }
            }
            else
            {
                ResetCameraPosition();
            }
        }

        private void HandleFootsteps(float bobValue)
        {
            if (bobValue < -0.95f)
            {
                if (!_isStepPlayed)
                {
                    PlayRandomFootstep();
                    _isStepPlayed = true;
                }
            }
            else if (bobValue > 0.0f) _isStepPlayed = false;
        }

        private void ResetCameraPosition()
        {
            if (Mathf.Abs(_cameraHolder.localPosition.y - _defaultYPos) > 0.001f)
            {
                _timer = 0;
                Vector3 targetPos = new Vector3(_cameraHolder.localPosition.x, _defaultYPos, _cameraHolder.localPosition.z);
                _cameraHolder.localPosition = Vector3.Lerp(_cameraHolder.localPosition, targetPos, Time.deltaTime * 5f);
            }
        }

        private void PlayRandomFootstep()
        {
            if (_footstepSounds == null || _footstepSounds.Length == 0 || SoundManager.Instance == null) return;
            string sound = _footstepSounds[Random.Range(0, _footstepSounds.Length)];
            SoundManager.Instance.PlaySFX(sound);
        }
    }
}