using UnityEngine;
using SyntaxError.Inputs;
using SyntaxError.Managers; // เพื่อเรียกใช้ SoundManager

namespace SyntaxError.Player
{
    public class FlashlightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private Light _lightSource; // ตัว Spotlight
        [SerializeField] private Transform _cameraTransform; // กล้องหลัก
        [SerializeField] private Transform _modelTransform; // Transform ของตัว Model ที่เป็นตำแหน่ง "ต้นฉบับ" ของไฟฉาย

        [Header("Settings")]
        [SerializeField] private bool _isOn = true;
        [SerializeField] private string _toggleSoundName = "Click"; // ชื่อเสียงใน SoundManager

        [Header("Sway Settings (ความหน่วง)")]
        [SerializeField] private float _swaySpeed = 5f; // ความไวในการหันตาม
        [SerializeField] private float _dragAmount = 2f; // ลากเมาส์แล้วไฟฉายเหวี่ยงแค่ไหน

        [Header("Collision / Obstruction Settings (ไม่ให้ไฟฉายทะลุกำแพง)")]
        [SerializeField] private bool _enableCollision = true;
        [SerializeField] private LayerMask _obstructionMask = ~0; // เริ่มต้นตรวจทุกชั้น
        [SerializeField] private float _collisionRadius = 0.08f; // radius สำหรับ SphereCast
        [SerializeField] private float _collisionOffset = 0.03f; // ระยะหนีจากผนังเมื่อชน
        [SerializeField] private float _maxDistanceFromCamera = 0.6f; // ระยะปกติของไฟฉายจากกล้อง
        [SerializeField] private float _minDistanceFromCamera = 0.12f; // ระยะใกล้สุดไม่ให้ไฟฉายชนกล้อง
        [SerializeField] private float _positionSmoothSpeed = 10f; // ความนุ่มของการเลื่อนตำแหน่ง

        private Quaternion _initialRotation;
        private Vector3 _initialLocalPosition;
        private Vector3 _positionVelocity;

        private void Start()
        {
            if (_inputManager == null) _inputManager = GetComponentInParent<InputManager>();
            if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;

            // ถ้าไม่ได้ระบุ model transform ให้พยายามหา child ชื่อ "Model" หรือใช้ตัวเองเป็น fallback
            if (_modelTransform == null)
            {
                var found = transform.Find("Model");
                _modelTransform = found != null ? found : transform;
            }

            _initialRotation = transform.localRotation;
            _initialLocalPosition = transform.localPosition;

            // เซ็ตค่าเริ่มต้น
            if (_lightSource != null) _lightSource.enabled = _isOn;
        }

        private void Update()
        {
            HandleInput();
            HandleSway();
            HandleCollision();
        }

        private void HandleInput()
        {
            if (_inputManager.IsFlashlightPressed)
            {
                ToggleLight();
                _inputManager.ConsumeFlashlightInput(); // รีเซ็ตปุ่มทันที (กดทีละครั้ง)
            }
        }

        private void ToggleLight()
        {
            _isOn = !_isOn;
            if (_lightSource != null) _lightSource.enabled = _isOn;

            // เล่นเสียง
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(_toggleSoundName);
        }

