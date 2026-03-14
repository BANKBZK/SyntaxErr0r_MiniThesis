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

        private void Start()
        {
            if (_doorModel != null) _initialPos = _doorModel.localPosition;
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

            // ถ้ามีข้อความ Custom ให้โชว์ข้อความ Custom
            if (!string.IsNullOrEmpty(_customPromptText))
            {
                return _customPromptText;
            }

            // ถ้าไม่ได้พิมพ์อะไรไว้ ให้ใช้ข้อความ Default
            return _isAnomalyExit ? "Report Anomaly" : "Proceed (Normal)";
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
            _isClicked = false;
            if (_doorModel != null) _doorModel.localPosition = _initialPos;
        }
    }
}