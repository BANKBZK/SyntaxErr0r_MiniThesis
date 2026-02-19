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

        // ป้องกันการกดค้าง
        private bool _hasInteractedThisFrame;
        private float _spamCooldown = 0f;


        private void Awake()
        {
            _inputManager = FindFirstObjectByType<InputManager>();
        }

        private void Update()
        {
            if (_spamCooldown > 0)
            {
                _spamCooldown -= Time.deltaTime;
            }
            CheckForInteractable();
            HandleInput();
        }

        private void CheckForInteractable()
        {
            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, _rayDistance, _interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    _currentInteractable = interactable;

                    if (_promptText != null)
                    {
                        _promptText.text = $"[E] {interactable.GetPromptText()}";
                        _promptText.gameObject.SetActive(true);
                    }
                    return;
                }
            }

            _currentInteractable = null;
            if (_promptText != null) _promptText.gameObject.SetActive(false);
        }
        private void HandleInput()
        {
            if (_inputManager.IsInteractPressed)
            {
                if (_currentInteractable != null && !_hasInteractedThisFrame && _spamCooldown <= 0)
                {
                    _currentInteractable.Interact();
                    _hasInteractedThisFrame = true; // ล็อกกดค้าง
                    _spamCooldown = 0.5f;
                }
            }
            else
            {
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