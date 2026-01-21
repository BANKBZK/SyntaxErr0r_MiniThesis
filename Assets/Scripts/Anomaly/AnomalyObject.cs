using UnityEngine;

namespace SyntaxError.Anomaly
{
    public class AnomalyObject : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private string _anomalyName = "Anomaly Prop";
        [SerializeField] private GameObject _normalState;
        [SerializeField] private GameObject _anomalyState;

        private void Start()
        {
            // เริ่มเกมมา สั่งรีเซ็ตตัวเองทันที (กันลืม)
            ResetToNormal();
        }

        public void ActivateAnomaly()
        {
            if (_normalState != null) _normalState.SetActive(false);
            if (_anomalyState != null) _anomalyState.SetActive(true);
        }

        public void ResetToNormal()
        {
            if (_anomalyState != null) _anomalyState.SetActive(false);
            if (_normalState != null) _normalState.SetActive(true);
        }
    }
}