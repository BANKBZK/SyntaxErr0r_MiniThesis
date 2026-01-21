namespace SyntaxError.Interaction
{
    // Interface นี้เปรียบเสมือน "สัญญา" ว่าวัตถุนี้ต้องมีฟังก์ชัน Interact
    public interface IInteractable
    {
        void Interact();          // สั่งให้ทำงาน (เช่น เปิดประตู, เก็บของ)
        string GetPromptText();   // ข้อความที่จะขึ้น UI (เช่น "Open", "Read Note")
    }
}