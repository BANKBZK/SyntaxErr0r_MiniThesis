using UnityEngine;
using UnityEngine.UI; // ต้องใช้สำหรับ Image
using UnityEngine.SceneManagement;
using System.Collections;
using SyntaxError.Story;

namespace SyntaxError.Managers
{
    public class EndingSceneSquence : MonoBehaviour
    {
        [Header("Ending Settings")]
        [Tooltip("เวลาที่ปล่อยให้ฉาก Cinematic เล่นจนจบ (วินาที) ก่อนจะเด้งกลับ Main Menu")]
        [SerializeField] private float _timeToWait = 15f;

        [Tooltip("ชื่อ Scene หลักของเกม (ที่มี Main Menu)")]
        [SerializeField] private string _mainSceneName = "MainScene";

        [Header("Fade Effects")]
        [Tooltip("สีของจอสว่างตอนเริ่มฉาก")]
        [SerializeField] private Color _fadeStartColor = Color.white;
        [Tooltip("เวลาที่ค่อยๆ หรี่จอสว่างลงจนเห็นฉาก (วินาที)")]
        [SerializeField] private float _fadeInDuration = 3f;
        [Tooltip("เวลาที่ค่อยๆ เฟดจอมืดลงก่อนตัดจบไปเมนู (วินาที)")]
        [SerializeField] private float _fadeOutDuration = 2f;

        private Image _fadeImage; // ตัวแปรเก็บแผ่นสีที่จะใช้เฟด

        private void Start()
        {
            // 1. ทำการ "ล้างบาง" ก้อน Manager ที่ตามมาจากฉากหลัก
            GameObject managersObj = GameObject.Find("--- MANAGERS ---");
            if (managersObj != null)
            {
                Destroy(managersObj);
            }
            else
            {
                if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
                if (UIManager.Instance != null) Destroy(UIManager.Instance.gameObject);
                if (LoopManager.Instance != null) Destroy(LoopManager.Instance.gameObject);
                if (SoundManager.Instance != null) Destroy(SoundManager.Instance.gameObject);
            }

            // 2. ล้างความทรงจำเนื้อเรื่อง
            StoryTrigger.ResetAllStoryMemory();

            // 3. ปลดล็อกเมาส์
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 4. สร้าง UI หน้าจอสว่าง และเริ่มการนับถอยหลัง
            CreateFadeOverlay();
            StartCoroutine(EndingRoutine());
        }

        // ==========================================
        // ฟังก์ชันสร้างหน้าจอสีขาวอัตโนมัติ ไม่ต้องพึ่ง UI ในฉาก
        // ==========================================
        private void CreateFadeOverlay()
        {
            // สร้าง Canvas
            GameObject canvasObj = new GameObject("EndingFadeCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // ให้อยู่หน้าสุดบังทุกอย่าง

            // สร้างแผ่นภาพ (Image) คลุมจอ
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);
            _fadeImage = imageObj.AddComponent<Image>();

            // ตั้งค่าให้เต็มจอ
            RectTransform rect = _fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // ตั้งสีเริ่มต้นเป็นสีสว่างสุด และปิดการบังคลิกเมาส์
            _fadeImage.color = _fadeStartColor;
            _fadeImage.raycastTarget = false;
        }

        private IEnumerator EndingRoutine()
        {
            // 🎬 เฟส 1: Fade In (จากสว่างวาบ ค่อยๆ หายไปจนเห็นฉาก)
            float t = 0f;
            Color clearColor = new Color(_fadeStartColor.r, _fadeStartColor.g, _fadeStartColor.b, 0f);

            while (t < _fadeInDuration)
            {
                t += Time.deltaTime;
                _fadeImage.color = Color.Lerp(_fadeStartColor, clearColor, t / _fadeInDuration);
                yield return null;
            }
            _fadeImage.color = clearColor; // บังคับใสสนิทเมื่อจบเฟส

            // 🎬 เฟส 2: นั่งดูฉาก Cinematic 
            // เอาเวลาที่ตั้งไว้ หักลบเวลาเฟดฉากจบออก เพื่อให้เวลาดูรวมๆ ตรงกับที่คุณตั้งไว้เป๊ะๆ
            float actualWaitTime = Mathf.Max(0, _timeToWait - _fadeOutDuration);
            yield return new WaitForSeconds(actualWaitTime);

            // 🎬 เฟส 3: Fade Out (จากใส ค่อยๆ มืดลงก่อนตัดจบ)
            t = 0f;
            Color endColor = Color.black; // ปกติตอนจบมักจะตัดเข้าจอดำ แต่ถ้าอยากให้ขาวก็เปลี่ยนเป็น Color.white ครับ

            while (t < _fadeOutDuration)
            {
                t += Time.deltaTime;
                _fadeImage.color = Color.Lerp(clearColor, endColor, t / _fadeOutDuration);
                yield return null;
            }
            _fadeImage.color = endColor; // บังคับดำสนิทก่อนโหลดหน้า

            // 🎬 โหลดหน้า Main Menu
            SceneManager.LoadScene(_mainSceneName);
        }
    }
}