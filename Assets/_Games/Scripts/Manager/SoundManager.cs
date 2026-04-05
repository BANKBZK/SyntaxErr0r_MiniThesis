using UnityEngine;
using UnityEngine.Audio; // ต้องใช้สำหรับ Audio Mixer
using System;

namespace SyntaxError.Managers
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        [Header("Audio Mixer Setup")]
        public AudioMixer audioMixer;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup envGroup;
        public AudioMixerGroup uiGroup;
        public AudioMixerGroup musicGroup;

        [Header("Sound Library")]
        public Sound[] sounds;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // สร้าง AudioSource และส่งเสียงเข้า Mixer Group ให้ตรงกับที่ตั้งไว้
            foreach (Sound s in sounds)
            {
                s.source = gameObject.AddComponent<AudioSource>();
                s.source.clip = s.clip;
                s.source.pitch = s.pitch;
                s.source.loop = s.loop;
                s.source.volume = s.volume; // ค่า Volume พื้นฐาน

                // โยนเสียงเข้า Mixer Group ให้ถูกต้อง
                switch (s.type)
                {
                    case SoundType.SFX: s.source.outputAudioMixerGroup = sfxGroup; break;
                    case SoundType.Environment: s.source.outputAudioMixerGroup = envGroup; break;
                    case SoundType.UI: s.source.outputAudioMixerGroup = uiGroup; break;
                    case SoundType.Music: s.source.outputAudioMixerGroup = musicGroup != null ? musicGroup : envGroup; break;
                }
            }
        }

        public void PlaySFX(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null) return;
            s.source.PlayOneShot(s.clip);
        }

        public void PlayMusic(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null) return;
            if (!s.source.isPlaying) s.source.Play();
        }

        public void StopMusic(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null) return;
            s.source.Stop();
        }

        // ==========================================
        // 🎚️ ฟังก์ชันปรับเสียงผ่าน Audio Mixer (รับค่า 0.0001 ถึง 1.0)
        // ==========================================
        public void SetMixerVolume(string parameterName, float sliderValue)
        {
            if (audioMixer == null) return;

            // กันบั๊กค่าเป็น 0 (เพราะ Log10(0) จะพัง)
            float val = Mathf.Max(sliderValue, 0.0001f);

            // แปลงค่าจาก 0-1 เป็นหน่วย Decibel (-80 ถึง 0)
            float db = Mathf.Log10(val) * 20f;

            audioMixer.SetFloat(parameterName, db);
        }
    }
}