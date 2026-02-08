using UnityEngine;
using UnityEngine.UI;
using TMPro; // ต้องการ TextMeshPro

namespace SyntaxError.Managers
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Image _crosshair; // จุดกลางจอ
        [SerializeField] private TextMeshProUGUI _loopText; // ตัวเลขบอก Loop
        [SerializeField] private GameObject _winScreen; // หน้าจอชนะ

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            UpdateLoopDisplay(0);
            if (_winScreen != null) _winScreen.SetActive(false);

            // ซ่อนเมาส์
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UpdateLoopDisplay(int loopCount)
        {
            if (_loopText != null) _loopText.text = $"Loop: {loopCount}";
        }

        public void ShowWinScreen()
        {
            if (_winScreen != null) _winScreen.SetActive(true);

            // ปลดล็อกเมาส์
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // หยุดการควบคุมผู้เล่น
            // Player.GetComponent<PlayerController>().enabled = false;
        }
    }
}