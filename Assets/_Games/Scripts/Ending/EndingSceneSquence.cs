using UnityEngine;
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

        private void Start()
        {
            // 1. ทำการ "ล้างบาง" ก้อน Manager ที่ตามมาจากฉากหลัก
            // สมมติว่า GameManager, UIManager ของคุณอยู่ในก้อนชื่อ "--- MANAGERS ---"
            GameObject managersObj = GameObject.Find("--- MANAGERS ---");
            if (managersObj != null)
            {
                Destroy(managersObj);
            }
            else
            {
                // ถ้าไม่ได้รวมเป็นก้อนเดียว ก็ไล่ทำลายทีละตัว
                if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
                if (UIManager.Instance != null) Destroy(UIManager.Instance.gameObject);
                if (LoopManager.Instance != null) Destroy(LoopManager.Instance.gameObject);
                if (SoundManager.Instance != null) Destroy(SoundManager.Instance.gameObject);
            }

            // 2. ล้างความทรงจำเนื้อเรื่อง (Static Variable) 
            StoryTrigger.ResetAllStoryMemory();

            // 3. ปลดล็อกเมาส์ เผื่อติดมาจากฉากเดิน
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 4. เริ่มนับถอยหลังจบฉาก
            StartCoroutine(WaitAndReturnToMainMenu());
        }

        private IEnumerator WaitAndReturnToMainMenu()
        {
            yield return new WaitForSeconds(_timeToWait);
            //maybeใส่fade out
            SceneManager.LoadScene(_mainSceneName);
        }
    }
}