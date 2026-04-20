using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic; // เพิ่มเข้ามาเพื่อใช้งาน Queue
using SyntaxError.Inputs;
using UnityEngine.SceneManagement;
using SyntaxError.Story;
using UnityEngine.UI;

namespace SyntaxError.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Scene Management")]
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
        public GameObject generalSettingsUI;
        public GameObject videoSettingsUI;
        public GameObject audioSettingsUI;

        // ==========================================
        // ⚙️ ระบบ Settings Variables
        // ==========================================
        [Header("--- General Settings ---")]
        public TextMeshProUGUI mouseSensitivityText;
        private int _currentSensitivity = 5;
        public int minSensitivity = 1;
        public int maxSensitivity = 10;

        [Header("--- Video Settings ---")]
        public TextMeshProUGUI graphicsQualityText;
        private int _graphicsQualityIndex = 2; // 0 = Low, 1 = Medium, 2 = High
        private readonly string[] _qualityNames = { "Low", "Medium", "High" };

        [Header("--- Audio Settings ---")]
        public Slider masterSlider;
        public Slider sfxSlider;
        public Slider environmentSlider;
        public Slider uiSlider;
        public Slider musicSlider;

        [Header("HUD Elements")]
        public TextMeshProUGUI loopText;

        [Header("Fade Settings")]
        public CanvasGroup fadeUI;
        public float fadeDuration = 1.0f;

        private InputSystem_Actions _inputActions;

        // [แก้ไข] เปลี่ยนเป็น Public Property เพื่อให้ Script อื่นๆ (เช่น StoryTrigger) เช็คได้ว่าเริ่มเกมหรือยัง
        public bool IsGameStarted { get; private set; } = false;

        private bool _isPaused = false;
        private bool _isTransitioning = false;

        [Header("Story UI")]
        public TextMeshProUGUI storyText;
        public float storyFadeSpeed = 1.5f;

        // ------------------------------------------
        // [เพิ่ม] ระบบคิวของ Story Message
        // ------------------------------------------
        private struct StoryMessage
        {
            public string text;
            public float duration;
        }
        private Queue<StoryMessage> _storyQueue = new Queue<StoryMessage>();
        private bool _isDisplayingStory = false;

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

            LoadSettings();
        }

        private void LoadSettings()
        {
            // --- General ---
            _currentSensitivity = PlayerPrefs.GetInt("MouseSensitivity", 5);
            UpdateSensitivityUI();

            // --- Video ---
            _graphicsQualityIndex = PlayerPrefs.GetInt("GraphicsQuality", 2);
            ApplyGraphicsQuality();

            // --- Audio ---
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            if (masterSlider) masterSlider.value = masterVol;
            OnMasterVolumeChanged(masterVol);

            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);
            if (sfxSlider) sfxSlider.value = sfxVol;
            OnSFXVolumeChanged(sfxVol);

            float envVol = PlayerPrefs.GetFloat("EnvVolume", 1f);
            if (environmentSlider) environmentSlider.value = envVol;
            OnEnvironmentVolumeChanged(envVol);

            float uiVol = PlayerPrefs.GetFloat("UIVolume", 1f);
            if (uiSlider) uiSlider.value = uiVol;
            OnUIVolumeChanged(uiVol);
            float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
            if (musicSlider) musicSlider.value = musicVol;
            OnMusicVolumeChanged(musicVol);
        }

        public void IncreaseSensitivity()
        {
            if (_currentSensitivity < maxSensitivity)
            {
                _currentSensitivity++;
                PlayerPrefs.SetInt("MouseSensitivity", _currentSensitivity);
                UpdateSensitivityUI();
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            }
        }

        public void DecreaseSensitivity()
        {
            if (_currentSensitivity > minSensitivity)
            {
                _currentSensitivity--;
                PlayerPrefs.SetInt("MouseSensitivity", _currentSensitivity);
                UpdateSensitivityUI();
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
            }
        }

        private void UpdateSensitivityUI()
        {
            if (mouseSensitivityText != null)
                mouseSensitivityText.text = _currentSensitivity.ToString();

            SyntaxError.Player.CameraController cam = Object.FindFirstObjectByType<SyntaxError.Player.CameraController>();
            if (cam != null)
            {
                cam.SetSensitivity(_currentSensitivity);
            }
        }

        public void NextGraphicsQuality()
        {
            _graphicsQualityIndex++;
            if (_graphicsQualityIndex > 2) _graphicsQualityIndex = 0;

            ApplyGraphicsQuality();
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
        }

        public void PreviousGraphicsQuality()
        {
            _graphicsQualityIndex--;
            if (_graphicsQualityIndex < 0) _graphicsQualityIndex = 2;

            ApplyGraphicsQuality();
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UIPress");
        }

        private void ApplyGraphicsQuality()
        {
            QualitySettings.SetQualityLevel(_graphicsQualityIndex, true);
            PlayerPrefs.SetInt("GraphicsQuality", _graphicsQualityIndex);

            if (graphicsQualityText != null)
                graphicsQualityText.text = _qualityNames[_graphicsQualityIndex];
        }

        public void OnMasterVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            if (SoundManager.Instance != null) SoundManager.Instance.SetMixerVolume("MasterVol", value);
        }

        public void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            if (SoundManager.Instance != null) SoundManager.Instance.SetMixerVolume("SFXVol", value);
        }

        public void OnEnvironmentVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("EnvVolume", value);
            if (SoundManager.Instance != null) SoundManager.Instance.SetMixerVolume("EnvVol", value);
        }

        public void OnUIVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("UIVolume", value);
            if (SoundManager.Instance != null) SoundManager.Instance.SetMixerVolume("UIVol", value);
        }
        public void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            if (SoundManager.Instance != null) SoundManager.Instance.SetMixerVolume("MusicVol", value);
        }

        private void ShowMainMenu()
        {
            IsGameStarted = false;
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
            if (_isTransitioning || IsGameStarted) return;
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

            IsGameStarted = true;
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

        private void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (_isTransitioning) return;

            if (optionUI != null && optionUI.activeSelf)
            {
                CloseOptionMenu();
                return;
            }

            if (creditUI != null && creditUI.activeSelf)
            {
                CloseCreditMenu();
                return;
            }

            if (IsGameStarted)
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

            if (!IsGameStarted)
            {
                if (mainMenuUI != null) mainMenuUI.SetActive(false);
            }
            else
            {
                if (pauseUI != null) pauseUI.SetActive(false);
            }

            if (optionUI != null) optionUI.SetActive(true);
            OpenGeneralSettings();
        }

        public void CloseOptionMenu()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("UICancelPress");
            if (optionUI != null) optionUI.SetActive(false);

            if (!IsGameStarted)
            {
                if (mainMenuUI != null) mainMenuUI.SetActive(true);
            }
            else
            {
                if (pauseUI != null) pauseUI.SetActive(true);
            }
        }

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
            SyntaxError.Events.StoryObject.ResetAllObjectMemory();

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

        // ==========================================
        // 📜 Story UI & Queue System
        // ==========================================
        public void ShowStoryText(string text, float duration)
        {
            // จับข้อความใส่คิวเอาไว้ก่อน
            _storyQueue.Enqueue(new StoryMessage { text = text, duration = duration });

            // ถ้ายังไม่มีข้อความไหนกำลังโชว์อยู่ ให้เริ่มรันคิวเลย
            if (!_isDisplayingStory)
            {
                StartCoroutine(ProcessStoryQueue());
            }
        }

        private IEnumerator ProcessStoryQueue()
        {
            _isDisplayingStory = true;

            // รันไปเรื่อยๆ จนกว่าคิวจะหมด
            while (_storyQueue.Count > 0)
            {
                StoryMessage currentMsg = _storyQueue.Dequeue();

                // รอให้ข้อความนี้แสดงและ Fade หายไปจนเสร็จ
                yield return StartCoroutine(StoryTextRoutine(currentMsg.text, currentMsg.duration));

                // พักหน้าจอโล่งๆ แป๊บนึงก่อนขึ้นข้อความถัดไป จะได้อ่านง่ายขึ้น
                yield return new WaitForSeconds(0.5f);
            }

            _isDisplayingStory = false;
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
        // VFX & Debug UI
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