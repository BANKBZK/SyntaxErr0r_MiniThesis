using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Managers;
using SyntaxError.Interfaces;

namespace SyntaxError.Ritual
{
    public class RitualManager : MonoBehaviour, IResettable
    {
        public static RitualManager Instance { get; private set; }

        [SerializeField] private GameObject _chaserAI;
        [SerializeField] private GameObject _ritualZoneEnvironment;
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private List<GameObject> _ritualItems;

        private int _itemsHolding = 0;
        private int _itemsPlaced = 0;

        private void Awake() { if (Instance == null) Instance = this; }

        private void Start()
        {
            if (LoopManager.Instance != null) LoopManager.Instance.Register(this);
            EndRitualPhase();
        }

        public void OnLoopReset(int currentLoop)
        {
            if (currentLoop == 0)
            {
                _itemsHolding = 0;
                _itemsPlaced = 0;
                EndRitualPhase();
                foreach (var item in _ritualItems) if (item != null) item.SetActive(false);
                Debug.Log("[RitualManager] Reset all items and status.");
            }
        }

        public void SetupRitualPhase()
        {
            _itemsHolding = 0;
            _itemsPlaced = 0;
            RandomizeItemSpawns(); // สุ่มของใหม่
            if (_ritualZoneEnvironment != null) _ritualZoneEnvironment.SetActive(true);
            if (_chaserAI != null) _chaserAI.SetActive(true);
        }

        public void EndRitualPhase()
        {
            if (_ritualZoneEnvironment != null) _ritualZoneEnvironment.SetActive(false);
            if (_chaserAI != null) _chaserAI.SetActive(false);
        }

        private void RandomizeItemSpawns()
        {
            List<Transform> available = new List<Transform>(_spawnPoints);
            foreach (var item in _ritualItems)
            {
                if (item == null || available.Count == 0) continue;
                int r = Random.Range(0, available.Count);
                item.transform.position = available[r].position;
                item.SetActive(true);
                available.RemoveAt(r);
            }
        }

        public void CollectItem() { _itemsHolding++; }
        public bool TryPlaceItems()
        {
            if (_itemsHolding > 0) { _itemsPlaced += _itemsHolding; _itemsHolding = 0; return true; }
            return false;
        }

        public int GetItemsHolding() => _itemsHolding;
        public int GetItemsPlaced() => _itemsPlaced;
    }
}