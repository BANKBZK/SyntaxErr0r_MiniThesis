using UnityEngine;

namespace SyntaxError.Managers
{
    // สร้างตัวเลือกประเภทของเสียง (เพิ่ม Music เข้ามาให้เผื่อใช้กับเพลงหลอนๆ)
    public enum SoundType
    {
        SFX,
        Environment,
        UI,
        Music
    }

    [System.Serializable]
    public class Sound
    {
        public string name;           // ชื่อที่ใช้เรียก
        public SoundType type = SoundType.SFX; // ประเภทของเสียง (จะมี Dropdown ให้เลือกใน Inspector)
        public AudioClip clip;        // ไฟล์เสียง

        [Range(0f, 1f)]
        public float volume = 0.7f;   // ความดังพื้นฐาน (ตั้งใน Inspector)
        [Range(0.1f, 3f)]
        public float pitch = 1f;      // ความทุ้มแหลม

        public bool loop = false;     // เล่นวนไหม?

        [HideInInspector]
        public AudioSource source;    // ตัวเล่นเสียง (ระบบจัดการเอง)
    }
}