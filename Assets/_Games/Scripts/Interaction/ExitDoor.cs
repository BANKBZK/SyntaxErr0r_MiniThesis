using UnityEngine;
using System.Collections;
using SyntaxError.Interfaces;
using SyntaxError.Managers;

namespace SyntaxError.Interaction
{
    public class ExitDoor : MonoBehaviour, IInteractable
    {
        [Header("Voting Settings")]
        [Tooltip("True = โหวตว่ามีผี / False = โหวตว่าปกติ")]
        [SerializeField] private bool _isAnomalyExit = false;

        [Header("Custom UI (Optional)")]
        [Tooltip("ถ้าพิมพ์ข้อความลงไป จะใช้คำนี้แทน Default (เช่น 'Escape to School')")]
        [SerializeField] private string _customPromptText = "";

        [Header("Animation")]
        [SerializeField] private Transform _doorModel;
        [SerializeField] private Vector3 _openOffset = new Vector3(0, 0, 1f);
        [SerializeField] private float _animationSpeed = 2f;

        private bool _isClicked = false;
        private Vector3 _initialPos;
        private bool _isInitialized = false; // 🛠️ ตัวแปรเช็คว่าจำตำแหน่งหรือยัง

        // 🛠️ เปลี่ยนจาก Start เป็น Awake เพื่อให้รันทันทีที่ Object ถูกเปิด
        private void Awake()
        {
            InitializeDoor();
        }

        // 🛠️ ฟังก์ชันสำหรับบังคับจำตำแหน่ง
        private void InitializeDoor()
        {
            if (!_isInitialized && _doorModel != null)
            {
                _initialPos = _doorModel.localPosition;
                _isInitialized = true;
            }
        }

        public void Interact()
        {
            if (LoopManager.Instance != null && LoopManager.Instance.IsTeleporting)
            {
                Debug.LogWarning("LoopManager is busy teleporting. Interaction Ignored.");
                return;
            }

            if (_isClicked) return;
            _isClicked = true;

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("DoorOpen");

            StartCoroutine(OpenAndSubmit());
        }

        public string GetPromptText()
        {
            if (_isClicked || (LoopManager.Instance != null && LoopManager.Instance.IsTeleporting))
                return "";

            if (!string.IsNullOrEmpty(_customPromptText)) return _customPromptText;

            return _isAnomalyExit ? "Report Anomaly" : "Proceed Normal";
        }

        private IEnumerator OpenAndSubmit()
        {
            if (_doorModel != null)
            {
                Vector3 startPos = _doorModel.localPosition;
                Vector3 endPos = _initialPos + _openOffset;
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime * _animationSpeed;
                    _doorModel.localPosition = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
            }

            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.SubmitVote(_isAnomalyExit);
            }
            else
            {
                _isClicked = false;
                Debug.LogError("Critical Error: LoopManager not found!");
            }
        }

        public void ResetDoor()
        {
            InitializeDoor(); // 🛠️ บังคับเช็คตำแหน่งให้ชัวร์ก่อนรีเซ็ต
            _isClicked = false;
            if (_doorModel != null) _doorModel.localPosition = _initialPos;
        }
    }
}