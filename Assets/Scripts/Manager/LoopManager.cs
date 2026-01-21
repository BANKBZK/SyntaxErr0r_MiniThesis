using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SyntaxError.Interfaces;
using SyntaxError.Player;

namespace SyntaxError.Managers
{
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _startPoint;
        [SerializeField] private CanvasGroup _fadeUI;

        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 1.0f;

        private List<IResettable> _resettableObjects = new List<IResettable>();
        private bool _isTeleporting = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // เริ่มเกม: บังคับจอมืด แล้วค่อยๆ สว่าง
            if (_fadeUI != null)
            {
                _fadeUI.alpha = 1f;
                _fadeUI.blocksRaycasts = false;
                StartCoroutine(FadeRoutine(1f, 0f));
            }
        }

        public void Register(IResettable obj) { if (!_resettableObjects.Contains(obj)) _resettableObjects.Add(obj); }
        public void Unregister(IResettable obj) { if (_resettableObjects.Contains(obj)) _resettableObjects.Remove(obj); }

        public void CompleteLoop()
        {
            if (!_isTeleporting) StartCoroutine(TeleportSequence());
        }

        private IEnumerator TeleportSequence()
        {
            _isTeleporting = true;

            // 1. Fade Out
            yield return StartCoroutine(FadeRoutine(0f, 1f));

            // 2. Logic Process (ทำตอนจอดำ)
            if (GameManager.Instance != null) GameManager.Instance.NextLoop();
            int loop = GameManager.Instance != null ? GameManager.Instance.CurrentLoop : 0;

            // 3. Teleport Player
            if (_characterController != null) _characterController.enabled = false;

            // ใช้ Position/Rotation จาก StartPoint
            _playerTransform.position = _startPoint.position;
            _playerTransform.rotation = _startPoint.rotation;
            Physics.SyncTransforms(); // สำคัญมากสำหรับการย้ายตำแหน่งทันที

            // 4. Reset Objects (ประตู, หน้าต่าง)
            foreach (var obj in _resettableObjects)
            {
                if (obj != null) obj.OnLoopReset(loop);
            }

            // 5. Manage Anomaly
            if (AnomalyManager.Instance != null)
            {
                AnomalyManager.Instance.ProcessLoop(loop);
            }

            // รอ 1 เฟรมให้ Physics เข้าที่
            yield return null;

            if (_characterController != null) _characterController.enabled = true;

            // 6. Fade In
            yield return StartCoroutine(FadeRoutine(1f, 0f));

            _isTeleporting = false;
        }

        private IEnumerator FadeRoutine(float start, float end)
        {
            float t = 0f;
            if (_fadeUI != null) _fadeUI.alpha = start;

            while (t < _fadeDuration)
            {
                t += Time.deltaTime;
                if (_fadeUI != null) _fadeUI.alpha = Mathf.Lerp(start, end, t / _fadeDuration);
                yield return null;
            }

            if (_fadeUI != null) _fadeUI.alpha = end;
        }
    }
}