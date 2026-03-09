using UnityEngine;
using System.Collections.Generic; // ต้องมีเพื่อใช้ HashSet
using SyntaxError.Managers;

namespace SyntaxError.Story
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoryTrigger : MonoBehaviour
    {
        [Header("Story Identification")]
        [Tooltip("ตั้งชื่อ ID ให้ไม่ซ้ำกัน เช่น 'Story_Intro', 'Story_Loop3'")]
        [SerializeField] private string _storyID = "Story_01";

        [Header("Story Settings")]
        [TextArea(3, 5)]
        [SerializeField] private string _storyText = "พิมพ์เนื้อเรื่องตรงนี้...";

        [Tooltip("ข้อความนี้จะโผล่ใน Loop ที่เท่าไหร่?")]
        [SerializeField] private int _targetLoop = 0;

        [Tooltip("ระยะเวลาที่ข้อความค้างอยู่บนจอ (วินาที)")]
        [SerializeField] private float _displayDuration = 4f;

        // ---------------------------------------------------------
        // ตัวแปร Static จะแชร์ข้อมูลกันทุก Trigger และจำค่าไว้ตลอดการเปิดเกม
        // (ตายกลับ Loop 0 โค้ดก็ยังจำได้ว่าเคยเล่น ID ไหนไปแล้ว)
        // ---------------------------------------------------------
        private static HashSet<string> _playedStories = new HashSet<string>();

        private void Start()
        {
            // บังคับให้เป็น Trigger อัตโนมัติ
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 1. ตรวจสอบความทรงจำ: ถ้า ID นี้เคยถูกเล่นไปแล้ว ให้ข้ามการทำงานทันที
            if (_playedStories.Contains(_storyID)) return;

            // 2. เช็คว่าเป็นผู้เล่นเดินมาชน
            if (other.CompareTag("Player"))
            {
                // 3. เช็คว่าเลข Loop ตรงกับที่ตั้งไว้ไหม
                if (GameManager.Instance != null && GameManager.Instance.CurrentLoop == _targetLoop)
                {
                    if (UIManager.Instance != null)
                    {
                        // สั่งโชว์ข้อความ
                        UIManager.Instance.ShowStoryText(_storyText, _displayDuration);

                        // 4. บันทึกความทรงจำ: เพิ่ม ID นี้ลงในลิสต์ว่า "เล่นไปแล้วนะ"
                        _playedStories.Add(_storyID);

                        Debug.Log($"[StorySystem] เล่นเนื้อเรื่องแล้ว: {_storyID}");
                    }
                }
            }
        }

        // ---------------------------------------------------------
        // ฟังก์ชันพิเศษ: เอาไว้ล้างความทรงจำทั้งหมด 
        // (เผื่อใช้ตอนกดปุ่ม "New Game" ในหน้า Main Menu)
        // ---------------------------------------------------------
        public static void ResetAllStoryMemory()
        {
            _playedStories.Clear();
            Debug.Log("[StorySystem] ล้างความทรงจำเนื้อเรื่องทั้งหมดแล้ว");
        }
    }
}