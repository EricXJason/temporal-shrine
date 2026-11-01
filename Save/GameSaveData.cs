using System;

/// <summary>
/// ============================================================================
/// 檔案名稱: GameSaveData.cs
/// 所屬模組: Save
/// 責任描述: 遊戲存檔資料傳輸物件 (DTO)。專門定義需要序列化儲存至硬碟的純資料結構。
/// 架構分層: Save / DTO
/// 聯動關係:
///   - 下游消費: 供 JsonSaveService 進行 JSON 序列化，供 SaveManager 進行快取管理
/// 邊界處理: 僅包含基礎資料型別與陣列，標記 [Serializable] 供 Unity JsonUtility 讀寫
/// ============================================================================
/// </summary>
[Serializable]
public class GameSaveData
{
    // 當前探索總進度 (0 ~ 5)
    public int progress = 0;

    // 各景點是否已通關解鎖 (長度為 5 的布林陣列)
    public bool[] unlockedSceneries = new bool[5];

    // 全域主音量大小 (0.0 ~ 1.0)
    public float masterVolume = 1.0f;

    // 最後存檔時間文字 (格式例如 "2026-11-01 11:22:00")
    public string lastSaveTime = string.Empty;

    /// <summary>
    /// 建構函式：新玩家首次啟動時賦予預設值
    /// </summary>
    public GameSaveData()
    {
        progress = 0;
        unlockedSceneries = new bool[5];
        masterVolume = 1.0f;
        UpdateSaveTime();
    }

    /// <summary>
    /// 更新最後存檔時間為當前標準時間
    /// </summary>
    public void UpdateSaveTime()
    {
        lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 重置所有進度至初始狀態
    /// </summary>
    public void ResetToDefault()
    {
        progress = 0;
        for (int i = 0; i < unlockedSceneries.Length; i++)
        {
            unlockedSceneries[i] = false;
        }
        masterVolume = 1.0f;
        UpdateSaveTime();
    }
}
