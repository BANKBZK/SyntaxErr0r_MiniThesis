using UnityEngine;
using UnityEngine.AI;
using SyntaxError.Player;
using SyntaxError.Inputs;

namespace SyntaxError.Enemy
{
    public enum AIState
    {
        Idle,     // หยุดยืนนิ่งๆ (พัก/ดักฟัง)
        Patrol,   // เดินสุ่ม
        Stalk,    // เดินดักหน้า (Predictive)
        Chase,    // วิ่งไล่ฆ่า
        Flee      // วิ่งหนีเพื่อไปอ้อมหลัง (Hit & Run)
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class ChaserAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Transform _playerCamera;
        [SerializeField] private InputManager _playerInput;
        [SerializeField] private FlashlightController _playerFlashlight;
        private NavMeshAgent _agent;

        [Header("AI State (For Debug)")]
        public AIState CurrentState = AIState.Idle;

        [Header("Director System (Menace Gauge)")]
        [SerializeField] private float _menaceGauge = 0f;
        [SerializeField] private float _maxMenace = 100f;

        [Header("Vision Settings (การมองเห็น)")]
        [SerializeField] private float _sightRadiusDark = 4f;
        [SerializeField] private float _sightRadiusLight = 25f;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Light Stun Settings (ส่องไฟไล่ผี)")]
        [Tooltip("ต้องส่องไฟกี่วินาทีผีถึงจะหนี")]
        [SerializeField] private float _timeToStun = 4.0f;
        [Tooltip("แบตเตอรี่ไฟฉายขั้นต่ำที่จะทำให้ผีแสบตา (สมมติเต็ม 100)")]
        [SerializeField] private float _minBatteryToStun = 50f;
        private float _currentStunTime = 0f; // ตัวนับเวลาส่องไฟ

        [Header("Hearing Settings (การได้ยิน)")]
        [SerializeField] private float _crankHearingRadius = 20f;
        [SerializeField] private float _sprintHearingRadius = 15f;
        [SerializeField] private float _walkHearingRadius = 4f;

        [Header("Movement & Speeds")]
        [SerializeField] private float _patrolSpeed = 1.5f;
        [SerializeField] private float _stalkSpeed = 2.5f;
        [SerializeField] private float _chaseSpeed = 4.8f;
        [SerializeField] private float _fleeSpeed = 6.0f;
        [SerializeField] private float _killDistance = 1.6f;

        [Header("Idle Settings (ยืนพัก)")]
        [SerializeField] private float _minIdleTime = 2f;
        [SerializeField] private float _maxIdleTime = 5f;
        private float _idleTimer = 0f;

        [Header("Turn & Acceleration (ความสมูท)")]
        [SerializeField] private float _turnSpeed = 150f;
        [SerializeField] private float _acceleration = 5f;

        private Vector3 _lastPlayerPos;
        private Vector3 _playerVelocity;
        private float _fleeTimer = 0f;

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();

            _agent.angularSpeed = _turnSpeed;
            _agent.acceleration = _acceleration;

            if (_playerTransform == null) _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            if (_playerCamera == null && Camera.main != null) _playerCamera = Camera.main.transform;
            if (_playerInput == null) _playerInput = FindFirstObjectByType<InputManager>();
            if (_playerFlashlight == null) _playerFlashlight = FindFirstObjectByType<FlashlightController>();

            _lastPlayerPos = _playerTransform.position;
            CurrentState = AIState.Idle;
            _idleTimer = Random.Range(_minIdleTime, _maxIdleTime);
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            CalculatePlayerVelocity();

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            bool canSeePlayer = CheckLineOfSight(distanceToPlayer);
            bool canHearPlayer = CheckHearing(distanceToPlayer);
            bool isPlayerLookingAtMe = CheckIfPlayerLooking();

            UpdateMenaceGauge(distanceToPlayer);
            HandleLightStunTimer(canSeePlayer, isPlayerLookingAtMe);
            HandleStateMachine(distanceToPlayer, canSeePlayer, canHearPlayer, isPlayerLookingAtMe);

            if (distanceToPlayer <= _killDistance && CurrentState != AIState.Flee)
            {
                TryKillPlayer(distanceToPlayer);
            }
            UpdateDebugUI();
        }

