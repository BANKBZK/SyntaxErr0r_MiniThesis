using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Interfaces;
using SyntaxError.Managers;

namespace SyntaxError.Events
{
    public class StoryObject : MonoBehaviour, IResettable
    {
        [Header("Story Object ID")]
        [Tooltip("ตั้งชื่อ ID ให้ไม่ซ้ำกัน เพื่อให้เกมจำได้ (เช่น 'GhostPiano_01')")]
        [SerializeField] private string _storyObjectID = "Obj_01";

        [Header("Condition")]
        [Tooltip("จะให้โผล่มาใน Loop ที่เท่าไหร่?")]
        [SerializeField] private int _targetLoop = 3;

        [Tooltip("ถ้าติ๊กถูก จะโผล่แค่ Loop นี้ Loop เดียวแล้วหายไปเลย")]
        [SerializeField] private bool _onlyThisLoop = true;

        [Tooltip("ถ้าติ๊กถูก โผล่มาแล้ว ครั้งหน้าถ้าตายกลับมา Loop 0 จะไม่โผล่ซ้ำอีก")]
        [SerializeField] private bool _showOnlyOncePerGame = true;

        [Header("Object To Control")]
        [SerializeField] private GameObject _contentObject;

        // หน่วยความจำที่จดจำว่า Object ID ไหนเคยโผล่มาแล้วบ้าง
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

            // เช็คความทรงจำ: ถ้าตั้งให้โผล่ครั้งเดียว และเคยโผล่ไปแล้ว ให้ปิดทิ้งถาวรเลย
            if (_showOnlyOncePerGame && _triggeredObjects.Contains(_storyObjectID))
            {
                _contentObject.SetActive(false);
                return;
            }

            bool shouldAppear = false;

            if (_onlyThisLoop)
            {
                shouldAppear = (currentLoop == _targetLoop);
            }
            else
            {
                shouldAppear = (currentLoop >= _targetLoop);
            }

            _contentObject.SetActive(shouldAppear);

            // ถ้าอีเวนต์นี้ถูกเรียกขึ้นมาแล้ว ให้จดลงหน่วยความจำ
            if (shouldAppear)
            {
                if (_showOnlyOncePerGame)
                {
                    _triggeredObjects.Add(_storyObjectID);
                }
                Debug.Log($"Story Event Triggered: {gameObject.name} in Loop {currentLoop}");
            }
        }

        // เอาไว้ล้างความจำตอนผู้เล่นกดกลับ Main Menu
        public static void ResetAllObjectMemory()
        {
            _triggeredObjects.Clear();
        }
    }
}