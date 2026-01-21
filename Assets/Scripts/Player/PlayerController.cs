using UnityEngine;
using SyntaxError.Inputs;

namespace SyntaxError.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputManager _inputManager;
        private CharacterController _controller;

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 4.0f;
        [SerializeField] private float _sprintSpeed = 7.0f;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _jumpHeight = 1.2f;

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundDistance = 0.4f;
        [SerializeField] private LayerMask _groundMask;

        private Vector3 _velocity;
        private bool _isGrounded;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            HandleMovement();
            HandleGravity();
        }

        private void HandleMovement()
        {
            // ตรวจสอบว่าวิ่งอยู่ไหม
            float currentSpeed = _inputManager.IsSprinting ? _sprintSpeed : _walkSpeed;

            // รับค่าทิศทาง (แกน X และ Z)
            Vector2 input = _inputManager.MoveInput;
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            // สั่ง CharacterController ให้เคลื่อนที่
            _controller.Move(move * currentSpeed * Time.deltaTime);

            // กระโดด
            if (_inputManager.IsJumpPressed && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
        }

        private void HandleGravity()
        {
            // เช็คว่าเท้าติดพื้นไหม
            _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundMask);

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // แรงกดเล็กน้อยเพื่อให้ติดพื้นตลอดเวลา
            }

            // คำนวณแรงโน้มถ่วง
            _velocity.y += _gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }
    }
}