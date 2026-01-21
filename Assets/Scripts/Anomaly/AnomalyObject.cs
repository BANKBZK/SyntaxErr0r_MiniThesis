using UnityEngine;

namespace SyntaxError.Anomaly
{
    public class AnomalyObject : MonoBehaviour
    {
        [Header("Anomaly Setup")]
        [Tooltip("ชื่อเอาไว้ดูใน Inspector")]
        [SerializeField] private string _anomalyName = "Moving Chair";

        [Tooltip("วัตถุตอนปกติ (เช่น เก้าอี้วางพื้น)")]
        [SerializeField] private GameObject _normalState;

        [Tooltip("วัตถุตอนหลอน (เช่น เก้าอี้ลอย)")]
        [SerializeField] private GameObject _anomalyState;

        // สั่งให้เป็นผี
        public void ActivateAnomaly()
        {
            if (_normalState != null) _normalState.SetActive(false);
            if (_anomalyState != null) _anomalyState.SetActive(true);
            Debug.Log($"Anomaly Activated: {_anomalyName}");
        }

        // สั่งให้กลับเป็นปกติ
        public void ResetToNormal()
        {
            if (_normalState != null) _normalState.SetActive(true);
            if (_anomalyState != null) _anomalyState.SetActive(false);
        }
    }
}