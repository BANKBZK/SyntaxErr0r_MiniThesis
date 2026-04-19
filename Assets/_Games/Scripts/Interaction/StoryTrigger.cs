using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Managers;
using SyntaxError.Interfaces; // เพิ่ม IResettable เพื่อให้รับรู้ตอนวาร์ปเปลี่ยนลูป

namespace SyntaxError.Story
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoryTrigger : MonoBehaviour, IResettable
    {
        public enum StoryFrequency
        {
            OncePerGame,   // โชว์ครั้งเดียวตลอดทั้งเกม (สำหรับเนื้อเรื่องหลัก)
            OncePerLoop,   // โชว์ลูปละ 1 ครั้ง (พอวาร์ปขึ้นลูปใหม่ จะกลับมาเดินเหยียบซ้ำได้)
            Unlimited      // โชว์ทุกครั้งที่เดินมาชน! (มีระบบดีเลย์กันข้อความเด้งรัว)
        }

        [Header("Story Identification")]
        [Tooltip("ตั้งชื่อ ID ให้ไม่ซ้ำกัน เช่น 'Tut_Move', 'Door_Locked'")]
        [SerializeField] private string _storyID = "Story_01";

        [Header("Story Settings (เนื้อเรื่องปกติ)")]
        [TextArea(3, 5)]
        [SerializeField] private string _storyText = "พิมพ์เนื้อเรื่องตรงนี้...";

        [Tooltip("ความถี่ในการแสดงข้อความนี้")]
        [SerializeField] private StoryFrequency _frequency = StoryFrequency.OncePerGame;

        [Tooltip("ระยะเวลาที่ข้อความค้างอยู่บนจอ (วินาที)")]
        [SerializeField] private float _displayDuration = 4f;

        [Header("Loop Conditions (เงื่อนไขการโชว์)")]
        [Tooltip("ให้ข้อความนี้โชว์ในทุกๆ ลูปเลยหรือไม่? (ติ๊กถูก = โชว์ทุกลูป)")]
        [SerializeField] private bool _triggerInAllLoops = true;

        [Tooltip("ถ้าไม่ได้ติ๊กทุกลูป จะให้โชว์เฉพาะที่ Loop ไหน?")]
        [SerializeField] private int _targetLoop = 0;

        [Header("Ending / Ritual Conditions (เช็คฉากจบ)")]
        [Tooltip("เปิดใช้งานเพื่อเช็คว่าทำพิธีเสร็จหรือยัง (จะข้ามข้อความปกติไปเลย)")]
        [SerializeField] private bool _checkRitualStatus = false;

        [TextArea(2, 4)]
        [SerializeField] private string _ritualCompleteText = "พิธีกรรมเสร็จสิ้นแล้ว... ออกไปได้แล้ว!";

        [TextArea(2, 4)]
        [SerializeField] private string _ritualIncompleteText = "ต้องหาของทำพิธีให้ครบก่อน...";

        // ระบบความจำ
        private static HashSet<string> _playedStories = new HashSet<string>();
        private bool _hasTriggeredThisLoop = false;
        private float _lastTriggerTime = 0f;

        private void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;

            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.Register(this);
            }
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.Unregister(this);
            }
        }

        // รีเซ็ตสถานะเมื่อผู้เล่นเปลี่ยนลูป
        public void OnLoopReset(int currentLoop)
        {
            _hasTriggeredThisLoop = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerStory(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TriggerStory(other);
        }

        private void TriggerStory(Collider other)
        {
            if (UIManager.Instance == null || !UIManager.Instance.IsGameStarted) return;
            if (!other.CompareTag("Player")) return;

            // 1. เช็คดีเลย์ (ป้องกันผู้เล่นยืนแช่แล้วข้อความเด้งรัวๆ ทับกัน)
            if (Time.time < _lastTriggerTime + _displayDuration + 0.5f) return;

            // 2. เช็คเงื่อนไข Loop ว่าล็อกไว้ไหม
            if (!_triggerInAllLoops && GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentLoop != _targetLoop) return;
            }

            // 3. ไปแสดงผลตามโหมดที่เลือก
            if (_checkRitualStatus)
            {
                HandleRitualStory();
            }
            else
            {
                HandleNormalStory();
            }
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
            // ถ้าเล่นครั้งเดียวเกม แล้วเคยเล่นไปแล้ว = ห้ามเล่นอีก
            if (_frequency == StoryFrequency.OncePerGame && _playedStories.Contains(id)) return false;

            // ถ้าเล่นลูปละครั้ง แล้วลูปนี้เล่นไปแล้ว = ห้ามเล่นอีก
            if (_frequency == StoryFrequency.OncePerLoop && _hasTriggeredThisLoop) return false;

            return true; // โหมด Unlimited หรือยังไม่เคยเล่นเลย
        }

        private void RecordStoryPlayed(string id)
        {
            _lastTriggerTime = Time.time;

            if (_frequency == StoryFrequency.OncePerGame)
                _playedStories.Add(id);

            if (_frequency == StoryFrequency.OncePerLoop)
                _hasTriggeredThisLoop = true;
        }

        public static void ResetAllStoryMemory()
        {
            _playedStories.Clear();
        }
    }
}