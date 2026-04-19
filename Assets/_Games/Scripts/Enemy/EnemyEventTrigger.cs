using UnityEngine;
using SyntaxError.Enemy;
using SyntaxError.Managers;
using SyntaxError.Interfaces;

namespace SyntaxError.Interaction
{
    public class EnemyEventTrigger : MonoBehaviour, IResettable
    {
        public enum ZoneAction
        {
            WakeUpAndHunt,           // ให้ผีตื่นแล้วเริ่มล่าทันที (เริ่มที่ Idle แล้วระบบจะรันเอง)
            CinematicThenHunt,       // ให้ผีเดินผ่านหน้ากล้อง แล้วจากนั้นค่อยสลับมาล่าเรา
            CinematicThenDisappear   // ให้ผีเดินผ่านหน้ากล้องหลอนๆ แล้วหายตัวไปเลย
        }

        [Header("Trigger Settings")]
        [Tooltip("พอเหยียบแล้ว จะให้ผีทำอะไร?")]
        [SerializeField] private ZoneAction _action = ZoneAction.CinematicThenHunt;

        [Tooltip("ลากตัวผีในฉากมาใส่ (ตอนเริ่มเกมควรตั้งค่าที่ตัวผีเป็น Start Dormant)")]
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
                // เปิด Object ผี (เผื่อโดนปิดไว้)
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
                    // ใช้ฟังก์ชัน WakeUp ปลุก AI ขึ้นมาแล้วเริ่มลูปหาผู้เล่น
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
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            if (GetComponent<BoxCollider>() != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(GetComponent<BoxCollider>().center, GetComponent<BoxCollider>().size);
            }
        }
    }
}