        // ==========================================
        // 🔦 ระบบจับเวลาส่องไฟฉาย
        // ==========================================
        private void HandleLightStunTimer(bool canSeePlayer, bool isLookingAtMe)
        {
            // เช็คว่าผู้เล่นเปิดไฟอยู่ และแบตเตอรี่เกินกำหนดที่ตั้งไว้หรือไม่
            bool isLightStrongEnough = _playerFlashlight != null && _playerFlashlight.IsLightOn && _playerFlashlight.CurrentBattery >= _minBatteryToStun;

            // ถ้าผู้เล่นมองอยู่ + ไฟแรงพอ + ผีอยู่ในระยะมองเห็น
            if (isLookingAtMe && isLightStrongEnough && canSeePlayer)
            {
                _currentStunTime += Time.deltaTime;
                // (Optional) ตรงนี้นายสามารถใส่เสียงผีกรีดร้องเบาๆ หรือเสียงไฟฉายซ่าๆ เพื่อเตือนผู้เล่นว่ามันกำลังจะทนไม่ไหวแล้ว
            }
            else
            {
                // ถ้าส่องไม่ต่อเนื่อง หลอดจะลดลงอย่างรวดเร็ว บังคับให้ต้องจ้องนิ่งๆ
                _currentStunTime = Mathf.Max(0, _currentStunTime - (Time.deltaTime * 2f));
            }
        }

