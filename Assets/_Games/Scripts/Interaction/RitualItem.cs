using UnityEngine;
using SyntaxError.Interaction;
using SyntaxError.Interfaces; // [เพิ่ม]
using SyntaxError.Managers;   // [เพิ่ม]

namespace SyntaxError.Ritual
{
    public class RitualItem : MonoBehaviour, Interaction.IInteractable, IResettable // [แก้ไข] เพิ่ม IResettable
    {
        [Header("Item Settings")]
        [SerializeField] private string _itemName = "Sacred Item";

        private bool _isCollected = false;

        private void Start()
        {
            // [เพิ่ม] ลงทะเบียนเพื่อให้ Reset ตัวเองได้
            if (LoopManager.Instance != null) LoopManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null) LoopManager.Instance.Unregister(this);
        }

        // ==========================================
        // [เพิ่ม] ฟังก์ชันรีเซ็ตค่าเพื่อให้เก็บใหม่ได้
        // ==========================================
        public void OnLoopReset(int currentLoop)
        {
            _isCollected = false;
            // หมายเหตุ: การ SetActive(true/false) จะถูกจัดการโดย RitualManager อยู่แล้ว
        }

        public void Interact()
        {
            if (_isCollected) return;

            if (RitualManager.Instance != null)
            {
                RitualManager.Instance.CollectItem();
            }

            _isCollected = true;
            gameObject.SetActive(false);
        }

        public string GetPromptText()
        {
            return $"Pick up {_itemName}";
        }
    }
}