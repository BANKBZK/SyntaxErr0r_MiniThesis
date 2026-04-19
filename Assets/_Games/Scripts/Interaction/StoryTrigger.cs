using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Managers;

namespace SyntaxError.Story
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoryTrigger : MonoBehaviour
    {
        [Header("Story Identification")]
        [Tooltip("ตั้งชื่อ ID ให้ไม่ซ้ำกัน เช่น 'Tut_Move', 'Story_Loop3'")]
        [SerializeField] private string _storyID = "Story_01";

        [Header("Story Settings (เนื้อเรื่องปกติ)")]
        [TextArea(3, 5)]
        [SerializeField] private string _storyText = "พิมพ์เนื้อเรื่องตรงนี้...";

        [Tooltip("ข้อความนี้จะโผล่ใน Loop ที่เท่าไหร่? (ใส่ -1 เพื่อให้ทำงานได้ทุก Loop จนกว่าจะเคยเล่นไปแล้ว)")]
        [SerializeField] private int _targetLoop = 0;

        [Tooltip("ระยะเวลาที่ข้อความค้างอยู่บนจอ (วินาที)")]
        [SerializeField] private float _displayDuration = 4f;

        [Header("Ending / Ritual Conditions (สำหรับฉากจบ)")]
        [Tooltip("เปิดใช้งานเพื่อเช็คว่าทำพิธีกรรมเสร็จหรือยัง (ข้ามเนื้อเรื่องปกติ)")]
        [SerializeField] private bool _checkRitualStatus = false;

        [Tooltip("ข้อความเมื่อ ทำพิธีเสร็จแล้ว")]
        [TextArea(2, 4)]
        [SerializeField] private string _ritualCompleteText = "พิธีกรรมเสร็จสิ้นแล้ว... ฉันออกไปจากที่นี่ได้แล้ว!";

        [Tooltip("ข้อความเมื่อ ยังทำพิธีไม่เสร็จ")]
        [TextArea(2, 4)]
        [SerializeField] private string _ritualIncompleteText = "ฉันยังออกไปไม่ได้... ต้องหาของทำพิธีให้ครบก่อน";

        [Tooltip("ยอมให้โชว์ข้อความ ยังทำไม่เสร็จ ซ้ำได้เรื่อยๆ ไหมเวลาเดินมาชนใหม่?")]
        [SerializeField] private bool _repeatIncomplete = true;

        // ตัวแปร Static จะแชร์ข้อมูลกันทุก Trigger และจำค่าไว้ตลอดการเปิดเกม
        private static HashSet<string> _playedStories = new HashSet<string>();

        // ตัวแปรกันบั๊กข้อความรัวค้างจอถ้ายืนแช่
        private float _lastTriggerTime = 0f;

        private void Start()
        {
            // บังคับให้เป็น Trigger อัตโนมัติ
            GetComponent<BoxCollider>().isTrigger = true;
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
            // เช็คว่ากด Start Game เพื่อเริ่มเล่นจริงๆ หรือยัง
            if (UIManager.Instance == null || !UIManager.Instance.IsGameStarted) return;
            if (!other.CompareTag("Player")) return;

            // เช็คเลข Loop
            bool isCorrectLoop = (_targetLoop == -1) ||
                (GameManager.Instance != null && GameManager.Instance.CurrentLoop == _targetLoop);

            if (!isCorrectLoop) return;

            // แยกระบบตามที่ติ๊กตั้งค่าไว้
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
            if (_playedStories.Contains(_storyID)) return;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowStoryText(_storyText, _displayDuration);
                _playedStories.Add(_storyID);
                Debug.Log($"[StorySystem] เล่นเนื้อเรื่องแล้ว: {_storyID}");
            }
        }

        private void HandleRitualStory()
        {
            if (GameManager.Instance == null) return;

            bool isComplete = GameManager.Instance.IsRitualComplete;

            // สร้าง ID แยกสำหรับตอนเสร็จและยังไม่เสร็จ (กันมันจำปนกัน)
            string currentID = isComplete ? _storyID + "_Complete" : _storyID + "_Incomplete";

            // ถ้าเป็นข้อความที่เคยโชว์แล้ว และไม่อนุญาตให้โชว์ซ้ำ ให้ข้ามไป
            if (_playedStories.Contains(currentID)) return;

            // ป้องกันบั๊กยืนแช่ใน Trigger แล้วคิวข้อความเด้งรัวๆ 
            if (Time.time < _lastTriggerTime + _displayDuration + 1f) return;

            if (UIManager.Instance != null)
            {
                string textToShow = isComplete ? _ritualCompleteText : _ritualIncompleteText;
                UIManager.Instance.ShowStoryText(textToShow, _displayDuration);
                _lastTriggerTime = Time.time;

                // ถ้าทำเสร็จแล้วให้จำไว้เลย จะได้ไม่โชว์ขึ้นมาซ้ำอีก
                if (isComplete)
                {
                    _playedStories.Add(currentID);
                }
                else
                {
                    // ถ้ายอมให้โชว์ Incomplete แค่ครั้งเดียว ก็บันทึกลงไป
                    if (!_repeatIncomplete)
                    {
                        _playedStories.Add(currentID);
                    }
                }

                Debug.Log($"[StorySystem] เล่นเนื้อเรื่องฉากจบ: {currentID}");
            }
        }

        public static void ResetAllStoryMemory()
        {
            _playedStories.Clear();
            Debug.Log("[StorySystem] ล้างความทรงจำเนื้อเรื่องทั้งหมดแล้ว");
        }
    }
}