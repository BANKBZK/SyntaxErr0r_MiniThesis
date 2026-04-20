using UnityEngine;
using System.Collections;
using SyntaxError.Interfaces;
using SyntaxError.Managers;

namespace SyntaxError.Interaction
{
    public class DoorController : MonoBehaviour, IInteractable, IResettable
    {
        [Header("Settings")]
        [SerializeField] private Transform _doorModel;
        [SerializeField] private Vector3 _moveOffset = new Vector3(1f, 0, 0);

        private Vector3 _closedPos;
        private Vector3 _openPos;
        private bool _isOpen = false;
        private Coroutine _animRoutine;
        private bool _isInitialized = false;

        private void Awake()
        {
            InitializeDoor();
        }

        private void InitializeDoor()
        {
            if (!_isInitialized)
            {
                if (_doorModel == null) _doorModel = transform;
                _closedPos = _doorModel.localPosition;
                _openPos = _closedPos + _moveOffset;
                _isInitialized = true;
            }
        }

        private void Start()
        {
            if (LoopManager.Instance != null) LoopManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null) LoopManager.Instance.Unregister(this);
        }

        public void Interact()
        {
            InitializeDoor(); // กันเหนียว
            _isOpen = !_isOpen;
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(MoveDoor(_isOpen ? _openPos : _closedPos));
        }

        public string GetPromptText()
        {
            return _isOpen ? "Close Door" : "Open Door";
        }

        public void OnLoopReset(int currentLoop)
        {
            InitializeDoor(); // 🛠️ บังคับเช็คตำแหน่งก่อนรีเซ็ต
            if (_animRoutine != null) StopCoroutine(_animRoutine);

            _isOpen = false;
            _doorModel.localPosition = _closedPos;
        }

        private IEnumerator MoveDoor(Vector3 target)
        {
            Vector3 start = _doorModel.localPosition;
            float time = 0;
            while (time < 1f)
            {
                time += Time.deltaTime * 2f;
                _doorModel.localPosition = Vector3.Lerp(start, target, time);
                yield return null;
            }
            _doorModel.localPosition = target;
        }
    }
}