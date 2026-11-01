using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// ============================================================================
/// 檔案名稱: AudioManager.cs
/// 所屬模組: Audio
/// 責任描述: 負責音效管線 (AudioMixer)、答題音效與通用按鈕音效之播放與排程。
///           實作 IAudioService 契約，音量初始化自全域 SaveManager 讀取，並響應 EventBus 動態更新。
/// 架構分層: Audio / Service
/// 聯動關係:
///   - 介面實作: IAudioService
///   - 上游依賴: EventBus (監聽 ButtonClickedEvent, QuizAnswerSubmittedEvent, AudioVolumeChangedEvent), SaveManager
///   - 下游消費: 驅動 AudioMixer 與 AudioSource
/// 邊界處理: 具備 AudioSource 空值防禦，音量換算採對數換算避免非線性爆音。
/// ----------------------------------------------------------------------------
/// 【Unity 場景掛載指引】
/// 1. 掛載物件: 場景根節點下的空物件，命名為「[AudioManager]」。
/// 2. 生命週期: 腳本內建 DontDestroyOnLoad，保持全域常駐。
/// 3. Inspector 參數配置:
///    - Audio Mixer: 拖入專案中的 MainAudioMixer 資產。
///    - Button Sound: 掛在同物件或子物件上的 AudioSource (設定點擊音效 Clip)。
///    - Correct Answer Sound: 掛在同物件或子物件上的 AudioSource (設定答對音效 Clip)。
///    - Incorrect Answer Sound: 掛在同物件或子物件上的 AudioSource (設定答錯音效 Clip)。
/// ============================================================================
/// </summary>
public class AudioManager : MonoBehaviour, IAudioService
{
    private static AudioManager instance;

    private const string MasterParameter = "Master";
    private const string BgmParameter = "BGM";
    private const string SfxParameter = "SFX";

    [Header("AudioMixer Configuration")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Source Components")]
    [SerializeField] private AudioSource buttonSound;
    [SerializeField] private AudioSource correctAnswerSound;
    [SerializeField] private AudioSource incorrectAnswerSound;

    /// <summary>
    /// 全域單例實例訪問入口
    /// </summary>
    public static AudioManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioLevels();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<ButtonClickedEvent>(OnButtonClicked);
        EventBus.Subscribe<QuizAnswerSubmittedEvent>(OnQuizAnswerSubmitted);
        EventBus.Subscribe<AudioVolumeChangedEvent>(OnAudioVolumeChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ButtonClickedEvent>(OnButtonClicked);
        EventBus.Unsubscribe<QuizAnswerSubmittedEvent>(OnQuizAnswerSubmitted);
        EventBus.Unsubscribe<AudioVolumeChangedEvent>(OnAudioVolumeChanged);
    }

    /// <summary>
    /// 從 SaveManager 讀取持久化設定並初始化 AudioMixer 參數
    /// </summary>
    private void InitializeAudioLevels()
    {
        if (audioMixer == null) return;

        float volume = 1f;
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
        {
            volume = SaveManager.Instance.CurrentSaveData.masterVolume;
        }

        SetMixerVolume(MasterParameter, volume);
        SetMixerVolume(BgmParameter, volume);
        SetMixerVolume(SfxParameter, volume);
    }

    /// <summary>
    /// 播放按鈕通用點擊音效
    /// </summary>
    public void PlayButtonSound()
    {
        if (buttonSound != null)
        {
            buttonSound.Play();
        }
    }

    /// <summary>
    /// 播放答題正確音效
    /// </summary>
    public void PlayCorrectAnswerSound()
    {
        if (correctAnswerSound != null)
        {
            correctAnswerSound.Play();
        }
    }

    /// <summary>
    /// 播放答題錯誤音效
    /// </summary>
    public void PlayIncorrectAnswerSound()
    {
        if (incorrectAnswerSound != null)
        {
            incorrectAnswerSound.Play();
        }
    }

    /// <summary>
    /// 將 0.0001 ~ 1.0 的線性音量轉換為分貝 (dB) 寫入 AudioMixer
    /// </summary>
    public void SetMixerVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null || string.IsNullOrEmpty(parameterName)) return;

        float clamped = Mathf.Clamp(normalizedVolume, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(parameterName, dB);
    }

    private void OnButtonClicked(ButtonClickedEvent evt)
    {
        PlayButtonSound();
    }

    private void OnQuizAnswerSubmitted(QuizAnswerSubmittedEvent evt)
    {
        if (evt.IsCorrect)
        {
            PlayCorrectAnswerSound();
        }
        else
        {
            PlayIncorrectAnswerSound();
        }
    }

    private void OnAudioVolumeChanged(AudioVolumeChangedEvent evt)
    {
        SetMixerVolume(evt.ParameterName, evt.NormalizedVolume);
    }
}
