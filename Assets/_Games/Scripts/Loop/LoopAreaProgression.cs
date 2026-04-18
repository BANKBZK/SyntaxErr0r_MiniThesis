using UnityEngine;
using SyntaxError.Interfaces;
using SyntaxError.Managers;

namespace SyntaxError.Ritual
{
    public class LoopAreaProgression : MonoBehaviour, IResettable
    {
        [Header("Stage GameObjects")]
        [Tooltip("ฉากสำหรับ Loop 1-3")]
        [SerializeField] private GameObject _stage1;

        [Tooltip("ฉากสำหรับ Loop 4-6")]
        [SerializeField] private GameObject _stage2;

        [Tooltip("ฉากสำหรับ Loop 7 ขึ้นไป")]
        [SerializeField] private GameObject _stage3;

        private void Start()
        {
            // ลงทะเบียนเพื่อรับคำสั่ง Reset เมื่อมีการวาร์ปเปลี่ยน Loop
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.Register(this);
            }

            // อัปเดตฉากครั้งแรกเมื่อเริ่มเกม
            int startLoop = GameManager.Instance != null ? GameManager.Instance.CurrentLoop : 0;
            UpdateAreaVisuals(startLoop);
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.Unregister(this);
            }
        }

        // ฟังก์ชันนี้จะถูก LoopManager เรียกอัตโนมัติทุกครั้งที่เปลี่ยนลูป
        public void OnLoopReset(int currentLoop)
        {
            UpdateAreaVisuals(currentLoop);
        }

        private void UpdateAreaVisuals(int loop)
        {
            // ปิดทุก Stage ก่อนเพื่อความชัวร์
            if (_stage1) _stage1.SetActive(false);
            if (_stage2) _stage2.SetActive(false);
            if (_stage3) _stage3.SetActive(false);

            // เงื่อนไขการเปิดตามที่คุณระบุ:
            // Loop 1-3 -> Stage 1
            if (loop >= 1 && loop <= 3)
            {
                if (_stage1) _stage1.SetActive(true);
            }
            // Loop 4-6 -> Stage 2
            else if (loop >= 4 && loop <= 6)
            {
                if (_stage2) _stage2.SetActive(true);
            }
            // Loop 7 -> Stage 3
            else if (loop >= 7)
            {
                if (_stage3) _stage3.SetActive(true);
            }

            Debug.Log($"<color=green>[LoopArea] อัปเดตฉากเป็น Stage ที่สอดคล้องกับ Loop {loop} แล้ว</color>");
        }
    }
}