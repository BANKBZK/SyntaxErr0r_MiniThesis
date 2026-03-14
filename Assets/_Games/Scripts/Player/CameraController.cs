using UnityEngine;
using SyntaxError.Inputs;

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
        // [ข้อควรระวัง] พอเอา Time.deltaTime ออก เมาส์จะไวขึ้นมาก แนะนำให้ปรับค่านี้ใน Inspector เหลือประมาณ 0.5 - 2.0 ครับ
        [SerializeField] private float _mouseSensitivity = 1f;
        [SerializeField] private float _topClamp = -90f;
        [SerializeField] private float _bottomClamp = 90f;

        private float _xRotation = 0f;

        private void Update()
        {
            HandleCameraLook();
        }

        private void HandleCameraLook()
        {
            if (_inputManager == null) return;

            // [แก้] เอา Time.deltaTime ออกจากการคำนวณ Mouse Delta
            float mouseX = _inputManager.LookInput.x * _mouseSensitivity;
            float mouseY = _inputManager.LookInput.y * _mouseSensitivity;

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