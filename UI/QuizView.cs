using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ============================================================================
/// 檔案名稱: QuizView.cs
/// 所屬模組: UI
/// 責任描述: 負責單一景點問答介面之文字渲染、按鈕互動、視覺反饋與作答協程。
/// 架構分層: UI / Views
/// 聯動關係:
///   - 上游依賴: QuizData (配置資料)、EventBus (事件中樞)
///   - 下游消費: 廣播 QuizAnswerSubmittedEvent 予 GameManager 與 AudioManager
/// 邊界處理: 在 Awake 預先快取 Image 與 Button 組件，徹底消除執行期 GetComponent 性能開銷。
/// ----------------------------------------------------------------------------
/// 【Unity 場景掛載指引】
/// 1. 掛載物件: 掛載在各景點的問答 UI 面板上 (例如 Canvas/Panels/QuizPanel_0)。
/// 2. Inspector 參數配置:
///    - Scenery Index: 該面板對應的景點關卡索引 (0 ~ 4)。
///    - Quiz Data: 拖入 Project 視窗建立的 QuizData_0.asset 資產。
///    - Background Image: 拖入該面板的背景 Image 元件。
///    - Tutorial Panel: 拖入答錯時導向的教學回顧面板 GameObject。
///    - Scene Name Text: 拖入景點名稱 TextMeshProUGUI。
///    - Question Text: 拖入題目描述 TextMeshProUGUI。
///    - Answer Texts: 依序拖入 3 個選項的 TextMeshProUGUI。
///    - Option Buttons: 依序拖入 3 個選項的 Button 元件。
/// ============================================================================
/// </summary>
public class QuizView : MonoBehaviour
{
    [Header("Scenery Level Index (0 ~ 4)")]
    [Range(0, 4)]
    [SerializeField] private int sceneryIndex;

    [Header("Quiz Data Asset")]
    [SerializeField] private QuizData quizData;

    [Header("UI Containers & Feedback Elements")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI sceneNameText;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI[] answerTexts = new TextMeshProUGUI[3];
    [SerializeField] private Button[] optionButtons = new Button[3];

    private Image[] optionButtonImages;
    private Coroutine feedbackCoroutine;

    private void Awake()
    {
        CacheButtonComponents();
    }

    private void OnEnable()
    {
        ResetVisualState();
    }

    private void OnDisable()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }
    }

    private void Start()
    {
        InitializeQuizDisplay();
        BindButtonEvents();
    }

    /// <summary>
    /// 快取所有選項按鈕之 Image 組件，避免執行期重複尋找
    /// </summary>
    private void CacheButtonComponents()
    {
        int length = optionButtons.Length;
        optionButtonImages = new Image[length];

        for (int i = 0; i < length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtonImages[i] = optionButtons[i].GetComponent<Image>();
            }
        }
    }

    /// <summary>
    /// 將 QuizData 配置內容寫入 UI 文本中
    /// </summary>
    private void InitializeQuizDisplay()
    {
        if (quizData == null) return;

        if (sceneNameText != null) sceneNameText.text = quizData.SceneName;
        if (questionText != null) questionText.text = quizData.Question;

        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < quizData.Answers.Length && answerTexts[i] != null)
            {
                answerTexts[i].text = quizData.Answers[i];
            }
        }
    }

    /// <summary>
    /// 註冊選項按鈕點擊監聽事件
    /// </summary>
    private void BindButtonEvents()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int buttonIndex = i;
            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(buttonIndex));
            }
        }
    }

    /// <summary>
    /// 重置介面至初始待作答狀態
    /// </summary>
    public void ResetVisualState()
    {
        if (quizData == null) return;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = quizData.ImageBase;
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].interactable = true;
            }

            if (optionButtonImages != null && optionButtonImages[i] != null)
            {
                optionButtonImages[i].sprite = quizData.ButtonBase;
            }
        }
    }

    /// <summary>
    /// 當玩家點擊選項按鈕時的處理邏輯
    /// </summary>
    /// <param name="selectedIndex">玩家選取之選項索引 (0 ~ 2)</param>
    private void OnOptionSelected(int selectedIndex)
    {
        // 點擊後鎖定所有按鈕，防止連點重入
        SetButtonsInteractable(false);

        bool isCorrect = quizData != null && quizData.CheckAnswer(selectedIndex);

        // 廣播作答事件 (供 GameManager 計算進度、AudioManager 播放正確/錯誤音效)
        EventBus.Publish(new QuizAnswerSubmittedEvent(sceneryIndex, selectedIndex, isCorrect));

        // 更新視覺反饋 (綠色答對 / 紅色答錯)
        ApplyFeedbackVisuals(selectedIndex, isCorrect);

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }
        feedbackCoroutine = StartCoroutine(HandleAnswerFeedbackSequence(isCorrect));
    }

    /// <summary>
    /// 根據作答結果替換背景與按鈕外觀貼圖
    /// </summary>
    private void ApplyFeedbackVisuals(int selectedIndex, bool isCorrect)
    {
        if (quizData == null) return;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = isCorrect ? quizData.ImageCorrect : quizData.ImageIncorrect;
        }

        if (optionButtonImages != null && selectedIndex >= 0 && selectedIndex < optionButtonImages.Length)
        {
            optionButtonImages[selectedIndex].sprite = isCorrect ? quizData.ButtonCorrect : quizData.ButtonIncorrect;
        }
    }

    /// <summary>
    /// 批次切換選項按鈕之互動可用性
    /// </summary>
    private void SetButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].interactable = interactable;
            }
        }
    }

    /// <summary>
    /// 作答結果展示延遲協程，停留 3 秒後導引至下一介面
    /// </summary>
    private IEnumerator HandleAnswerFeedbackSequence(bool isCorrect)
    {
        yield return new WaitForSeconds(3f);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HandleQuizFinished(isCorrect, tutorialPanel);
        }

        feedbackCoroutine = null;
    }
}
