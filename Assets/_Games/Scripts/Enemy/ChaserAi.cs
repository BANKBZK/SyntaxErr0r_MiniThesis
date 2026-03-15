using UnityEngine;
using UnityEngine.AI;
using SyntaxError.Player;
using SyntaxError.Inputs;
using SyntaxError.Managers;

namespace SyntaxError.Enemy
{
    public enum AIState
    {
        Idle,
        Patrol,
        Stalk,
        Chase,
        Flee
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
        [Range(1, 20)]
        [SerializeField] private int _aiLevel = 14;
        private string _lastRollResult = "";

        [Header("State Intervals")]
        [SerializeField] private float _idleTimeMin = 2f;
        [SerializeField] private float _idleTimeMax = 4f;
        [SerializeField] private float _patrolWaitTime = 2f;
        [SerializeField] private float _stalkWaitTime = 0.5f;

        private float _stateTimer = 0f;

        [Header("Impatience System (หาจังหวะทีเผลอ)")]
        [SerializeField] private float _impatienceGauge = 0f;
        [SerializeField] private float _maxImpatience = 100f;

        [Header("Vision Settings")]
        [SerializeField] private float _sightRadiusDark = 4f;
        [SerializeField] private float _sightRadiusLight = 25f;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Light Stun Settings")]
        [SerializeField] private float _timeToStun = 3.0f;
        [SerializeField] private float _minBatteryToStun = 50f;
        [SerializeField] private float _stunSlowdownMultiplier = 0.1f;
        private float _currentStunTime = 0f;

        [Header("Hearing Settings")]
        [SerializeField] private float _crankHearingRadius = 25f;
        [SerializeField] private float _sprintHearingRadius = 15f;
        [SerializeField] private float _walkHearingRadius = 4f;

        [Header("Audio (ความหลอน)")]
        [SerializeField] private string _stalkStartSound = "GhostWhisper";
        [SerializeField] private string _chaseMusic = "ChaseTheme";
        [SerializeField] private float _stalkSoundCooldown = 15f;
        private float _stalkSoundTimer = 0f;

        [Header("Movement & Speeds")]
        [SerializeField] private float _patrolSpeed = 1.2f;
        [SerializeField] private float _stalkSpeed = 3.5f;
        [SerializeField] private float _chaseSpeed = 5.5f;
        [SerializeField] private float _fleeSpeed = 8.0f;
        [SerializeField] private float _killDistance = 1.6f;

        [Header("Chase Settings")]
        [SerializeField] private float _maxChaseTime = 10f;

        [Header("Turn & Acceleration")]
        [SerializeField] private float _turnSpeed = 150f;
        [SerializeField] private float _acceleration = 6f;

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
            ChangeState(AIState.Idle);
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            if (_stalkSoundTimer > 0) _stalkSoundTimer -= Time.deltaTime;

            CalculatePlayerVelocity();

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            bool canSeePlayer = CheckLineOfSight(distanceToPlayer);
            bool canHearPlayer = CheckHearing(distanceToPlayer);
            bool isPlayerLookingAtMe = CheckIfPlayerLooking();

            UpdateImpatienceGauge(distanceToPlayer, isPlayerLookingAtMe);
            HandleLightStunTimer(canSeePlayer, isPlayerLookingAtMe);
            HandleStateMachine(distanceToPlayer, canSeePlayer, canHearPlayer, isPlayerLookingAtMe);

            if (distanceToPlayer <= _killDistance && CurrentState != AIState.Flee)
            {
                TryKillPlayer(distanceToPlayer);
            }

            UpdateDebugUI();
        }

        private void ChangeState(AIState newState)
        {
            if (CurrentState == AIState.Chase && newState != AIState.Chase)
            {
                if (SoundManager.Instance != null && !string.IsNullOrEmpty(_chaseMusic))
                    SoundManager.Instance.StopMusic(_chaseMusic);
            }

            CurrentState = newState;

            switch (newState)
            {
                case AIState.Idle:
                    _stateTimer = Random.Range(_idleTimeMin, _idleTimeMax);
                    _impatienceGauge = 0f;
                    SafeSetDestination(transform.position);
                    break;

                case AIState.Patrol:
                    _stateTimer = _patrolWaitTime;
                    PatrolRandomly(transform.position, 15f);
                    break;

                case AIState.Stalk:
                    _stateTimer = _stalkWaitTime;
                    PickFlankPosition();

                    if (SoundManager.Instance != null && !string.IsNullOrEmpty(_stalkStartSound) && _stalkSoundTimer <= 0f)
                    {
                        SoundManager.Instance.PlaySFX(_stalkStartSound);
                        _stalkSoundTimer = _stalkSoundCooldown;
                    }
                    break;

                case AIState.Chase:
                    _stateTimer = _maxChaseTime;
                    SafeSetDestination(_playerTransform.position);

                    if (SoundManager.Instance != null && !string.IsNullOrEmpty(_chaseMusic))
                    {
                        SoundManager.Instance.PlayMusic(_chaseMusic);
                    }
                    break;

                case AIState.Flee:
                    _stateTimer = 4.0f;
                    _currentStunTime = 0f;
                    PickFleePosition();
                    break;
            }

            UpdateAgentSpeed();
        }

