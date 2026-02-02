using UnityEngine;

namespace SyntaxError.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Global State")]
        public int CurrentLoop = 0; // เริ่มต้นที่ 0 (Tutorial)
        public bool IsPuzzleSolved = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void NextLoop()
        {
            CurrentLoop++;
            Debug.Log($"--- ENTERING LOOP {CurrentLoop} ---");
        }

        public void ResetToZero()
        {
            CurrentLoop = 0;
            Debug.Log("<color=red>WRONG CHOICE! Resetting to Loop 0.</color>");
        }
    }
}