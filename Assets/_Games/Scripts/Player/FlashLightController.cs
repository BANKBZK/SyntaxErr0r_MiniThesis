using UnityEngine;
using SyntaxError.Inputs;
using SyntaxError.Managers;

namespace SyntaxError.Player
{
    public class FlashlightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private Light _lightSource;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _modelTransform;

        [Header("Dynamo / Battery Settings")]
        [Tooltip("สถานะความตั้งใจว่าอยากเปิดไฟค้างไว้ไหม")]
        [SerializeField] private bool _wantsLightOn = false;

        [SerializeField] private float _maxBattery = 100f;

        [Tooltip("แบตเตอรี่ลดลงวินาทีละเท่าไหร่ (ค่าน้อย = อยู่นานขึ้น แนะนำ 2.0f คืออยู่นาน 50 วินาที)")]
        [SerializeField] private float _drainRate = 2.0f;

        // --- AI จะเห็นแสงไฟ ก็ต่อเมื่อตั้งใจเปิด และ แบตต้องไม่หมด ---
        public bool IsLightOn => _wantsLightOn && CurrentBattery > 0;
        public float CurrentBattery { get; private set; }

        [Header("Sound Settings")]
        [SerializeField] private string _toggleSoundName = "Click";
        [SerializeField] private string _crankSoundName = "FlashlightCrank";

        [Header("Sway & Collision Settings")]
        [SerializeField] private float _swaySpeed = 5f;
        [SerializeField] private float _dragAmount = 2f;
        [SerializeField] private bool _enableCollision = true;
        [SerializeField] private LayerMask _obstructionMask = ~0;
        [SerializeField] private float _collisionRadius = 0.08f;
        [SerializeField] private float _collisionOffset = 0.03f;
        [SerializeField] private float _maxDistanceFromCamera = 0.6f;
        [SerializeField] private float _minDistanceFromCamera = 0.12f;
        [SerializeField] private float _positionSmoothSpeed = 10f;

        private Quaternion _initialRotation;
        private Vector3 _positionVelocity;
        private float _initialIntensity;

        // ตัวเช็คการกดปั่นไฟแค่ครั้งเดียว (One-shot click)
        private bool _wasCranking = false;

        private void Start()
        {
            if (_inputManager == null) _inputManager = GetComponentInParent<InputManager>();
            if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;
            if (_modelTransform == null)
            {
                var found = transform.Find("Model");
                _modelTransform = found != null ? found : transform;
            }

            _initialRotation = transform.localRotation;
            CurrentBattery = _maxBattery;

            if (_lightSource != null)
            {
                _initialIntensity = _lightSource.intensity;
                _lightSource.enabled = IsLightOn;
            }
        }

        private void Update()
        {
            HandleDynamoBattery();
            HandleInput();
            HandleSway();
            HandleCollision();
        }

        private void HandleDynamoBattery()
        {
            // 1. กดปั่นไฟ "ครั้งเดียว" (เช็คว่าเฟรมก่อนหน้านี้ยังไม่ได้กด) เพื่อชาร์จเต็ม 100% ทันที
            if (_inputManager.IsCranking && !_wasCranking)
            {
                CurrentBattery = _maxBattery;

                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(_crankSoundName);

                // ถ้าผู้เล่นเคยเปิดไฟค้างไว้ พอชาร์จปุ๊บให้ไฟติดขึ้นมาเองเลยไม่ต้องกด F ซ้ำ
                if (_wantsLightOn && _lightSource != null)
                {
                    _lightSource.enabled = true;
                }
            }

            // อัปเดตสถานะการกด
            _wasCranking = _inputManager.IsCranking;

            // 2. แบตเตอรี่ลดลงเรื่อยๆ ถ้าไฟติดอยู่
            if (IsLightOn)
            {
                CurrentBattery -= _drainRate * Time.deltaTime;

                if (CurrentBattery <= 0)
                {
                    CurrentBattery = 0;
                    // ดับไฟฉายเพราะแบตหมด แต่ยังคงสถานะ _wantsLightOn ไว้อยู่
                    if (_lightSource != null) _lightSource.enabled = false;
                }
            }

            // 3. หรี่ไฟและกะพริบเตือนตอนแบตใกล้จะหมด (เตือนล่วงหน้าที่ 20%)
            if (IsLightOn && _lightSource != null)
            {
                float batteryPercent = CurrentBattery / _maxBattery;
                _lightSource.intensity = _initialIntensity * batteryPercent;

                if (batteryPercent < 0.2f)
                {
                    _lightSource.intensity += Random.Range(-0.2f, 0.2f);
                }
            }
        }

