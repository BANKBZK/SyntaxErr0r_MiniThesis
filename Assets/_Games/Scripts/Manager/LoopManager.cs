using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SyntaxError.Interfaces;
using SyntaxError.Player;
using SyntaxError.Interaction;
using SyntaxError.Ritual;
using UnityEngine.SceneManagement;
using SyntaxError.Story;

namespace SyntaxError.Managers
{
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private CharacterController _characterController;

        [Header("Teleport Points")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _ritualStartPoint;

        [Header("Loop Sequence Settings")]
        [SerializeField] private int _ritualLoopTrigger = 4;

        [Header("UI Settings")]
        [SerializeField] private CanvasGroup _fadeUI;
        [SerializeField] private float _fadeDuration = 1.0f;

        [Header("Ending Settings")]
        [SerializeField] private int _finalLoopTrigger = 8;
        public int trueEndingScene;
        public int falseEndingScene;

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

        // ===========================================
        // 💀 Hard Reset (ตาย หรือ เริ่มเกมใหม่ทั้งหมด)
        // ===========================================
        public void FullGameReset()
        {
            if (_isTeleporting) return;
            StartCoroutine(TeleportSequence(false));
        }

        // ===========================================
        // 🚪 Soft Reset (ทายถูก / ทายผิดประตู)
        // ===========================================
        public void SubmitVote(bool votedAnomaly)
        {
            if (_isTeleporting) return;
            bool actuallyHasAnomaly = AnomalyManager.Instance != null && AnomalyManager.Instance.IsAnomalyActive;
            bool isCorrect = (votedAnomaly == actuallyHasAnomaly);
            StartCoroutine(TeleportSequence(isCorrect));
        }

        private IEnumerator TeleportSequence(bool isCorrect)
        {
            _isTeleporting = true;
            yield return StartCoroutine(FadeRoutine(0f, 1f));

            if (GameManager.Instance != null)
            {
                if (isCorrect)
                {
                    GameManager.Instance.NextLoop();
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("Correct");

                    if (GameManager.Instance.CurrentLoop == _finalLoopTrigger)
                    {
                        if (GameManager.Instance.IsRitualComplete) SceneManager.LoadScene(trueEndingScene);
                        else SceneManager.LoadScene(falseEndingScene);
                        yield break;
                    }
                }
                else
                {
                    // Soft Reset ทายผิด กลับลูป 0 แต่อย่างอื่นไม่รีเซ็ต
                    GameManager.Instance.ResetToZero();
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("Wrong");
                }
            }

            int loop = GameManager.Instance != null ? GameManager.Instance.CurrentLoop : 0;
            if (_characterController != null) _characterController.enabled = false;

            if (loop == _ritualLoopTrigger && isCorrect)
            {
                if (_ritualStartPoint != null) { _playerTransform.position = _ritualStartPoint.position; _playerTransform.rotation = _ritualStartPoint.rotation; }
                PlayerController pController = _characterController.GetComponent<PlayerController>();
                if (pController != null) pController.SetSprintAbility(true);
                if (RitualManager.Instance != null) RitualManager.Instance.SetupRitualPhase();
            }
            else
            {
                _playerTransform.position = _startPoint.position;
                _playerTransform.rotation = _startPoint.rotation;
                PlayerController pController = _characterController.GetComponent<PlayerController>();
                if (pController != null) pController.SetSprintAbility(false);
                if (RitualManager.Instance != null) RitualManager.Instance.EndRitualPhase();
            }

            Physics.SyncTransforms();
            foreach (var obj in _resettableObjects) if (obj != null) obj.OnLoopReset(loop);

            ExitDoor[] exits = FindObjectsByType<ExitDoor>(FindObjectsSortMode.None);
            foreach (var exit in exits) exit.ResetDoor();

            // ประมวลผลและสุ่ม Anomaly ใหม่
            if (AnomalyManager.Instance != null) AnomalyManager.Instance.ProcessLoop(loop);

            yield return null;
            if (_characterController != null) _characterController.enabled = true;
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