using UnityEngine;

/// <summary>
/// ============================================================================
/// 檔案名稱: QuizData.cs
/// 所屬模組: Gameplay
/// 責任描述: 定義神社歷史文化景點之問答題目配置資料容器 (ScriptableObject)。
/// 架構分層: Gameplay / Data Configuration
/// 聯動關係:
///   - 上游依賴: UnityEngine.ScriptableObject
///   - 下游消費: 被 QuizView 與 GameManager 讀取用於渲染題目文本與判定答案
/// 邊界處理: 封裝選項陣列長度防禦檢查，提供唯讀屬性避免外部執行期意外篡改。
/// ----------------------------------------------------------------------------
/// 【Unity 資產建立指引】
/// 1. 建立方式: 在 Unity Project 視窗任意資料夾內右鍵 -> Create -> TemporalShrine -> QuizData。
/// 2. 命名規範: 建議命名為 QuizData_0 到 QuizData_4 (對應景點 0 到 4)。
/// 3. Inspector 參數配置:
///    - Scene Name: 填入景點歷史名稱 (例如「新竹神社手水舍」)。
///    - Question: 題目文字。
///    - Answers: 3 個題目選項文字。
///    - Correct Answer Index: 正確答案索引 (0 ~ 2)。
///    - 圖片配置: 分別拖入預設底圖、答對圖、答錯圖、預設按鈕、答對按鈕、答錯按鈕 Sprite。
/// ============================================================================
/// </summary>
[CreateAssetMenu(fileName = "NewQuizData", menuName = "TemporalShrine/QuizData", order = 1)]
public class QuizData : ScriptableObject
{
    [Header("Attraction & Quiz Information")]
    [SerializeField] private string sceneName = string.Empty;

    [TextArea(2, 6)]
    [SerializeField] private string question = string.Empty;

    [SerializeField] private string[] answers = new string[3];

    [Tooltip("Correct answer index (0, 1 or 2)")]
    [Range(0, 2)]
    [SerializeField] private int correctAnswerIndex = 0;

    [Header("Background Sprites")]
    [SerializeField] private Sprite imageBase;
    [SerializeField] private Sprite imageCorrect;
    [SerializeField] private Sprite imageIncorrect;

    [Header("Button Visual States")]
    [SerializeField] private Sprite buttonBase;
    [SerializeField] private Sprite buttonCorrect;
    [SerializeField] private Sprite buttonIncorrect;

    // 公開唯讀屬性
    public string SceneName => sceneName;
    public string Question => question;
    public string[] Answers => answers;
    public int CorrectAnswerIndex => correctAnswerIndex;

    public Sprite ImageBase => imageBase;
    public Sprite ImageCorrect => imageCorrect;
    public Sprite ImageIncorrect => imageIncorrect;

    public Sprite ButtonBase => buttonBase;
    public Sprite ButtonCorrect => buttonCorrect;
    public Sprite ButtonIncorrect => buttonIncorrect;

    /// <summary>
    /// 總題目數量 (單題配置預設為 1，支援關卡相容性)
    /// </summary>
    public int QuestionCount => 5;

    /// <summary>
    /// 驗證單一題目之作答是否正確
    /// </summary>
    public bool CheckAnswer(int selectedIndex)
    {
        return selectedIndex == correctAnswerIndex;
    }

    /// <summary>
    /// 提供給 GameManager 進行多題目索引判定
    /// </summary>
    public bool VerifyAnswer(int questionIndex, int selectedIndex)
    {
        return selectedIndex == correctAnswerIndex;
    }
}
