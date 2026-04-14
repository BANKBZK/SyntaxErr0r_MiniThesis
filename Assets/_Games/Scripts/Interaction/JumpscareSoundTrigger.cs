using UnityEngine;
using SyntaxError.Managers; //
using SyntaxError.Interfaces; //

namespace SyntaxError.Interaction
{
    [RequireComponent(typeof(BoxCollider))]
    public class JumpscareSoundTrigger : MonoBehaviour, IResettable
    {
        [Header("Sound Settings")]
        [Tooltip("ชื่อเสียงที่ตั้งไว้ใน SoundManager")]
        [SerializeField] private string _soundName = "Jumpscare";

        [Tooltip("ใช้ระบบ Music เพื่อให้สามารถ สั่งหยุด(Stop) หรือเล่นวน(Loop) ได้")]
        [SerializeField] private bool _useMusicSystem = false;

        [Header("Trigger Conditions")]
        [Tooltip("เล่นแค่ครั้งเดียว (ถ้า Reset Loop ถึงจะกลับมาเล่นใหม่ได้)")]
        [SerializeField] private bool _playOnlyOnce = true;

        [Tooltip("หยุดเสียงทันทีเมื่อเดินออกจาก Collider (ต้องเปิด Use Music System ด้วย)")]
        [SerializeField] private bool _stopOnExit = false;

        private bool _hasPlayed = false;

        private void Start()
        {
            // บังคับให้ Collider เป็น Trigger เสมอ
            if (GetComponent<BoxCollider>() != null)
                GetComponent<BoxCollider>().isTrigger = true;

            // ลงทะเบียนกับ LoopManager (ถ้ามี) เพื่อให้ Reset ได้ตอนตาย
            if (LoopManager.Instance != null)
                LoopManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null)
                LoopManager.Instance.Unregister(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            // เช็คว่าเป็นผู้เล่นไหม
            if (!other.CompareTag("Player")) return;

            // เช็คเงื่อนไขเล่นครั้งเดียว
            if (_playOnlyOnce && _hasPlayed) return;

            PlayJumpscare();
            _hasPlayed = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // ถ้าตั้งค่าให้หยุดเสียงตอนเดินออก
            if (_stopOnExit && _useMusicSystem)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.StopMusic(_soundName);
                }
            }
        }

        private void PlayJumpscare()
        {
            if (SoundManager.Instance == null) return;

            if (_useMusicSystem)
            {
                // ใช้ระบบ Music เพื่อให้สั่ง Stop หรือ Loop ตามที่ตั้งค่าใน Sound List ได้
                SoundManager.Instance.PlayMusic(_soundName);
            }
            else
            {
                // ใช้ระบบ SFX ปกติ (เล่นทับกันได้ แต่สั่งหยุดกลางคันไม่ได้)
                SoundManager.Instance.PlaySFX(_soundName);
            }

            Debug.Log($"[Jumpscare] Triggered: {_soundName}");
        }

        // ==========================================
        // ระบบ Reset
        // ==========================================
        public void OnLoopReset(int currentLoop)
        {
            // ถ้าเริ่ม Loop 0 ใหม่ ให้กลับมาเล่นได้อีกครั้ง
            if (currentLoop == 0)
            {
                _hasPlayed = false;

                // สั่งหยุดเสียงที่อาจจะค้างอยู่
                if (SoundManager.Instance != null && _useMusicSystem)
                {
                    SoundManager.Instance.StopMusic(_soundName);
                }
            }
        }
    }
}