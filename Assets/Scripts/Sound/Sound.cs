using UnityEngine;

namespace SyntaxError.Managers
{
    [System.Serializable]
    public class Sound
    {
        public string name;           // ชื่อที่ใช้เรียก (เช่น "DoorOpen", "Correct")
        public AudioClip clip;        // ไฟล์เสียง

        [Range(0f, 1f)]
        public float volume = 0.7f;   // ความดัง
        [Range(0.1f, 3f)]
        public float pitch = 1f;      // ความทุ้มแหลม

        public bool loop = false;     // เล่นวนไหม?

        [HideInInspector]
        public AudioSource source;    // ตัวเล่นเสียง (ระบบจัดการเอง)
    }
}