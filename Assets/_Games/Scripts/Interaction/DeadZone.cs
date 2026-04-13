using UnityEngine;
using SyntaxError.Managers;

public class DeadZone : MonoBehaviour
{
    [Header("Ending Settings")]
    [Tooltip("เวลาที่ปล่อยให้ผีจ้องหน้า (วินาที) ก่อนจะโดนส่งกลับไป Loop 0")]
    [SerializeField] private float _timeToWait = 3f;

    private float currentTime = 0f;
    private bool isPlayerInZone = false;

    private void Update()
    {
        if (isPlayerInZone)
        {
            currentTime += Time.deltaTime;
            if (currentTime >= _timeToWait)
            {
                currentTime = 0f;
                isPlayerInZone = false;

                // [Updated] เรียกใช้ Soft Reset แทนการโหลด Scene ใหม่
                if (LoopManager.Instance != null)
                {
                    LoopManager.Instance.FullGameReset();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered Dead Zone...");
            isPlayerInZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            currentTime = 0f;
            isPlayerInZone = false;
        }
    }
}