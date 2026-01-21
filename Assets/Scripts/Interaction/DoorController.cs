using UnityEngine;
using System.Collections; // จำเป็นสำหรับ Coroutine

namespace SyntaxError.Interaction
{
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        [SerializeField] private Transform _doorModel; // โมเดลบานประตูที่จะขยับ
        [SerializeField] private Vector3 _slideDirection = new Vector3(1f, 0f, 0f); // ทิศทางที่จะเลื่อน (เช่น แกน X)
        [SerializeField] private float _slideDistance = 1.2f; // ระยะทางที่เลื่อน (เมตร)
        [SerializeField] private float _speed = 2.0f; // ความเร็วในการเลื่อน

        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private bool _isOpen = false;
        private Coroutine _animationCoroutine; // เก็บตัวแปร Coroutine เพื่อหยุดถ้ากดรัว

        private void Start()
        {
            // ถ้าลืมลากโมเดลมาใส่ ให้ใช้ตัวมันเองเป็นโมเดล
            if (_doorModel == null) _doorModel = transform;

            // คำนวณตำแหน่งปิดและเปิด
            _closedPosition = _doorModel.localPosition;
            _openPosition = _closedPosition + (_slideDirection.normalized * _slideDistance);
        }

        // ฟังก์ชันจาก Interface IInteractable
        public void Interact()
        {
            _isOpen = !_isOpen; // สลับสถานะ
            // ถ้ากำลังขยับอยู่ ให้หยุดก่อนแล้วขยับใหม่ (กันประตูวาร์ป)
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);

            // เริ่มขยับ
            _animationCoroutine = StartCoroutine(MoveDoor(_isOpen ? _openPosition : _closedPosition));
        }

        // ข้อความที่จะโชว์บนจอ
        public string GetPromptText()
        {
            return _isOpen ? "Close Door" : "Open Door";
        }

        // ระบบ Animation แบบบ้านๆ (ใช้ Lerp)
        private IEnumerator MoveDoor(Vector3 targetPosition)
        {
            Vector3 startPosition = _doorModel.localPosition;
            float time = 0f;
            float duration = Vector3.Distance(startPosition, targetPosition) / _speed;

            while (time < duration)
            {
                _doorModel.localPosition = Vector3.Lerp(startPosition, targetPosition, time / duration);
                time += Time.deltaTime;
                yield return null; // รอเฟรมถัดไป
            }

            _doorModel.localPosition = targetPosition; // จบแล้ววางให้เป๊ะ
        }
    }
}