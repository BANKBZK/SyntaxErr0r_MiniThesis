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
        private CharacterController _controller;

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3.0f;
        [SerializeField] private float _sprintSpeed = 6.0f;
        [SerializeField] private float _gravity = -9.81f;

        [Header("Stamina & Ritual Settings")]
        [Tooltip("ถ้าเป็น False จะวิ่งไม่ได้เลย (ใช้ช่วงเดินโถงปกติ)")]
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
        }

        private void Update()
        {
            HandleMovementAndStamina();
            ApplyGravity();
        }

        private void HandleMovementAndStamina()
        {
            Vector2 input = _inputManager.MoveInput;
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            // --- เช็คเงื่อนไขการวิ่ง ---
            // จะพยายามวิ่งได้ก็ต่อเมื่อ 1. อนุญาตให้วิ่ง (_canSprint) 2. กดปุ่มวิ่ง 3. มีการขยับตัวจริงๆ
            bool isTryingToSprint = _canSprint && _inputManager.IsSprinting && move.magnitude > 0.1f;
            // ถ้าระบบวิ่งถูกเปิดใช้งาน ถึงจะเริ่มคำนวณ Stamina
            if (_canSprint)
            {
                if (isTryingToSprint && !_isExhausted)
                {
                    _currentStamina -= _staminaDrainRate * Time.deltaTime;

                    // จังหวะที่ Stamina เพิ่งหมดก๊อกแรก
                    if (_currentStamina <= 0)
                    {
                        _currentStamina = 0;
                        _isExhausted = true; // ล็อกสถานะเหนื่อยหอบ

                        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("PlayerPant");

                        // สั่ง UIManager เปิดหน้ามืด
                        if (UIManager.Instance != null) UIManager.Instance.ToggleExhaustionEffect(true);
                    }
                }
                else
                {
                    // ฟื้นฟู Stamina
                    if (_currentStamina < _maxStamina)
                    {
                        _currentStamina += _staminaRegenRate * Time.deltaTime;
                        if (_currentStamina >= _maxStamina)
                        {
                            _currentStamina = _maxStamina;
                            if (_isExhausted)
                            {
                                _isExhausted = false;

                                // สั่ง UIManager ปิดเอฟเฟกต์หน้ามืด
                                if (UIManager.Instance != null) UIManager.Instance.ToggleExhaustionEffect(false);
                            }
                        }
                    }
                }
            }
            float currentSpeed = (isTryingToSprint && !_isExhausted) ? _sprintSpeed : _walkSpeed;

            _controller.Move(move * currentSpeed * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            _velocity.y += _gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        public void SetSprintAbility(bool canSprint)
        {
            _canSprint = canSprint;

            // ถ้ายกเลิกการวิ่ง ให้รีเซ็ต Stamina กลับมาเต็มทันที
            if (!canSprint)
            {
                _currentStamina = _maxStamina;
                _isExhausted = false;
            }

            Debug.Log($"[PlayerController] Sprint ability set to: {_canSprint}");
        }

        // (Optional) เผื่อใช้ส่งค่าไปให้ UIManager วาดหลอด Stamina
        public float GetStaminaNormalized()
        {
            return _currentStamina / _maxStamina;
        }
    }
}