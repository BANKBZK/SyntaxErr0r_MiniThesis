using UnityEngine;
using SyntaxError.Managers;
using SyntaxError.Interfaces; // 🛠️ 1. เพิ่มการเรียกใช้ Interface

// 🛠️ 2. ใส่ , IResettable ต่อท้าย
public class DeadZone : MonoBehaviour, IResettable
{
    [Header("Ending Settings")]
    [Tooltip("เวลาที่ปล่อยให้ผีจ้องหน้า (วินาที) ก่อนจะโดนส่งกลับไป Loop 0")]
    [SerializeField] private float _timeToWait = 3f;

    private float currentTime = 0f;
    private bool isPlayerInZone = false;

    // 🛠️ 3. ลงทะเบียนตัวเองกับระบบเวลาเริ่มเกม
    private void Start()
    {
        if (LoopManager.Instance != null) LoopManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (LoopManager.Instance != null) LoopManager.Instance.Unregister(this);
    }

    // 🛠️ 4. ฟังก์ชันลบล้างความจำตอนเริ่มลูป 0 ใหม่
    public void OnLoopReset(int currentLoop)
    {
        currentTime = 0f;
        isPlayerInZone = false;
        Debug.Log("[DeadZone] ล้างสถานะอันตรายเรียบร้อย!");
    }

    private void Update()
    {
        if (isPlayerInZone)
        {
            currentTime += Time.deltaTime;
            if (currentTime >= _timeToWait)
            {
                currentTime = 0f;
                isPlayerInZone = false;

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