using System;
using System.IO;
using UnityEngine;

/// <summary>
/// ============================================================================
/// 檔案名稱: JsonSaveService.cs
/// 所屬模組: Save
/// 責任描述: 基於原生 C# File I/O 與 JsonUtility 之通用 JSON 磁碟持久化服務。
///           實作 ISaveService 泛型介面契約，提供跨平台可靠存取。
/// 架構分層: Save / Implementation
/// 聯動關係:
///   - 介面實作: ISaveService
/// ============================================================================
/// </summary>
public class JsonSaveService : ISaveService
{
    private string GetPath(string key) => Path.Combine(Application.persistentDataPath, $"{key}.json");

    public void Save<T>(string key, T data)
    {
        if (data == null) return;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetPath(key), json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JsonSaveService] Failed to save {key}: {ex.Message}");
        }
    }

    public T Load<T>(string key)
    {
        string path = GetPath(key);
        if (!File.Exists(path)) return default;
        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JsonSaveService] Failed to load {key}: {ex.Message}");
            return default;
        }
    }

    public bool Exists(string key)
    {
        return File.Exists(GetPath(key));
    }

    public void Delete(string key)
    {
        string path = GetPath(key);
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveService] Failed to delete {key}: {ex.Message}");
            }
        }
    }
}
