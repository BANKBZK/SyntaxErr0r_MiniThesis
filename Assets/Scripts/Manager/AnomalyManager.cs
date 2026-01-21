using UnityEngine;
using System.Collections.Generic;

namespace SyntaxError.Managers
{
    public class AnomalyManager : MonoBehaviour
    {
        public static AnomalyManager Instance { get; private set; }

        [Header("Settings")]
        [Range(0, 100)]
        [SerializeField] private int _chance = 40;

        [Tooltip("Loop ที่ห้ามมีผี (เช่น Loop 0 Tutorial)")]
        [SerializeField] private List<int> _safeLoops = new List<int>() { 0 };

        [Header("References")]
        [Tooltip("ลาก GameObject ใน Scene มาใส่เท่านั้น (ห้ามใส่ Prefab)")]
        [SerializeField] private List<Anomaly.AnomalyObject> _allAnomalies;

        public bool IsAnomalyActive { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // เริ่มเกม: บังคับ Reset ทุกตัวให้เป็น Normal ทันที
            ForceResetAll();
            IsAnomalyActive = false;
        }

        public void ProcessLoop(int currentLoop)
        {
            // 1. ล้างสถานะเก่า
            ForceResetAll();

            // 2. เช็ค Safe Loop (เช่น Loop 0)
            if (_safeLoops.Contains(currentLoop))
            {
                IsAnomalyActive = false;
                return; // จบงาน ไม่สุ่ม
            }

            // 3. สุ่ม RNG
            int roll = Random.Range(0, 100);
            if (roll < _chance)
            {
                IsAnomalyActive = true;
                SpawnRandomAnomaly();
            }
            else
            {
                IsAnomalyActive = false;
            }
        }

        private void SpawnRandomAnomaly()
        {
            if (_allAnomalies == null || _allAnomalies.Count == 0) return;

            // สุ่มเลือก 1 ตัว
            int index = Random.Range(0, _allAnomalies.Count);
            var target = _allAnomalies[index];

            if (target != null)
            {
                // Safety Check: กันคนลาก Prefab มาใส่ (เตือนใน Console)
                if (target.gameObject.scene.name == null)
                {
                    Debug.LogError($"[AnomalyManager] Error: '{target.name}' is a Prefab! Please assign the Scene Object.");
                    return;
                }

                target.ActivateAnomaly();
                Debug.Log($"Anomaly Spawned: {target.name}");
            }
        }

        public void ForceResetAll()
        {
            if (_allAnomalies == null) return;
            foreach (var anomaly in _allAnomalies)
            {
                if (anomaly != null) anomaly.ResetToNormal();
            }
        }
    }
}