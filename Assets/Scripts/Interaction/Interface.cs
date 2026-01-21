using UnityEngine;

namespace SyntaxError.Interfaces
{
    // สำหรับของที่กดได้ (ประตู, กระดาษ, ไอเทม)
    public interface IInteractable
    {
        void Interact();
        string GetPromptText();
    }

    // สำหรับของที่ต้องกลับสภาพเดิมเมื่อเริ่มรอบใหม่ (ประตู, ผีประจำ Loop)
    public interface IResettable
    {
        void OnLoopReset(int currentLoop); // ส่งเลข Loop มาบอกด้วย เผื่อใช้เช็คเงื่อนไข
    }
}