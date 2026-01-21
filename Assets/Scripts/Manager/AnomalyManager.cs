using UnityEngine;
using System.Collections.Generic;

namespace SyntaxError.Managers
{
    public class AnomalyManager : MonoBehaviour
    {
        public static AnomalyManager Instance { get; private set; }

        [Header("Settings")]
        [Range(0, 100)]
        [SerializeField] private int _chance = 50; // ลองปรับเป็น 100 เพื่อเทส

        [Tooltip("Loop ที่ห้ามมีผี")]
        [SerializeField] private List<int> _safeLoops = new List<int>() { 0 };

        [Header("References")]
        [SerializeField] private List<Anomaly.AnomalyObject> _allAnomalies;

        public bool IsAnomalyActive { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ProcessLoop(int currentLoop)
        {
            Debug.Log($"<color=yellow>[AnomalyManager] Processing Loop: {currentLoop}</color>");

            // 1. Reset ของเก่าก่อน
            ForceResetAll();

            // 2. เช็ค Safe Loop
            if (_safeLoops.Contains(currentLoop))
            {
                Debug.Log($"[AnomalyManager] Loop {currentLoop} is SAFE. Skipping RNG.");
                IsAnomalyActive = false;
                return;
            }

            // 3. สุ่ม RNG
            int roll = Random.Range(0, 100);
            Debug.Log($"[AnomalyManager] Rolled: {roll} (Chance needs < {_chance})");

            if (roll < _chance)
            {
                IsAnomalyActive = true;
                SpawnRandomAnomaly();
            }
            else
            {
                IsAnomalyActive = false;
                Debug.Log("[AnomalyManager] Result: Normal Loop");
            }
        }

        private void SpawnRandomAnomaly()
        {
            // เช็คว่าลืมใส่ของใน List ไหม?
            if (_allAnomalies == null || _allAnomalies.Count == 0)
            {
                Debug.LogError("<color=red>[AnomalyManager] Error: List is Empty! ลากของใส่ใน Inspector ด้วย!</color>");
                return;
            }

            int index = Random.Range(0, _allAnomalies.Count);
            var target = _allAnomalies[index];

            // --- เช็คว่าเป็น Prefab หรือไม่ (สำคัญมาก) ---
            if (target.gameObject.scene.name == null)
            {
                Debug.LogError($"<color=red>[AnomalyManager] CRITICAL ERROR: '{target.name}' เป็น Prefab! คุณต้องลากของจาก Hierarchy (ในฉาก) มาใส่เท่านั้น ห้ามลากจากโฟลเดอร์!</color>");
                return;
            }
            // ----------------------------------------

            Debug.Log($"<color=red>[AnomalyManager] ACTIVATING: {target.name}</color>");
            target.ActivateAnomaly();
        }

        public void ForceResetAll()
        {
            if (_allAnomalies == null) return;
            Debug.Log("[AnomalyManager] Resetting all objects...");

            foreach (var anomaly in _allAnomalies)
            {
                if (anomaly != null) anomaly.ResetToNormal();
            }
        }
    }
}