        private void HandleInput()
        {
            if (_inputManager.IsFlashlightPressed)
            {
                ToggleLight();
                _inputManager.ConsumeFlashlightInput();
            }
        }

        private void ToggleLight()
        {
            // สลับสถานะความตั้งใจว่าอยากเปิดหรือปิด
            _wantsLightOn = !_wantsLightOn;

            // ไฟจะแสดงผลจริง ก็ต่อเมื่ออยากให้เปิด และ แบตต้องไม่เหลือ 0
            if (_lightSource != null) _lightSource.enabled = IsLightOn;

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(_toggleSoundName);
        }

        private void HandleSway()
        {
            if (_cameraTransform == null) return;
            float mouseX = _inputManager.LookInput.x;
            float mouseY = _inputManager.LookInput.y;
            Quaternion rotationX = Quaternion.AngleAxis(-mouseY * _dragAmount, Vector3.right);
            Quaternion rotationY = Quaternion.AngleAxis(mouseX * _dragAmount, Vector3.up);
            Quaternion targetRotation = _initialRotation * rotationX * rotationY;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, _swaySpeed * Time.deltaTime);
        }

        private void HandleCollision()
        {
            if (!_enableCollision || _cameraTransform == null || _modelTransform == null) return;
            Vector3 desiredWorldPos = _modelTransform.position;
            Vector3 origin = _cameraTransform.position;
            Vector3 direction = desiredWorldPos - origin;
            float baselineDist = direction.magnitude;

            if (baselineDist <= 0.0001f)
            {
                SetFlashlightWorldPosition(desiredWorldPos);
                return;
            }

            Vector3 baselineDir = direction / baselineDist;
            float effectiveBaselineDist = Mathf.Min(baselineDist, _maxDistanceFromCamera);
            RaycastHit hit;
            bool isHit = Physics.SphereCast(origin, _collisionRadius, baselineDir, out hit, effectiveBaselineDist, _obstructionMask, QueryTriggerInteraction.Ignore);
            Vector3 currentWorldPos = transform.parent == _cameraTransform ? _cameraTransform.TransformPoint(transform.localPosition) : transform.position;
            float currentDistAlong = Vector3.Dot(currentWorldPos - origin, baselineDir);
            Vector3 targetWorldPos;

            if (isHit)
            {
                float hitDist = hit.distance;
                float targetDist = hitDist - _collisionOffset;
                targetDist = Mathf.Clamp(targetDist, _minDistanceFromCamera, effectiveBaselineDist);
                if (currentDistAlong <= targetDist) targetWorldPos = currentWorldPos;
                else targetWorldPos = origin + baselineDir * targetDist;
            }
            else
            {
                if (baselineDist > _maxDistanceFromCamera) targetWorldPos = origin + baselineDir * _maxDistanceFromCamera;
                else targetWorldPos = desiredWorldPos;
            }
            SetFlashlightWorldPositionSmooth(targetWorldPos);
        }

        private void SetFlashlightWorldPosition(Vector3 worldPos)
        {
            if (transform.parent == _cameraTransform) transform.localPosition = _cameraTransform.InverseTransformPoint(worldPos);
            else transform.position = worldPos;
        }

        private void SetFlashlightWorldPositionSmooth(Vector3 worldPos)
        {
            if (transform.parent == _cameraTransform)
            {
                Vector3 targetLocal = _cameraTransform.InverseTransformPoint(worldPos);
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetLocal, ref _positionVelocity, 1f / Mathf.Max(0.0001f, _positionSmoothSpeed));
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, worldPos, ref _positionVelocity, 1f / Mathf.Max(0.0001f, _positionSmoothSpeed));
            }
        }
    }
}