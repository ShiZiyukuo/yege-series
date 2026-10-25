using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 教程 9.1.2「制作用于房间管理的游戏物体」+ 10.2.4「在切换场景时保存数据」+
/// 10.4.3「播放BGM」。
/// </summary>
public class RoomManager : MonoBehaviour
{
    /// <summary>玩家进出的出入口编号（static：跨场景保持）。</summary>
    public static int doorNumber = 0;

    void Start()
    {
        // 找到与进入时相同编号的出入口，把玩家角色放到它的位置上
        GameObject[] enters = GameObject.FindGameObjectsWithTag("Exit");
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (doorNumber != 0 && player != null)
        {
            for (int i = 0; i < enters.Length; i++)
            {
                Exit exit = enters[i].GetComponent<Exit>();
                if (exit != null && exit.doorNumber == doorNumber)
                {
                    Vector2 offset = OffsetOf(exit.direction);
                    player.transform.position = (Vector2)enters[i].transform.position + offset;

                    PlayerController plcnt = player.GetComponent<PlayerController>();
                    if (plcnt != null)
                    {
                        plcnt.FaceDirection(exit.direction);
                    }
                    break;
                }
            }
        }

        // 刚进入房间时短暂忽略出入口触发
        Exit.lockUntil = Time.time + 0.6f;

        // 教程 10.4.3：按当前场景切换 BGM（boss 关播放 boss 战 BGM）
        string nowScene = PlayerPrefs.GetString("LastScene", "");
        if (string.IsNullOrEmpty(nowScene))
        {
            nowScene = SceneManager.GetActiveScene().name;
        }
        if (nowScene == UniTopSceneNames.BossStage)
        {
            SoundManager.PlayBgm(BGMType.Boss);
        }
        else
        {
            SoundManager.PlayBgm(BGMType.Game);
        }
    }

    /// <summary>玩家角色从出入口出来的方向 → 相对出入口的偏移。</summary>
    static Vector2 OffsetOf(ExitDirection direction)
    {
        const float dist = 1.6f;
        switch (direction)
        {
            case ExitDirection.up: return new Vector2(0.0f, dist);
            case ExitDirection.left: return new Vector2(-dist, 0.0f);
            case ExitDirection.right: return new Vector2(dist, 0.0f);
            default: return new Vector2(0.0f, -dist);
        }
    }

    /// <summary>切换场景，并保存当前房间的配置数据（教程 10.2.4）。</summary>
    public static void ChangeScene(string sceneName, int doornum, ExitDirection direction)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        // 保存当前房间的配置数据（已开的门、宝箱、拾取的道具、击倒的敌人）
        string nowScene = PlayerPrefs.GetString("LastScene", "");
        if (!string.IsNullOrEmpty(nowScene))
        {
            SaveDataManager.SaveArrangeData(nowScene);
        }

        PlayerPrefs.SetString("LastScene", sceneName);
        PlayerPrefs.SetInt("LastDoor", doornum);
        doorNumber = doornum;

        ItemKeeper.SaveItem();
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }
}
