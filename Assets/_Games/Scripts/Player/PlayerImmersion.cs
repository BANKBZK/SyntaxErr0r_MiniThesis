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
        [SerializeField] private InputManager _inputManager; // เพิ่มตัวช่วยเช็ค Input

        [Header("Head Bob Settings")]
        [SerializeField] private bool _enableHeadBob = true;
        [SerializeField] private float _bobFrequency = 14.0f;
        [SerializeField] private float _bobAmplitude = 0.05f;
        [SerializeField] private float _sprintMultiplier = 1.5f;

        [Header("Footstep Settings")]
        [SerializeField] private bool _enableFootsteps = true;
        [SerializeField] private string[] _footstepSounds;

        private float _defaultYPos = 0;
        private float _timer = 0;
        private bool _isStepPlayed = false;

        private void Start()
        {
            // Auto Find ถ้าลืมลาก
            if (_controller == null) _controller = GetComponent<CharacterController>();
            if (_inputManager == null) _inputManager = GetComponent<InputManager>();

            if (_cameraHolder != null)
            {
                _defaultYPos = _cameraHolder.localPosition.y;
            }
            else
            {
                Debug.LogError("❌ PlayerImmersion: ไม่ได้ลาก Camera Holder ใส่ใน Inspector!");
                enabled = false; // ปิดสคริปต์ไปเลยถ้าไม่มีของ
            }
        }

        private void Update()
        {
            if (_enableHeadBob) HandleHeadBob();
        }

        private void HandleHeadBob()
        {
            if (_controller == null || _cameraHolder == null) return;

            // 1. เช็คความเร็วจริง
            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z);
            float speed = horizontalVelocity.magnitude;

            // 2. เช็คว่าผู้เล่นกดเดินไหม (Input) - ใช้เป็นตัวสำรองเผื่อ CharacterController บั๊ก
            bool hasInput = _inputManager != null && _inputManager.MoveInput.magnitude > 0.1f;

            // เงื่อนไข: มีความเร็ว หรือ มีการกดปุ่มเดิน (และต้องไม่กระโดดสูงเกินไป)
            if ((speed > 0.1f || hasInput) && _controller.isGrounded)
            {
                // Toggle Run/Walk
                bool isSprinting = (_inputManager != null && _inputManager.IsSprinting);

                float currentFreq = isSprinting ? _bobFrequency * _sprintMultiplier : _bobFrequency;
                float currentAmp = isSprinting ? _bobAmplitude * _sprintMultiplier : _bobAmplitude;

                _timer += Time.deltaTime * currentFreq;

                // คำนวณตำแหน่งใหม่
                float newY = _defaultYPos + Mathf.Sin(_timer) * currentAmp;
                _cameraHolder.localPosition = new Vector3(_cameraHolder.localPosition.x, newY, _cameraHolder.localPosition.z);

                // เสียงเท้า
                if (_enableFootsteps) HandleFootsteps(Mathf.Sin(_timer));
            }
            else
            {
                ResetCameraPosition();
            }
        }

        private void HandleFootsteps(float bobValue)
        {
            // เล่นเสียงตอนกราฟตกสุด (เท้ากระแทกพื้น)
            if (bobValue < -0.95f)
            {
                if (!_isStepPlayed)
                {
                    PlayRandomFootstep();
                    _isStepPlayed = true;
                }
            }
            else if (bobValue > 0.0f) // ยกขา
            {
                _isStepPlayed = false;
            }
        }

        private void ResetCameraPosition()
        {
            if (Mathf.Abs(_cameraHolder.localPosition.y - _defaultYPos) > 0.001f)
            {
                _timer = 0; // Reset timer เพื่อให้เริ่มก้าวใหม่
                Vector3 targetPos = new Vector3(_cameraHolder.localPosition.x, _defaultYPos, _cameraHolder.localPosition.z);
                _cameraHolder.localPosition = Vector3.Lerp(_cameraHolder.localPosition, targetPos, Time.deltaTime * 5f);
            }
        }

        private void PlayRandomFootstep()
        {
            if (_footstepSounds.Length == 0 || SoundManager.Instance == null) return;

            // สุ่มเสียง
            string sound = _footstepSounds[Random.Range(0, _footstepSounds.Length)];
            SoundManager.Instance.PlaySFX(sound);
        }
    }
}