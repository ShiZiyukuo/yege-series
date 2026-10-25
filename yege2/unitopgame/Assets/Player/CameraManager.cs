using UnityEngine;

/// <summary>
/// 教程 8.4.12「制作跟随玩家角色的摄像机」+ 10.3.8「调整boss角色战斗时的摄像机的使用」。
/// 平时以玩家角色为中心；设置了 otherTarget（boss）后取两者的中间点。
/// </summary>
public class CameraManager : MonoBehaviour
{
    /// <summary>boss 角色的游戏物体（仅在 boss 关设置）。</summary>
    public GameObject otherTarget;

    void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;   // 游戏失败后玩家角色消失，摄像机维持在原位置
        }

        Vector3 pos = player.transform.position;
        if (otherTarget != null)
        {
            Vector2 mid = Vector2.Lerp(player.transform.position, otherTarget.transform.position, 0.5f);
            pos = new Vector3(mid.x, mid.y, 0.0f);
        }

        // Z 轴设为 -10，使摄像机显示在玩家角色之上
        transform.position = new Vector3(pos.x, pos.y, -10.0f);
    }
}
