using UnityEngine;

/// <summary>
/// ============================================================================
/// 檔案名稱: GameManager.cs
/// 所屬模組: Gameplay
/// 責任描述: 神社探索遊戲主流程控制器。實作 IQuizService 契約，專職負責題目進度
///           仲裁、正確答案比對與通關狀態管理，完全解耦具體 UI 與存檔落盤實作。
/// 架構分層: Gameplay / Flow Controller
/// 聯動關係:
///   - 介面實作: IQuizService
///   - 上游依賴: EventBus (監聽 QuizAnswerSubmittedEvent)
///   - 下游消費: 透過 EventBus 廣播 GameProgressUpdatedEvent
/// 邊界處理: 針對題目越界與無效回答做安全防禦
/// ----------------------------------------------------------------------------
/// 【Unity 場景掛載指引】
/// 1. 掛載物件: 場景根節點下的空物件，命名為「[GameManager]」。
/// 2. 生命週期: 腳本內建 DontDestroyOnLoad，跨場景保持單例唯一性。
/// 3. Inspector 參數配置:
///    - Quiz Database: 拖入 Project 視窗建立的 QuizData.asset 總資料庫。
/// ============================================================================
/// </summary>
public class GameManager : MonoBehaviour, IQuizService
{
    private static GameManager instance;

    [Header("Data Configuration")]
    [Tooltip("Global quiz database asset")]
    [SerializeField] private QuizData quizDatabase;

    private int currentProgress = 0;
    private int currentQuizIndex = 0;

    /// <summary>
    /// 全域單例實例訪問入口
    /// </summary>
    public static GameManager Instance => instance;

    /// <summary>
    /// 當前探索進度
    /// </summary>
    public int CurrentProgress => currentProgress;

    /// <summary>
    /// 當前進行中的題目索引
    /// </summary>
    public int CurrentQuizIndex => currentQuizIndex;

    /// <summary>
    /// 問答題庫總資料
    /// </summary>
    public QuizData QuizDatabase => quizDatabase;

    /// <summary>
    /// 總題目數量
    /// </summary>
    public int TotalQuestions => quizDatabase != null ? quizDatabase.QuestionCount : 5;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 啟動時從 SaveManager 獲取初始進度 (若 SaveManager 存在)
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
        {
            currentProgress = SaveManager.Instance.CurrentSaveData.progress;
        }
        else
        {
            currentProgress = 0;
        }

        // 發布初始進度事件同步全站 UI
        PublishProgressEvent();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<QuizAnswerSubmittedEvent>(OnQuizAnswerSubmitted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<QuizAnswerSubmittedEvent>(OnQuizAnswerSubmitted);
    }

    /// <summary>
    /// 監聽玩家提交答案之領域事件
    /// </summary>
    private void OnQuizAnswerSubmitted(QuizAnswerSubmittedEvent evt)
    {
        bool isCorrect = VerifyAnswer(evt.SceneryIndex, evt.SelectedAnswerIndex);

        if (isCorrect)
        {
            AdvanceProgress();
        }

#if UNITY_EDITOR
        Debug.Log($"[GameManager] Player answered question {evt.SceneryIndex} option {evt.SelectedAnswerIndex}, result: {(isCorrect ? "Correct" : "Incorrect")}");
#endif
    }

    /// <summary>
    /// 驗證特定題目與選項是否正確
    /// </summary>
    public bool VerifyAnswer(int questionIndex, int selectedIndex)
    {
        if (quizDatabase == null || questionIndex < 0 || questionIndex >= quizDatabase.QuestionCount)
        {
            return false;
        }

        return quizDatabase.VerifyAnswer(questionIndex, selectedIndex);
    }

    /// <summary>
    /// 推進遊戲進度並透過 EventBus 廣播狀態變更
    /// </summary>
    public void AdvanceProgress()
    {
        currentProgress++;
        PublishProgressEvent();

#if UNITY_EDITOR
        Debug.Log($"[GameManager] Progress advanced to: {currentProgress}");
#endif
    }

    /// <summary>
    /// 發布進度更新事件
    /// </summary>
    private void PublishProgressEvent()
    {
        int total = TotalQuestions;
        bool isFinished = currentProgress >= total;
        EventBus.Publish(new GameProgressUpdatedEvent(currentProgress, total, isFinished));
    }

    /// <summary>
    /// 設定當前題目索引
    /// </summary>
    public void SetCurrentQuizIndex(int index)
    {
        if (quizDatabase != null && index >= 0 && index < quizDatabase.QuestionCount)
        {
            currentQuizIndex = index;
        }
    }

    /// <summary>
    /// 重置全域遊戲進度
    /// </summary>
    public void ResetProgress()
    {
        currentProgress = 0;
        currentQuizIndex = 0;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetSaveData();
        }

        PublishProgressEvent();

#if UNITY_EDITOR
        Debug.Log("[GameManager] Game progress reset to zero.");
#endif
    }

#if UNITY_EDITOR
    private void Update()
    {
        // 編輯器專屬除錯快捷鍵 (C 鍵測試快速推進進度)
        if (Input.GetKeyDown(KeyCode.C))
        {
            AdvanceProgress();
        }
    }
#endif
}
