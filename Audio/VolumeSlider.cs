using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ============================================================================
/// 檔案名稱: VolumeSlider.cs
/// 所屬模組: Audio
/// 責任描述: 負責 UI 音量滑桿 (Slider) 與全域混音器參數之雙向綁定。
///           初始值自 SaveManager 讀取，滑動時透過 EventBus 發布變更事件。
/// 架構分層: Audio / UI Component
/// 聯動關係:
///   - 上游依賴: UnityEngine.UI.Slider, SaveManager
///   - 下游消費: 透過 EventBus 廣播 AudioVolumeChangedEvent 同步混音參數與存檔
/// 邊界處理: 限制最小音量為 0.0001f，避免 Mathf.Log10(0) 產生負無窮大 (NaN)。
/// ----------------------------------------------------------------------------
/// 【Unity 場景掛載指引】
/// 1. 掛載物件: 掛載在帶有 UnityEngine.UI.Slider 組件的 UI 物件上 (例如 SettingsPanel/MasterSlider)。
/// 2. 相依性: 自動依賴 [RequireComponent(typeof(Slider))]。
/// 3. Inspector 參數配置:
///    - Audio Parameter: 填入對應 AudioMixer 中的 Exposed Parameter 名稱 (預設為 "Master")。
/// ============================================================================
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [Header("Mixer Parameter Settings")]
    [Tooltip("Exposed parameter name in AudioMixer (e.g. Master, BGM, SFX)")]
    [SerializeField] private string audioParameter = "Master";

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        SetupSlider();
    }

    /// <summary>
    /// 初始化音量滑桿設定與事件監聽
    /// </summary>
    public void SetupSlider()
    {
        if (slider == null) return;

        slider.minValue = 0.0001f;
        slider.maxValue = 1f;

        // 從 SaveManager 讀取預設音量
        float initialVolume = 1f;
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
        {
            initialVolume = SaveManager.Instance.CurrentSaveData.masterVolume;
        }

        slider.value = initialVolume;

        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    /// <summary>
    /// 當滑桿數值變動時的回呼處理
    /// </summary>
    /// <param name="normalizedValue">線性音量 (0.0001 ~ 1.0)</param>
    private void OnSliderValueChanged(float normalizedValue)
    {
        EventBus.Publish(new AudioVolumeChangedEvent(audioParameter, normalizedValue));
    }
}
