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
        [Tooltip("ปรับลดลงเหลือ 10 เพื่อให้จังหวะเดินดูหน่วงและสมจริงขึ้น")]
        [SerializeField] private float _bobFrequency = 10.0f;
        [SerializeField] private float _bobAmplitude = 0.05f;
        [Tooltip("ปรับตัวคูณตอนวิ่งลงไม่ให้ภาพสวิงแรงเกินไป")]
        [SerializeField] private float _sprintMultiplier = 1.3f;

        [Header("Footstep Settings")]
        [SerializeField] private bool _enableFootsteps = true;
        [SerializeField] private string[] _footstepSounds;

        private float _defaultYPos = 0;
        private float _timer = 0;
        private bool _isStepPlayed = false;

        private void Start()
        {
            if (_controller == null) _controller = GetComponent<CharacterController>();
            if (_inputManager == null) _inputManager = GetComponent<InputManager>();

            if (_cameraHolder != null)
            {
                _defaultYPos = _cameraHolder.localPosition.y;
            }
            else
            {
                Debug.LogError("❌ PlayerImmersion: ไม่ได้ลาก Camera Holder ใส่ใน Inspector!");
                enabled = false;
            }
        }

        private void Update()
        {
            if (_enableHeadBob) HandleHeadBob();
        }

        private void HandleHeadBob()
        {
            if (_controller == null || _cameraHolder == null) return;

            // 1. เช็คความเร็วจริงของ CharacterController
            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z);
            float speed = horizontalVelocity.magnitude;

            // 2. เช็ค Input สำรอง
            bool hasInput = _inputManager != null && _inputManager.MoveInput.magnitude > 0.1f;

            if ((speed > 0.1f || hasInput) && _controller.isGrounded)
            {
                bool isActuallySprinting = speed > 4.0f;
                float currentFreq = isActuallySprinting ? _bobFrequency * _sprintMultiplier : _bobFrequency;
                float currentAmp = isActuallySprinting ? _bobAmplitude * _sprintMultiplier : _bobAmplitude;
                _timer += Time.deltaTime * currentFreq;
                float newY = _defaultYPos + Mathf.Sin(_timer) * currentAmp;
                _cameraHolder.localPosition = new Vector3(_cameraHolder.localPosition.x, newY, _cameraHolder.localPosition.z);
                if (_enableFootsteps) HandleFootsteps(Mathf.Sin(_timer));
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
            else if (bobValue > 0.0f)
            {
                _isStepPlayed = false;
            }
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
            if (_footstepSounds.Length == 0 || SoundManager.Instance == null) return;

            string sound = _footstepSounds[Random.Range(0, _footstepSounds.Length)];

            // ใช้เทคนิค Random Pitch ที่คุยกันรอบก่อน เพื่อให้เสียงดูสมจริงและประหยัดไฟล์
            // (ถ้าไม่ได้ทำ PlaySFXRandomPitch ไว้ ให้เปลี่ยนกลับเป็น PlaySFX เฉยๆ นะครับ)
            SoundManager.Instance.PlaySFX(sound);
        }
    }
}