using UnityEngine;
using SyntaxError.Managers;

namespace SyntaxError.Anomaly
{
    // บังคับให้ต้องมี Collider เพื่อทำ Trigger
    [RequireComponent(typeof(Collider))]
    public class AnomalyTriggerObject : AnomalyObject
    {
        [Header("Surprise Settings")]
        [Tooltip("เสียงที่จะเล่นตอนผีโผล่มาหลอก (เว้นว่างได้)")]
        [SerializeField] private string _jumpscareSound = "";

        // ตัวแปรเช็คว่า "รอบนี้โดนสุ่มเลือกให้เป็นผีไหม?"
        private bool _isArmed = false;

        private void Awake()
        {
            // บังคับให้เป็น Trigger เสมอ
            GetComponent<Collider>().isTrigger = true;
        }

        public override void ActivateAnomaly()
        {
            // แทนที่จะเปิดโมเดลผีทันที เราแค่ "ตั้งค่ากับดัก" เอาไว้
            _isArmed = true;
            Debug.Log($"[TriggerAnomaly] {_anomalyName} is armed and waiting in the shadows...");
        }

        // -----------------------------------------------------
        // 2. เขียนทับคำสั่งตอนเริ่ม Loop ใหม่
        // -----------------------------------------------------
        public override void ResetToNormal()
        {
            // ปลดกับดัก
            _isArmed = false;

            // ให้คลาสแม่ทำหน้าที่ซ่อนผี/โชว์ของปกติ เหมือนเดิม
            base.ResetToNormal();
        }

        // -----------------------------------------------------
        // 3. จังหวะที่ผู้เล่นเดินมาชน
        // -----------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            // ถ้ารอบนี้ไม่ได้โดนสุ่มให้เป็นผีไม่ต้องทำอะไร
            if (!_isArmed) return;

            if (other.CompareTag("Player"))
            {
                // สั่งให้คลาสแม่ทำหน้าที่เปิดโมเดลผี
                base.ActivateAnomaly();
                // ปลดกับดัก ป้องกันผู้เล่นเดินถอยหลังมาชนซ้ำ
                _isArmed = false;
                // เล่นเสียงตกใจ
                if (!string.IsNullOrEmpty(_jumpscareSound) && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(_jumpscareSound);
                }
                Debug.Log($"<color=red>[TriggerAnomaly] SURPRISE! {_anomalyName} activated right in front of the player!</color>");
            }
        }
    }
}