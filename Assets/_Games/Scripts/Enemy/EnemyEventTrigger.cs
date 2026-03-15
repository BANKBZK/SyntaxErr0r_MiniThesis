using UnityEngine;
using SyntaxError.Enemy;
using SyntaxError.Managers; // เผื่อใส่เสียง

namespace SyntaxError.Interaction
{
    public class EnemyEventTrigger : MonoBehaviour
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

        private void OnTriggerEnter(Collider other)
        {
            // ถ้าเคยชนแล้ว หรือไม่ใช่ Player ให้ข้ามไป
            if (_hasTriggered || !other.CompareTag("Player")) return;

            _hasTriggered = true;

            if (_ghostAI != null)
            {
                // 1. เปิดสวิตช์ปลุกผีให้ตื่น!
                _ghostAI.gameObject.SetActive(true);

                // 2. เช็คว่าตั้งค่าให้ทำอะไร
                if (_action == ZoneAction.CinematicThenHunt)
                {
                    // เดินผ่านหน้า แล้วสลับไปล่าต่อ
                    _ghostAI.PlayCinematicEvent(_spawnPoint.position, _destinationPoint.position, _walkSpeed, false);
                }
                else if (_action == ZoneAction.CinematicThenDisappear)
                {
                    // เดินผ่านหน้า แล้วหายวับไปเลย
                    _ghostAI.PlayCinematicEvent(_spawnPoint.position, _destinationPoint.position, _walkSpeed, true);
                }
                else if (_action == ZoneAction.WakeUpAndHunt)
                {
                    // ปลุกเฉยๆ ถ้าระบุจุดเกิดก็วาร์ปไปตรงนั้น
                    if (_spawnPoint != null)
                    {
                        _ghostAI.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(_spawnPoint.position);
                    }
                    // มันจะเริ่มล่าจากสถานะ Idle อัตโนมัติ
                }

                // 3. เล่นเสียงเอฟเฟกต์ตกใจ
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