using UnityEngine;
using SyntaxError.Interfaces;
using SyntaxError.Managers;

namespace SyntaxError.Environment
{
    public class LoopDisplayBoard : MonoBehaviour, IResettable
    {
        [Header("Display Settings")]
        [Tooltip("ลาก GameObject ตัวเลขมาใส่เรียงลำดับ 0, 1, 2... (Index 0 = เลข 0)")]
        [SerializeField] private GameObject[] _loopObjects;

        private void Start()
        {
            // ลงทะเบียนกับ LoopManager เพื่อให้ได้รับคำสั่งรีเซ็ตอัตโนมัติ
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.Register(this);
            }

            // อัปเดตหน้าตาป้ายครั้งแรกตามค่าปัจจุบันใน GameManager
            if (GameManager.Instance != null)
            {
                UpdateDisplay(GameManager.Instance.CurrentLoop);
            }
        }

        private void OnDestroy()
        {
            // ถอนชื่อออกเมื่อ Object ถูกทำลาย
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.Unregister(this);
            }
        }

        // ==========================================
        // ฟังก์ชันนี้จะถูกเรียกโดย LoopManager ตอนจอมืด
        // ==========================================
        public void OnLoopReset(int currentLoop)
        {
            UpdateDisplay(currentLoop);
        }

        private void UpdateDisplay(int currentLoop)
        {
            if (_loopObjects == null || _loopObjects.Length == 0) return;

            // วนลูปปิด GameObject ทุกตัวก่อน
            for (int i = 0; i < _loopObjects.Length; i++)
            {
                if (_loopObjects[i] != null)
                {
                    _loopObjects[i].SetActive(false);
                }
            }

            // ตรวจสอบว่าเลข Loop ปัจจุบัน มี GameObject รองรับไหม
            // ถ้า Loop สูงเกินจำนวนที่มี จะเปิดตัวสุดท้ายค้างไว้ (หรือนายจะเพิ่มเงื่อนไขอื่นก็ได้)
            int indexToActivate = Mathf.Clamp(currentLoop, 0, _loopObjects.Length - 1);

            if (_loopObjects[indexToActivate] != null)
            {
                _loopObjects[indexToActivate].SetActive(true);
                Debug.Log($"[LoopDisplay] {gameObject.name} updated to Loop {currentLoop}");
            }
        }
    }
}