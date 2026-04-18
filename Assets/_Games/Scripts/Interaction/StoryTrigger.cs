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

        [Header("Story Settings")]
        [TextArea(3, 5)]
        [SerializeField] private string _storyText = "พิมพ์เนื้อเรื่องตรงนี้...";

        [Tooltip("ข้อความนี้จะโผล่ใน Loop ที่เท่าไหร่? (ใส่ -1 เพื่อให้ทำงานได้ทุก Loop จนกว่าจะเคยเล่นไปแล้ว)")]
        [SerializeField] private int _targetLoop = 0;

        [Tooltip("ระยะเวลาที่ข้อความค้างอยู่บนจอ (วินาที)")]
        [SerializeField] private float _displayDuration = 4f;

        // ตัวแปร Static จะแชร์ข้อมูลกันทุก Trigger และจำค่าไว้ตลอดการเปิดเกม
        private static HashSet<string> _playedStories = new HashSet<string>();

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
            // รองรับระบบ Seamless Menu เผื่อผู้เล่นเกิดมาทับจุด Trigger พอดีตอนที่ยังไม่ได้กด Start Game
            TriggerStory(other);
        }

        private void TriggerStory(Collider other)
        {
            // 1. เช็คว่ากด Start Game เพื่อเริ่มเล่นจริงๆ หรือยัง (ถ้ายังอยู่หน้า Menu ให้ข้ามไปก่อน)
            if (UIManager.Instance == null || !UIManager.Instance.IsGameStarted) return;

            // 2. ตรวจสอบความทรงจำ: ถ้า ID นี้เคยถูกเล่นไปแล้ว ให้ข้ามการทำงานทันที
            if (_playedStories.Contains(_storyID)) return;

            // 3. เช็คว่าเป็นผู้เล่นเดินมาชน
            if (other.CompareTag("Player"))
            {
                // 4. เช็คว่าเลข Loop ตรงกับที่ตั้งไว้ไหม (ถ้าเป็น -1 คือยอมให้โผล่ทุกลูป)
                bool isCorrectLoop = (_targetLoop == -1) ||
                    (GameManager.Instance != null && GameManager.Instance.CurrentLoop == _targetLoop);

                if (isCorrectLoop)
                {
                    if (UIManager.Instance != null)
                    {
                        // สั่งโชว์ข้อความ (ระบบคิวใน UIManager จะจัดการไม่ให้มันทับกันเอง)
                        UIManager.Instance.ShowStoryText(_storyText, _displayDuration);

                        // บันทึกความทรงจำ: เพิ่ม ID นี้ลงในลิสต์ว่า "เล่นไปแล้วนะ"
                        _playedStories.Add(_storyID);

                        Debug.Log($"[StorySystem] เล่นเนื้อเรื่องแล้ว: {_storyID}");
                    }
                }
            }
        }

        // ฟังก์ชันพิเศษ: เอาไว้ล้างความทรงจำทั้งหมด (เผื่อใช้ตอนกดปุ่ม "New Game" หรือ Hard Reset)
        public static void ResetAllStoryMemory()
        {
            _playedStories.Clear();
            Debug.Log("[StorySystem] ล้างความทรงจำเนื้อเรื่องทั้งหมดแล้ว");
        }
    }
}