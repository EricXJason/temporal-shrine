using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ============================================================================
/// 檔案名稱: UIManager.cs
/// 所屬模組: UI
/// 責任描述: 負責整體 UI 介面面板流轉、進度 HUD 顯示、景點地圖開合與按鈕點擊廣播。
/// 架構分層: UI / Coordinator
/// 聯動關係:
///   - 上游依賴: EventBus (監聽 GameProgressUpdatedEvent)
///   - 下游消費: 廣播 ButtonClickedEvent
/// 邊界處理: 動畫參數全面使用 Animator.StringToHash 快取，消滅所有執行期字串雜湊與 GetComponent 開銷。
/// ----------------------------------------------------------------------------
/// 【Unity 場景掛載指引】
/// 1. 掛載物件: 掛載在 UI 畫布根物件「Canvas」或空物件「[UIManager]」上。
/// 2. Inspector 參數配置:
///    - Main Menu Panel / Gameplay Panel / Ending Panel: 分別拖入對應的面板 GameObject。
///    - All Panels: 將所有切換面板陣列全數加入 (供 CloseAllPanels 統一收合)。
///    - Progress Text: 拖入顯示 "X/5" 的 TextMeshProUGUI。
///    - Map Object: 拖入帶有展開動畫 Animator 的地圖 GameObject。
///    - Scenery Button Objects: 拖入景點 0 ~ 4 的導航按鈕 GameObject 清單。
///    - Scene Base Images / Scene Finish Images: 拖入未通關與通關後的 Sprite 圖片清單。
///    - Interactive Buttons: 拖入需要統一播放點擊音效的所有 UI 按鈕。
/// ============================================================================
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    private static readonly int IsEnabledParamHash = Animator.StringToHash("isEnabled");

    [Header("Core Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private GameObject[] allPanels;

    [Header("Progress & Map Elements")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject mapObject;

    [Header("Navigation Button Group")]
    [SerializeField] private GameObject[] sceneryButtonObjects;
    [SerializeField] private Sprite[] sceneBaseImages;
    [SerializeField] private Sprite[] sceneFinishImages;

    [Header("Interactive Buttons (Auto SFX Binding)")]
    [SerializeField] private Button[] interactiveButtons;

    // 組件快取欄位 (Zero-GC Optimization)
    private Image[] sceneryButtonImages;
    private Button[] sceneryButtonButtons;
    private Animator[] sceneryButtonAnimators;
    private RectTransform[] sceneryButtonRects;

    private Animator mapAnimator;
    private RectTransform mapRectTransform;

    private bool isSceneryButtonOpened = false;
    private bool isMapButtonOpened = false;

    private int cachedCurrentProgress = 0;
    private int cachedTotalProgress = 5;

    /// <summary>
    /// 全域單例實例訪問入口
    /// </summary>
    public static UIManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        CacheAllComponents();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameProgressUpdatedEvent>(OnGameProgressUpdated);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameProgressUpdatedEvent>(OnGameProgressUpdated);
    }

    private void Start()
    {
        InitializeInterface();
        BindInteractiveButtons();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // 編輯器專用測試快捷鍵：按下 A 鍵手動刷新 UI 數據
        if (Input.GetKeyDown(KeyCode.A))
        {
            RefreshUIData();
        }
    }
#endif

    /// <summary>
    /// 預先快取所有 UI 組件引用，徹底杜絕執行期 GetComponent 性能損耗
    /// </summary>
    private void CacheAllComponents()
    {
        if (mapObject != null)
        {
            mapAnimator = mapObject.GetComponent<Animator>();
            mapRectTransform = mapObject.GetComponent<RectTransform>();
        }

        int count = sceneryButtonObjects != null ? sceneryButtonObjects.Length : 0;
        sceneryButtonImages = new Image[count];
        sceneryButtonButtons = new Button[count];
        sceneryButtonAnimators = new Animator[count];
        sceneryButtonRects = new RectTransform[count];

        for (int i = 0; i < count; i++)
        {
            if (sceneryButtonObjects[i] != null)
            {
                sceneryButtonImages[i] = sceneryButtonObjects[i].GetComponent<Image>();
                sceneryButtonButtons[i] = sceneryButtonObjects[i].GetComponent<Button>();
                sceneryButtonAnimators[i] = sceneryButtonObjects[i].GetComponent<Animator>();
                sceneryButtonRects[i] = sceneryButtonObjects[i].GetComponent<RectTransform>();
            }
        }
    }

    /// <summary>
    /// 初始化介面顯示，開啟主選單並刷新存檔資料
    /// </summary>
    private void InitializeInterface()
    {
        CloseAllPanels();
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        RefreshUIData();
    }

    /// <summary>
    /// 為所有通用按鈕註冊全域點擊事件廣播
    /// </summary>
    private void BindInteractiveButtons()
    {
        if (interactiveButtons == null) return;

        foreach (var button in interactiveButtons)
        {
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    EventBus.Publish(new ButtonClickedEvent());
                });
            }
        }
    }

    /// <summary>
    /// 根據快取進度與各景點解鎖狀態刷新 UI 顯示
    /// </summary>
    public void RefreshUIData()
    {
        if (progressText != null)
        {
            progressText.text = $"{cachedCurrentProgress}/{cachedTotalProgress}";
        }

        int count = sceneryButtonObjects != null ? sceneryButtonObjects.Length : 0;
        for (int i = 0; i < count; i++)
        {
            bool isCompleted = i < cachedCurrentProgress;

            if (sceneryButtonImages != null && i < sceneryButtonImages.Length && sceneryButtonImages[i] != null)
            {
                if (sceneFinishImages != null && i < sceneFinishImages.Length && isCompleted)
                {
                    sceneryButtonImages[i].sprite = sceneFinishImages[i];
                }
                else if (sceneBaseImages != null && i < sceneBaseImages.Length)
                {
                    sceneryButtonImages[i].sprite = sceneBaseImages[i];
                }
            }

            if (sceneryButtonButtons != null && i < sceneryButtonButtons.Length && sceneryButtonButtons[i] != null)
            {
                sceneryButtonButtons[i].interactable = !isCompleted;
                if (isCompleted)
                {
                    sceneryButtonButtons[i].transition = Selectable.Transition.None;
                }
            }
        }
    }

    /// <summary>
    /// 切換景點選單按鈕的展開/收合動畫狀態
    /// </summary>
    public void ToggleSceneryButtons()
    {
        RefreshUIData();
        isSceneryButtonOpened = !isSceneryButtonOpened;

        if (sceneryButtonAnimators != null)
        {
            for (int i = 0; i < sceneryButtonAnimators.Length; i++)
            {
                if (sceneryButtonObjects[i] != null)
                {
                    sceneryButtonObjects[i].SetActive(true);
                }
                if (sceneryButtonAnimators[i] != null)
                {
                    sceneryButtonAnimators[i].SetBool(IsEnabledParamHash, isSceneryButtonOpened);
                }
            }
        }
    }

    /// <summary>
    /// 關閉並收合景點按鈕列表
    /// </summary>
    public void CloseSceneryButtons()
    {
        if (isSceneryButtonOpened)
        {
            isSceneryButtonOpened = false;
            if (sceneryButtonAnimators != null)
            {
                foreach (var animator in sceneryButtonAnimators)
                {
                    if (animator != null)
                    {
                        animator.SetBool(IsEnabledParamHash, false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 切換地圖面板展開狀態
    /// </summary>
    public void ToggleMapButton()
    {
        isMapButtonOpened = !isMapButtonOpened;
        if (mapAnimator != null)
        {
            mapAnimator.SetBool(IsEnabledParamHash, isMapButtonOpened);
        }
    }

    /// <summary>
    /// 重置地圖面板位置與動畫狀態
    /// </summary>
    public void ResetMapPosition()
    {
        isMapButtonOpened = false;
        if (mapAnimator != null)
        {
            mapAnimator.SetBool(IsEnabledParamHash, false);
        }
        if (mapRectTransform != null)
        {
            mapRectTransform.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 處理答題完成後的面板切換 (依據作答結果導向下一畫面)
    /// </summary>
    /// <param name="isCorrect">是否答對</param>
    /// <param name="tutorialPanel">教學面板</param>
    public void HandleQuizFinished(bool isCorrect, GameObject tutorialPanel)
    {
        RefreshUIData();

        if (isCorrect)
        {
            if (cachedCurrentProgress >= cachedTotalProgress)
            {
                OpenEndingPanel();
            }
            else
            {
                BackToGameplayPanel();
            }
        }
        else
        {
            if (tutorialPanel != null)
            {
                OpenPanel(tutorialPanel);
            }
        }
    }

    public void CloseAllPanels()
    {
        if (allPanels == null) return;
        foreach (var panel in allPanels)
        {
            if (panel != null) panel.SetActive(false);
        }
    }

    public void OpenPanel(GameObject panel)
    {
        CloseAllPanels();
        if (panel != null) panel.SetActive(true);
    }

    public void OpenPanelOnly(GameObject panel)
    {
        if (panel != null) panel.SetActive(true);
    }

    public void BackToGameplayPanel()
    {
        OpenPanel(gameplayPanel);
    }

    public void OpenEndingPanel()
    {
        CloseAllPanels();
        if (endingPanel != null) endingPanel.SetActive(true);
    }

    /// <summary>
    /// 跨平台開啟外部歷史導覽連結或 YouTube 影片
    /// </summary>
    public void OpenLink(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        string videoId = ExtractYoutubeVideoId(url);

#if UNITY_EDITOR
        Application.OpenURL(url);
#elif UNITY_ANDROID
        if (!string.IsNullOrEmpty(videoId))
            Application.OpenURL($"vnd.youtube:{videoId}");
        else
            Application.OpenURL(url);
#elif UNITY_IOS
        if (!string.IsNullOrEmpty(videoId))
            Application.OpenURL($"youtube://{videoId}");
        else
            Application.OpenURL(url);
#else
        Application.OpenURL(url);
#endif
    }

    /// <summary>
    /// 純文字解析 YouTube 網址以擷取 Video ID，無須依賴 System.Web.HttpUtility
    /// </summary>
    private string ExtractYoutubeVideoId(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        try
        {
            var match = Regex.Match(url, @"(?:youtu\.be\/|v=|\/v\/|embed\/|watch\?v=)([\w-]{11})");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void OnGameProgressUpdated(GameProgressUpdatedEvent evt)
    {
        cachedCurrentProgress = evt.CurrentProgress;
        cachedTotalProgress = evt.TotalProgress;

        RefreshUIData();

        if (evt.IsGameFinished)
        {
            OpenEndingPanel();
        }
    }
}
