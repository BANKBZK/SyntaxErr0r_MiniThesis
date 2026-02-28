using UnityEngine;
using SyntaxError.Inputs; // เรียกใช้ Namespace ของ Input

namespace SyntaxError.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("ลาก InputManager มาใส่ หรือปล่อยว่างถ้าอยู่ในตัวเดียวกัน")]
        [SerializeField] private InputManager _inputManager;
        [Tooltip("ใส่ Transform ของตัวละครเพื่อสั่งให้หันซ้ายขวา")]
        [SerializeField] private Transform _playerBody;

        [Header("Settings")]
        [SerializeField] private float _mouseSensitivity = 100f;
        [SerializeField] private float _topClamp = -90f;
        [SerializeField] private float _bottomClamp = 90f;

        private float _xRotation = 0f;

        //private void Start()
        //{
        //    // ล็อกเมาส์ให้อยู่กลางจอ
        //    Cursor.lockState = CursorLockMode.Locked;
        //    Cursor.visible = false;
        //}

        private void Update()
        {
            HandleCameraLook();
        }

        private void HandleCameraLook()
        {
            if (_inputManager == null) return;

            // รับค่าจาก InputManager
            float mouseX = _inputManager.LookInput.x * _mouseSensitivity * Time.deltaTime;
            float mouseY = _inputManager.LookInput.y * _mouseSensitivity * Time.deltaTime;

            // คำนวณการก้มเงย (Rotation รอบแกน X)
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, _topClamp, _bottomClamp);

            // หมุนกล้อง (ก้มเงย)
            transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            // หมุนตัวละคร (หันซ้ายขวา)
            _playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}