        private float GetBaseSpeedForState(AIState state)
        {
            switch (state)
            {
                case AIState.Idle: return 0f;
                case AIState.Patrol: return _patrolSpeed;
                case AIState.Stalk: return _stalkSpeed;
                case AIState.Chase: return _chaseSpeed;
                case AIState.Flee: return _fleeSpeed;
                default: return 0f;
            }
        }

        private void UpdateAgentSpeed()
        {
            float baseSpeed = GetBaseSpeedForState(CurrentState);

            if (_currentStunTime > 0 && CurrentState != AIState.Flee)
            {
                _agent.speed = baseSpeed * _stunSlowdownMultiplier;
            }
            else
            {
                _agent.speed = baseSpeed;
            }
        }

        private void HandleLightStunTimer(bool canSeePlayer, bool isLookingAtMe)
        {
            if (CurrentState == AIState.Chase)
            {
                _currentStunTime = 0f;
                UpdateAgentSpeed();
                return;
            }

            bool isLightStrongEnough = _playerFlashlight != null && _playerFlashlight.IsLightOn && _playerFlashlight.CurrentBattery >= _minBatteryToStun;

            if (isLookingAtMe && isLightStrongEnough && canSeePlayer)
            {
                _currentStunTime += Time.deltaTime;
                UpdateAgentSpeed();
            }
            else
            {
                _currentStunTime = Mathf.Max(0, _currentStunTime - (Time.deltaTime * 2f));
                UpdateAgentSpeed();
            }
        }

        private void HandleStateMachine(float distance, bool canSee, bool canHear, bool isLookingAtMe)
        {
            if (_currentStunTime >= _timeToStun && CurrentState != AIState.Flee && CurrentState != AIState.Chase)
            {
                ChangeState(AIState.Flee);
                return;
            }

            // 🛠️ แก้บั๊ก 1: เพิ่มเงื่อนไขว่าต้องอยู่ใกล้ๆ รัศมีการได้ยิน ถึงจะโกรธเสียงปั่นไฟฉาย
            if (_playerInput.IsCranking && distance <= _crankHearingRadius && CurrentState != AIState.Chase && CurrentState != AIState.Flee)
            {
                _lastRollResult = "ได้ยินเสียงปั่นไฟฉาย! วิ่งชาร์จ!";
                ChangeState(AIState.Chase);
                return;
            }

            switch (CurrentState)
            {
                case AIState.Idle:
                    if (canHear) { ChangeState(AIState.Stalk); break; }
                    if (canSee && distance < 10f) { ChangeState(AIState.Chase); break; }

                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        int roll = Random.Range(1, 21);
                        if (roll <= _aiLevel)
                        {
                            _lastRollResult = $"ทอยได้ {roll} <= {_aiLevel}: ย่องไปดักหลัง!";
                            ChangeState(AIState.Stalk);
                        }
                        else
                        {
                            _lastRollResult = $"ทอยได้ {roll} > {_aiLevel}: เดินสุ่มปกติ";
                            ChangeState(AIState.Patrol);
                        }
                    }
                    break;

                case AIState.Patrol:
                    if (canHear) { ChangeState(AIState.Stalk); break; }
                    if (canSee && distance < 10f) { ChangeState(AIState.Chase); break; }

                    if (!_agent.pathPending && _agent.remainingDistance < 1.0f)
                    {
                        _stateTimer -= Time.deltaTime;
                        if (_stateTimer <= 0f)
                        {
                            int roll = Random.Range(1, 21);
                            if (roll <= _aiLevel)
                            {
                                _lastRollResult = $"ทอยได้ {roll} <= {_aiLevel}: เข้าโหมด Stalk!";
                                ChangeState(AIState.Stalk);
                            }
                            else
                            {
                                _lastRollResult = $"ทอยได้ {roll} > {_aiLevel}: ยืนพักเหนื่อย";
                                ChangeState(AIState.Idle);
                            }
                        }
                    }
                    break;

                case AIState.Stalk:
                    if (_impatienceGauge >= _maxImpatience)
                    {
                        _lastRollResult = "เหยื่อเผลอแล้ว! วิ่งชาร์จ!";
                        ChangeState(AIState.Chase);
                        break;
                    }

                    if (canSee && distance < 6f) { ChangeState(AIState.Chase); break; }
                    if (!canHear && !canSee && !_agent.pathPending && _agent.remainingDistance < 1f && _stateTimer <= 0f)
                    {
                        ChangeState(AIState.Idle); break;
                    }

                    if (!_agent.pathPending && _agent.remainingDistance < 1.0f)
                    {
                        _stateTimer -= Time.deltaTime;
                        if (_stateTimer <= 0f)
                        {
                            int roll = Random.Range(1, 21);
                            if (roll <= _aiLevel)
                            {
                                _lastRollResult = $"ทอยได้ {roll} <= {_aiLevel}: แอบตามต่อ!";
                                ChangeState(AIState.Stalk);
                            }
                            else
                            {
                                _lastRollResult = $"ทอยได้ {roll} > {_aiLevel}: เลิกตาม กลับไปพัก";
                                ChangeState(AIState.Idle);
                            }
                        }
                    }
                    else if (distance > 18f)
                    {
                        PickFlankPosition();
                    }
                    break;

                case AIState.Chase:
                    SafeSetDestination(_playerTransform.position);

                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        // 🛠️ แก้บั๊ก 2: ถ้าวิ่งไล่จนหมดเวลา ให้ "วิ่งหนี (Flee)" ไปซ่อนตัว แทนการยืนนิ่งๆ โง่ๆ 
                        _lastRollResult = "วิ่งไล่นานเกินไป ถอยไปตั้งหลักดีกว่า!";
                        ChangeState(AIState.Flee);
                        break;
                    }

