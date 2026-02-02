using UnityEngine;
using TMPro; // ต้องการ TextMeshPro
using SyntaxError.Inputs;
using SyntaxError.Interaction;

namespace SyntaxError.Player
{
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float _rayDistance = 2.5f; // ระยะเอื้อมถึง
        [SerializeField] private LayerMask _interactableLayer; // เลเยอร์ของสิ่งของ
        [SerializeField] private Transform _cameraTransform; // ตำแหน่งกล้อง (Main Camera)

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _promptText; // ลาก TextMeshPro UI มาใส่ตรงนี้

        private InputManager _inputManager;
        private IInteractable _currentInteractable;
        private bool _hasInteractedThisFrame; // ตัวกันกดรัว

        private void Awake()
        {
            _inputManager = FindFirstObjectByType<InputManager>();
        }

        private void Update()
        {
            CheckForInteractable();
            HandleInput();
        }

        // 1. ยิง Raycast ตรวจสอบของตรงหน้า
        private void CheckForInteractable()
        {
            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
            RaycastHit hit;
            // ยิง Ray ไปข้างหน้า
            if (Physics.Raycast(ray, out hit, _rayDistance, _interactableLayer))
            {
                // ลองดึง Component IInteractable ออกมา
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    _currentInteractable = interactable;

                    // อัปเดต UI ข้อความ
                    if (_promptText != null)
                    {
                        _promptText.text = $"[E] {interactable.GetPromptText()}";
                        _promptText.gameObject.SetActive(true);
                    }
                    return; // เจอของแล้ว จบฟังก์ชันเลย
                }
            }

            // ถ้าไม่เจออะไรเลย หรือเจอของที่ Interact ไม่ได้
            _currentInteractable = null;
            if (_promptText != null) _promptText.gameObject.SetActive(false);
        }

        // 2. รับปุ่มกด
        private void HandleInput()
        {
            if (_inputManager.IsInteractPressed)
            {
                // ถ้ากดปุ่ม และมีของอยู่ตรงหน้า และยังไม่ได้กดค้างไว้
                if (_currentInteractable != null && !_hasInteractedThisFrame)
                {
                    _currentInteractable.Interact();
                    _hasInteractedThisFrame = true; // ล็อกไว้ไม่ให้กดรัว
                }
            }
            else
            {
                // ปล่อยปุ่มแล้ว รีเซ็ตตัวล็อก
                _hasInteractedThisFrame = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (_cameraTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(_cameraTransform.position, _cameraTransform.forward * _rayDistance);
            }
        }
    }
}