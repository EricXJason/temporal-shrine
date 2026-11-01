/// <summary>
/// ============================================================================
/// 檔案名稱: IAudioService.cs
/// 所屬模組: Audio
/// 責任描述: 定義全域音訊與混音器管線之服務行為契約 (Interface)。
/// 架構分層: Audio / Abstraction
/// 聯動關係:
///   - 下游消費: 供 AudioManager 實作，供 UI 與交互系統調用音效與音量控制
/// 邊界處理: 規範線性音量與各類通用回饋音效播放標準
/// ============================================================================
/// </summary>
public interface IAudioService
{
    void PlayButtonSound();
    void PlayCorrectAnswerSound();
    void PlayIncorrectAnswerSound();
    void SetMixerVolume(string parameterName, float normalizedVolume);
}
