using UnityEngine;

/// <summary>道具的种类（教程 9.2.2 的枚举类型）。</summary>
public enum ItemType { Arrow, Key, Life }

/// <summary>
/// 教程 9.2.2「编写用于道具管理的脚本」+ 10.2.3「生命道具恢复HP」+
/// 10.2.4「保存已拿取的道具」。
/// </summary>
public class ItemData : MonoBehaviour
{
    public ItemType type = ItemType.Arrow;   // 道具种类
    public int count = 1;                    // 箭矢时的获取数量
    public int arrangeId = 0;                // 保存配置数据用的编号

    bool isPicked = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isPicked || other.tag != "Player")
        {
            return;
        }
        isPicked = true;

        switch (type)
        {
            case ItemType.Arrow:
                ItemKeeper.hasArrows += count;
                break;
            case ItemType.Key:
                ItemKeeper.hasKeys += count;
                break;
            case ItemType.Life:
                // 教程 10.2.3：拾取生命道具后保存玩家的 HP
                PlayerController.hp += 1;
                if (PlayerController.hp > 3)
                {
                    PlayerController.hp = 3;
                }
                PlayerPrefs.SetInt("PlayerHP", PlayerController.hp);
                break;
        }

        SaveDataManager.SetArrangeId(arrangeId, gameObject.tag);
        SoundManager.SEPlay(SEType.Get);

        // 拾取特效：向上弹起，0.5s 后删除
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        Rigidbody2D rbody = GetComponent<Rigidbody2D>();
        if (rbody != null)
        {
            rbody.velocity = Vector2.zero;
        }
        transform.position = transform.position + new Vector3(0.0f, 0.5f, 0.0f);
        Destroy(gameObject, 0.5f);
    }
}
