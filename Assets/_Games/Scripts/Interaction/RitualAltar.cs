using UnityEngine;
using SyntaxError.Interaction;
using SyntaxError.Interfaces;
using SyntaxError.Managers;

namespace SyntaxError.Ritual
{
    public class RitualAltar : MonoBehaviour, Interaction.IInteractable, IResettable
    {
        [SerializeField] private GameObject[] _itemVisuals;

        private void Start()
        {
            if (LoopManager.Instance != null) LoopManager.Instance.Register(this);
            UpdateVisuals();
        }

        public void OnLoopReset(int currentLoop) { UpdateVisuals(); }

        public void Interact()
        {
            if (RitualManager.Instance != null && RitualManager.Instance.TryPlaceItems()) UpdateVisuals();
        }

        public string GetPromptText() => "Place Sacred Items";

        private void UpdateVisuals()
        {
            if (RitualManager.Instance == null) return;
            int placed = RitualManager.Instance.GetItemsPlaced();
            for (int i = 0; i < _itemVisuals.Length; i++)
            {
                if (_itemVisuals[i] != null) _itemVisuals[i].SetActive(i < placed);
            }
        }
    }
}