using UnityEngine;

/// <summary>
/// 教程 9.2.4「编写用于宝箱管理的脚本」+ 10.2.4「保存开启的宝箱」。
/// </summary>
public class ItemBox : MonoBehaviour
{
    public Sprite openImage;         // 开启后的精灵
    public GameObject itemPrefab;    // 从宝箱中出现的道具预制
    public bool isClosed = true;     // 是否处于关闭状态
    public int arrangeId = 0;        // 保存配置数据用的编号

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag != "Player")
        {
            return;
        }
        if (!isClosed)
        {
            return;
        }

        isClosed = false;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && openImage != null)
        {
            renderer.sprite = openImage;
        }

        // 空箱子（未设置预制）只是打开而已
        if (itemPrefab != null)
        {
            Instantiate(itemPrefab, transform.position, Quaternion.identity);
        }

        SoundManager.SEPlay(SEType.DoorOpen);
        SaveDataManager.SetArrangeId(arrangeId, gameObject.tag);
    }
}
