/// <summary>
/// ============================================================================
/// 檔案名稱: ISaveManager.cs
/// 所屬模組: Save
/// 責任描述: 定義全域存檔狀態管理器之行為契約 (Interface)。
/// 架構分層: Save / Interfaces
/// 聯動關係:
///   - 下游消費: 供 SaveManager 實作，供外部模組查詢存檔快取與觸發持久化
/// 邊界處理: 僅暴露唯讀資料與高階控制方法，隱藏內部 Dirty Flag 與定時器排程細節
/// ============================================================================
/// </summary>
public interface ISaveManager
{
    GameSaveData CurrentSaveData { get; }
    void CommitSave();
    void ResetSaveData();
}
