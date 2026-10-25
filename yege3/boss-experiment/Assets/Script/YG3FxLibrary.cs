using UnityEngine;

/// <summary>
/// 战斗资产库：由 YG3Builder 在每个场景创建并注入模板引用。
/// 运行时脚本一律从这里拿预制体（编辑器程序集不能被运行时代码引用）。
/// </summary>
public class YG3FxLibrary : MonoBehaviour
{
    public static YG3FxLibrary Instance;

    [Header("特效")]
    public GameObject slash;        // 挥剑弧光
    public GameObject parry;        // 弹反光环
    public GameObject spark;        // 命中火花

    [Header("弹体")]
    public GameObject arrow;
    public GameObject needle;
    public GameObject shockwave;

    [Header("单位")]
    public GameObject hedgehog;

    void Awake()
    {
        Instance = this;
        YG3Fx.BindPrefabs(spark, parry);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>取活动实例（模板保持 inactive）。</summary>
    public static GameObject Spawn(GameObject template, Vector3 pos, Quaternion rot)
    {
        if (template == null)
        {
            return null;
        }
        GameObject go = Instantiate(template, pos, rot);
        go.SetActive(true);
        return go;
    }
}
