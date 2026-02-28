using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using SyntaxError.Inputs; // เรียกใช้ InputManager ของผู้เล่น

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
        private bool _isTransitioning = false; // กันคนกดปุ่มรัวๆ ตอนกำลังจอดำ

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

            // ปิดการบังคับตัวละคร
            if (_playerInput != null) _playerInput.enabled = false;

            // โชว์/ซ่อน UI
            mainMenuUI.SetActive(true);
            if (optionUI != null) optionUI.SetActive(false);
            if (creditUI != null) creditUI.SetActive(false);
            if (hudUI != null) hudUI.SetActive(false);
            if (pauseUI != null) pauseUI.SetActive(false);

            // ปลดล็อกเมาส์ให้กดเมนูได้
            SetCursorState(true);

            // เฟดภาพสว่างขึ้น (เผื่อทำฉากหลังสวยๆ ไว้ตรงจุดเกิด)
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
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("ButtonPress");
            // 2. เฟดจอดำ
            if (fadeUI != null)
            {
                fadeUI.blocksRaycasts = true;
                yield return StartCoroutine(FadeRoutine(0f, 1f));
            }
            // 3. สลับ UI หลังจอดำ
            _isGameStarted = true;
            mainMenuUI.SetActive(false);
            if (optionUI != null) optionUI.SetActive(false);
            if (creditUI != null) creditUI.SetActive(false);
            if (hudUI != null) hudUI.SetActive(true); // เปิดเป้าเล็ง
            // 4. เปิดการควบคุมตัวละคร และล็อกเมาส์
            if (_playerInput != null) _playerInput.enabled = true;
            SetCursorState(false);
            // 5. เฟดจอสว่าง
            if (fadeUI != null)
            {
                yield return StartCoroutine(FadeRoutine(1f, 0f));
                fadeUI.blocksRaycasts = false;
            }
            _isTransitioning = false;
        }
        // --- ระบบปุ่ม Cancel (ESC) แบบครอบจักรวาล ---
        private void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (_isTransitioning) return;
            // กรณี 1: อยู่ใน Main Menu และเปิดหน้า Option/Credit ค้างไว้
            if (!_isGameStarted)
            {
                if ((optionUI != null && optionUI.activeSelf) || (creditUI != null && creditUI.activeSelf))
                {
                    if (optionUI != null) optionUI.SetActive(false);
                    if (creditUI != null) creditUI.SetActive(false);
                    mainMenuUI.SetActive(true);

                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("Click");
                }
            }
            // กรณี 2: เล่นเกมอยู่ ให้สลับ Pause / Resume
            else
            {
                if (_isPaused) ResumeGame();
                else PauseGame();
            }
        }
        public void PauseGame()
        {
            _isPaused = true;
            Time.timeScale = 0f; // หยุดเวลา (ตัวละครจะหยุดนิ่ง)
            if (pauseUI != null) pauseUI.SetActive(true);
            if (hudUI != null) hudUI.SetActive(false);
            // ปิด Input ชั่วคราว และปลดเมาส์
            if (_playerInput != null) _playerInput.enabled = false;
            SetCursorState(true);
        }
        public void ResumeGame()
        {
            _isPaused = false;
            Time.timeScale = 1f; // เดินเวลาปกติ
            if (pauseUI != null) pauseUI.SetActive(false);
            if (hudUI != null) hudUI.SetActive(true);
            // เปิด Input และล็อกเมาส์
            if (_playerInput != null) _playerInput.enabled = true;
            SetCursorState(false);
        }

        public void OnExitGame()
        {
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
    }
}