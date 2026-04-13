using UnityEngine;
using SyntaxError.Enemy;
using SyntaxError.Managers;
using SyntaxError.Interfaces; // [เพิ่ม] เพื่อเรียกใช้ IResettable

namespace SyntaxError.Interaction
{
    public class EnemyEventTrigger : MonoBehaviour, IResettable // [แก้ไข] เพิ่ม IResettable
    {
        public enum ZoneAction
        {
            WakeUpAndHunt,           // ให้ผีตื่นแล้วเริ่มล่าทันที (เริ่มที่ Idle)
            CinematicThenHunt,       // ให้ผีเดินผ่านหน้า แล้วจากนั้นค่อยสลับมาล่าเรา
            CinematicThenDisappear   // ให้ผีเดินผ่านหน้า แล้วหายตัวไปเลย
        }

        [Header("Trigger Settings")]
        [Tooltip("พอเหยียบแล้ว จะให้ผีทำอะไร?")]
        [SerializeField] private ZoneAction _action = ZoneAction.CinematicThenHunt;

        [Tooltip("ลากตัวผีในฉากมาใส่ (ตอนเริ่มเกมควรปิดตาผีตัวนี้ไว้ใน Hierarchy)")]
        [SerializeField] private ChaserAI _ghostAI;

        [Header("Cinematic Settings (สำหรับโหมด Cinematic)")]
        [Tooltip("จุดที่ผีจะโผล่ขึ้นมาตอนเริ่มฉาก")]
        [SerializeField] private Transform _spawnPoint;
        [Tooltip("จุดที่ผีจะเดินไป (เช่น เดินเข้าประตูห้อง)")]
        [SerializeField] private Transform _destinationPoint;
        [SerializeField] private float _walkSpeed = 2f;

        [Header("Effects")]
        [Tooltip("เสียงจั้มสแกร์ หรือ เสียงตุ้ง! ตอนเหยียบทริกเกอร์")]
        [SerializeField] private string _jumpscareSound;

        private bool _hasTriggered = false;

        // ==========================================
        // [เพิ่ม] ระบบลงทะเบียนกับ LoopManager
        // ==========================================
        private void Start()
        {
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.Register(this);
            }
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.Unregister(this);
            }
        }

        // ==========================================
        // [เพิ่ม] ฟังก์ชันรีเซ็ตสถานะทริกเกอร์
        // ==========================================
        public void OnLoopReset(int currentLoop)
        {
            // เมื่อ Reset กลับมาที่ Loop 0 ให้สามารถเหยียบ Trigger นี้ได้อีกครั้ง
            _hasTriggered = false;
            Debug.Log($"[EnemyEventTrigger] {gameObject.name} has been reset for Loop {currentLoop}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered || !other.CompareTag("Player")) return;

            _hasTriggered = true;

            if (_ghostAI != null)
            {
                // เปิด Object (ถ้ามันปิดอยู่)
                _ghostAI.gameObject.SetActive(true);

                if (_action == ZoneAction.CinematicThenHunt)
                {
                    _ghostAI.PlayCinematicEvent(_spawnPoint.position, _destinationPoint.position, _walkSpeed, false);
                }
                else if (_action == ZoneAction.CinematicThenDisappear)
                {
                    _ghostAI.PlayCinematicEvent(_spawnPoint.position, _destinationPoint.position, _walkSpeed, true);
                }
                else if (_action == ZoneAction.WakeUpAndHunt)
                {
                    // ใช้ฟังก์ชัน WakeUp ปลุก AI ขึ้นมา
                    Vector3? targetPos = _spawnPoint != null ? _spawnPoint.position : (Vector3?)null;
                    _ghostAI.WakeUp(targetPos);
                }

                if (SoundManager.Instance != null && !string.IsNullOrEmpty(_jumpscareSound))
                {
                    SoundManager.Instance.PlaySFX(_jumpscareSound);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // สีแดงโปร่งแสงจะได้เห็นง่าย
            if (GetComponent<BoxCollider>() != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(GetComponent<BoxCollider>().center, GetComponent<BoxCollider>().size);
            }
        }
    }
}