using UnityEngine;

namespace SyntaxError.Anomaly
{
    public class AnomalyObject : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] protected string _anomalyName = "Anomaly Prop";
        [SerializeField] protected GameObject _normalState;
        [SerializeField] protected GameObject _anomalyState;

        private void Start()
        {
            // เริ่มเกมมา สั่งรีเซ็ตตัวเองทันที (กันลืม)
            ResetToNormal();
        }

        public virtual void ActivateAnomaly()
        {
            if (_normalState != null) _normalState.SetActive(false);
            if (_anomalyState != null) _anomalyState.SetActive(true);
        }

        public virtual void ResetToNormal()
        {
            if (_anomalyState != null) _anomalyState.SetActive(false);
            if (_normalState != null) _normalState.SetActive(true);
        }
    }
}