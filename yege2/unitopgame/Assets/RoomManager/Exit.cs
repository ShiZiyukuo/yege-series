using UnityEngine;

/// <summary>出入口的方向（教程 9.1.1 的 enum ExitDirection）。</summary>
public enum ExitDirection { right, left, down, up }

/// <summary>
/// 教程 9.1.1「制作用于出入口的游戏物体和脚本」。
/// 玩家接触后切换场景；doorNumber 为 100 时判定为游戏通关（教程 10.3.12）。
/// </summary>
public class Exit : MonoBehaviour
{
    public string sceneName = "";                                  // 要切换到的场景名
    public int doorNumber = 0;                                     // 出入口编号（同一场景内不重复）
    public ExitDirection direction = ExitDirection.down;           // 玩家从出入口的哪个方向出来

    /// <summary>刚被放置到出入口上时，在这之前忽略触发，避免立刻又被传送回去。</summary>
    public static float lockUntil = 0.0f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < lockUntil)
        {
            return;
        }
        if (other.tag != "Player")
        {
            return;
        }

        // 教程 10.3.12：boss 房间深处的门编号为 100，进门即通关
        if (doorNumber == 100)
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.GameClear();
            }
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        RoomManager.ChangeScene(sceneName, doorNumber, direction);
    }
}
