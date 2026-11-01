/// <summary>
/// ============================================================================
/// 檔案名稱: IQuizService.cs
/// 所屬模組: Gameplay
/// 責任描述: 定義神社問答與進度仲裁之服務行為契約 (Interface)。
/// 架構分層: Gameplay / Abstraction
/// 聯動關係:
///   - 下游消費: 供 GameManager 實作，供 UI 或測試模組查詢進度與驗證答案
/// 邊界處理: 封裝內部題庫比對邏輯，僅提供純粹的業務介面
/// ============================================================================
/// </summary>
public interface IQuizService
{
    int CurrentProgress { get; }
    int TotalQuestions { get; }
    int CurrentQuizIndex { get; }

    bool VerifyAnswer(int questionIndex, int selectedIndex);
    void AdvanceProgress();
    void ResetProgress();
}
