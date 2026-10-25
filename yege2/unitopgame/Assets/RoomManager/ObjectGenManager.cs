using UnityEngine;

/// <summary>
/// 教程 10.3.11「实现自动配置道具的机制」。
/// 常时检查箭矢数量，为 0 时在随机的 ObjGen 位置补充箭矢，避免箭射光后卡关。
/// 教程用 Update 每帧全场景查找，这里加入 0.5s 节流以降低移动端开销。
/// </summary>
public class ObjectGenManager : MonoBehaviour
{
    const float CHECK_INTERVAL = 0.5f;

    ObjectGenPoint[] objGens;
    float timer = 0.0f;

    void Start()
    {
        objGens = FindObjectsOfType<ObjectGenPoint>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < CHECK_INTERVAL)
        {
            return;
        }
        timer = 0.0f;

        if (PlayerController.gameState != "playing")
        {
            return;
        }

        // 场景中还留有箭矢道具的话就不补充
        ItemData[] items = FindObjectsOfType<ItemData>();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].type == ItemType.Arrow)
            {
                return;
            }
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || objGens == null || objGens.Length == 0)
        {
            return;
        }

        // 持有的箭矢为 0 时才在随机位置补充
        if (ItemKeeper.hasArrows == 0)
        {
            int index = Random.Range(0, objGens.Length);
            if (objGens[index] != null)
            {
                objGens[index].ObjectCreate();
            }
        }
    }
}
