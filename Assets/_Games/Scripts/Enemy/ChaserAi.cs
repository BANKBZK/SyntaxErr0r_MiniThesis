using UnityEngine;
using UnityEngine.AI;
using SyntaxError.Player;
using SyntaxError.Inputs;

namespace SyntaxError.Enemy
{
    public enum AIState
    {
        Idle,     // หยุดยืนนิ่งๆ
        Patrol,   // เดินสุ่ม
        Stalk,    // แอบตาม (อ้อมหลัง)
        Chase,    // วิ่งไล่ฆ่า
        Flee      // วิ่งหนีไปซ่อน
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

        [Header("Opportunity System (RNG)")]
        [Tooltip("ความฉลาด/ความดุ ของ AI (1-20) ยิ่งเยอะยิ่งขยับอ้อมหลังบ่อย")]
        [Range(1, 20)]
        [SerializeField] private int _aiLevel = 12;
        private string _lastRollResult = ""; // เอาไว้โชว์บน Debug UI

        [Header("State Intervals (ระยะเวลาของแต่ละโหมด)")]
        [Tooltip("เวลาที่ใช้นืนพัก (สั้นสุด)")]
        [SerializeField] private float _idleTimeMin = 1f;
        [SerializeField] private float _idleTimeMax = 3f;

        [Tooltip("เวลาที่ใช้เดินสุ่มหา (ปานกลาง)")]
        [SerializeField] private float _patrolTimeMin = 5f;
        [SerializeField] private float _patrolTimeMax = 10f;

        [Tooltip("เวลาที่ใช้แอบอ้อมหลัง (นานที่สุด)")]
        [SerializeField] private float _stalkTimeMin = 10f;
        [SerializeField] private float _stalkTimeMax = 15f;

        private float _stateTimer = 0f; // ตัวนับเวลาถอยหลังของโหมดปัจจุบัน

        [Header("Director System (Menace Gauge)")]
        [SerializeField] private float _menaceGauge = 0f;
        [SerializeField] private float _maxMenace = 100f;

        [Header("Vision Settings (การมองเห็น)")]
        [SerializeField] private float _sightRadiusDark = 4f;
        [SerializeField] private float _sightRadiusLight = 25f;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Light Stun Settings (ส่องไฟไล่ผี)")]
        [SerializeField] private float _timeToStun = 4.0f;
        [SerializeField] private float _minBatteryToStun = 50f;
        private float _currentStunTime = 0f;

        [Header("Hearing Settings (การได้ยิน)")]
        [SerializeField] private float _crankHearingRadius = 20f;
        [SerializeField] private float _sprintHearingRadius = 15f;
        [SerializeField] private float _walkHearingRadius = 4f;

        [Header("Movement & Speeds")]
        [SerializeField] private float _patrolSpeed = 1.5f;
        [SerializeField] private float _stalkSpeed = 2.5f;
        [SerializeField] private float _chaseSpeed = 4.8f;
        [SerializeField] private float _fleeSpeed = 7.0f;
        [SerializeField] private float _killDistance = 1.6f;

        [Header("Turn & Acceleration")]
        [SerializeField] private float _turnSpeed = 150f;
        [SerializeField] private float _acceleration = 5f;

        private Vector3 _lastPlayerPos;
        private Vector3 _playerVelocity;

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

            // เริ่มต้นด้วยการยืน Idle
            ChangeState(AIState.Idle);
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
        // 🔄 ระบบเปลี่ยน State (จัดการเวลา)
        // ==========================================
        private void ChangeState(AIState newState)
        {
            CurrentState = newState;

            switch (newState)
            {
                case AIState.Idle:
                    _agent.speed = 0f;
                    SafeSetDestination(transform.position); // หยุดเดิน
                    _stateTimer = Random.Range(_idleTimeMin, _idleTimeMax); // สุ่มเวลาพักสั้นๆ
                    break;

                case AIState.Patrol:
                    _agent.speed = _patrolSpeed;
                    _stateTimer = Random.Range(_patrolTimeMin, _patrolTimeMax); // สุ่มเวลาเดินปานกลาง
                    PatrolRandomly(transform.position, 15f);
                    break;

                case AIState.Stalk:
                    _agent.speed = _stalkSpeed;
                    _stateTimer = Random.Range(_stalkTimeMin, _stalkTimeMax); // สุ่มเวลาแอบตามนานๆ
                    PickFlankPosition();
                    break;

                case AIState.Chase:
                    _agent.speed = _chaseSpeed;
                    break;

                case AIState.Flee:
                    _agent.speed = _fleeSpeed;
                    _stateTimer = 5.0f; // เวลาที่ใช้วิ่งหนี
                    _currentStunTime = 0f;
                    PickFleePosition();
                    break;
            }
        }

