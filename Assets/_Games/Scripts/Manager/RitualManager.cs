using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Managers;
using SyntaxError.Interfaces;

namespace SyntaxError.Ritual
{
    public class RitualManager : MonoBehaviour, IResettable
    {
        public static RitualManager Instance { get; private set; }

        [Header("Zone Control (Performance)")]
        [Tooltip("ลาก GameObject ของผี Chaser AI มาใส่")]
        [SerializeField] private GameObject _chaserAI;
        [Tooltip("ลาก GameObject แม่ที่คลุมของในฉาก Ritual (กำแพง, ไฟ, ฯลฯ) มาใส่")]
        [SerializeField] private GameObject _ritualZoneEnvironment;

        [Header("Ritual Setup")]
        public int totalItemsNeeded = 3;

        [Header("Random Spawning")]
        [SerializeField] private List<Transform> _spawnPoints;
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
            // เริ่มเกมมา ซ่อนของไหว้ และ ปิดการทำงานโซน Ritual ทิ้งเพื่อประหยัดพลังงาน
            foreach (var item in _ritualItems)
            {
                if (item != null) item.SetActive(false);
            }

            EndRitualPhase(); // ปิดโซนไว้ก่อน
        }

        // เรียกตอนเข้าโซน Ritual (LoopManager สั่ง)
        public void SetupRitualPhase()
        {
            _itemsHolding = 0;
            _itemsPlaced = 0;
            RandomizeItemSpawns();

            // เปิดการทำงานของ Zone และ AI
            if (_ritualZoneEnvironment != null) _ritualZoneEnvironment.SetActive(true);
            if (_chaserAI != null) _chaserAI.SetActive(true);

            Debug.Log("<color=orange>[RitualManager] เริ่มช่วงพิธีกรรม! เปิดการทำงานโซนและ AI</color>");
        }

        // เรียกเพื่อเคลียร์โซน ปิดผี (เช่น ตอนโดนฆ่า หรือ จบเกม หรือตอบผิด)
        public void EndRitualPhase()
        {
            // ปิดการทำงานของ Zone และ AI
            if (_ritualZoneEnvironment != null) _ritualZoneEnvironment.SetActive(false);
            if (_chaserAI != null) _chaserAI.SetActive(false);

            // หยุดเสียงเพลงไล่ล่าเผื่อมันค้างอยู่
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopMusic("ChaseTheme");
            }
        }

        private void RandomizeItemSpawns()
        {
            // (โค้ดเดิมของคุณที่ใช้สุ่มเกิดไอเทม ไม่ต้องแก้)
            if (_spawnPoints.Count < _ritualItems.Count)
            {
                Debug.LogError("[Ritual] มีจุด Spawn น้อยกว่าจำนวนไอเทม! กรุณาเพิ่มจุดเกิดใน Inspector");
                return;
            }

            List<Transform> availableSpawns = new List<Transform>(_spawnPoints);
            for (int i = 0; i < availableSpawns.Count; i++)
            {
                Transform temp = availableSpawns[i];
                int randomIndex = Random.Range(i, availableSpawns.Count);
                availableSpawns[i] = availableSpawns[randomIndex];
                availableSpawns[randomIndex] = temp;
            }

            for (int i = 0; i < _ritualItems.Count; i++)
            {
                if (_ritualItems[i] != null)
                {
                    _ritualItems[i].transform.position = availableSpawns[i].position;
                    _ritualItems[i].transform.rotation = availableSpawns[i].rotation;
                    _ritualItems[i].SetActive(true);
                }
            }
        }

        public void CollectItem()
        {
            _itemsHolding++;
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("PickupItem");
        }

        public bool TryPlaceItems()
        {
            if (_itemsHolding > 0)
            {
                _itemsPlaced += _itemsHolding;
                _itemsHolding = 0;

                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("PlaceItem");
                CheckWinCondition();
                return true;
            }
            return false;
        }

        private void CheckWinCondition()
        {
            if (_itemsPlaced >= totalItemsNeeded)
            {
                if (GameManager.Instance != null) GameManager.Instance.IsRitualComplete = true;
                if (UIManager.Instance != null) UIManager.Instance.ShowStoryText("The ritual is complete... Find the exit!", 4f);
            }
        }

        public int GetItemsHolding() => _itemsHolding;
        public int GetItemsPlaced() => _itemsPlaced;

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
    }
}