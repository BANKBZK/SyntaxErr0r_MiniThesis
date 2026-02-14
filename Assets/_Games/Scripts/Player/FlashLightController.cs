using UnityEngine;
using SyntaxError.Inputs;
using SyntaxError.Managers; // เพื่อเรียกใช้ SoundManager

namespace SyntaxError.Player
{
    public class FlashlightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private Light _lightSource; // ตัว Spotlight
        [SerializeField] private Transform _cameraTransform; // กล้องหลัก

        [Header("Settings")]
        [SerializeField] private bool _isOn = true;
        [SerializeField] private string _toggleSoundName = "Click"; // ชื่อเสียงใน SoundManager

        [Header("Sway Settings (ความหน่วง)")]
        [SerializeField] private float _swaySpeed = 5f; // ความไวในการหันตาม
        [SerializeField] private float _dragAmount = 2f; // ลากเมาส์แล้วไฟฉายเหวี่ยงแค่ไหน

        private Quaternion _initialRotation;

        private void Start()
        {
            if (_inputManager == null) _inputManager = GetComponentInParent<InputManager>();
            if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;

            _initialRotation = transform.localRotation;

            // เซ็ตค่าเริ่มต้น
            if (_lightSource != null) _lightSource.enabled = _isOn;
        }

        private void Update()
        {
            HandleInput();
            HandleSway();
        }

        private void HandleInput()
        {
            if (_inputManager.IsFlashlightPressed)
            {
                ToggleLight();
                _inputManager.ConsumeFlashlightInput(); // รีเซ็ตปุ่มทันที (กดทีละครั้ง)
            }
        }

        private void ToggleLight()
        {
            _isOn = !_isOn;
            if (_lightSource != null) _lightSource.enabled = _isOn;

            // เล่นเสียง
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(_toggleSoundName);
        }

        private void HandleSway()
        {
            if (_cameraTransform == null) return;

            // รับค่าเมาส์จาก InputManager
            float mouseX = _inputManager.LookInput.x;
            float mouseY = _inputManager.LookInput.y;

            // คำนวณตำแหน่งที่จะเหวี่ยงไป (กลับทิศทางเมาส์)
            Quaternion rotationX = Quaternion.AngleAxis(-mouseY * _dragAmount, Vector3.right);
            Quaternion rotationY = Quaternion.AngleAxis(mouseX * _dragAmount, Vector3.up);

            Quaternion targetRotation = _initialRotation * rotationX * rotationY;

            // ค่อยๆ หมุนไปหาจุดเป้าหมาย (Lerp)
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, _swaySpeed * Time.deltaTime);
        }
    }
}