        // ==========================================
        // 🔦 ระบบ Stun 
        // ==========================================
        private void HandleLightStunTimer(bool canSeePlayer, bool isLookingAtMe)
        {
            bool isLightStrongEnough = _playerFlashlight != null && _playerFlashlight.IsLightOn && _playerFlashlight.CurrentBattery >= _minBatteryToStun;

            if (isLookingAtMe && isLightStrongEnough && canSeePlayer)
            {
                if (CurrentState == AIState.Stalk || CurrentState == AIState.Patrol) _agent.speed = 0f;
                _currentStunTime += Time.deltaTime;
            }
            else
            {
                _currentStunTime = Mathf.Max(0, _currentStunTime - (Time.deltaTime * 2f));
                if (CurrentState == AIState.Stalk) _agent.speed = _stalkSpeed;
                if (CurrentState == AIState.Patrol) _agent.speed = _patrolSpeed;
            }
        }

        // ==========================================
        // 🧠 AI State Machine
        // ==========================================
        private void HandleStateMachine(float distance, bool canSee, bool canHear, bool isLookingAtMe)
        {
            switch (CurrentState)
            {
                case AIState.Idle:
                    // โดนขัดจังหวะด้วยเสียงหรือการมองเห็น
                    if (canHear) { ChangeState(AIState.Stalk); break; }
                    if (canSee && distance < 10f) { ChangeState(AIState.Chase); break; }

                    // นับเวลาถอยหลังจนกว่าจะสุ่มใหม่
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        int roll = Random.Range(1, 21);
                        if (roll <= _aiLevel)
                        {
                            _lastRollResult = $"ทอยได้ {roll} <= {_aiLevel}: เลิกพัก.. ย่องไปดักหลัง!";
                            ChangeState(AIState.Stalk);
                        }
                        else
                        {
                            _lastRollResult = $"ทอยได้ {roll} > {_aiLevel}: เดินสุ่มตรวจตราปกติ";
                            ChangeState(AIState.Patrol);
                        }
                    }
                    break;

                case AIState.Patrol:
                    if (canHear) { ChangeState(AIState.Stalk); break; }
                    if (canSee && distance < 10f) { ChangeState(AIState.Chase); break; }

                    // ถ้าเดินไปถึงจุดหมายก่อนหมดเวลา ให้สุ่มจุดเดินใหม่
                    if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                    {
                        PatrolRandomly(transform.position, 15f);
                    }

                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        int roll = Random.Range(1, 21);
                        if (roll <= _aiLevel)
                        {
                            _lastRollResult = $"ทอยได้ {roll} <= {_aiLevel}: สบโอกาส เข้าโหมด Stalk!";
                            ChangeState(AIState.Stalk);
                        }
                        else
                        {
                            _lastRollResult = $"ทอยได้ {roll} > {_aiLevel}: หยุดยืนพักเหนื่อย";
                            ChangeState(AIState.Idle);
                        }
                    }
                    break;

                case AIState.Stalk:
                    if (_currentStunTime >= _timeToStun) { ChangeState(AIState.Flee); break; }
                    if (_playerInput.IsCranking || (canSee && distance < 6f)) { ChangeState(AIState.Chase); break; }
                    if (!canHear && !canSee && _agent.remainingDistance < 1f) { ChangeState(AIState.Idle); break; } // คลาดกัน หาไม่เจอ

                    // ถ้าเดินมาถึงจุดซุ่มก่อนเวลาหมด ให้ขยับหาจุดใหม่เรื่อยๆ
                    if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                    {
                        PickFlankPosition();
                    }

                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        int roll = Random.Range(1, 21);
                        if (roll <= _aiLevel)
                        {
                            _lastRollResult = $"ทอยได้ {roll} <= {_aiLevel}: ตามซุ่มต่อ หาจุดใหม่!";
                            ChangeState(AIState.Stalk); // เริ่มเวลา Stalk ใหม่
                        }
                        else
                        {
                            _lastRollResult = $"ทอยได้ {roll} > {_aiLevel}: เลิกตามแอบ กลับไปยืนพัก";
                            ChangeState(AIState.Idle);
                        }
                    }
                    break;

                case AIState.Chase:
                    SafeSetDestination(_playerTransform.position);

                    if (_menaceGauge >= _maxMenace) ChangeState(AIState.Flee);
                    else if (_currentStunTime >= _timeToStun) ChangeState(AIState.Flee);
                    else if (distance > 25f) ChangeState(AIState.Idle); // วิ่งหลุดระยะแล้ว
                    break;

