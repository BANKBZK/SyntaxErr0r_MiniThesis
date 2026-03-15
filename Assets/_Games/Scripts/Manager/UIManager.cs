using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using SyntaxError.Inputs;

namespace SyntaxError.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Player Control")]
        [Tooltip("ลากตัว Player ที่มี InputManager มาใส่ เพื่อใช้เปิด/ปิดการควบคุม")]
        [SerializeField] private InputManager _playerInput;

        [Header("UI Panels")]
        public GameObject mainMenuUI;
        public GameObject optionUI;
        public GameObject creditUI;
        public GameObject hudUI;
        public GameObject pauseUI;

        [Header("HUD Elements")]
        public TextMeshProUGUI loopText;

        [Header("Fade Settings")]
        public CanvasGroup fadeUI;
        public float fadeDuration = 1.0f;

        private InputSystem_Actions _inputActions;
        private bool _isGameStarted = false;
        private bool _isPaused = false;
        private bool _isTransitioning = false;

        [Header("Story UI")]
        public TextMeshProUGUI storyText;
        public float storyFadeSpeed = 1.5f;

        [Header("Exhaustion VFX")]
        public CanvasGroup exhaustionUI;
        public float exhaustionPulseSpeed = 2f;
        public float maxExhaustionDarkness = 0.7f;

        private Coroutine _exhaustionRoutine;

        // ==========================================
        // 🛠️ ส่วนที่เพิ่มใหม่: ระบบ Debug AI
        // ==========================================
        [Header("Debug UI")]
        public TextMeshProUGUI aiDebugText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            _inputActions.UI.Enable();
            _inputActions.UI.Cancel.performed += OnCancelPressed;
        }

        private void OnDisable()
        {
            _inputActions.UI.Cancel.performed -= OnCancelPressed;
            _inputActions.UI.Disable();
        }

        private void Start()
        {
            ShowMainMenu();
            UpdateLoopDisplay(0);
        }

        private void ShowMainMenu()
        {
            _isGameStarted = false;

            if (_playerInput != null) _playerInput.enabled = false;

            mainMenuUI.SetActive(true);
            if (optionUI != null) optionUI.SetActive(false);
            if (creditUI != null) creditUI.SetActive(false);
            if (hudUI != null) hudUI.SetActive(false);
            if (pauseUI != null) pauseUI.SetActive(false);

            SetCursorState(true);

            if (fadeUI != null)
            {
                fadeUI.alpha = 1f;
                StartCoroutine(FadeRoutine(1f, 0f));
            }
        }

        public void OnStartGame()
        {
            if (_isTransitioning || _isGameStarted) return;
            StartCoroutine(StartGameSequence());
        }

        private IEnumerator StartGameSequence()
        {
            _isTransitioning = true;
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");

            if (fadeUI != null)
            {
                fadeUI.blocksRaycasts = true;
                yield return StartCoroutine(FadeRoutine(0f, 1f));
            }

            _isGameStarted = true;
            mainMenuUI.SetActive(false);
            if (optionUI != null) optionUI.SetActive(false);
            if (creditUI != null) creditUI.SetActive(false);
            if (hudUI != null) hudUI.SetActive(true);

            if (_playerInput != null) _playerInput.enabled = true;
            SetCursorState(false);

            if (fadeUI != null)
            {
                yield return StartCoroutine(FadeRoutine(1f, 0f));
                fadeUI.blocksRaycasts = false;
            }
            _isTransitioning = false;
        }

        private void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (_isTransitioning) return;

            if (!_isGameStarted)
            {
                if ((optionUI != null && optionUI.activeSelf) || (creditUI != null && creditUI.activeSelf))
                {
                    if (optionUI != null) optionUI.SetActive(false);
                    if (creditUI != null) creditUI.SetActive(false);
                    mainMenuUI.SetActive(true);

                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UICancelPress");
                }
            }
            else
            {
                if (_isPaused) ResumeGame();
                else PauseGame();
            }
        }

        public void PauseGame()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            if (pauseUI != null) pauseUI.SetActive(true);
            if (hudUI != null) hudUI.SetActive(false);

            if (_playerInput != null) _playerInput.enabled = false;
            SetCursorState(true);
        }

        public void ResumeGame()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            if (pauseUI != null) pauseUI.SetActive(false);
            if (hudUI != null) hudUI.SetActive(true);

            if (_playerInput != null) _playerInput.enabled = true;
            SetCursorState(false);
        }

        public void OnOpenOption()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            if (optionUI != null) optionUI.SetActive(true);
        }

        public void OnCreditOpen()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            if (creditUI != null) creditUI.SetActive(true);
        }

        public void OnExitGame()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            Application.Quit();
            Debug.Log("Game Exited");
        }

        private void SetCursorState(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
        {
            float timer = 0f;
            if (fadeUI != null) fadeUI.alpha = startAlpha;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                if (fadeUI != null) fadeUI.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);
                yield return null;
            }

            if (fadeUI != null) fadeUI.alpha = endAlpha;
        }

        public void UpdateLoopDisplay(int currentLoop)
        {
            if (loopText != null) loopText.text = $"Loop : {currentLoop}";
        }

        // ==========================================
        // Story System
        // ==========================================
        public void ShowStoryText(string text, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(StoryTextRoutine(text, duration));
        }

        private IEnumerator StoryTextRoutine(string text, float duration)
        {
            if (storyText == null) yield break;

            storyText.text = text;
            storyText.gameObject.SetActive(true);

            Color color = storyText.color;
            color.a = 0f;
            storyText.color = color;

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * storyFadeSpeed;
                color.a = t;
                storyText.color = color;
                yield return null;
            }

            yield return new WaitForSeconds(duration);

            t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime * storyFadeSpeed;
                color.a = t;
                storyText.color = color;
                yield return null;
            }

            storyText.gameObject.SetActive(false);
        }

        // ==========================================
        // Exhaustion System
        // ==========================================
        public void ToggleExhaustionEffect(bool isExhausted)
        {
            if (isExhausted)
            {
                if (_exhaustionRoutine == null)
                {
                    if (exhaustionUI != null) exhaustionUI.gameObject.SetActive(true);
                    _exhaustionRoutine = StartCoroutine(ExhaustionPulseRoutine());
                }
            }
            else
            {
                if (_exhaustionRoutine != null)
                {
                    StopCoroutine(_exhaustionRoutine);
                    _exhaustionRoutine = null;
                }

                if (exhaustionUI != null)
                {
                    exhaustionUI.alpha = 0f;
                    exhaustionUI.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator ExhaustionPulseRoutine()
        {
            while (true)
            {
                if (exhaustionUI != null)
                {
                    float alpha = Mathf.PingPong(Time.time * exhaustionPulseSpeed, maxExhaustionDarkness);
                    exhaustionUI.alpha = Mathf.Clamp(alpha, 0.2f, maxExhaustionDarkness);
                }
                yield return null;
            }
        }

        // ==========================================
        // 🛠️ ฟังก์ชันเรียกอัปเดต Debug ข้อความผี
        // ==========================================
        public void UpdateAIDebugText(string text)
        {
            if (aiDebugText != null)
            {
                aiDebugText.text = text;
            }
        }
    }
}