                    if (distance > 25f) ChangeState(AIState.Idle);
                    break;

                case AIState.Flee:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        _impatienceGauge = 0f;
                        ChangeState(AIState.Idle);
                    }
                    break;
            }
        }

        private void UpdateImpatienceGauge(float distance, bool isLookingAtMe)
        {
            if (CurrentState == AIState.Stalk && distance < 12f && !isLookingAtMe)
            {
                _impatienceGauge += (12f - distance) * Time.deltaTime * 4f;
            }
            else if (isLookingAtMe)
            {
                _impatienceGauge -= Time.deltaTime * 50f;
            }
            else
            {
                _impatienceGauge -= Time.deltaTime * 5f;
            }

            _impatienceGauge = Mathf.Clamp(_impatienceGauge, 0f, _maxImpatience);
        }

        private void PickFlankPosition()
        {
            bool foundValidSpot = false;

            for (int i = 0; i < 3; i++)
            {
                Vector3 randomDir = Random.insideUnitSphere;
                randomDir -= _playerTransform.forward;
                randomDir.y = 0;

                Vector3 flankPos = _playerTransform.position + (randomDir.normalized * Random.Range(4f, 8f));

                NavMeshHit hit;
                if (NavMesh.SamplePosition(flankPos, out hit, 4f, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (_agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        SafeSetDestination(hit.position);
                        foundValidSpot = true;
                        break;
                    }
                }
            }

            if (!foundValidSpot) SafeSetDestination(_playerTransform.position);
        }

        private void PickFleePosition()
        {
            bool foundValidSpot = false;

            for (int i = 0; i < 3; i++)
            {
                Vector3 fleeDirection = -_playerCamera.forward;
                fleeDirection += Random.insideUnitSphere * 0.5f;
                fleeDirection.y = 0;

                Vector3 fleePos = transform.position + (fleeDirection.normalized * Random.Range(15f, 25f));

                NavMeshHit hit;
                if (NavMesh.SamplePosition(fleePos, out hit, 5f, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (_agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        SafeSetDestination(hit.position);
                        foundValidSpot = true;
                        break;
                    }
                }
            }

            if (!foundValidSpot) PatrolRandomly(transform.position, 15f);
        }

        private void PatrolRandomly(Vector3 origin, float radius)
        {
            bool foundValidSpot = false;
            for (int i = 0; i < 3; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * radius;
                randomDirection += origin;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (_agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        SafeSetDestination(hit.position);
                        foundValidSpot = true;
                        break;
                    }
                }
            }
            if (!foundValidSpot) SafeSetDestination(_playerTransform.position);
        }

        private void SafeSetDestination(Vector3 targetPos)
        {
            if (_agent != null && _agent.isOnNavMesh) _agent.SetDestination(targetPos);
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
        }

        private void UpdateDebugUI()
        {
            if (Managers.UIManager.Instance == null) return;

            string debugString = $"<color=yellow>Enemy State: {CurrentState}</color>\n";

            if (CurrentState == AIState.Stalk || CurrentState == AIState.Idle || CurrentState == AIState.Patrol)
            {
                debugString += $"<color=orange>[RNG] {_lastRollResult}</color>\n";
            }

            debugString += $"<color=red>Impatience: {_impatienceGauge:F0}%</color>\n";

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