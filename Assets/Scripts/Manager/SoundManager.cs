using UnityEngine;
using UnityEngine.Audio;
using System;

namespace SyntaxError.Managers
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        [Header("Sound Library")]
        public Sound[] sounds; // <-- ลิสต์ที่นายต้องการ

        private void Awake()
        {
            // Singleton Pattern
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

            // Setup AudioSources สำหรับเสียงทุกตัวเตรียมไว้เลย
            foreach (Sound s in sounds)
            {
                s.source = gameObject.AddComponent<AudioSource>();
                s.source.clip = s.clip;

                s.source.volume = s.volume;
                s.source.pitch = s.pitch;
                s.source.loop = s.loop;
            }
        }

        // ฟังก์ชันเรียกใช้: SoundManager.Instance.PlaySFX("ชื่อเสียง");
        public void PlaySFX(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null)
            {
                Debug.LogWarning("Sound: " + name + " not found!");
                return;
            }
            s.source.PlayOneShot(s.clip);
        }

        // ฟังก์ชันสำหรับเสียงเพลง (BGM)
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
    }
}