                case AIState.Flee:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        _menaceGauge = 0f;
                        ChangeState(AIState.Idle); // หนีพ้นแล้วไปแอบยืนนิ่งๆ
                    }
                    break;
            }
        }

        // ==========================================
        // 📍 ฟังก์ชันคำนวณจุดเดิน (Flank, Flee, Patrol)
        // ==========================================
        private void PickFlankPosition()
        {
            // หาจุดอ้อมไปด้านหลัง หรือด้านข้างของผู้เล่น
            Vector3 flankPos = _playerTransform.position
                             - (_playerTransform.forward * Random.Range(8f, 15f))
                             + (_playerTransform.right * Random.Range(-10f, 10f));

            NavMeshHit hit;
            if (NavMesh.SamplePosition(flankPos, out hit, 10f, NavMesh.AllAreas))
            {
                SafeSetDestination(hit.position);
            }
        }

        private void PickFleePosition()
        {
            // หนีไปในทิศตรงข้ามกับที่ผู้เล่นมองอยู่
            Vector3 fleeDirection = -_playerCamera.forward;
            Vector3 fleePos = transform.position + (fleeDirection * 20f) + (Random.insideUnitSphere * 5f);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePos, out hit, 15f, NavMesh.AllAreas))
            {
                SafeSetDestination(hit.position);
            }
        }

        private void PatrolRandomly(Vector3 origin, float radius)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += origin;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            {
                SafeSetDestination(hit.position);
            }
        }

        // ==========================================
        // 🛡️ Helper Methods
        // ==========================================
        private void SafeSetDestination(Vector3 targetPos)
        {
            if (_agent != null && _agent.isOnNavMesh) _agent.SetDestination(targetPos);
        }

        private void UpdateMenaceGauge(float distance)
        {
            if (distance < 10f && CurrentState != AIState.Flee) _menaceGauge += (10f - distance) * Time.deltaTime * 2f;
            else _menaceGauge -= Time.deltaTime * 3f;
            _menaceGauge = Mathf.Clamp(_menaceGauge, 0f, _maxMenace);
        }

        private bool CheckIfPlayerLooking()
        {
            if (_playerCamera == null) return false;
            Vector3 dirToAI = (transform.position - _playerCamera.position).normalized;
            return Vector3.Dot(_playerCamera.forward, dirToAI) > 0.85f;
        }

        private void CalculatePlayerVelocity()
        {
            if (Time.deltaTime > 0f)
            {
                _playerVelocity = (_playerTransform.position - _lastPlayerPos) / Time.deltaTime;
                if (_playerVelocity.magnitude > 15f) _playerVelocity = _playerVelocity.normalized * 15f;
            }
            else _playerVelocity = Vector3.zero;

            _lastPlayerPos = _playerTransform.position;
        }

        private void TryKillPlayer(float distance)
        {
            Vector3 aiEyePos = transform.position + Vector3.up;
            Vector3 playerEyePos = _playerTransform.position + Vector3.up;
            Vector3 dirToPlayer = (playerEyePos - aiEyePos).normalized;

            if (!Physics.Raycast(aiEyePos, dirToPlayer, distance, _obstacleMask)) CatchPlayer();
        }

        private bool CheckLineOfSight(float distanceToPlayer)
        {
            float currentSightRadius = (_playerFlashlight != null && _playerFlashlight.IsLightOn) ? _sightRadiusLight : _sightRadiusDark;
            if (distanceToPlayer > currentSightRadius) return false;

            Vector3 aiEyePos = transform.position + Vector3.up;
            Vector3 playerEyePos = _playerTransform.position + Vector3.up;
            return !Physics.Raycast(aiEyePos, (playerEyePos - aiEyePos).normalized, distanceToPlayer, _obstacleMask);
        }

        private bool CheckHearing(float distanceToPlayer)
        {
            if (_playerInput == null) return false;
            float currentNoiseLevel = 0f;
            bool isMoving = _playerInput.MoveInput.magnitude > 0.1f;

            if (_playerInput.IsCranking) currentNoiseLevel = Mathf.Max(currentNoiseLevel, _crankHearingRadius);
            if (isMoving)
            {
                if (_playerInput.IsCrouching) currentNoiseLevel = Mathf.Max(currentNoiseLevel, 1f);
                else if (_playerInput.IsSprinting) currentNoiseLevel = Mathf.Max(currentNoiseLevel, _sprintHearingRadius);
                else currentNoiseLevel = Mathf.Max(currentNoiseLevel, _walkHearingRadius);
            }
            return distanceToPlayer <= currentNoiseLevel;
        }

        private void CatchPlayer()
        {
            Debug.Log("<color=red>YOU DIED! โดนจับได้แล้ว!</color>");
            //_agent.isStopped = true;
            //this.enabled = false;
        }

        private void UpdateDebugUI()
        {
            if (Managers.UIManager.Instance == null) return;

            string debugString = $"<color=yellow>Enemy State: {CurrentState}</color>\n";

            if (CurrentState == AIState.Stalk || CurrentState == AIState.Idle || CurrentState == AIState.Patrol)
            {
                debugString += $"<color=orange>[RNG] {_lastRollResult}</color>\n";
            }

            if (CurrentState == AIState.Flee)
            {
                debugString += "<color=green>Enemy โดน สตั้นแล้ว!</color>";
            }
            else if (_currentStunTime > 0)
            {
                float stunPercent = (_currentStunTime / _timeToStun) * 100f;
                debugString += $"<color=orange>กำลังสตั้น enemy... {stunPercent:F0}%</color>";
            }
            else
            {
                debugString += "<color=white>Stun: 0%</color>";
            }

            Managers.UIManager.Instance.UpdateAIDebugText(debugString);
        }
    }
}