using UnityEngine;
using SyntaxError.Interaction;

namespace SyntaxError.Ritual
{
    public class RitualAltar : MonoBehaviour, IInteractable
    {
        [Header("Visual Feedback")]
        [Tooltip("ลากโมเดลของไหว้ที่ 'วางอยู่บนแท่นแล้ว' มาใส่เรียงกัน (ซ่อนตาไว้ก่อน)")]
        [SerializeField] private GameObject[] _itemVisuals;

        public void Interact()
        {
            if (RitualManager.Instance == null) return;

            // สั่งพยายามวางของ
            bool success = RitualManager.Instance.TryPlaceItems();

            if (success)
            {
                UpdateVisuals();
            }
            else
            {
                Debug.Log("ไม่มีของเซ่นไหว้ในมือ ไปหามาก่อน!");
            }
        }

        public string GetPromptText()
        {
            if (RitualManager.Instance == null) return "";

            int holding = RitualManager.Instance.GetItemsHolding();
            int placed = RitualManager.Instance.GetItemsPlaced();
            int total = RitualManager.Instance.totalItemsNeeded;

            if (placed >= total) return "Ritual Complete";
            if (holding > 0) return $"Place {holding} Item(s)";

            return "Requires Sacred Items"; // ยังไม่มีของในมือ
        }

        private void UpdateVisuals()
        {
            int placedCount = RitualManager.Instance.GetItemsPlaced();

            for (int i = 0; i < _itemVisuals.Length; i++)
            {
                if (_itemVisuals[i] != null)
                {
                    // โชว์ของตามจำนวนชิ้นที่วางไปแล้ว
                    _itemVisuals[i].SetActive(i < placedCount);
                }
            }
        }
    }
}