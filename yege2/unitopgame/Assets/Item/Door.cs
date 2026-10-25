using UnityEngine;

/// <summary>
/// 教程 9.1.5「制作门」+ 10.2.4「保存打开的门」。
/// 玩家持有钥匙时接触门即可打开（删除门自身）。
/// </summary>
public class Door : MonoBehaviour
{
    public int arrangeId = 0;   // 保存配置数据用的编号

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag != "Player")
        {
            return;
        }

        if (ItemKeeper.hasKeys > 0)
        {
            ItemKeeper.hasKeys -= 1;
            SaveDataManager.SetArrangeId(arrangeId, gameObject.tag);
            SoundManager.SEPlay(SEType.DoorOpen);
            Destroy(gameObject);
        }
        else
        {
            // 门锁着（教程 10.4 提到门锁着时播放 SE）
            SoundManager.SEPlay(SEType.DoorClosed);
        }
    }
}
