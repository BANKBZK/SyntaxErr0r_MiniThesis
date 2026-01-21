using UnityEngine;

namespace SyntaxError.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        public int CurrentLoop = 0;
        public bool IsPuzzleSolved = false;

        private void Awake()
        {
            // Singleton Pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // ข้าม Scene ไม่หาย (เผื่อใช้ตอน Load Ending!!!)
        }

        public void IncrementLoop()
        {
            CurrentLoop++;
            Debug.Log($"Loop Count: {CurrentLoop}");
        }

        public void ResetLoop()
        {
            CurrentLoop = 0;
            Debug.Log("Loop Reset to 0!");
        }
    }
}