        private void HandleStateMachine(float distance, bool canSee, bool canHear, bool isLookingAtMe)
        {
            switch (CurrentState)
            {
                case AIState.Idle:
                    _agent.speed = 0f;

                    if (canHear) CurrentState = AIState.Stalk;
                    if (canSee && distance < 10f) CurrentState = AIState.Chase;

                    _idleTimer -= Time.deltaTime;
                    if (_idleTimer <= 0f)
                    {
                        CurrentState = AIState.Patrol;
                        PatrolRandomly(transform.position, 15f);
                    }
                    break;

                case AIState.Patrol:
                    _agent.speed = _patrolSpeed;
                    if (canHear) CurrentState = AIState.Stalk;
                    if (canSee && distance < 10f) CurrentState = AIState.Chase;

                    if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                    {
                        CurrentState = AIState.Idle;
                        _idleTimer = Random.Range(_minIdleTime, _maxIdleTime);
                    }
                    break;

                case AIState.Stalk:
                    _agent.speed = _stalkSpeed;

                    // --- แก้บั๊ก Invalid AABB (ดักหน้าปลอดภัยขึ้น) ---
                    Vector3 predictedPos = _playerTransform.position + (_playerVelocity * 3.0f);
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(predictedPos, out hit, 5f, NavMesh.AllAreas))
                    {
                        _agent.SetDestination(hit.position);
                    }
                    else
                    {
                        // ถ้าจุดพยากรณ์อยู่นอก NavMesh ให้เดินไปหาผู้เล่นตรงๆ แทน
                        _agent.SetDestination(_playerTransform.position);
                    }

                    // ถอนการวิ่งหนีแบบทันทีออก เปลี่ยนมาเช็คตัวจับเวลาส่องไฟแทน
                    if (_currentStunTime >= _timeToStun)
                    {
                        StartFleeing();
                    }
                    else if (_playerInput.IsCranking || (canSee && distance < 8f))
                    {
                        CurrentState = AIState.Chase;
                    }
                    else if (!canHear && !canSee && _agent.remainingDistance < 1f)
                    {
                        CurrentState = AIState.Idle;
                        _idleTimer = Random.Range(_minIdleTime, _maxIdleTime);
                    }
                    break;

                case AIState.Chase:
                    _agent.speed = _chaseSpeed;
                    _agent.SetDestination(_playerTransform.position);

                    if (_menaceGauge >= _maxMenace)
                    {
                        StartFleeing();
                    }
                    // เช็คตัวจับเวลาส่องไฟแทนการวิ่งหนีทันที
                    else if (_currentStunTime >= _timeToStun)
                    {
                        StartFleeing();
                    }
                    else if (distance > 20f)
                    {
                        CurrentState = AIState.Idle;
                        _idleTimer = 3f;
                    }
                    break;

                case AIState.Flee:
                    _agent.speed = _fleeSpeed;
                    _fleeTimer -= Time.deltaTime;

                    if (_fleeTimer <= 0f)
                    {
                        _menaceGauge = 0f;
                        CurrentState = AIState.Stalk;
                    }
                    break;
            }
        }

        private void StartFleeing()
        {
            CurrentState = AIState.Flee;
            _fleeTimer = 4.0f;
            _currentStunTime = 0f; // รีเซ็ตตัวจับเวลาส่องไฟ

            Vector3 fleeDirection = (transform.position - _playerTransform.position).normalized;
            Vector3 fleePos = transform.position + (fleeDirection * 15f);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePos, out hit, 10f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }

        private void UpdateMenaceGauge(float distance)
        {
            if (distance < 10f && CurrentState != AIState.Flee)
            {
                _menaceGauge += (10f - distance) * Time.deltaTime * 2f;
            }
            else
            {
                _menaceGauge -= Time.deltaTime * 3f;
            }
            _menaceGauge = Mathf.Clamp(_menaceGauge, 0f, _maxMenace);
        }

        private bool CheckIfPlayerLooking()
        {
            if (_playerCamera == null) return false;

            Vector3 dirToAI = (transform.position - _playerCamera.position).normalized;
            float dotProduct = Vector3.Dot(_playerCamera.forward, dirToAI);

            return dotProduct > 0.85f;
        }

        // ==========================================
        // 🔧 แก้บั๊ก Invalid AABB (การคำนวณ Velocity)
        // ==========================================
        private void CalculatePlayerVelocity()
        {
            if (Time.deltaTime > 0f)
            {
                _playerVelocity = (_playerTransform.position - _lastPlayerPos) / Time.deltaTime;

                // ป้องกัน AABB error เวลามีการวาร์ป ทำให้ความเร็วพุ่งเกินจริง
                if (_playerVelocity.magnitude > 15f)
                {
                    _playerVelocity = _playerVelocity.normalized * 15f;
                }
            }
            else
            {
                _playerVelocity = Vector3.zero;
            }

            _lastPlayerPos = _playerTransform.position;
        }

        private void TryKillPlayer(float distance)
        {
            Vector3 aiEyePos = transform.position + Vector3.up;
            Vector3 playerEyePos = _playerTransform.position + Vector3.up;
            Vector3 dirToPlayer = (playerEyePos - aiEyePos).normalized;

            if (!Physics.Raycast(aiEyePos, dirToPlayer, distance, _obstacleMask))
            {
                CatchPlayer();
            }
        }

        private bool CheckLineOfSight(float distanceToPlayer)
        {
            float currentSightRadius = (_playerFlashlight != null && _playerFlashlight.IsLightOn)
                                        ? _sightRadiusLight
                                        : _sightRadiusDark;

            if (distanceToPlayer > currentSightRadius) return false;

            Vector3 aiEyePos = transform.position + Vector3.up;
            Vector3 playerEyePos = _playerTransform.position + Vector3.up;
            Vector3 dirToPlayer = (playerEyePos - aiEyePos).normalized;

            if (Physics.Raycast(aiEyePos, dirToPlayer, distanceToPlayer, _obstacleMask))
            {
                return false;
            }

            return true;
        }

        private bool CheckHearing(float distanceToPlayer)
        {
            if (_playerInput == null) return false;

            float currentNoiseLevel = 0f;
            bool isMoving = _playerInput.MoveInput.magnitude > 0.1f;

            if (_playerInput.IsCranking)
                currentNoiseLevel = Mathf.Max(currentNoiseLevel, _crankHearingRadius);

            if (isMoving)
            {
                if (_playerInput.IsCrouching)
                {
                    currentNoiseLevel = Mathf.Max(currentNoiseLevel, 1f);
                }
                else if (_playerInput.IsSprinting)
                {
                    currentNoiseLevel = Mathf.Max(currentNoiseLevel, _sprintHearingRadius);
                }
                else
                {
                    currentNoiseLevel = Mathf.Max(currentNoiseLevel, _walkHearingRadius);
                }
            }

            return distanceToPlayer <= currentNoiseLevel;
        }

        private void PatrolRandomly(Vector3 origin, float radius)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += origin;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }

        private void CatchPlayer()
        {
            Debug.Log("<color=red>YOU DIED! โดนจับได้แล้ว!</color>");
            _agent.isStopped = true;
            this.enabled = false;

            // TODO: เรียกหน้าจอ Game Over
        }

        private void SafeSetDestination(Vector3 targetPos)
        {
            // เช็คชัวร์ๆ ว่า Agent เปิดอยู่ และยืนอยู่บน NavMesh จริงๆ ค่อยสั่งเดิน
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.SetDestination(targetPos);
            }
        }
        // ==========================================
        // 🛠️ ระบบ Debug UI โชว์สถานะผี
        // ==========================================
        private void UpdateDebugUI()
        {
            if (Managers.UIManager.Instance == null) return;

            // 1. โชว์ State ปัจจุบัน
            string debugString = $"<color=yellow>Enemy State: {CurrentState}</color>\n";

            // 2. โชว์สถานะการ Stun
            if (CurrentState == AIState.Flee)
            {
                // ถ้าผีอยู่ในโหมด Flee (วิ่งหนี) แปลว่ามันโดนสตั้นสำเร็จแล้ว หรือแกล้งหนี
                debugString += "<color=green>Enemy โดน สตั้นแล้ว!</color>";
            }
            else if (_currentStunTime > 0)
            {
                // กำลังส่องไฟอัดหน้ามันอยู่ คำนวณเป็นเปอร์เซ็นต์ (0 - 100%)
                float stunPercent = (_currentStunTime / _timeToStun) * 100f;
                debugString += $"<color=orange>กำลังสตั้น enemy... {stunPercent:F0}%</color>";
            }
            else
            {
                // ไม่ได้ส่องไฟ
                debugString += "<color=white>Stun: 0%</color>";
            }

            // ส่งข้อความไปให้ UIManager อัปเดตขึ้นจอ
            Managers.UIManager.Instance.UpdateAIDebugText(debugString);
        }
    }
}