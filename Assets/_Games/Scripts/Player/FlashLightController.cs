using UnityEngine;
using SyntaxError.Inputs;
using SyntaxError.Managers;
using SyntaxError.Interfaces; // [Added] เพื่อใช้ IResettable

namespace SyntaxError.Player
{
    public class FlashlightController : MonoBehaviour, IResettable
    {
        [Header("References")]
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private Light _lightSource;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _modelTransform;

        [Header("Dynamo / Battery Settings")]
        [SerializeField] private bool _wantsLightOn = false;
        [SerializeField] private float _maxBattery = 100f;
        [SerializeField] private float _drainRate = 2.0f;
        [SerializeField] private float _crankCooldown = 0.5f;
        private float _crankTimer = 0f;

        public bool IsLightOn => _wantsLightOn && CurrentBattery > 0;
        public float CurrentBattery { get; private set; }

        // ... (ตัวแปร Sway & Collision คงเดิม) ...
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
        private bool _wasCranking = false;

        private void Start()
        {
            if (_inputManager == null) _inputManager = GetComponentInParent<InputManager>();
            if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;

            _initialRotation = transform.localRotation;
            CurrentBattery = _maxBattery;

            if (_lightSource != null)
            {
                _initialIntensity = _lightSource.intensity;
                _lightSource.enabled = IsLightOn;
            }

            // [Added] ลงทะเบียนกับ LoopManager
            if (LoopManager.Instance != null) LoopManager.Instance.Register(this);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBatteryUI(CurrentBattery, _maxBattery);
                UIManager.Instance.SetFlashlightState(IsLightOn);
            }
        }

        private void OnDestroy()
        {
            // [Added] ถอนการลงทะเบียน
            if (LoopManager.Instance != null) LoopManager.Instance.Unregister(this);
        }

        // [New Function] รีเซ็ตแบตเตอรี่เมื่อเริ่มลูปใหม่
        public void OnLoopReset(int currentLoop)
        {
            CurrentBattery = _maxBattery;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBatteryUI(CurrentBattery, _maxBattery);
            }
            Debug.Log("[Flashlight] Battery Refilled on Reset.");
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
            if (_crankTimer > 0f) _crankTimer -= Time.deltaTime;

            if (_inputManager.IsCranking && !_wasCranking && _crankTimer <= 0f)
            {
                CurrentBattery += (_maxBattery * 0.4f);
                if (CurrentBattery > _maxBattery) CurrentBattery = _maxBattery;
                _crankTimer = _crankCooldown;

                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(_crankSoundName);
                if (UIManager.Instance != null) { UIManager.Instance.UpdateBatteryUI(CurrentBattery, _maxBattery); UIManager.Instance.ShowBatteryTemp(); }

                if (_wantsLightOn && _lightSource != null && !_lightSource.enabled)
                {
                    _lightSource.enabled = true;
                    if (UIManager.Instance != null) UIManager.Instance.SetFlashlightState(true);
                }
            }
            _wasCranking = _inputManager.IsCranking;

            if (IsLightOn)
            {
                CurrentBattery -= _drainRate * Time.deltaTime;
                if (UIManager.Instance != null) UIManager.Instance.UpdateBatteryUI(CurrentBattery, _maxBattery);
                if (CurrentBattery <= 0)
                {
                    CurrentBattery = 0;
                    if (_lightSource != null) _lightSource.enabled = false;
                    if (UIManager.Instance != null) UIManager.Instance.SetFlashlightState(false);
                }
            }

            if (IsLightOn && _lightSource != null)
            {
                float batteryPercent = CurrentBattery / _maxBattery;
                _lightSource.intensity = _initialIntensity * batteryPercent;
                if (batteryPercent < 0.2f) _lightSource.intensity += Random.Range(-0.2f, 0.2f);
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
            _wantsLightOn = !_wantsLightOn;
            if (_lightSource != null) _lightSource.enabled = IsLightOn;
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(_toggleSoundName);
            if (UIManager.Instance != null) { UIManager.Instance.UpdateBatteryUI(CurrentBattery, _maxBattery); UIManager.Instance.SetFlashlightState(IsLightOn); }
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

            if (baselineDist <= 0.0001f) { SetFlashlightWorldPosition(desiredWorldPos); return; }

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
            else { targetWorldPos = (baselineDist > _maxDistanceFromCamera) ? origin + baselineDir * _maxDistanceFromCamera : desiredWorldPos; }
            SetFlashlightWorldPositionSmooth(targetWorldPos);
        }

        private void SetFlashlightWorldPosition(Vector3 worldPos) { if (transform.parent == _cameraTransform) transform.localPosition = _cameraTransform.InverseTransformPoint(worldPos); else transform.position = worldPos; }
        private void SetFlashlightWorldPositionSmooth(Vector3 worldPos) { if (transform.parent == _cameraTransform) { Vector3 targetLocal = _cameraTransform.InverseTransformPoint(worldPos); transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetLocal, ref _positionVelocity, 1f / Mathf.Max(0.0001f, _positionSmoothSpeed)); } else { transform.position = Vector3.SmoothDamp(transform.position, worldPos, ref _positionVelocity, 1f / Mathf.Max(0.0001f, _positionSmoothSpeed)); } }
    }
}