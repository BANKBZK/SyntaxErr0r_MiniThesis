using UnityEngine;
using UnityEngine.AI;
using SyntaxError.Player;
using SyntaxError.Inputs;

namespace SyntaxError.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ChaserAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private InputManager _playerInput;
        [SerializeField] private FlashlightController _playerFlashlight;
        private NavMeshAgent _agent;

        [Header("Vision Settings (การมองเห็น)")]
        [Tooltip("ระยะมองเห็นในความมืด (บอดมาก)")]
        [SerializeField] private float _sightRadiusDark = 4f;
        [Tooltip("ระยะมองเห็นเมื่อผู้เล่นเปิดไฟฉาย (เห็นชัดมาก)")]
        [SerializeField] private float _sightRadiusLight = 25f;
        [Tooltip("เลเยอร์กำแพง/ต้นไม้ เพื่อไม่ให้ผีมองทะลุกำแพง")]
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Hearing Settings (การได้ยิน)")]
        [Tooltip("ระยะได้ยินเสียงปั่นไฟฉาย Dynamo (ดังมาก)")]
        [SerializeField] private float _crankHearingRadius = 20f;
        [Tooltip("ระยะได้ยินเสียงวิ่ง")]
        [SerializeField] private float _sprintHearingRadius = 15f;
        [Tooltip("ระยะได้ยินเสียงเดินปกติ")]
        [SerializeField] private float _walkHearingRadius = 4f;

        [Header("Movement Speeds")]
        [SerializeField] private float _patrolSpeed = 1.5f;
        [SerializeField] private float _chaseSpeed = 4.5f;

        private Vector3 _investigateTarget;
        private bool _isChasing = false;

        [Header("Kill Settings")]
        [SerializeField] private float _killDistance = 1.5f; // ระยะงับหัว

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_playerTransform == null) _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            if (_playerInput == null) _playerInput = FindFirstObjectByType<InputManager>();
            if (_playerFlashlight == null) _playerFlashlight = FindFirstObjectByType<FlashlightController>();
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            // 1. เช็คว่าผี "เห็น" หรือ "ได้ยิน" ผู้เล่นไหม?
            bool canSeePlayer = CheckLineOfSight(distanceToPlayer);
            bool canHearPlayer = CheckHearing(distanceToPlayer);

            // 2. ตัดสินใจการกระทำ
            if (canSeePlayer || canHearPlayer)
            {
                // โดนเจอตัว! วิ่งเข้าใส่ทันที
                _isChasing = true;
                _agent.speed = _chaseSpeed;
                _agent.SetDestination(_playerTransform.position);
                _investigateTarget = _playerTransform.position; // จำจุดล่าสุดไว้เผื่อผู้เล่นหนีพ้น
            }
            else
            {
                // ไม่เห็นและไม่ได้ยิน
                if (_isChasing)
                {
                    // เปลี่ยนจากวิ่งไล่ เป็นเดินย่องไปสำรวจจุดล่าสุด
                    _isChasing = false;
                    _agent.speed = _patrolSpeed;
                    _agent.SetDestination(_investigateTarget);
                }
                else
                {
                    // ถ้าเดินมาถึงจุดที่สงสัยแล้วไม่เจอใคร ให้เดินสุ่ม (Patrol) ต่อ
                    if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                    {
                        PatrolRandomly();
                    }
                }
            }

            // 3. เช็คระยะตาย (Game Over)
            if (distanceToPlayer <= _killDistance)
            {
                CatchPlayer();
            }
        }

        private bool CheckLineOfSight(float distanceToPlayer)
        {
            // ระยะมองเห็นจะไกลขึ้นมาก ถ้าผู้เล่นเปิดไฟฉาย
            float currentSightRadius = (_playerFlashlight != null && _playerFlashlight.IsLightOn)
                                        ? _sightRadiusLight
                                        : _sightRadiusDark;

            if (distanceToPlayer > currentSightRadius) return false;

            // เช็คว่ามีกำแพงหรือต้นไม้บังไหม (Raycast)
            Vector3 dirToPlayer = (_playerTransform.position - transform.position).normalized;
            // ยิง Raycast จากระดับอกผี ไปหาระดับอกผู้เล่น
            if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, _obstacleMask))
            {
                return false; // โดนกำแพง/ต้นไม้บัง
            }

            return true; // เห็นเต็มๆ!
        }

        private bool CheckHearing(float distanceToPlayer)
        {
            if (_playerInput == null) return false;

            float currentNoiseLevel = 0f;
            bool isMoving = _playerInput.MoveInput.magnitude > 0.1f;

            // เสียงปั่นไฟฉายดังมากก
            if (_playerInput.IsCranking) currentNoiseLevel = Mathf.Max(currentNoiseLevel, _crankHearingRadius);
            // เสียงวิ่ง
            if (isMoving && _playerInput.IsSprinting) currentNoiseLevel = Mathf.Max(currentNoiseLevel, _sprintHearingRadius);
            // เสียงเดิน
            else if (isMoving && !_playerInput.IsSprinting) currentNoiseLevel = Mathf.Max(currentNoiseLevel, _walkHearingRadius);

            return distanceToPlayer <= currentNoiseLevel;
        }

        private void PatrolRandomly()
        {
            Vector3 randomDirection = Random.insideUnitSphere * 15f;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 15f, NavMesh.AllAreas))
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
    }
}