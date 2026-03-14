using UnityEngine;
using SyntaxError.Interaction; // เรียกใช้ IInteractable ของคุณ

namespace SyntaxError.Ritual
{
    public class RitualItem : MonoBehaviour, IInteractable
    {
        [Header("Item Settings")]
        [SerializeField] private string _itemName = "Sacred Item";

        private bool _isCollected = false;

        public void Interact()
        {
            if (_isCollected) return;

            // 1. บอก Manager ว่าเก็บของแล้ว
            if (RitualManager.Instance != null)
            {
                RitualManager.Instance.CollectItem();
            }

            // 2. ซ่อนตัวเองทิ้ง (ลบออกจากฉาก)
            _isCollected = true;
            gameObject.SetActive(false);
        }

        public string GetPromptText()
        {
            return $"Pick up {_itemName}";
        }
    }
}