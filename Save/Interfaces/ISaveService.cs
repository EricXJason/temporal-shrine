/// <summary>
/// ============================================================================
/// 檔案名稱: ISaveService.cs
/// 所屬模組: Save
/// 責任描述: 泛型磁碟資料持久化驅動抽象介面，體現依賴倒置原則 (DIP)。
///           支援跨專案不同領域存檔 DTO 的存取，保持高度復用性。
/// 架構分層: Save / Interfaces
/// 聯動關係:
///   - 下游消費: 由 JsonSaveService 實作，由 SaveManager 依賴注入調用
/// ============================================================================
/// </summary>
public interface ISaveService
{
    /// <summary>
    /// 將泛型存檔資料物件寫入持久化儲存媒介
    /// </summary>
    /// <typeparam name="T">存檔資料模型型別</typeparam>
    /// <param name="key">存檔識別鍵值 (通常對應檔名)</param>
    /// <param name="data">欲持久化的資料實體</param>
    void Save<T>(string key, T data);

    /// <summary>
    /// 從持久化儲存媒介讀取指定鍵值之存檔資料物件
    /// </summary>
    /// <typeparam name="T">存檔資料模型型別</typeparam>
    /// <param name="key">存檔識別鍵值</param>
    /// <returns>反序列化後的資料實體，若檔案不存在或讀取失敗則返回 default(T)</returns>
    T Load<T>(string key);

    /// <summary>
    /// 檢查特定鍵值之本機存檔是否存在
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// 刪除特定鍵值之本機存檔
    /// </summary>
    void Delete(string key);
}
