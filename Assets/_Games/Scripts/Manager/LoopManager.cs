using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SyntaxError.Interfaces;
using SyntaxError.Player;
using SyntaxError.Interaction; // เรียก ExitDoor

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

        // --- ไม่ต้องลากไฟล์เสียงตรงนี้แล้ว ไปลากใน SoundManager แทน ---
        // [SerializeField] private AudioClip _correctSound; 
        // [SerializeField] private AudioClip _wrongSound;

        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 1.0f;

        private List<IResettable> _resettableObjects = new List<IResettable>();
        private bool _isTeleporting = false;
        public bool IsTeleporting => _isTeleporting;

        private void Awake() { if (Instance == null) Instance = this; }

        private void Start()
        {
            if (_fadeUI != null) { _fadeUI.alpha = 1f; _fadeUI.blocksRaycasts = false; StartCoroutine(FadeRoutine(1f, 0f)); }

            // (Optional) เล่นเสียงบรรยากาศตอนเริ่มเกม
            // if (SoundManager.Instance != null) SoundManager.Instance.PlayMusic("Ambience");
        }

        public void Register(IResettable obj) { if (!_resettableObjects.Contains(obj)) _resettableObjects.Add(obj); }
        public void Unregister(IResettable obj) { if (_resettableObjects.Contains(obj)) _resettableObjects.Remove(obj); }

        public void SubmitVote(bool votedAnomaly)
        {
            if (_isTeleporting) return;

            // ตรวจคำตอบ
            bool actuallyHasAnomaly = false;
            if (AnomalyManager.Instance != null) actuallyHasAnomaly = AnomalyManager.Instance.IsAnomalyActive;

            bool isCorrect = (votedAnomaly == actuallyHasAnomaly);
            Debug.Log($"Vote Result: {(isCorrect ? "CORRECT" : "WRONG")}");

            StartCoroutine(TeleportSequence(isCorrect));
        }

        private IEnumerator TeleportSequence(bool isCorrect)
        {
            _isTeleporting = true;
            yield return StartCoroutine(FadeRoutine(0f, 1f)); // จอมืด

            // --- Logic คำนวณผล ---
            if (GameManager.Instance != null)
            {
                if (isCorrect)
                {
                    GameManager.Instance.NextLoop();
                    // เรียกเสียงจาก SoundManager
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("Correct");
                }
                else
                {
                    GameManager.Instance.ResetToZero();
                    // เรียกเสียงจาก SoundManager
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("Wrong");
                }
            }

            int loop = GameManager.Instance != null ? GameManager.Instance.CurrentLoop : 0;

            // --- ย้ายผู้เล่น & Reset ---
            if (_characterController != null) _characterController.enabled = false;
            _playerTransform.position = _startPoint.position;
            _playerTransform.rotation = _startPoint.rotation;
            Physics.SyncTransforms();

            foreach (var obj in _resettableObjects) if (obj != null) obj.OnLoopReset(loop);

            // Reset ประตู ExitDoor (หาแบบ FindObject เพราะประตูไม่ได้ลงทะเบียน IResettable)
            ExitDoor[] exits = FindObjectsByType<ExitDoor>(FindObjectsSortMode.None);
            foreach (var exit in exits) exit.ResetDoor();

            // คำนวณ Anomaly รอบใหม่
            if (AnomalyManager.Instance != null) AnomalyManager.Instance.ProcessLoop(loop);

            yield return null;
            if (_characterController != null) _characterController.enabled = true;

            yield return StartCoroutine(FadeRoutine(1f, 0f)); // จอสว่าง
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