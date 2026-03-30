using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using SyntaxError.Inputs;
using UnityEngine.SceneManagement;
using SyntaxError.Story;

namespace SyntaxError.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Scene Management")]
        [Tooltip("ชื่อ Scene หลักของเกมเพื่อใช้ตอนกดกลับ Main Menu")]
        public string mainSceneName = "MainScene";

        [Header("Player Control")]
        [SerializeField] private InputManager _playerInput;

        [Header("UI Panels")]
        public GameObject mainMenuUI;
        public GameObject optionUI;
        public GameObject creditUI;
        public GameObject hudUI;
        public GameObject pauseUI;

        [Header("Settings Sub-Panels")]
        [Tooltip("หน้าต่างย่อยในหน้า Option")]
        public GameObject generalSettingsUI;
        public GameObject videoSettingsUI;
        public GameObject audioSettingsUI;

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

        [Header("Debug UI")]
        public TextMeshProUGUI aiDebugText;

        [Header("Flashlight UI")]
        public CanvasGroup batteryCanvasGroup;
        public GameObject[] batteryBlocks;
        public float batteryFadeSpeed = 2f;

        private Coroutine _batteryFadeRoutine;
        private bool _isFlashlightOn = false;

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
            if (batteryCanvasGroup != null) batteryCanvasGroup.alpha = 0f;
        }

        private void ShowMainMenu()
        {
            _isGameStarted = false;

            if (_playerInput != null) _playerInput.enabled = false;

            if (mainMenuUI != null) mainMenuUI.SetActive(true);
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
            if (mainMenuUI != null) mainMenuUI.SetActive(false);
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

        // ==========================================
        // 🔄 ระบบปุ่ม ESC หรือปุ่ม Cancel (ทำงานเป็น Step-by-Step)
        // ==========================================
        private void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (_isTransitioning) return;

            // 1. ถ้าเปิดหน้า Option อยู่ ให้ปิด Option แล้วกลับไปหน้าก่อนหน้า
            if (optionUI != null && optionUI.activeSelf)
            {
                CloseOptionMenu();
                return;
            }

            // 2. ถ้าเปิดหน้า Credit อยู่ ให้ปิด Credit แล้วกลับไป Main Menu
            if (creditUI != null && creditUI.activeSelf)
            {
                CloseCreditMenu();
                return;
            }

            // 3. ถ้าอยู่ในเกม และไม่ได้เปิดหน้าต่างอะไรซ้อนอยู่ ให้สลับการ Pause/Resume
            if (_isGameStarted)
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

        // ==========================================
        // ⚙️ ระบบ Options Menu
        // ==========================================

        // ฟังก์ชันเดียวใช้ร่วมกันทั้งปุ่ม Option ใน Main Menu และ Pause Menu
        public void OnOpenOption()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");

            // ให้โค้ดเช็คเองว่ากดมาจากหน้าไหน
            if (!_isGameStarted)
            {
                // ถ้ายังไม่เริ่มเกม แปลว่าอยู่หน้า Main Menu ให้ซ่อน Main Menu
                if (mainMenuUI != null) mainMenuUI.SetActive(false);
            }
            else
            {
                // ถ้าเริ่มเกมไปแล้ว แปลว่าอยู่หน้า Pause Menu ให้ซ่อน Pause Menu
                if (pauseUI != null) pauseUI.SetActive(false);
            }

            if (optionUI != null) optionUI.SetActive(true);
            OpenGeneralSettings(); // เปิดมาให้แสดงหน้า General ก่อนเสมอ
        }

        // ฟังก์ชันสำหรับปุ่ม Back หรือการกด ESC เพื่อออกจากหน้า Option
        public void CloseOptionMenu()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UICancelPress");
            if (optionUI != null) optionUI.SetActive(false);

            // เช็คว่ากลับไปที่ไหน
            if (!_isGameStarted)
            {
                if (mainMenuUI != null) mainMenuUI.SetActive(true);
            }
            else
            {
                if (pauseUI != null) pauseUI.SetActive(true);
            }
        }

        // --- ฟังก์ชันสำหรับสลับหน้าต่างย่อยใน Settings ---
        public void OpenGeneralSettings()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            if (generalSettingsUI != null) generalSettingsUI.SetActive(true);
            if (videoSettingsUI != null) videoSettingsUI.SetActive(false);
            if (audioSettingsUI != null) audioSettingsUI.SetActive(false);
        }

        public void OpenVideoSettings()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            if (generalSettingsUI != null) generalSettingsUI.SetActive(false);
            if (videoSettingsUI != null) videoSettingsUI.SetActive(true);
            if (audioSettingsUI != null) audioSettingsUI.SetActive(false);
        }

        public void OpenAudioSettings()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            if (generalSettingsUI != null) generalSettingsUI.SetActive(false);
            if (videoSettingsUI != null) videoSettingsUI.SetActive(false);
            if (audioSettingsUI != null) audioSettingsUI.SetActive(true);
        }

        // ==========================================
        // 📜 ระบบ Credits
        // ==========================================
        public void OnCreditOpen()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            if (mainMenuUI != null) mainMenuUI.SetActive(false);
            if (creditUI != null) creditUI.SetActive(true);
        }

        public void CloseCreditMenu()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UICancelPress");
            if (creditUI != null) creditUI.SetActive(false);
            if (mainMenuUI != null) mainMenuUI.SetActive(true);
        }

        public void OnExitGame()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            Application.Quit();
            Debug.Log("Game Exited");
        }

        public void OnReturnToMainMenu()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");

            Time.timeScale = 1f;
            StoryTrigger.ResetAllStoryMemory();

            GameObject managersObj = GameObject.Find("--- MANAGERS ---");
            if (managersObj != null)
            {
                Destroy(managersObj);
            }
            else
            {
                if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
                if (LoopManager.Instance != null) Destroy(LoopManager.Instance.gameObject);
                if (SoundManager.Instance != null) Destroy(SoundManager.Instance.gameObject);
            }

            SceneManager.LoadScene(mainSceneName);
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

        public void UpdateAIDebugText(string text)
        {
            if (aiDebugText != null)
            {
                aiDebugText.text = text;
            }
        }

        public void UpdateBatteryUI(float current, float max)
        {
            if (batteryBlocks == null || batteryBlocks.Length == 0) return;

            int activeBlocks = Mathf.CeilToInt((current / max) * batteryBlocks.Length);

            for (int i = 0; i < batteryBlocks.Length; i++)
            {
                batteryBlocks[i].SetActive(i < activeBlocks);
            }
        }

        public void SetFlashlightState(bool isOn)
        {
            _isFlashlightOn = isOn;
            if (_batteryFadeRoutine != null) StopCoroutine(_batteryFadeRoutine);

            if (isOn)
            {
                if (batteryCanvasGroup != null) batteryCanvasGroup.alpha = 1f;
            }
            else
            {
                _batteryFadeRoutine = StartCoroutine(FadeBatteryOutRoutine(0f));
            }
        }

        public void ShowBatteryTemp()
        {
            if (_isFlashlightOn) return;

            if (_batteryFadeRoutine != null) StopCoroutine(_batteryFadeRoutine);

            _batteryFadeRoutine = StartCoroutine(FadeBatteryOutRoutine(3.0f));
        }

        private IEnumerator FadeBatteryOutRoutine(float delayBeforeFade)
        {
            if (batteryCanvasGroup != null) batteryCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(delayBeforeFade);

            while (batteryCanvasGroup != null && batteryCanvasGroup.alpha > 0f)
            {
                batteryCanvasGroup.alpha -= Time.deltaTime * batteryFadeSpeed;
                yield return null;
            }
        }
    }
}