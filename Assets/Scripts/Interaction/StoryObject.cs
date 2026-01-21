using UnityEngine;
using SyntaxError.Interfaces;
using SyntaxError.Managers;

namespace SyntaxError.Events
{
    public class StoryObject : MonoBehaviour, IResettable
    {
        [Header("Condition")]
        [Tooltip("จะให้โผล่มาใน Loop ที่เท่าไหร่?")]
        [SerializeField] private int _targetLoop = 3;

        [Tooltip("ถ้าติ๊กถูก จะโผล่แค่ Loop นี้ Loop เดียวแล้วหายไปเลย")]
        [SerializeField] private bool _onlyThisLoop = true;

        [Header("Object To Control")]
        [SerializeField] private GameObject _contentObject; // ลากตัวโมเดลผี/ของ มาใส่ตรงนี้

        private void Start()
        {
            // เริ่มเกมมา ปิดไปก่อนเลย
            if (_contentObject != null) _contentObject.SetActive(false);

            // ลงทะเบียน
            LoopManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null) LoopManager.Instance.Unregister(this);
        }

        // ฟังก์ชันเช็คเงื่อนไข (ทำงานตอนจอดำ)
        public void OnLoopReset(int currentLoop)
        {
            if (_contentObject == null) return;

            bool shouldAppear = false;

            if (_onlyThisLoop)
            {
                // โผล่เฉพาะ Loop เป้าหมายเป๊ะๆ (เช่น Loop 3 เท่านั้น)
                shouldAppear = (currentLoop == _targetLoop);
            }
            else
            {
                // โผล่ตั้งแต่ Loop เป้าหมายเป็นต้นไป (เช่น ตั้งแต่ Loop 3 เป็นต้นไปเจอได้ตลอด)
                shouldAppear = (currentLoop >= _targetLoop);
            }

            _contentObject.SetActive(shouldAppear);

            if (shouldAppear)
            {
                Debug.Log($"Story Event Triggered: {gameObject.name} in Loop {currentLoop}");
            }
        }
    }
}