using UnityEngine;

namespace SyntaxError.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Global State")]
        public int CurrentLoop = 0;
        public bool IsPuzzleSolved = false;
        public bool IsRitualComplete = false;

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
            if (UIManager.Instance != null) UIManager.Instance.UpdateLoopDisplay(CurrentLoop);
            Debug.Log($"--- ENTERING LOOP {CurrentLoop} ---");
        }

        public void ResetToZero()
        {
            CurrentLoop = 0;
            IsRitualComplete = false; // [Updated] รีเซ็ตสถานะพิธีกรรม
            IsPuzzleSolved = false;   // [Updated] รีเซ็ตปริศนา
            if (UIManager.Instance != null) UIManager.Instance.UpdateLoopDisplay(0);
            Debug.Log("<color=red>GAME OVER! Resetting all progress to Loop 0.</color>");
        }
    }
}