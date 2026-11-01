using System;
using UnityEngine;

/// <summary>
/// ============================================================================
/// 檔案名稱: SaveManager.cs
/// 所屬模組: Save
/// 責任描述: 全域存檔狀態管理器。實作 ISaveManager 契約，負責維護記憶體中的
///           玩家存檔資料快取 (GameSaveData)，透過事件匯流排自動監聽遊戲進度與設定變更，
///           並排程非同步或批次持久化。
/// 架構分層: Save / State Manager
/// 聯動關係:
///   - 介面實作: ISaveManager
///   - 上游依賴: EventBus (接收 GameProgressUpdatedEvent, AudioVolumeChangedEvent), ISaveService
///   - 下游消費: 提供只讀的 GameSaveData 供外部查詢
/// 邊界處理: 採用髒標記 (Dirty Flag) 機制，避免高頻磁碟寫入造成手機端掉幀 (Jank)
/// ----------------------------------------------------------------------------
/// 【Unity 場景掛載指引】
/// 1. 掛載物件: 場景根節點下的空物件，命名為「[SaveManager]」。
/// 2. 生命週期: 腳本內建 DontDestroyOnLoad，保持全生命週期常駐。
/// 3. Inspector 參數配置:
///    - Auto Save Interval: 自動存檔檢測間隔 (預設 30 秒)。
/// ============================================================================
/// </summary>
public class SaveManager : MonoBehaviour, ISaveManager
{
    private static SaveManager instance;

    private const string SaveKey = "temporal_shrine_save";

    [Header("Save Settings")]
    [Tooltip("Auto save interval in seconds")]
    [SerializeField] private float autoSaveInterval = 30f;

    private ISaveService saveService;
    private GameSaveData currentSaveData;
    private bool isDirty = false;
    private float autoSaveTimer = 0f;

    /// <summary>
    /// 全域單例實例訪問入口
    /// </summary>
    public static SaveManager Instance => instance;

    /// <summary>
    /// 當前記憶體中的存檔唯讀資料
    /// </summary>
    public GameSaveData CurrentSaveData => currentSaveData;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化存檔驅動服務 (依賴注入實作)
        saveService = new JsonSaveService();

        // 載入磁碟存檔至記憶體快取
        currentSaveData = saveService.Load<GameSaveData>(SaveKey);
        if (currentSaveData == null)
        {
            currentSaveData = new GameSaveData();
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameProgressUpdatedEvent>(OnGameProgressUpdated);
        EventBus.Subscribe<AudioVolumeChangedEvent>(OnAudioVolumeChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameProgressUpdatedEvent>(OnGameProgressUpdated);
        EventBus.Unsubscribe<AudioVolumeChangedEvent>(OnAudioVolumeChanged);
    }

    private void Update()
    {
        // 週期性檢查髒標記，定時平滑存檔
        if (isDirty)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                CommitSave();
            }
        }
    }

    /// <summary>
    /// 監聽進度更新事件
    /// </summary>
    private void OnGameProgressUpdated(GameProgressUpdatedEvent evt)
    {
        if (currentSaveData == null) return;

        currentSaveData.progress = evt.CurrentProgress;
        currentSaveData.UpdateSaveTime();
        isDirty = true;

        // 進度變更是里程碑，立即落盤保存
        CommitSave();
    }

    /// <summary>
    /// 監聽音量更新事件
    /// </summary>
    private void OnAudioVolumeChanged(AudioVolumeChangedEvent evt)
    {
        if (currentSaveData == null) return;

        currentSaveData.masterVolume = evt.NormalizedVolume;
        currentSaveData.UpdateSaveTime();
        isDirty = true;
    }

    /// <summary>
    /// 將快取資料寫入持久化儲存檔案
    /// </summary>
    public void CommitSave()
    {
        if (!isDirty || currentSaveData == null) return;

        saveService.Save(SaveKey, currentSaveData);
        isDirty = false;
        autoSaveTimer = 0f;

#if UNITY_EDITOR
        Debug.Log("[SaveManager] Save data synced to disk successfully.");
#endif
    }

    /// <summary>
    /// 重置所有存檔資料至初始狀態
    /// </summary>
    public void ResetSaveData()
    {
        currentSaveData.ResetToDefault();
        saveService.Delete(SaveKey);
        isDirty = false;

#if UNITY_EDITOR
        Debug.Log("[SaveManager] Save data reset.");
#endif
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // 手機切到背景時強制落盤
        if (pauseStatus)
        {
            CommitSave();
        }
    }

    private void OnApplicationQuit()
    {
        // 遊戲退出前強制落盤
        CommitSave();
    }
}
