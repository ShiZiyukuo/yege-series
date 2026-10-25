using UnityEngine;

/// <summary>
/// 教程 10.2.4「使用JSON记录房间的配置」。
/// 用 JsonUtility 把配置数据转为 JSON 文本，再用 PlayerPrefs 以场景名为键保存。
/// </summary>
public class SaveDataManager : MonoBehaviour
{
    public static SaveDataList arrangeDataList;

    void Start()
    {
        // 初始化空数据
        arrangeDataList = new SaveDataList();
        arrangeDataList.saveDatas = new SaveData[0];

        string sceneName = PlayerPrefs.GetString("LastScene", "");
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        string json = PlayerPrefs.GetString(sceneName, "");
        if (string.IsNullOrEmpty(json))
        {
            return;   // 没有存档数据
        }

        arrangeDataList = JsonUtility.FromJson<SaveDataList>(json);
        if (arrangeDataList == null || arrangeDataList.saveDatas == null)
        {
            arrangeDataList = new SaveDataList();
            arrangeDataList.saveDatas = new SaveData[0];
            return;
        }

        // 复原上一状态：已开的门、已拿取的道具、已击倒的敌人重新删除；已开的宝箱改为开启状态
        for (int i = 0; i < arrangeDataList.saveDatas.Length; i++)
        {
            SaveData data = arrangeDataList.saveDatas[i];
            if (data == null || string.IsNullOrEmpty(data.objTag))
            {
                continue;
            }

            GameObject[] objs = GameObject.FindGameObjectsWithTag(data.objTag);
            for (int j = 0; j < objs.Length; j++)
            {
                if (data.objTag == "Door")
                {
                    Door door = objs[j].GetComponent<Door>();
                    if (door != null && door.arrangeId == data.arrangeId)
                    {
                        Destroy(objs[j]);
                    }
                }
                else if (data.objTag == "ItemBox")
                {
                    ItemBox itemBox = objs[j].GetComponent<ItemBox>();
                    if (itemBox != null && itemBox.arrangeId == data.arrangeId)
                    {
                        itemBox.isClosed = false;
                        SpriteRenderer renderer = objs[j].GetComponent<SpriteRenderer>();
                        if (renderer != null && itemBox.openImage != null)
                        {
                            renderer.sprite = itemBox.openImage;
                        }
                    }
                }
                else if (data.objTag == "Item")
                {
                    ItemData item = objs[j].GetComponent<ItemData>();
                    if (item != null && item.arrangeId == data.arrangeId)
                    {
                        Destroy(objs[j]);
                    }
                }
                else if (data.objTag == "Enemy")
                {
                    EnemyController enemy = objs[j].GetComponent<EnemyController>();
                    if (enemy != null && enemy.arrangeId == data.arrangeId)
                    {
                        Destroy(objs[j]);
                    }
                }
            }
        }
    }

    /// <summary>记录一个配置物。arrangeId 为 0 或标签为空时不记录。</summary>
    public static void SetArrangeId(int arrangeId, string objTag)
    {
        if (arrangeId == 0 || string.IsNullOrEmpty(objTag))
        {
            return;
        }
        if (arrangeDataList == null)
        {
            arrangeDataList = new SaveDataList();
            arrangeDataList.saveDatas = new SaveData[0];
        }

        SaveData[] newDatas = new SaveData[arrangeDataList.saveDatas.Length + 1];
        for (int i = 0; i < arrangeDataList.saveDatas.Length; i++)
        {
            newDatas[i] = arrangeDataList.saveDatas[i];
        }

        SaveData data = new SaveData();
        data.arrangeId = arrangeId;
        data.objTag = objTag;
        newDatas[arrangeDataList.saveDatas.Length] = data;
        arrangeDataList.saveDatas = newDatas;
    }

    /// <summary>以场景名为键保存 JSON 数据。</summary>
    public static void SaveArrangeData(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }
        if (arrangeDataList != null && arrangeDataList.saveDatas != null)
        {
            string json = JsonUtility.ToJson(arrangeDataList);
            PlayerPrefs.SetString(sceneName, json);
        }
    }
}
