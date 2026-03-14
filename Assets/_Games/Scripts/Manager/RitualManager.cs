using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Managers; // เพื่อเรียกใช้ SoundManager / UIManager

namespace SyntaxError.Ritual
{
    public class RitualManager : MonoBehaviour
    {
        public static RitualManager Instance { get; private set; }

        [Header("Ritual Setup")]
        public int totalItemsNeeded = 3;

        [Header("Random Spawning")]
        [Tooltip("จุดเกิดทั้งหมดในแมพ (ควรสร้าง Empty GameObject วางตามพื้นแล้วลากมาใส่ แนะนำ 5-10 จุด)")]
        [SerializeField] private List<Transform> _spawnPoints;

        [Tooltip("โมเดลไอเทมทั้ง 3 ชิ้นในฉาก (ลากมาใส่ตรงนี้)")]
        [SerializeField] private List<GameObject> _ritualItems;

        [Header("Current Status (Read Only)")]
        [SerializeField] private int _itemsHolding = 0;
        [SerializeField] private int _itemsPlaced = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // เริ่มเกมมาให้ซ่อนของไหว้ไปก่อน
            foreach (var item in _ritualItems)
            {
                if (item != null) item.SetActive(false);
            }
        }

        // ฟังก์ชันนี้จะถูกเรียกโดย LoopManager ตอนที่วาร์ปผู้เล่นมาถึงด่านหนีผี
        public void SetupRitualPhase()
        {
            _itemsHolding = 0;
            _itemsPlaced = 0;
            RandomizeItemSpawns();
            Debug.Log("<color=orange>[RitualManager] เริ่มช่วงพิธีกรรม! สุ่มจุดเกิดไอเทมเรียบร้อย</color>");
        }

        private void RandomizeItemSpawns()
        {
            if (_spawnPoints.Count < _ritualItems.Count)
            {
                Debug.LogError("[Ritual] มีจุด Spawn น้อยกว่าจำนวนไอเทม! กรุณาเพิ่มจุดเกิดใน Inspector");
                return;
            }

            // 1. ก๊อปปี้ลิสต์จุดเกิดมา เพื่อเอามาสับเปลี่ยน (Shuffle)
            List<Transform> availableSpawns = new List<Transform>(_spawnPoints);

            // 2. สับเปลี่ยนตำแหน่งใน List (เทคนิค Fisher-Yates Shuffle)
            for (int i = 0; i < availableSpawns.Count; i++)
            {
                Transform temp = availableSpawns[i];
                int randomIndex = Random.Range(i, availableSpawns.Count);
                availableSpawns[i] = availableSpawns[randomIndex];
                availableSpawns[randomIndex] = temp;
            }

            // 3. เอาไอเทมไปวางตามจุดที่ถูกสุ่มขึ้นมา (เอาแค่ 3 จุดแรกของลิสต์ที่สับแล้ว)
            for (int i = 0; i < _ritualItems.Count; i++)
            {
                if (_ritualItems[i] != null)
                {
                    _ritualItems[i].transform.position = availableSpawns[i].position;
                    // ถ้าอยากให้ของวางเอียงตามพื้นด้วย ให้ใช้ rotation ของจุดเกิด
                    _ritualItems[i].transform.rotation = availableSpawns[i].rotation;
                    _ritualItems[i].SetActive(true); // เปิดให้ผู้เล่นมองเห็น
                }
            }
        }

        public void CollectItem()
        {
            _itemsHolding++;
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("PickupItem");
            Debug.Log($"[Ritual] เก็บของแล้ว! ถืออยู่: {_itemsHolding} ชิ้น");
        }

        public bool TryPlaceItems()
        {
            if (_itemsHolding > 0)
            {
                _itemsPlaced += _itemsHolding;
                _itemsHolding = 0;

                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("PlaceItem");
                Debug.Log($"[Ritual] วางของแล้ว! รวมวางไป: {_itemsPlaced}/{totalItemsNeeded}");

                CheckWinCondition();
                return true; // วางสำเร็จ
            }
            return false; // ไม่มีของในมือ
        }

        private void CheckWinCondition()
        {
            if (_itemsPlaced >= totalItemsNeeded)
            {
                Debug.Log("<color=green>TRUE ENDING UNLOCKED! พิธีกรรมสมบูรณ์!</color>");
                // TODO: เดี๋ยวนายสามารถไปทำระบบฉากจบได้ตรงนี้
                // UIManager.Instance.ShowWinScreen(); 
            }
        }

        public int GetItemsHolding() => _itemsHolding;
        public int GetItemsPlaced() => _itemsPlaced;
    }
}