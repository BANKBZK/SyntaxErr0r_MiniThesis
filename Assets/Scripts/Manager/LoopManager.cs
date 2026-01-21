using UnityEngine;
using System.Collections;
using SyntaxError.Player; // อ้างอิง PlayerController

namespace SyntaxError.Managers
{
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private CharacterController _characterController; // ต้องปิด CharacterController ก่อนวาร์ป ไม่งั้นบั๊ก
        [SerializeField] private Transform _startPoint; // จุดที่จะให้วาร์ปกลับไป
        [SerializeField] private CanvasGroup _blackScreenFader; // UI จอดำ

        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 1.0f;

        private bool _isTeleporting = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // เริ่มเกมมาให้จอมืดแล้วค่อยๆ สว่าง (Intro)
            StartCoroutine(FadeRoutine(1, 0));
        }

        public void CompleteLoop()
        {
            if (_isTeleporting) return; // ป้องกันการชนรัวๆ
            StartCoroutine(TeleportSequence());
        }

        private IEnumerator TeleportSequence()
        {
            _isTeleporting = true;

            // 1. Fade Out (จอมืดลง)
            yield return StartCoroutine(FadeRoutine(0, 1));

            // 2. Logic การคำนวณ Loop (เดี๋ยวมาใส่ Logic เช็คประตูถูก/ผิด ตรงนี้ในอนาคต)
            GameManager.Instance.IncrementLoop();

            // 3. Teleport Player
            // ต้องปิด Controller ก่อนย้ายตำแหน่ง (เป็นข้อจำกัดของ Unity CharacterController)
            _characterController.enabled = false;
            _playerTransform.position = _startPoint.position;
            _playerTransform.rotation = _startPoint.rotation;
            yield return null; // รอ 1 เฟรมให้ Physics อัปเดต
            _characterController.enabled = true;

            // 4. (TODO) จุดนี้จะสั่งสลับของ Anomaly (Object Swapping)
            // AnomalyManager.Instance.Randomize(); 

            // 5. Fade In (จอสว่างขึ้น)
            yield return StartCoroutine(FadeRoutine(1, 0));

            _isTeleporting = false;
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
        {
            float timer = 0;
            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                _blackScreenFader.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / _fadeDuration);
                yield return null;
            }
            _blackScreenFader.alpha = endAlpha;
        }
    }
}