using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Interfaces;
using SyntaxError.Managers;

namespace SyntaxError.Events
{
    public class StoryObject : MonoBehaviour, IResettable
    {
        [Header("Story Object ID")]
        [SerializeField] private string _storyObjectID = "Obj_01";

        [Header("Condition")]
        [SerializeField] private int _targetLoop = 3;
        [SerializeField] private bool _onlyThisLoop = true;
        [SerializeField] private bool _showOnlyOncePerGame = true;

        [Header("Object To Control")]
        [SerializeField] private GameObject _contentObject;

        private static HashSet<string> _triggeredObjects = new HashSet<string>();

        private void Start()
        {
            if (_contentObject != null) _contentObject.SetActive(false);
            if (LoopManager.Instance != null) LoopManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null) LoopManager.Instance.Unregister(this);
        }

        public void OnLoopReset(int currentLoop)
        {
            if (_contentObject == null) return;

            // เช็คความจำ: ถ้าตั้งให้โผล่ครั้งเดียว (OncePerGame) และเคยโผล่ไปแล้ว ให้ปิดทิ้งเลย
            if (_showOnlyOncePerGame && _triggeredObjects.Contains(_storyObjectID))
            {
                _contentObject.SetActive(false);
                return;
            }

            bool shouldAppear = false;

            if (_onlyThisLoop) shouldAppear = (currentLoop == _targetLoop);
            else shouldAppear = (currentLoop >= _targetLoop);

            _contentObject.SetActive(shouldAppear);

            // ถ้าโชว์แล้วให้จดลงหน่วยความจำ
            if (shouldAppear && _showOnlyOncePerGame)
            {
                _triggeredObjects.Add(_storyObjectID);
                Debug.Log($"[StoryObject] {gameObject.name} โผล่แล้ว และจะถูกจำไว้ไม่ให้โผล่ซ้ำ!");
            }
        }

        // ล้างความจำ (จะถูกเรียกตอนกลับหน้า Main Menu เท่านั้น)
        public static void ResetAllObjectMemory()
        {
            _triggeredObjects.Clear();
        }
    }
}