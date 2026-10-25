using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 冒烟校验：编译通过后检查四个场景与所有预制体的关键组件是否齐全。
/// 重点覆盖两个已知易错点：EventSystem 缺失、Button 的 interactable 被绕过。
/// </summary>
public static class UniTopSmokeTest
{
    const string PFB = "Assets/Prefab/";
    const string SCN = "Assets/Scenes/";

    [MenuItem("UniTop/3. 冒烟校验")]
    public static void Verify()
    {
        List<string> errors = new List<string>();

        CheckPrefabs(errors);

        CheckScene(UniTopSceneNames.Title, errors, false);
        CheckScene(UniTopSceneNames.WorldMap, errors, true);
        CheckScene(UniTopSceneNames.Dungeon1, errors, true);
        CheckScene(UniTopSceneNames.BossStage, errors, true);

        CheckBuildSettings(errors);
        CheckTagsAndLayers(errors);

        if (errors.Count == 0)
        {
            Debug.Log("[UniTop][冒烟] 全部校验通过");
            EditorApplication.Exit(0);
        }
        else
        {
            foreach (string e in errors)
            {
                Debug.LogError("[UniTop][冒烟] " + e);
            }
            Debug.LogError("[UniTop][冒烟] 共 " + errors.Count + " 处问题");
            EditorApplication.Exit(1);
        }
    }

    // ===================================================================

    static void CheckPrefabs(List<string> errors)
    {
        string[] prefabs =
        {
            "Player", "Arrow", "Bow", "Exit", "RoomManager", "Door",
            "Item_Key", "Item_Arrow", "Item_Life", "ItemBox", "Enemy",
            "Boss", "Bullet", "SoundManager", "Canvas", "ObjGen"
        };
        foreach (string name in prefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + name + ".prefab");
            if (prefab == null)
            {
                errors.Add("缺少预制体 " + name);
            }
        }

        // 玩家：三件套必须齐全，且弓/箭矢预制已挂上
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Player.prefab");
        if (player != null)
        {
            Require<PlayerController>(player, errors, "Player");
            Require<ItemKeeper>(player, errors, "Player");
            ArrowShoot shoot = Require<ArrowShoot>(player, errors, "Player");
            if (shoot != null)
            {
                if (shoot.bowPrefab == null)
                {
                    errors.Add("Player 的 ArrowShoot 未设置 Bow Prefab");
                }
                if (shoot.arrowPrefab == null)
                {
                    errors.Add("Player 的 ArrowShoot 未设置 Arrow Prefab");
                }
            }
            if (player.GetComponent<Rigidbody2D>() == null)
            {
                errors.Add("Player 缺少 Rigidbody2D");
            }
            if (player.GetComponent<Animator>() == null || player.GetComponent<Animator>().runtimeAnimatorController == null)
            {
                errors.Add("Player 缺少 Animator 或动画控制器");
            }
        }

