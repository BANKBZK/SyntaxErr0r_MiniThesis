using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SyntaxError.Interfaces;
using SyntaxError.Player;
using SyntaxError.Interaction;
using SyntaxError.Ritual;

namespace SyntaxError.Managers
{
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private CharacterController _characterController;

        [Header("Teleport Points")]
        [Tooltip("จุดเกิดปกติ (Loop 0-3)")]
        [SerializeField] private Transform _startPoint;

        [Tooltip("จุดเกิดในด่านหนีผี (Ritual Level)")]
        [SerializeField] private Transform _ritualStartPoint;

        [Header("Loop Sequence Settings")]
        [Tooltip("เข้าสู่ด่าน Ritual เมื่อเริ่ม Loop ที่เท่าไหร่? (เช่น 4)")]
        [SerializeField] private int _ritualLoopTrigger = 4;

        [Header("UI Settings")]
        [SerializeField] private CanvasGroup _fadeUI;
        [SerializeField] private float _fadeDuration = 1.0f;

        private List<IResettable> _resettableObjects = new List<IResettable>();
        private bool _isTeleporting = false;
        public bool IsTeleporting => _isTeleporting;

        private void Awake() { if (Instance == null) Instance = this; }

        private void Start()
        {
            if (_fadeUI != null) { _fadeUI.alpha = 1f; _fadeUI.blocksRaycasts = false; StartCoroutine(FadeRoutine(1f, 0f)); }
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
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("Correct");
                }
                else
                {
                    GameManager.Instance.ResetToZero();
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("Wrong");
                }
            }

            int loop = GameManager.Instance != null ? GameManager.Instance.CurrentLoop : 0;

            // ปิด CharacterController ก่อนย้ายตำแหน่ง
            if (_characterController != null) _characterController.enabled = false;

            // ==========================================
            // 🚂 ระบบสับราง (The Loop Router)
            // ==========================================
            if (loop == _ritualLoopTrigger && isCorrect)
            {
                // 1. วาร์ปไปด่าน Ritual
                if (_ritualStartPoint != null)
                {
                    _playerTransform.position = _ritualStartPoint.position;
                    _playerTransform.rotation = _ritualStartPoint.rotation;
                }

                // 2. สั่งเปิดระบบวิ่ง
                PlayerController pController = _characterController.GetComponent<PlayerController>();
                if (pController != null) pController.SetSprintAbility(true);

                // 3. เริ่มระบบหาของไหว้ (สุ่มเกิดไอเทม)
                if (RitualManager.Instance != null) RitualManager.Instance.SetupRitualPhase();

                Debug.Log("<color=red>Entering Ritual Phase! วิ่งงงง!!</color>");
            }
            else
            {
                // กลับมาเดินโถงปกติ (ไม่ว่าจะลูป 1-3 หรือตอบผิดกลับลูป 0)
                _playerTransform.position = _startPoint.position;
                _playerTransform.rotation = _startPoint.rotation;

                // ล็อกไม่ให้วิ่ง (เผื่อเพิ่งตายกลับมาจากด่าน Ritual จะได้โดนริบความสามารถวิ่งคืน)
                PlayerController pController = _characterController.GetComponent<PlayerController>();
                if (pController != null) pController.SetSprintAbility(false);
            }
            // ==========================================

            Physics.SyncTransforms();

            // รีเซ็ตของในฉาก (ทางเดินปกติ)
            foreach (var obj in _resettableObjects) if (obj != null) obj.OnLoopReset(loop);

            ExitDoor[] exits = FindObjectsByType<ExitDoor>(FindObjectsSortMode.None);
            foreach (var exit in exits) exit.ResetDoor();

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