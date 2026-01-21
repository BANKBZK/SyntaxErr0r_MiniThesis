using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SyntaxError.Interfaces; // ต้องมี Interface IResettable
using SyntaxError.Player;     // อ้างอิง PlayerController (ถ้ามี)

namespace SyntaxError.Managers
{
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _startPoint;
        [SerializeField] private CanvasGroup _fadeUI;

        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 1.5f;

        // ลิสต์เก็บของที่ต้อง Reset (ประตู, หน้าต่าง ฯลฯ)
        private List<IResettable> _resettableObjects = new List<IResettable>();
        private bool _isTeleporting = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        // --- 1. แก้ปัญหาจอดำไม่ทำงานตอนเริ่มเกม ---
        private void Start()
        {
            // บังคับค่าเริ่มต้นทันทีที่เริ่มเกม
            if (_fadeUI != null)
            {
                _fadeUI.alpha = 1f; // จอมืดตึ๊บ
                _fadeUI.blocksRaycasts = false; // กันไว้ก่อน
            }

            // สั่ง Fade In (สว่างขึ้น)
            StartCoroutine(FadeRoutine(1f, 0f));
        }

        // --- ระบบลงทะเบียน (Registration) ---
        public void Register(IResettable obj)
        {
            if (!_resettableObjects.Contains(obj)) _resettableObjects.Add(obj);
        }

        public void Unregister(IResettable obj)
        {
            if (_resettableObjects.Contains(obj)) _resettableObjects.Remove(obj);
        }

        // --- คำสั่งจบ Loop (เรียกจาก Trigger) ---
        public void CompleteLoop()
        {
            if (_isTeleporting) return; // ป้องกันการชนซ้ำ
            StartCoroutine(TeleportSequence());
        }

        // --- Sequence การวาร์ป ---
        private IEnumerator TeleportSequence()
        {
            _isTeleporting = true;

            // 1. Fade Out (มืดลง)
            yield return StartCoroutine(FadeRoutine(0f, 1f));

            // --- เริ่มโซน Logic (ใส่ Try-Catch กันเกมค้าง) ---
            try
            {
                // A. อัปเดต Loop Count
                if (GameManager.Instance != null) GameManager.Instance.NextLoop();
                int currentLoop = GameManager.Instance != null ? GameManager.Instance.CurrentLoop : 0;
                Debug.Log($"--- STARTING LOOP {currentLoop} ---");

                // B. ย้าย Player (ปิด Controller ชั่วคราว)
                if (_characterController != null) _characterController.enabled = false;

                if (_playerTransform != null && _startPoint != null)
                {
                    _playerTransform.position = _startPoint.position;
                    _playerTransform.rotation = _startPoint.rotation;
                    Physics.SyncTransforms(); // บังคับ Physics อัปเดตทันที (สำคัญ!)
                }

                // C. สั่ง Reset ของในฉาก (ประตู)
                for (int i = _resettableObjects.Count - 1; i >= 0; i--)
                {
                    if (_resettableObjects[i] == null)
                        _resettableObjects.RemoveAt(i);
                    else
                        _resettableObjects[i].OnLoopReset(currentLoop);
                }

                // D. จัดการ Anomaly (แก้ปัญหาซ้อนทับ)
                if (AnomalyManager.Instance != null)
                {
                    // D1. บังคับ Reset ของเก่าทิ้งให้หมดก่อน (Double Check)
                    // (เพิ่มฟังก์ชันนี้ใน AnomalyManager ถ้ายังไม่มี: public void ForceResetAll() )
                    // AnomalyManager.Instance.ForceResetAll(); 

                    // D2. สั่งประมวลผลรอบใหม่
                    AnomalyManager.Instance.ProcessLoop(currentLoop);
                }
                else
                {
                    Debug.LogWarning("AnomalyManager missing! Spawning Normal Loop.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in Loop Sequence: {e.Message}\n{e.StackTrace}");
            }

            // รอ 1 เฟรมให้ทุกอย่างเข้าที่
            yield return null;

            // เปิดการควบคุมคืน
            if (_characterController != null) _characterController.enabled = true;

            // 2. Fade In (สว่างขึ้น)
            yield return StartCoroutine(FadeRoutine(1f, 0f));

            _isTeleporting = false;
        }

        // --- ระบบ Fade ที่เสถียรขึ้น ---
        private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
        {
            float timer = 0f;

            // ตั้งค่าเริ่มต้นให้ชัวร์
            if (_fadeUI != null) _fadeUI.alpha = startAlpha;

            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / _fadeDuration;

                if (_fadeUI != null)
                {
                    _fadeUI.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                }
                yield return null;
            }

            // จบแล้วตั้งค่าปลายทางให้เป๊ะ
            if (_fadeUI != null) _fadeUI.alpha = endAlpha;
        }
    }
}