        private void HandleSway()
        {
            if (_cameraTransform == null) return;

            // รับค่าเมาส์จาก InputManager
            float mouseX = _inputManager.LookInput.x;
            float mouseY = _inputManager.LookInput.y;

            // คำนวณตำแหน่งที่จะเหวี่ยงไป (กลับทิศทางเมาส์)
            Quaternion rotationX = Quaternion.AngleAxis(-mouseY * _dragAmount, Vector3.right);
            Quaternion rotationY = Quaternion.AngleAxis(mouseX * _dragAmount, Vector3.up);

            Quaternion targetRotation = _initialRotation * rotationX * rotationY;

            // ค่อยๆ หมุนไปหาจุดเป้าหมาย (Lerp) — camera ที่ส่งข้อมูลการหันเท่านั้น ไม่เปลี่ยนตำแหน่ง
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, _swaySpeed * Time.deltaTime);
        }

        private void HandleCollision()
        {
            if (!_enableCollision) return;
            if (_cameraTransform == null) return;
            if (_modelTransform == null) return;

            // ใช้ตำแหน่งของ model เป็นตำแหน่ง "ต้นฉบับ" ที่ไฟฉายควรจะไปหา
            Vector3 desiredWorldPos = _modelTransform.position;

            // origin คือตำแหน่งกล้อง — ตรวจดูว่ามีสิ่งกีดขวางระหว่างกล้องกับ model หรือไม่
            Vector3 origin = _cameraTransform.position;
            Vector3 direction = desiredWorldPos - origin;
            float baselineDist = direction.magnitude;

            if (baselineDist <= 0.0001f)
            {
                // หาก model อยู่ที่ตำแหน่งกล้อง ให้วางไฟฉายที่ตำแหน่ง model โดยตรง
                SetFlashlightWorldPosition(desiredWorldPos);
                return;
            }

            Vector3 baselineDir = direction / baselineDist;

            // ไม่พิจารณาเกิน max distance แต่โดยดีฟอลต์ เราต้องการให้ไฟฉายตาม model ถ้าไม่มีการบัง
            float effectiveBaselineDist = Mathf.Min(baselineDist, _maxDistanceFromCamera);

            // SphereCast จากกล้องไปหา model เพื่อตรวจการบัง
            RaycastHit hit;
            bool isHit = Physics.SphereCast(origin, _collisionRadius, baselineDir, out hit, effectiveBaselineDist, _obstructionMask, QueryTriggerInteraction.Ignore);

            // ปรับเป้าหมาย world position ตามผลการตรวจ
            Vector3 currentWorldPos = transform.parent == _cameraTransform ? _cameraTransform.TransformPoint(transform.localPosition) : transform.position;
            float currentDistAlong = Vector3.Dot(currentWorldPos - origin, baselineDir);

            Vector3 targetWorldPos;

            if (isHit)
            {
                // หากมีการชน ให้คำนวณตำแหน่งที่ปลอดภัยก่อนชน
                float hitDist = hit.distance;
                float targetDist = hitDist - _collisionOffset;
                targetDist = Mathf.Clamp(targetDist, _minDistanceFromCamera, effectiveBaselineDist);

                // ผู้ใช้ต้องการ "เฉพาะถอยไปข้างหลัง" เป็นการลดระยะ (pull closer to camera) เมื่อชน
                // ดังนั้นถ้า currentDistAlong <= targetDist (ไฟฉายอยู่ใกล้กว่าหรือเท่ากับตำแหน่งปลอดภัยแล้ว) ให้คงตำแหน่งไว้
                if (currentDistAlong <= targetDist)
                {
                    targetWorldPos = currentWorldPos;
                }
                else
                {
                    // ย้ายไฟฉายเข้าไปใกล้กล้อง (ลดระยะ) ให้เป็น targetDist ตาม baselineDir
                    targetWorldPos = origin + baselineDir * targetDist;
                }
            }
            else
            {
                // ไม่มีสิ่งกีดขวาง — เป้าหมายคือตำแหน่งของ model (ตามที่ user ต้องการ)
                // แต่หาก model อยู่ไกลเกิน maxDistance ให้จำกัดไว้ที่ maxDistance ตาม baselineDir
                if (baselineDist > _maxDistanceFromCamera)
                {
                    targetWorldPos = origin + baselineDir * _maxDistanceFromCamera;
                }
                else
                {
                    targetWorldPos = desiredWorldPos;
                }
            }

            // เคลื่อนที่ไปยัง targetWorldPos อย่างนุ่มนวล (หรือ snap หากต้องการ)
            SetFlashlightWorldPositionSmooth(targetWorldPos);
        }

        private void SetFlashlightWorldPosition(Vector3 worldPos)
        {
            if (transform.parent == _cameraTransform)
            {
                transform.localPosition = _cameraTransform.InverseTransformPoint(worldPos);
            }
            else
            {
                transform.position = worldPos;
            }
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

        // Draw debug gizmos for the spherecast path and the computed safe position
        private void OnDrawGizmosSelected()
        {
            if (_cameraTransform == null)
            {
                if (Camera.main != null)
                    _cameraTransform = Camera.main.transform;
                else
                    return;
            }

            if (_modelTransform == null)
            {
                var found = transform.Find("Model");
                _modelTransform = found != null ? found : transform;
            }

            Vector3 origin = _cameraTransform.position;
            Vector3 desiredWorldPos = _modelTransform.position;
            Vector3 direction = desiredWorldPos - origin;
            float baselineDist = direction.magnitude;
            if (baselineDist <= 0.0001f) return;
            Vector3 baselineDir = direction / baselineDist;

            float effectiveBaselineDist = Mathf.Min(baselineDist, _maxDistanceFromCamera);

            // Draw baseline line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + baselineDir * effectiveBaselineDist);

            // Draw wire spheres along the path to visualize the spherecast (sampled)
            int samples = Mathf.Clamp(Mathf.CeilToInt(effectiveBaselineDist / (_collisionRadius * 0.5f)), 4, 64);
            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector3 pos = origin + baselineDir * (t * effectiveBaselineDist);
                Gizmos.color = new Color(1f, 1f, 0f, 0.12f);
                Gizmos.DrawSphere(pos, _collisionRadius * 0.6f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pos, _collisionRadius);
            }

            // Perform a Physics.SphereCast for visualization
            RaycastHit hit;
            bool isHit = Physics.SphereCast(origin, _collisionRadius, baselineDir, out hit, effectiveBaselineDist, _obstructionMask, QueryTriggerInteraction.Ignore);
            if (isHit)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hit.point, _collisionRadius * 0.6f);
                Gizmos.color = Color.magenta;
                Vector3 safePoint = hit.point - baselineDir * _collisionOffset;
                Gizmos.DrawSphere(safePoint, _collisionRadius * 0.6f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.1f);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(desiredWorldPos, _collisionRadius * 0.6f);
            }

            // Draw current flashlight world position for reference
            Vector3 currentWorldPos = transform.parent == _cameraTransform ? _cameraTransform.TransformPoint(transform.localPosition) : transform.position;
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(currentWorldPos, _collisionRadius * 0.5f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(origin, currentWorldPos);
        }
    }
}