        // 声音：BGM 与 SE 必须全部挂上
        GameObject sound = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "SoundManager.prefab");
        if (sound != null)
        {
            SoundManager sm = sound.GetComponent<SoundManager>();
            if (sm == null)
            {
                errors.Add("SoundManager 缺少脚本");
            }
            else
            {
                CheckClip("BGM_title", sm.bgmTitle, errors);
                CheckClip("BGM_game_00", sm.bgmGame, errors);
                CheckClip("BGM_boss", sm.bgmBoss, errors);
                CheckClip("se_shoot", sm.seShoot, errors);
                CheckClip("se_killed", sm.seKilled, errors);
                CheckClip("se_damage", sm.seDamage, errors);
                CheckClip("se_get", sm.seGet, errors);
                CheckClip("se_doorOpen", sm.seDoorOpen, errors);
                CheckClip("se_doorClosed", sm.seDoorClosed, errors);
                CheckClip("se_bosskilled", sm.seBossKilled, errors);
                CheckClip("se_button", sm.seButton, errors);
                CheckClip("ME_Clear", sm.meClear, errors);
                CheckClip("ME_GameOver", sm.meGameOver, errors);
            }
        }

        // UI：HP 四张图与三张状态图必须齐全
        GameObject canvas = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Canvas.prefab");
        if (canvas != null)
        {
            UIManager ui = canvas.GetComponent<UIManager>();
            if (ui == null)
            {
                errors.Add("Canvas 缺少 UIManager");
            }
            else
            {
                CheckSprite("hp0Spr", ui.hp0Spr, errors);
                CheckSprite("hp1Spr", ui.hp1Spr, errors);
                CheckSprite("hp2Spr", ui.hp2Spr, errors);
                CheckSprite("hp3Spr", ui.hp3Spr, errors);
                CheckSprite("gameStartSpr", ui.gameStartSpr, errors);
                CheckSprite("gameOverSpr", ui.gameOverSpr, errors);
                CheckSprite("gameClearSpr", ui.gameClearSpr, errors);
                if (ui.virtualPad == null || !ui.virtualPad.is4DPad)
                {
                    errors.Add("VirtualPad 未设置或未开启 Is 4D Pad");
                }
                else
                {
                    // 摇杆要挂在底盘上（整块区域可触摸），不能只挂在 120×120 的手柄上，
                    // 否则手机上手指很难精确按住
                    if (ui.virtualPad.tab == null)
                    {
                        errors.Add("VirtualPad 未设置手柄引用 tab");
                    }
                    RectTransform padRect = ui.virtualPad.GetComponent<RectTransform>();
                    if (padRect != null && padRect.sizeDelta.x < 200f)
                    {
                        errors.Add("VirtualPad 的触摸区只有 " + padRect.sizeDelta + "，手机上太小，应挂在底盘上");
                    }
                }
                if (ui.attackButton == null)
                {
                    errors.Add("UIManager 未设置攻击按钮");
                }
                if (ui.resetButton == null || ui.resetButton.GetComponent<UnityEngine.UI.Button>() == null)
                {
                    errors.Add("RETRY 按钮不是 Button（会绕过 interactable）");
                }
            }
        }

        // Boss：子弹预制与动画事件
        GameObject boss = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Boss.prefab");
        if (boss != null)
        {
            BossController bc = boss.GetComponent<BossController>();
            if (bc == null)
            {
                errors.Add("Boss 缺少 BossController");
            }
            else if (bc.bulletPrefab == null)
            {
                errors.Add("Boss 未设置子弹预制");
            }
            if (boss.transform.Find("gate") == null)
            {
                errors.Add("Boss 缺少发射口 gate");
            }
        }
    }

    static void CheckScene(string sceneName, List<string> errors, bool isGameScene)
    {
        string path = SCN + sceneName + ".unity";
        if (!System.IO.File.Exists(path))
        {
            errors.Add("缺少场景 " + path);
            return;
        }

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();

        // EventSystem：没有它所有 uGUI 点击与拖拽都会失效
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            errors.Add(sceneName + " 缺少 EventSystem");
        }

        // 标题界面没有玩家角色，只有关卡场景才需要
        if (isGameScene)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                errors.Add(sceneName + " 场景中没有带 Player 标签的对象");
            }
            else
            {
                Require<PlayerController>(playerObj, errors, sceneName + "/Player");
                Require<ItemKeeper>(playerObj, errors, sceneName + "/Player");
            }
        }

        if (GameObject.Find("SoundManager") == null)
        {
            errors.Add(sceneName + " 缺少 SoundManager");
        }

        Camera main = Camera.main;
        if (main == null)
        {
            errors.Add(sceneName + " 没有 Main Camera");
        }
        else if (isGameScene && main.GetComponent<CameraManager>() == null)
        {
            errors.Add(sceneName + " 的摄像机缺少 CameraManager");
        }

        if (!isGameScene)
        {
            return;
        }

        GameObject roomManager = GameObject.Find("RoomManager");
        if (roomManager == null)
        {
            errors.Add(sceneName + " 缺少 RoomManager");
        }
        else
        {
            Require<RoomManager>(roomManager, errors, sceneName + "/RoomManager");
            Require<SaveDataManager>(roomManager, errors, sceneName + "/RoomManager");
        }

        if (Object.FindObjectOfType<Tilemap>() == null)
        {
            errors.Add(sceneName + " 缺少贴片地图");
        }

        UIManager ui = Object.FindObjectOfType<UIManager>();
        if (ui == null)
        {
            errors.Add(sceneName + " 缺少 UIManager");
        }
        else
        {
            if (ui.retrySceneName != sceneName)
            {
                errors.Add(sceneName + " 的 Retry Scene Name 未设置为本场景");
            }
            Canvas canvas = ui.GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
            {
                errors.Add(sceneName + " 的 Canvas 未设置 Render Camera");
            }
        }

        Exit[] exits = Object.FindObjectsOfType<Exit>();
        if (exits.Length == 0)
        {
            errors.Add(sceneName + " 没有配置任何出入口");
        }
        foreach (Exit exit in exits)
        {
            if (exit.doorNumber == 0)
            {
                errors.Add(sceneName + " 中存在 doorNumber 为 0 的出入口");
            }
            CheckWalkable(sceneName, exit.gameObject.name + "(doorNumber=" + exit.doorNumber + ")",
                exit.transform.position, errors, Object.FindObjectOfType<TilemapCollider2D>());
        }

        // 教程 8.3.6：贴片地图必须真的有碰撞体积，否则玩家会直接穿墙。
        // 只检查「组件存在」不够（CompositeCollider2D 会生成不出几何），
        // 必须用 OverlapPoint 实测物理结果。
        TilemapCollider2D mapCollider = Object.FindObjectOfType<TilemapCollider2D>();
        if (mapCollider == null)
        {
            errors.Add(sceneName + " 的贴片地图缺少 Tilemap Collider 2D");
        }
        else
        {
            // 三张地图的左下角 (0,0) 一定是海或石墙，必须能挡住玩家
            if (!mapCollider.OverlapPoint(new Vector2(0.5f, 0.5f)))
            {
                errors.Add(sceneName + " 的贴片地图没有生成碰撞体积（(0.5,0.5) 处应被挡住）");
            }
        }

        // 玩家出生点必须落在可通行的位置，否则一开局就被卡在墙里
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CheckWalkable(sceneName, "Player", player.transform.position, errors, mapCollider);
        }

        CheckGating(sceneName, errors, mapCollider);
        CheckBossFiringArc(sceneName, errors, mapCollider);
        CheckBossBlocksPath(sceneName, errors, mapCollider);
    }

    /// <summary>
    /// 教程 10.3.10：boss 的碰撞体积必须挡住它后面的宝箱，玩家不能绕过。
    /// 检查 boss 正后方一格及其左右各一格 —— 凡是贴片可通行的，都必须被 boss 的碰撞体覆盖。
    /// </summary>
    static void CheckBossBlocksPath(string sceneName, List<string> errors, TilemapCollider2D mapCollider)
    {
        BossController boss = Object.FindObjectOfType<BossController>();
        if (boss == null || mapCollider == null)
        {
            return;
        }
        CircleCollider2D bossCollider = boss.GetComponent<CircleCollider2D>();
        if (bossCollider == null)
        {
            return;
        }

        Vector2 behind = (Vector2)boss.transform.position + new Vector2(0.0f, 2.0f);
        for (int dx = -1; dx <= 1; dx++)
        {
            Vector2 point = new Vector2(Mathf.Floor(behind.x) + 0.5f + dx, Mathf.Floor(behind.y) + 0.5f);
            if (mapCollider.OverlapPoint(point))
            {
                continue;   // 本来就是墙，不需要 boss 去堵
            }
            if (!bossCollider.OverlapPoint(point))
            {
                errors.Add(sceneName + " 的 boss 没有堵住身后的通路 " + point
                    + "，玩家能绕过它直接拿到宝箱");
            }
        }
    }

    /// <summary>
    /// boss 的发射口必须处在开阔位置。
    /// 之前 boss 被放进窄通道，它的发射口 gate 被两侧墙体夹住，子弹一出膛就撞墙，
    /// 攻击范围被压缩到只剩正下方一小片。这里向 12 个方向采样射程来验证。
    /// </summary>
    static void CheckBossFiringArc(string sceneName, List<string> errors, TilemapCollider2D mapCollider)
    {
        BossController boss = Object.FindObjectOfType<BossController>();
        if (boss == null || boss.gate == null || mapCollider == null)
        {
            return;
        }

        Vector2 origin = boss.gate.transform.position;
        const float REACH = 3.0f;
        int open = 0;
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30.0f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            bool blocked = false;
            for (float d = 0.5f; d <= REACH; d += 0.25f)
            {
                if (mapCollider.OverlapPoint(origin + dir * d))
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked)
            {
                open++;
            }
        }

        if (open < 8)
        {
            errors.Add(sceneName + " 的 boss 发射口被地形夹住：12 个方向中只有 " + open
                + " 个射程达到 3 格（应 ≥8），攻击角度会被严重限制");
        }
    }

    /// <summary>
    /// 用洪水填充验证关卡的「门」逻辑，只看坐标是抓不住这类问题的：
    /// 1) 门必须真的封死一条通路（把门当障碍，可达范围必须变小），否则玩家能绕过去；
    /// 2) 装钥匙的宝箱必须在开门之前就能走到，否则钥匙永远拿不到 —— 那样是死锁。
    /// </summary>
    static void CheckGating(string sceneName, List<string> errors, TilemapCollider2D mapCollider)
    {
        Tilemap tilemap = Object.FindObjectOfType<Tilemap>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (tilemap == null || player == null || mapCollider == null)
        {
            return;
        }

        HashSet<Vector3Int> doorCells = new HashSet<Vector3Int>();
        foreach (Door door in Object.FindObjectsOfType<Door>())
        {
            doorCells.Add(tilemap.WorldToCell(door.transform.position));
        }
        if (doorCells.Count == 0)
        {
            return;
        }

        HashSet<Vector3Int> locked = FloodFill(tilemap, mapCollider, player.transform.position, doorCells);
        HashSet<Vector3Int> opened = FloodFill(tilemap, mapCollider, player.transform.position, new HashSet<Vector3Int>());

        if (locked.Count == opened.Count)
        {
            errors.Add(sceneName + " 的门没有封住任何通路，玩家可以直接绕过去（门形同虚设）");
        }

        // 装钥匙的宝箱必须在开门之前就能走到
        foreach (ItemBox box in Object.FindObjectsOfType<ItemBox>())
        {
            if (box.itemPrefab == null)
            {
                continue;
            }
            ItemData data = box.itemPrefab.GetComponent<ItemData>();
            if (data == null || data.type != ItemType.Key)
            {
                continue;
            }
            Vector3Int cell = tilemap.WorldToCell(box.transform.position);
            if (!locked.Contains(cell))
            {
                errors.Add(sceneName + " 的钥匙宝箱被门挡在后面 " + cell + "，玩家拿不到钥匙，这一关无法通过");
            }
        }
    }

    /// <summary>从玩家位置开始洪水填充，返回所有可到达的格子；blocked 中的格子视为障碍。</summary>
    static HashSet<Vector3Int> FloodFill(Tilemap tilemap, TilemapCollider2D collider,
        Vector3 start, HashSet<Vector3Int> blocked)
    {
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Vector3Int startCell = tilemap.WorldToCell(start);
        if (blocked.Contains(startCell) || !IsWalkable(collider, startCell))
        {
            return visited;
        }

        BoundsInt bounds = tilemap.cellBounds;
        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(startCell);
        visited.Add(startCell);

        while (queue.Count > 0)
        {
            Vector3Int cell = queue.Dequeue();
            foreach (Vector3Int dir in dirs)
            {
                Vector3Int next = cell + dir;
                if (visited.Contains(next) || blocked.Contains(next))
                {
                    continue;
                }
                if (!bounds.Contains(next) || !IsWalkable(collider, next))
                {
                    continue;
                }
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return visited;
    }

    /// <summary>格子中心是否被贴片碰撞体挡住。</summary>
    static bool IsWalkable(TilemapCollider2D collider, Vector3Int cell)
    {
        return !collider.OverlapPoint(new Vector2(cell.x + 0.5f, cell.y + 0.5f));
    }

    /// <summary>检查某个世界坐标既要有贴片、又不能被碰撞体积挡住。</summary>
    static void CheckWalkable(string sceneName, string who, Vector3 position, List<string> errors,
        TilemapCollider2D mapCollider)
    {
        Tilemap tilemap = Object.FindObjectOfType<Tilemap>();
        if (tilemap == null)
        {
            return;
        }

        Vector3Int cell = tilemap.WorldToCell(position);
        TileBase tileBase = tilemap.GetTile(cell);
        if (tileBase == null)
        {
            errors.Add(sceneName + " 的 " + who + " 位于地图空白处 " + cell);
            return;
        }

        if (mapCollider != null && mapCollider.OverlapPoint(position))
        {
            errors.Add(sceneName + " 的 " + who + " 被贴片碰撞体积挡住 " + cell);
        }
    }

    static void CheckBuildSettings(List<string> errors)
    {
        string[] expected =
        {
            SCN + UniTopSceneNames.Title + ".unity",
            SCN + UniTopSceneNames.WorldMap + ".unity",
            SCN + UniTopSceneNames.Dungeon1 + ".unity",
            SCN + UniTopSceneNames.BossStage + ".unity"
        };
        List<string> actual = new List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.enabled)
            {
                actual.Add(s.path);
            }
        }
        foreach (string e in expected)
        {
            if (!actual.Contains(e))
            {
                errors.Add("构建列表中缺少 " + e);
            }
        }
        if (actual.Count != expected.Length)
        {
            errors.Add("构建场景数量应为 " + expected.Length + "，实际为 " + actual.Count);
        }
    }

    static void CheckTagsAndLayers(List<string> errors)
    {
        string[] tags = { "Exit", "Door", "ItemBox", "Item", "Enemy", "Arrow" };
        foreach (string tag in tags)
        {
            bool found = false;
            foreach (string t in UnityEditorInternal.InternalEditorUtility.tags)
            {
                if (t == tag)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                errors.Add("缺少标签 " + tag);
            }
        }

        string[] layers = { "Player", "Arrow", "Enemy", "Bullet", "Ground" };
        foreach (string layer in layers)
        {
            if (LayerMask.NameToLayer(layer) < 0)
            {
                errors.Add("缺少层 " + layer);
            }
        }
    }

    // ------------------------------------------------------------------

    static T Require<T>(GameObject go, List<string> errors, string owner) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            errors.Add(owner + " 缺少组件 " + typeof(T).Name);
        }
        return component;
    }

    static void CheckClip(string name, AudioClip clip, List<string> errors)
    {
        if (clip == null)
        {
            errors.Add("SoundManager 的 " + name + " 未设置");
        }
    }

    static void CheckSprite(string name, Sprite sprite, List<string> errors)
    {
        if (sprite == null)
        {
            errors.Add("UIManager 的 " + name + " 未设置");
        }
    }
}
