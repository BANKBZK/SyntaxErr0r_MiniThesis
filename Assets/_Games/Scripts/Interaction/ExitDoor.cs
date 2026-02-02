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
            if (_isClicked) return;
            _isClicked = true;

            // สั่งเล่นเสียงเปิดประตู
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("DoorOpen");

            StartCoroutine(OpenAndSubmit());
        }

        public string GetPromptText()
        {
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

            // ส่งคำตอบ
            if (LoopManager.Instance != null) LoopManager.Instance.SubmitVote(_isAnomalyExit);
        }

        public void ResetDoor()
        {
            _isClicked = false;
            // ประตูจะถูกย้ายกลับที่โดยอัตโนมัติตอน Scene Reset หรือถ้าจะให้ชัวร์ก็สั่งตรงนี้ได้
            if (_doorModel != null) _doorModel.localPosition = _initialPos;
        }
    }
}