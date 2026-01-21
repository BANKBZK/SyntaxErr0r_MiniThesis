using SyntaxError.Managers;
using UnityEngine;

namespace SyntaxError.Loop
{
    public class LoopTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            // เช็คว่าเป็น Player เดินมาชนไหม
            if (other.CompareTag("Player"))
            {
                // สั่งให้ LoopManager ทำงาน
                LoopManager.Instance.CompleteLoop();
            }
        }
    }
}