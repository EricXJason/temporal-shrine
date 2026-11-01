/// <summary>
/// ============================================================================
/// 檔案名稱: GameEvents.cs
/// 所屬模組: Infrastructure
/// 責任描述: 集中宣告全域領域事件資料契約 (Data Contract)。
/// 架構分層: Infrastructure / EventBus / Events
/// 聯動關係:
///   - 下游消費: 供 Gameplay, UI, Audio, Save 等領域模組訂閱消費
/// 邊界處理: 事件結構設計為唯讀結構體 (readonly struct)，杜絕廣播過程中的副作用與 GC 消耗。
/// ============================================================================
/// </summary>

/// <summary>
/// 遊戲進度更新事件 (例如答對題目或重置進度時廣播)
/// </summary>
public readonly struct GameProgressUpdatedEvent
{
    public int CurrentProgress { get; }
    public int TotalProgress { get; }
    public bool IsGameFinished { get; }

    public GameProgressUpdatedEvent(int currentProgress, int totalProgress, bool isGameFinished)
    {
        CurrentProgress = currentProgress;
        TotalProgress = totalProgress;
        IsGameFinished = isGameFinished;
    }
}

/// <summary>
/// 玩家提交答案事件 (由 QuizView 觸發，供 GameManager 判定與音效監聽)
/// </summary>
public readonly struct QuizAnswerSubmittedEvent
{
    public int SceneryIndex { get; }
    public int SelectedAnswerIndex { get; }
    public bool IsCorrect { get; }

    public QuizAnswerSubmittedEvent(int sceneryIndex, int selectedAnswerIndex, bool isCorrect)
    {
        SceneryIndex = sceneryIndex;
        SelectedAnswerIndex = selectedAnswerIndex;
        IsCorrect = isCorrect;
    }
}

/// <summary>
/// 任意 UI 按鈕點擊事件 (用於集中播放通用點擊音效)
/// </summary>
public readonly struct ButtonClickedEvent
{
}

/// <summary>
/// 音量參數變更事件 (由 VolumeSlider 發布，供 AudioManager 與 SaveManager 監聽)
/// </summary>
public readonly struct AudioVolumeChangedEvent
{
    public string ParameterName { get; }
    public float NormalizedVolume { get; }

    public AudioVolumeChangedEvent(string parameterName, float normalizedVolume)
    {
        ParameterName = parameterName;
        NormalizedVolume = normalizedVolume;
    }
}
