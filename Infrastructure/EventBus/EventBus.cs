using System;
using System.Collections.Generic;

/// <summary>
/// ============================================================================
/// 檔案名稱: EventBus.cs
/// 所屬模組: Infrastructure
/// 責任描述: 全域事件匯流排。使用標準 Dictionary 字典儲存事件與回呼方法，
///           邏輯透明直觀，提供訂閱 (Subscribe)、退訂 (Unsubscribe) 與發布 (Publish) 功能。
/// 架構分層: Infrastructure / EventBus
/// 聯動關係:
///   - 下游消費: 供全專案所有領域 (Gameplay, UI, Save, Audio) 進行事件解耦通訊
/// 邊界處理: 透過 ContainsKey 進行安全檢查，使用標準 C# 委派加減 (+/-)
/// ============================================================================
/// </summary>
public static class EventBus
{
    // 儲存事件型別與對應回呼委派的字典表
    private static readonly Dictionary<Type, Delegate> eventTable = new Dictionary<Type, Delegate>();

    /// <summary>
    /// 訂閱事件：將回呼方法註冊至字典中
    /// </summary>
    /// <typeparam name="T">事件型別</typeparam>
    /// <param name="listener">事件觸發時執行的回呼方法</param>
    public static void Subscribe<T>(Action<T> listener)
    {
        if (listener == null) return;

        Type type = typeof(T);

        if (eventTable.ContainsKey(type))
        {
            // 字典已存在此事件，使用 + 運算子串接新的監聽方法
            eventTable[type] = (Action<T>)eventTable[type] + listener;
        }
        else
        {
            // 首次註冊此事件，直接存入字典
            eventTable[type] = listener;
        }
    }

    /// <summary>
    /// 取消訂閱：將回呼方法從字典中移除
    /// </summary>
    /// <typeparam name="T">事件型別</typeparam>
    /// <param name="listener">欲移除的回呼方法</param>
    public static void Unsubscribe<T>(Action<T> listener)
    {
        if (listener == null) return;

        Type type = typeof(T);

        if (eventTable.ContainsKey(type))
        {
            // 使用 - 運算子移除監聽方法
            Action<T> currentDelegate = (Action<T>)eventTable[type] - listener;

            if (currentDelegate == null)
            {
                eventTable.Remove(type);
            }
            else
            {
                eventTable[type] = currentDelegate;
            }
        }
    }

    /// <summary>
    /// 發布廣播：從字典找出該事件的所有監聽者並執行
    /// </summary>
    /// <typeparam name="T">事件型別</typeparam>
    /// <param name="eventData">傳遞的事件資料實例</param>
    public static void Publish<T>(T eventData)
    {
        Type type = typeof(T);

        if (eventTable.ContainsKey(type))
        {
            Action<T> callback = (Action<T>)eventTable[type];
            callback?.Invoke(eventData);
        }
    }

    /// <summary>
    /// 清空所有事件監聽者 (供場景重置或測試時調用)
    /// </summary>
    public static void Clear()
    {
        eventTable.Clear();
    }
}
