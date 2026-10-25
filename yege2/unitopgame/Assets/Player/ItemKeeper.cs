using UnityEngine;

/// <summary>
/// 教程 8.4.7「制作管理持有的道具的机制」+ 10.2.2「保存和读取道具数量」。
/// 钥匙与箭矢的数量用 static 变量保存，跨场景保持；再用 PlayerPrefs 持久化。
/// </summary>
public class ItemKeeper : MonoBehaviour
{
    public static int hasArrows = 0;   // 持有的箭矢数量
    public static int hasKeys = 0;     // 持有的钥匙数量

    void Start()
    {
        hasArrows = PlayerPrefs.GetInt("Arrows", 0);
        hasKeys = PlayerPrefs.GetInt("Keys", 0);
    }

    /// <summary>保存道具数量（由 RoomManager 的 ChangeScene 调用）。</summary>
    public static void SaveItem()
    {
        PlayerPrefs.SetInt("Arrows", hasArrows);
        PlayerPrefs.SetInt("Keys", hasKeys);
    }
}
