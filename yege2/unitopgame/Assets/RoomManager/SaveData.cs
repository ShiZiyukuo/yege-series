/// <summary>
/// 教程 10.2.4「使用JSON记录房间的配置」：
/// 记录一个配置物（门 / 宝箱 / 道具 / 敌人）。
/// </summary>
[System.Serializable]
public class SaveData
{
    public int arrangeId = 0;    // 识别配置物的数值
    public string objTag = "";   // 配置物的标签（类型）
}

/// <summary>包含多个配置物的数组，用于整体转换为 JSON。</summary>
[System.Serializable]
public class SaveDataList
{
    public SaveData[] saveDatas;
}
