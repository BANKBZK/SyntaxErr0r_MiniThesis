using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Managers;
using SyntaxError.Interfaces;

namespace SyntaxError.Story
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoryTrigger : MonoBehaviour, IResettable
    {
        public enum StoryFrequency
        {
            OncePerGame,
            OncePerLoop,
            Unlimited
        }

        [Header("Story Identification")]
        [SerializeField] private string _storyID = "Story_01";

        [Header("Story Settings")]
        [TextArea(3, 5)]
        [SerializeField] private string _storyText = "พิมพ์เนื้อเรื่องตรงนี้...";
        [SerializeField] private StoryFrequency _frequency = StoryFrequency.OncePerGame;
        [SerializeField] private float _displayDuration = 4f;

        [Header("Loop Conditions")]
        [SerializeField] private bool _triggerInAllLoops = true;
        [SerializeField] private int _targetLoop = 0;

        [Header("Ending / Ritual Conditions")]
        [SerializeField] private bool _checkRitualStatus = false;
        [TextArea(2, 4)]
        [SerializeField] private string _ritualCompleteText = "พิธีกรรมเสร็จสิ้นแล้ว... ออกไปได้แล้ว!";
        [TextArea(2, 4)]
        [SerializeField] private string _ritualIncompleteText = "ต้องหาของทำพิธีให้ครบก่อน...";

        private static HashSet<string> _playedStories = new HashSet<string>();
        private bool _hasTriggeredThisLoop = false;
        private float _lastTriggerTime = 0f;

        // 🛠️ ตัวแปรใหม่: จำว่าผู้เล่นยืนอยู่ในกล่องหรือเปล่า
        private bool _isPlayerInside = false;

        private void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            if (LoopManager.Instance != null) LoopManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null) LoopManager.Instance.Unregister(this);
        }

        public void OnLoopReset(int currentLoop)
        {
            _hasTriggeredThisLoop = false;
        }

        // 🛠️ แก้ไข: แค่จำว่าผู้เล่นเดินเข้ามาแล้ว
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) _isPlayerInside = true;
        }

        // 🛠️ แก้ไข: จำว่าผู้เล่นเดินออกไปแล้ว
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) _isPlayerInside = false;
        }

        // 🛠️ แก้ไข: ให้ Update คอยเช็คตลอดเวลา ถ้าเกมเริ่มแล้วและคนยังอยู่ข้างใน ก็โชว์เลย!
        private void Update()
        {
            if (_isPlayerInside)
            {
                TriggerStory();
            }
        }

        private void TriggerStory()
        {
            // ถ้าเกมยังไม่เริ่ม ให้รอก่อน (Update จะวนมาเช็คใหม่เรื่อยๆ)
            if (UIManager.Instance == null || !UIManager.Instance.IsGameStarted) return;

            if (Time.time < _lastTriggerTime + _displayDuration + 0.5f) return;

            if (!_triggerInAllLoops && GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentLoop != _targetLoop) return;
            }

            if (_checkRitualStatus) HandleRitualStory();
            else HandleNormalStory();
        }

        private void HandleNormalStory()
        {
            if (!CanPlayStory(_storyID)) return;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowStoryText(_storyText, _displayDuration);
                RecordStoryPlayed(_storyID);
            }
        }

        private void HandleRitualStory()
        {
            if (GameManager.Instance == null) return;

            bool isComplete = GameManager.Instance.IsRitualComplete;
            string currentID = isComplete ? _storyID + "_Complete" : _storyID + "_Incomplete";

            if (!CanPlayStory(currentID)) return;

            if (UIManager.Instance != null)
            {
                string textToShow = isComplete ? _ritualCompleteText : _ritualIncompleteText;
                UIManager.Instance.ShowStoryText(textToShow, _displayDuration);
                RecordStoryPlayed(currentID);
            }
        }

        private bool CanPlayStory(string id)
        {
            if (_frequency == StoryFrequency.OncePerGame && _playedStories.Contains(id)) return false;
            if (_frequency == StoryFrequency.OncePerLoop && _hasTriggeredThisLoop) return false;
            return true;
        }

        private void RecordStoryPlayed(string id)
        {
            _lastTriggerTime = Time.time;
            if (_frequency == StoryFrequency.OncePerGame) _playedStories.Add(id);
            if (_frequency == StoryFrequency.OncePerLoop) _hasTriggeredThisLoop = true;
        }

        public static void ResetAllStoryMemory()
        {
            _playedStories.Clear();
        }
    }
}