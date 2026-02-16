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

        [Header("Animation")]
        [SerializeField] private Transform _doorModel;
        [SerializeField] private Vector3 _openOffset = new Vector3(0, 0, 1f);
        [SerializeField] private float _animationSpeed = 2f;

        private bool _isClicked = false;
        private Vector3 _initialPos;

        private void Start()
        {
            if (_doorModel != null) _initialPos = _doorModel.localPosition;
        }

        public void Interact()
        {
            // 1. เช็คความปลอดภัย: ถ้า LoopManager กำลังทำงานอยู่ ห้ามยุ่งเด็ดขาด!
            if (LoopManager.Instance != null && LoopManager.Instance.IsTeleporting)
            {
                Debug.LogWarning("LoopManager is busy teleporting. Interaction Ignored.");
                return;
            }

            if (_isClicked) return; // กันกดซ้ำในตัวมันเอง
            _isClicked = true;

            // เล่นเสียง
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("DoorOpen");

            StartCoroutine(OpenAndSubmit());
        }

        public string GetPromptText()
        {
            // ถ้ากดไปแล้ว หรือระบบกำลังวาร์ป ไม่ต้องขึ้นข้อความ
            if (_isClicked || (LoopManager.Instance != null && LoopManager.Instance.IsTeleporting))
                return "";

            return _isAnomalyExit ? "Report Anomaly" : "Proceed (Normal)";
        }

        private IEnumerator OpenAndSubmit()
        {
            // Animation ประตูเปิด
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

            // ส่งคำตอบให้ LoopManager
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.SubmitVote(_isAnomalyExit);
            }
            else
            {
                // Safety: ถ้าหา LoopManager ไม่เจอ ให้ปลดล็อกเพื่อให้ลองกดใหม่ได้
                _isClicked = false;
                Debug.LogError("Critical Error: LoopManager not found!");
            }
        }

        public void ResetDoor()
        {
            _isClicked = false;
            if (_doorModel != null) _doorModel.localPosition = _initialPos;
        }
    }
}