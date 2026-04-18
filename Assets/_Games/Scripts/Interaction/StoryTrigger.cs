using UnityEngine;
using System.Collections.Generic;
using SyntaxError.Managers;

namespace SyntaxError.Story
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoryTrigger : MonoBehaviour
    {
        [Header("Story Identification")]
        [SerializeField] private string _storyID = "Story_01";

        [Header("Story Settings")]
        [TextArea(3, 5)]
        [SerializeField] private string _storyText = "พิมพ์เนื้อเรื่องตรงนี้...";

        [Tooltip("ข้อความนี้จะโผล่ใน Loop ที่เท่าไหร่? (ใส่ -1 เพื่อให้ทำงานได้ทุก Loop จนกว่าจะเคยเล่นไปแล้ว)")]
        [SerializeField] private int _targetLoop = 0;

        [SerializeField] private float _displayDuration = 4f;

        private static HashSet<string> _playedStories = new HashSet<string>();

        private void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_playedStories.Contains(_storyID)) return;

            if (other.CompareTag("Player"))
            {
                // [แก้ไข] เช็คว่าถ้าระบุ -1 คือยอมให้ทำงานได้ทุกลูป
                bool isCorrectLoop = (_targetLoop == -1) ||
                    (GameManager.Instance != null && GameManager.Instance.CurrentLoop == _targetLoop);

                if (isCorrectLoop)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowStoryText(_storyText, _displayDuration);
                        _playedStories.Add(_storyID);
                        Debug.Log($"[StorySystem] เล่นเนื้อเรื่องแล้ว: {_storyID}");
                    }
                }
            }
        }

        public static void ResetAllStoryMemory()
        {
            _playedStories.Clear();
            Debug.Log("[StorySystem] ล้างความทรงจำเนื้อเรื่องทั้งหมดแล้ว");
        }
    }
}