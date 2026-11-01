using UnityEngine;

/// <summary>
/// ============================================================================
/// 檔案名稱: AREndingModelController.cs
/// 所屬模組: AR
/// 責任描述: 負責通關終點之古蹟 3D 模型結算展示。透過訂閱 EventBus 緩存進度，
///           當 Vuforia 辨識到特定終點目標且進度達標時，啟動模型與旋轉展示動畫。
/// 架構分層: AR / Presentation
/// 聯動關係:
///   - 上游依賴: EventBus (監聽 GameProgressUpdatedEvent)
///   - 下游消費: 驅動 3D Model GameObject 與 Transform
/// 邊界處理: 零單例強耦合，當模型未指派或進度不足時靜態防禦
/// ----------------------------------------------------------------------------
/// 【Unity 場景掛載指引】
/// 1. 掛載物件: 掛載在 Vuforia 的特定結算 ImageTarget 物件上 (例如 ImageTarget_Ending)。
/// 2. Vuforia 事件綁定:
///    - 在該 ImageTarget 上的 DefaultObserverEventHandler 元件中：
///      - On Target Found -> 拖入此腳本所在物件，選擇 AREndingModelController.OnTargetFound
///      - On Target Lost  -> 拖入此腳本所在物件，選擇 AREndingModelController.OnTargetLost
/// 3. Inspector 參數配置:
///    - Ending Model: 拖入其子節點下的 3D 神社古蹟模型 GameObject (預設隱藏)。
///    - Required Clear Progress: 結算門檻進度 (預設 5)。
///    - Rotation Speed: 模型自體旋轉展示速度 (預設 15 度/秒)。
/// ============================================================================
/// </summary>
public class AREndingModelController : MonoBehaviour
{
    [Header("Display Target")]
    [Tooltip("3D shrine heritage model object for final showcase")]
    [SerializeField] private GameObject endingModel;

    [Header("Unlock Threshold")]
    [Tooltip("Minimum exploration progress required to unlock")]
    [SerializeField] private int requiredClearProgress = 5;

    [Header("Animation Effects")]
    [Tooltip("3D model rotation display speed in degrees per second")]
    [SerializeField] private float rotationSpeed = 15f;

    private int cachedProgress = 0;
    private bool isModelVisible = false;

    private void Awake()
    {
        if (endingModel != null)
        {
            endingModel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 透過 EventBus 監聽進度更新，完全無需依賴 GameManager 單例
        EventBus.Subscribe<GameProgressUpdatedEvent>(OnProgressUpdated);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameProgressUpdatedEvent>(OnProgressUpdated);
    }

    /// <summary>
    /// 監聽進度事件並更新本地快取
    /// </summary>
    private void OnProgressUpdated(GameProgressUpdatedEvent evt)
    {
        cachedProgress = evt.CurrentProgress;
    }

    /// <summary>
    /// 由 Vuforia 的 DefaultObserverEventHandler 在 Target 成功辨識時觸發 (OnTargetFound)
    /// </summary>
    public void OnTargetFound()
    {
        if (cachedProgress >= requiredClearProgress)
        {
            if (endingModel != null)
            {
                endingModel.SetActive(true);
                isModelVisible = true;
            }

#if UNITY_EDITOR
            Debug.Log($"[AREndingModelController] Exploration progress reached ({cachedProgress}/{requiredClearProgress}), unlocking heritage model.");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log($"[AREndingModelController] Exploration progress not met ({cachedProgress}/{requiredClearProgress}), heritage model locked.");
#endif
        }
    }

    /// <summary>
    /// 由 Vuforia 在 Target 遺失視野時觸發 (OnTargetLost)
    /// </summary>
    public void OnTargetLost()
    {
        if (endingModel != null)
        {
            endingModel.SetActive(false);
            isModelVisible = false;
        }
    }

    private void Update()
    {
        // 僅在模型可見時進行平滑旋轉展示
        if (isModelVisible && endingModel != null)
        {
            endingModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
