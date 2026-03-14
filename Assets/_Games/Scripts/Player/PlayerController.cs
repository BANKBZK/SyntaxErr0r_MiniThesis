using UnityEngine;
using SyntaxError.Inputs;
using SyntaxError.Managers;

namespace SyntaxError.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private Transform _cameraHolder; // [เพิ่ม] ใส่ CameraHolder ตรงนี้เพื่อให้กล้องเตี้ยลงตอนย่อ
        private CharacterController _controller;

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3.0f;
        [SerializeField] private float _sprintSpeed = 6.0f;
        [SerializeField] private float _crouchSpeed = 1.5f;
        [SerializeField] private float _gravity = -9.81f;

        [Header("Crouch Settings")]
        [SerializeField] private float _normalHeight = 1.7f; // ปรับตามความสูงโมเดล
        [SerializeField] private float _crouchHeight = 1.0f;
        [SerializeField] private float _crouchTransitionSpeed = 10f;

        // เก็บความสูงเดิมของกล้องตอนยืน
        private float _normalCameraY;
        private float _crouchCameraY;

        [Header("Stamina & Ritual Settings")]
        [SerializeField] private bool _canSprint = false;
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private float _staminaDrainRate = 20f;
        [SerializeField] private float _staminaRegenRate = 15f;

        private float _currentStamina;
        private bool _isExhausted = false;
        private Vector3 _velocity;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            if (_inputManager == null) _inputManager = GetComponent<InputManager>();

            _currentStamina = _maxStamina;

            // จัดการความสูงกล้อง
            if (_cameraHolder != null)
            {
                _normalCameraY = _cameraHolder.localPosition.y;
                _crouchCameraY = _normalCameraY - (_normalHeight - _crouchHeight) / 2f;
            }
        }

        private void Update()
        {
            if (!_controller.enabled) return;

            HandleMovementAndStamina();
            ApplyGravity();
        }

        private void HandleMovementAndStamina()
        {
            Vector2 input = _inputManager.MoveInput;
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            bool isCrouching = _inputManager.IsCrouching;
            bool isTryingToSprint = !isCrouching && _canSprint && _inputManager.IsSprinting && move.magnitude > 0.1f;

            // --- ระบบย่อตัว ---
            float targetHeight = isCrouching ? _crouchHeight : _normalHeight;
            _controller.height = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * _crouchTransitionSpeed);

            // [แก้] ปรับ Center ให้ฐาน (เท้า) อยู่ที่ Y = 0 เสมอ
            _controller.center = new Vector3(0, _controller.height / 2f, 0);

            // [เพิ่ม] ขยับกล้องให้เตี้ยลงเวลานั่งยอง
            if (_cameraHolder != null)
            {
                Vector3 camPos = _cameraHolder.localPosition;
                float targetCamY = isCrouching ? _crouchCameraY : _normalCameraY;
                camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * _crouchTransitionSpeed);
                _cameraHolder.localPosition = camPos;
            }

            // --- ระบบคำนวณ Stamina ---
            // (โค้ด Stamina ส่วนนี้ใช้งานได้ดีอยู่แล้วครับ)
            if (_canSprint)
            {
                if (isTryingToSprint && !_isExhausted)
                {
                    _currentStamina -= _staminaDrainRate * Time.deltaTime;
                    if (_currentStamina <= 0)
                    {
                        _currentStamina = 0;
                        _isExhausted = true;
                        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("PlayerPant");
                        if (UIManager.Instance != null) UIManager.Instance.ToggleExhaustionEffect(true);
                    }
                }
                else
                {
                    if (_currentStamina < _maxStamina)
                    {
                        _currentStamina += _staminaRegenRate * Time.deltaTime;
                        if (_currentStamina >= _maxStamina)
                        {
                            _currentStamina = _maxStamina;
                            if (_isExhausted)
                            {
                                _isExhausted = false;
                                if (UIManager.Instance != null) UIManager.Instance.ToggleExhaustionEffect(false);
                            }
                        }
                    }
                }
            }

            // --- กำหนดความเร็วจริง ---
            float currentSpeed = _walkSpeed;
            if (isCrouching) currentSpeed = _crouchSpeed;
            else if (isTryingToSprint && !_isExhausted) currentSpeed = _sprintSpeed;

            _controller.Move(move * currentSpeed * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded && _velocity.y < 0) _velocity.y = -2f;
            _velocity.y += _gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        public void SetSprintAbility(bool canSprint)
        {
            _canSprint = canSprint;
            if (!canSprint)
            {
                _currentStamina = _maxStamina;
                _isExhausted = false;
            }
        }
    }
}