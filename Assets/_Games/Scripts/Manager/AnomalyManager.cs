using UnityEngine;
using System.Collections.Generic;

namespace SyntaxError.Managers
{
    public class AnomalyManager : MonoBehaviour
    {
        public static AnomalyManager Instance { get; private set; }

        [Header("Settings")]
        [Range(0, 100)]
        [SerializeField] private int _chance = 51;

        [Tooltip("Loop ที่ห้ามมีผี (เช่น Loop 0 Tutorial)")]
        [SerializeField] private List<int> _safeLoops = new List<int>() { 0 };

        [Header("References")]
        [Tooltip("ลาก GameObject ใน Scene มาใส่เท่านั้น (ห้ามใส่ Prefab)")]
        [SerializeField] private List<Anomaly.AnomalyObject> _allAnomalies;

        // 🎒 ถุงสุ่ม (เก็บรายชื่อที่ยังไม่เคยโผล่)
        [SerializeField] private List<Anomaly.AnomalyObject> _availableAnomalies = new List<Anomaly.AnomalyObject>();

        public bool IsAnomalyActive { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // เติมของลงถุงสุ่มตอนเริ่มเกม
            ResetAnomalyPool();

            ForceResetAll();
            IsAnomalyActive = false;
        }

        // ==============================================
        // 🔄 ฟังก์ชันสำหรับเติม Anomaly ให้เต็มถุงอีกครั้ง (เรียกตอนโดน Hard Reset)
        // ==============================================
        public void ResetAnomalyPool()
        {
            if (_allAnomalies == null) return;

            _availableAnomalies.Clear();
            _availableAnomalies.AddRange(_allAnomalies);
            Debug.Log($"[AnomalyManager] รีเซ็ตถุงสุ่มใหม่แล้ว! มี Anomaly พร้อมใช้งาน {_availableAnomalies.Count} ตัว");
        }

        public void ProcessLoop(int currentLoop)
        {
            ForceResetAll();

            if (_safeLoops.Contains(currentLoop))
            {
                IsAnomalyActive = false;
                return;
            }

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

            // ⚠️ ป้องกันกรณีผู้เล่นวนลูปผิดหลายรอบจนของในถุงหมด ให้เติมของใหม่เลยกันเกมบั๊ก
            if (_availableAnomalies.Count == 0)
            {
                Debug.Log("[AnomalyManager] Anomaly หมดถุงแล้ว! ทำการเติมให้ใหม่...");
                ResetAnomalyPool();
            }

            // สุ่มจากลิสต์ "ถุงสุ่ม" ที่ยังไม่เคยออก
            int index = Random.Range(0, _availableAnomalies.Count);
            var target = _availableAnomalies[index];

            // 🌟 ทิ้งตัวที่สุ่มได้ออกจากถุงสุ่ม เพื่อไม่ให้โผล่ซ้ำ!
            _availableAnomalies.RemoveAt(index);

            if (target != null)
            {
                if (target.gameObject.scene.name == null)
                {
                    Debug.LogError($"[AnomalyManager] Error: '{target.name}' is a Prefab! Please assign the Scene Object.");
                    return;
                }

                target.ActivateAnomaly();
                Debug.Log($"Anomaly Spawned: {target.name} (เหลือในถุงสุ่มอีก {_availableAnomalies.Count} แบบ)");
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