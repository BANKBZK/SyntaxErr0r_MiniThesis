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
        [Tooltip("ตัวคูณความเร็วเมาส์พื้นฐาน (ปรับสมดุลว่าเลข 1-10 ควรไวแค่ไหน)")]
        [SerializeField] private float _sensitivityMultiplier = 0.2f;
        [SerializeField] private float _topClamp = -90f;
        [SerializeField] private float _bottomClamp = 90f;

        private float _xRotation = 0f;
        private float _actualSensitivity = 1f;

        private void Start()
        {
            // โหลดค่า Sensitivity จาก PlayerPrefs (ค่าเริ่มต้นคือ 5) ทันทีที่เริ่มด่าน
            int savedSens = PlayerPrefs.GetInt("MouseSensitivity", 5);
            SetSensitivity(savedSens);
        }

        private void Update()
        {
            HandleCameraLook();
        }

        // ฟังก์ชันรับค่าจาก UIManager (ระดับ 1-10)
        public void SetSensitivity(int level)
        {
            // แปลงจากเลข 1-10 เป็นความเร็วจริงๆ (เช่น level 5 * 0.2 = ความเร็ว 1.0)
            _actualSensitivity = level * _sensitivityMultiplier;
        }

        private void HandleCameraLook()
        {
            if (_inputManager == null) return;

            float mouseX = _inputManager.LookInput.x * _actualSensitivity;
            float mouseY = _inputManager.LookInput.y * _actualSensitivity;

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