using UnityEngine;

/// <summary>
/// 教程 10.3.11「实现自动配置道具的机制」中的对象生成器。
/// </summary>
public class ObjectGenPoint : MonoBehaviour
{
    public GameObject objPrefab;   // 要生成的游戏物体的预制（箭矢）

    /// <summary>在当前位置从预制生成游戏物体。</summary>
    public void ObjectCreate()
    {
        if (objPrefab == null)
        {
            return;
        }
        Instantiate(objPrefab, transform.position, Quaternion.identity);
    }
}
