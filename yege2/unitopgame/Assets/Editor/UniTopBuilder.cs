using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// 按《轻松上手2D游戏开发：Unity入门》第 8~10 章的步骤，程序化生成全部内容：
/// 标签层、动画、预制体、4 个场景（Title / WorldMap / dungeon1 / BossStage）与构建设置。
/// 菜单：UniTop / 2. 生成全部内容
/// </summary>
public static class UniTopBuilder
{
    const string MAP = "Assets/Map/";
    const string PLR = "Assets/Player/";
    const string ENM = "Assets/Enemy/";
    const string ITM = "Assets/Item/";
    const string BOS = "Assets/Boss/";
    const string UI = "Assets/UIManager/";
    const string TTL = "Assets/Title/";
    const string SND = "Assets/SoundManager/";
    const string PFB = "Assets/Prefab/";
    const string SCN = "Assets/Scenes/";

    // 层（教程 9.1.1 / 10.3.1 / 10.3.3 需要 Player / Arrow / Enemy / Bullet 四层）
    const int LAYER_PLAYER = 8;
    const int LAYER_ARROW = 9;
    const int LAYER_ENEMY = 10;
    const int LAYER_BULLET = 11;
    const int LAYER_GROUND = 12;

    // 瓦片索引
    const int T_SEA = 0;
    const int T_SHALLOW = 1;
    const int T_SAND = 2;
    const int T_GRASS = 3;
    const int T_THICK = 4;
    const int T_BRICK = 5;
    const int T_BRIDGE = 6;
    const int T_TREE = 7;
    const int T_MOUNT = 8;
    const int T_ENTRANCE = 9;
    const int T_ROCK1 = 10;
    const int T_ROCK2 = 11;
    const int T_ROCK3 = 12;
    const int T_ROCK4 = 13;
    const int T_ROCK5 = 14;
    const int T_FLOOR1 = 15;
    const int T_FLOOR2 = 16;
    const int T_FRAME = 20;

    [MenuItem("UniTop/2. 生成全部内容")]
    public static void BuildAll()
    {
        EnsureTagsAndLayers();
        EnsureLayerCollisions();
        UniTopImportSettings.CreateTiles();

        AnimatorController playerAnime = CreatePlayerAnimator();
        AnimatorController enemyAnime = CreateEnemyAnimator();
        AnimatorController bossAnime = CreateBossAnimator();

        CreatePrefabs(playerAnime, enemyAnime, bossAnime);

        BuildTitleScene();
        BuildWorldMapScene();
        BuildDungeon1Scene();
        BuildBossStageScene();

        ApplyBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UniTop] 全部内容生成完成");
    }

    // ===================================================================
    //  标签与层
    // ===================================================================

    static void EnsureTagsAndLayers()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        SerializedObject so = new SerializedObject(assets[0]);

        SerializedProperty tags = so.FindProperty("tags");
        AddTag(tags, "Exit");
        AddTag(tags, "Door");
        AddTag(tags, "ItemBox");
        AddTag(tags, "Item");
        AddTag(tags, "Enemy");
        AddTag(tags, "Arrow");

        SerializedProperty layers = so.FindProperty("layers");
        SetLayerName(layers, LAYER_PLAYER, "Player");
        SetLayerName(layers, LAYER_ARROW, "Arrow");
        SetLayerName(layers, LAYER_ENEMY, "Enemy");
        SetLayerName(layers, LAYER_BULLET, "Bullet");
        SetLayerName(layers, LAYER_GROUND, "Ground");

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UniTop] 标签与层设置完成");
    }

    static void AddTag(SerializedProperty tags, string tag)
    {
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
            {
                return;
            }
        }
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
    }

    static void SetLayerName(SerializedProperty layers, int index, string name)
    {
        if (index < layers.arraySize)
        {
            layers.GetArrayElementAtIndex(index).stringValue = name;
        }
    }

    /// <summary>
    /// 教程 8.4.10 / 10.3.3：关闭 Player×Arrow、Enemy×Bullet、Bullet×Bullet 的接触判定。
    /// </summary>
    static void EnsureLayerCollisions()
    {
        Physics2D.IgnoreLayerCollision(LAYER_PLAYER, LAYER_ARROW, true);
        Physics2D.IgnoreLayerCollision(LAYER_ENEMY, LAYER_BULLET, true);
        Physics2D.IgnoreLayerCollision(LAYER_BULLET, LAYER_BULLET, true);

        // 让设置写回 Physics2DSettings.asset
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
        if (assets.Length > 0)
        {
            EditorUtility.SetDirty(assets[0]);
        }
        Debug.Log("[UniTop] 2D 接触矩阵设置完成");
    }

    // ===================================================================
    //  动画（8.4.2~8.4.4 / 9.3.2 / 10.3.2）
    // ===================================================================

    static AnimatorController CreatePlayerAnimator()
    {
        // 帧序（已用像素分析确认）：0,1 向下 / 2,3 向上 / 4,5 向左 / 6,7 向右 / 8 死亡 / 9 弓 / 10 箭矢
        List<Sprite> s = UniTopImportSettings.LoadSprites(PLR + "PlayerImage.png");
        if (s.Count < 9)
        {
            Debug.LogError("[UniTop] PlayerImage 切分失败");
            return null;
        }

        AnimatorController controller = NewController(PLR + "PlayerAnime.controller");
        AddClipState(controller, "PlayerDown", new[] { s[0], s[1] }, 4f, true, true);
        AddClipState(controller, "PlayerUp", new[] { s[2], s[3] }, 4f, true, false);
        AddClipState(controller, "PlayerLeft", new[] { s[4], s[5] }, 4f, true, false);
        AddClipState(controller, "PlayerRight", new[] { s[6], s[7] }, 4f, true, false);
        AddClipState(controller, "PlayerDead", new[] { s[8] }, 1f, false, false, 1f, true);
        return controller;
    }

    static AnimatorController CreateEnemyAnimator()
    {
        // 0,1 向下 / 2,3 向上 / 4,5 向左 / 6,7 向右 / 8 死亡 / 9 待机
        List<Sprite> s = UniTopImportSettings.LoadSprites(ENM + "EnemyImage.png");
        if (s.Count < 10)
        {
            Debug.LogError("[UniTop] EnemyImage 切分失败");
            return null;
        }

        AnimatorController controller = NewController(ENM + "EnemyAnime.controller");
        AddClipState(controller, "EnemyIdle", new[] { s[9] }, 1f, true, true);
        AddClipState(controller, "EnemyDown", new[] { s[0], s[1] }, 4f, true, false);
        AddClipState(controller, "EnemyUp", new[] { s[2], s[3] }, 4f, true, false);
        AddClipState(controller, "EnemyLeft", new[] { s[4], s[5] }, 4f, true, false);
        AddClipState(controller, "EnemyRight", new[] { s[6], s[7] }, 4f, true, false);
        AddClipState(controller, "EnemyDead", new[] { s[8] }, 1f, false, false, 0.5f, true);
        return controller;
    }

    static AnimatorController CreateBossAnimator()
    {
        List<Sprite> s = UniTopImportSettings.LoadSprites(BOS + "Boss.png");
        if (s.Count < 5)
        {
            Debug.LogError("[UniTop] Boss 切分失败");
            return null;
        }

        AnimatorController controller = NewController(BOS + "BossAnime.controller");

        // 待机：Boss_0 / Boss_1，每秒 2 帧
        AddClipState(controller, "BossIdle", new[] { s[0], s[1] }, 2f, true, true);

        // 攻击：保持准备姿势 0.25s，0.75s 时切到攻击帧并触发发射事件
        AnimationClip attack = new AnimationClip();
        attack.name = "BossAttack";
        attack.frameRate = 4f;
        EditorCurveBinding binding = SpriteBinding();
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[4];
        float[] times = { 0.00f, 0.25f, 0.50f, 0.75f };
        Sprite[] frames = { s[2], s[2], s[2], s[3] };
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe();
            keys[i].time = times[i];
            keys[i].value = frames[i];
        }
        AnimationUtility.SetObjectReferenceCurve(attack, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(attack);
        settings.loopTime = true;
        settings.stopTime = 1.0f;
        AnimationUtility.SetAnimationClipSettings(attack, settings);

        // 教程 10.3.6：在动画的这一帧上设置事件，调用 BossController.Attack()
        AnimationEvent shootEvent = new AnimationEvent();
        shootEvent.time = 0.75f;
        shootEvent.functionName = "Attack";
        AnimationUtility.SetAnimationEvents(attack, new[] { shootEvent });

        AssetDatabase.AddObjectToAsset(attack, controller);
        AnimatorState attackState = controller.AddMotion(attack, 0);
        attackState.name = "BossAttack";

        // 死亡：Boss_4
        AddClipState(controller, "BossDead", new[] { s[4] }, 1f, false, false, 1f, true);
        return controller;
    }

    static AnimatorController NewController(string path)
    {
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        return controller;
    }

    static EditorCurveBinding SpriteBinding()
    {
        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";
        binding.propertyName = "m_Sprite";
        return binding;
    }

    static void AddClipState(AnimatorController controller, string name, Sprite[] sprites,
        float fps, bool loop, bool isDefault)
    {
        AddClipState(controller, name, sprites, fps, loop, isDefault, 0f, false);
    }

    static void AddClipState(AnimatorController controller, string name, Sprite[] sprites,
        float fps, bool loop, bool isDefault, float fadeOutDuration, bool fadeOut)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = fps;

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe();
            keys[i].time = i / fps;
            keys[i].value = sprites[i];
        }
        AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding(), keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (fadeOut)
        {
            // 与 Side View 一样，失败时逐渐变得透明
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 1.0f);
            curve.AddKey(fadeOutDuration, 0.0f);
            clip.SetCurve("", typeof(SpriteRenderer), "m_Color.a", curve);
        }

        AssetDatabase.AddObjectToAsset(clip, controller);
        AnimatorState state = controller.AddMotion(clip, 0);
        state.name = name;
        if (isDefault)
        {
            controller.layers[0].stateMachine.defaultState = state;
        }
    }

    // ===================================================================
    //  预制体
    // ===================================================================

    static void CreatePrefabs(AnimatorController playerAnime, AnimatorController enemyAnime, AnimatorController bossAnime)
    {
        // 预制体必须在场景中创建才能存盘，先开一个空场景
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        System.IO.Directory.CreateDirectory(PFB);

        CreateBow();
        CreateArrow();
        CreatePlayer(playerAnime);
        CreateExit();
        CreateRoomManager();
        CreateDoor();
        CreateItems();
        CreateItemBox();
        CreateEnemy(enemyAnime);
        CreateBullet();          // 必须先于 Boss：BossController 需要引用子弹预制
        CreateBoss(bossAnime);
        CreateSoundManager();
        CreateGameCanvas();
        CreateObjGen();

        AssetDatabase.SaveAssets();
        Debug.Log("[UniTop] 预制体生成完成");
    }

    /// <summary>教程 8.4.8：弓（作为玩家角色的子对象，位置归零）。</summary>
    static void CreateBow()
    {
        GameObject go = new GameObject("Bow", typeof(SpriteRenderer));
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = SubSprite(PLR + "PlayerImage.png", "PlayerImage_9");
        sr.sortingOrder = 3;
        SaveAsPrefab(go, "Bow");
    }

    /// <summary>教程 8.4.8：箭矢（Arrow 标签与 Arrow 层）。</summary>
    static void CreateArrow()
    {
        GameObject go = new GameObject("Arrow", typeof(SpriteRenderer));
        go.tag = "Arrow";
        go.layer = LAYER_ARROW;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = SubSprite(PLR + "PlayerImage.png", "PlayerImage_10");
        sr.sortingOrder = 3;

        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.gravityScale = 0f;
        rbody.drag = 0f;
        rbody.angularDrag = 0f;

        // 碰撞体积只覆盖箭头部分
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.16f;
        col.offset = new Vector2(0.12f, 0f);

        go.AddComponent<ArrowController>();
        SaveAsPrefab(go, "Arrow");
    }

    /// <summary>教程 8.4.2：玩家角色。</summary>
    static void CreatePlayer(AnimatorController anime)
    {
        GameObject go = new GameObject("Player", typeof(SpriteRenderer));
        go.tag = "Player";
        go.layer = LAYER_PLAYER;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = SubSprite(PLR + "PlayerImage.png", "PlayerImage_0");
        sr.sortingOrder = 3;

        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = anime;

        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.gravityScale = 0f;
        rbody.freezeRotation = true;
        rbody.drag = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.28f;

        go.AddComponent<PlayerController>();
        go.AddComponent<ItemKeeper>();

        ArrowShoot shoot = go.AddComponent<ArrowShoot>();
        shoot.bowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Bow.prefab");
        shoot.arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Arrow.prefab");

        SaveAsPrefab(go, "Player");
    }

    /// <summary>教程 9.1.1：出入口（圆形触发器）。</summary>
    static void CreateExit()
    {
        GameObject go = new GameObject("Exit");
        go.tag = "Exit";
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;
        go.AddComponent<Exit>();
        SaveAsPrefab(go, "Exit");
    }

    /// <summary>教程 9.1.2 + 10.2.4：房间管理（同时负责保存配置数据）。</summary>
    static void CreateRoomManager()
    {
        GameObject go = new GameObject("RoomManager");
        go.AddComponent<RoomManager>();
        go.AddComponent<SaveDataManager>();
        SaveAsPrefab(go, "RoomManager");
    }

    /// <summary>教程 9.1.5：门。</summary>
    static void CreateDoor()
    {
        GameObject go = new GameObject("Door", typeof(SpriteRenderer));
        go.tag = "Door";
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ITM + "Door.png");
        sr.sortingOrder = 1;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        go.AddComponent<Door>();
        SaveAsPrefab(go, "Door");
    }

    /// <summary>教程 9.2.1：三种道具（钥匙 / 箭矢 / 生命）。</summary>
    static void CreateItems()
    {
        CreateItem("Item_Key", "Key", 0, ItemType.Key, 1);
        CreateItem("Item_Arrow", "Arrow", 1, ItemType.Arrow, 3);   // 箭矢每次拾取 3 支
        CreateItem("Item_Life", "Life", 2, ItemType.Life, 1);
    }

    static void CreateItem(string assetName, string objName, int spriteIndex, ItemType type, int count)
    {
        GameObject go = new GameObject(objName, typeof(SpriteRenderer));
        go.tag = "Item";
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = SubSprite(ITM + "Items.png", "Item_" + spriteIndex);
        sr.sortingOrder = 5;

        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.gravityScale = 0f;

        // 碰撞体积比游戏物体稍大，保证从宝箱弹出时能被拾取
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.55f;
        col.isTrigger = true;

        ItemData data = go.AddComponent<ItemData>();
        data.type = type;
        data.count = count;

        SaveAsPrefab(go, assetName);
    }

    /// <summary>教程 9.2.3：宝箱。</summary>
    static void CreateItemBox()
    {
        GameObject go = new GameObject("ItemBox", typeof(SpriteRenderer));
        go.tag = "ItemBox";
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = SubSprite(ITM + "Items.png", "Item_3");
        sr.sortingOrder = 2;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;

        ItemBox box = go.AddComponent<ItemBox>();
        box.openImage = SubSprite(ITM + "Items.png", "Item_4");

        SaveAsPrefab(go, "ItemBox");
    }

    /// <summary>教程 9.3.1：敌方角色。</summary>
    static void CreateEnemy(AnimatorController anime)
    {
        GameObject go = new GameObject("Enemy", typeof(SpriteRenderer));
        go.tag = "Enemy";
        go.layer = LAYER_ENEMY;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = SubSprite(ENM + "EnemyImage.png", "EnemyImage_9");
        sr.sortingOrder = 3;

        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = anime;

        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.gravityScale = 0f;
        rbody.freezeRotation = true;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.32f;

        go.AddComponent<EnemyController>();
        SaveAsPrefab(go, "Enemy");
    }

    /// <summary>教程 10.3.1：boss 角色（Enemy 标签与 Enemy 层，无 Rigidbody2D）。</summary>
    static void CreateBoss(AnimatorController anime)
    {
        GameObject go = new GameObject("Boss", typeof(SpriteRenderer));
        go.tag = "Enemy";
        go.layer = LAYER_ENEMY;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = SubSprite(BOS + "Boss.png", "Boss_0");
        sr.sortingOrder = 2;

        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = anime;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 1.3f;
        col.offset = new Vector2(0f, 1.4f);

        BossController boss = go.AddComponent<BossController>();
        boss.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Bullet.prefab");

        // 子弹的发射口（教程 10.3.1：位于 boss 角色中央）
        GameObject gate = new GameObject("gate");
        gate.transform.SetParent(go.transform, false);
        gate.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        boss.gate = gate;

        SaveAsPrefab(go, "Boss");
    }

    /// <summary>教程 10.3.3：boss 发射的子弹（Enemy 标签 + Bullet 层）。</summary>
    static void CreateBullet()
    {
        GameObject go = new GameObject("Bullet", typeof(SpriteRenderer));
        go.tag = "Enemy";
        go.layer = LAYER_BULLET;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BOS + "bullet.png");
        sr.sortingOrder = 3;

        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.gravityScale = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.18f;

        go.AddComponent<BulletController>();
        SaveAsPrefab(go, "Bullet");
    }

    /// <summary>教程 10.4.1：用于播放 BGM / SE 的对象（单例）。</summary>
    static void CreateSoundManager()
    {
        GameObject go = new GameObject("SoundManager");
        SoundManager sm = go.AddComponent<SoundManager>();
        sm.bgmTitle = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "BGM_title.wav");
        sm.bgmGame = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "BGM_game_00.wav");
        sm.bgmBoss = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "BGM_boss.wav");

        sm.seShoot = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "se_shoot.mp3");
        sm.seKilled = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "se_killed.mp3");
        sm.seDamage = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "se_damage.mp3");
        sm.seGet = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "se_get.mp3");
        sm.seDoorOpen = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "se_doorOpen.mp3");
        sm.seDoorClosed = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "se_doorClosed.mp3");
        sm.seBossKilled = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "se_bosskilled.mp3");
        sm.seButton = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "se_button.mp3");
        sm.meClear = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "ME_Clear.mp3");
        sm.meGameOver = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "ME_GameOver.mp3");

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = true;
        source.loop = true;
        source.volume = 1.0f;

        SaveAsPrefab(go, "SoundManager");
    }

    /// <summary>教程 10.3.11：对象生成器（生成箭矢的位置）。</summary>
    static void CreateObjGen()
    {
        GameObject go = new GameObject("ObjGen");
        ObjectGenPoint gen = go.AddComponent<ObjectGenPoint>();
        gen.objPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Item_Arrow.prefab");
        SaveAsPrefab(go, "ObjGen");
    }

    // ===================================================================
    //  游戏用 Canvas（教程 9.4）
    // ===================================================================

    static void CreateGameCanvas()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // ---- 道具数量显示区（左上角）----
        Image itemImage = AddImage(canvasGo.transform, "ItemImage",
            AssetDatabase.LoadAssetAtPath<Sprite>(UI + "ItemImage.png"),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(245f, -70f), new Vector2(470f, 100f));
        itemImage.preserveAspect = true;

        // 左胶囊 = 箭矢，右胶囊 = 钥匙（ItemImage 原图即为此顺序）
        Text arrowText = AddText(itemImage.transform, "ArrowText", "0", 52,
            new Vector2(-233f, -55f), new Vector2(-2f, 55f));
        Text keyText = AddText(itemImage.transform, "KeyText", "0", 52,
            new Vector2(2f, -55f), new Vector2(233f, 55f));

        // ---- HP 显示（右上角）----
        Image hpImage = AddImage(canvasGo.transform, "LifeImage",
            SubSprite(UI + "Life.png", "Life_3"),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(-190f, -70f), new Vector2(256f, 96f));
        hpImage.preserveAspect = true;

        // ---- 游戏状态显示（画面中央偏上）----
        Image mainImage = AddImage(canvasGo.transform, "MainImage",
            AssetDatabase.LoadAssetAtPath<Sprite>(UI + "GameStart.png"),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -330f), new Vector2(640f, 124f));
        mainImage.preserveAspect = true;
        mainImage.gameObject.SetActive(false);

        // ---- 再挑战按钮 ----
        GameObject retryGo = new GameObject("RetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        retryGo.transform.SetParent(canvasGo.transform, false);
        RectTransform retryRt = retryGo.GetComponent<RectTransform>();
        retryRt.anchorMin = new Vector2(0.5f, 0.5f);
        retryRt.anchorMax = new Vector2(0.5f, 0.5f);
        retryRt.anchoredPosition = new Vector2(0f, -220f);
        retryRt.sizeDelta = new Vector2(400f, 160f);
        Image retryImage = retryGo.GetComponent<Image>();
        retryImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "button.png");
        AddText(retryGo.transform, "Text", "RETRY", 52,
            new Vector2(-200f, -80f), new Vector2(200f, 80f));

        // ---- 操作 UI ----
        GameObject inputPanel = new GameObject("InputUI", typeof(RectTransform));
        inputPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform inputRt = inputPanel.GetComponent<RectTransform>();
        inputRt.anchorMin = Vector2.zero;
        inputRt.anchorMax = Vector2.one;
        inputRt.offsetMin = Vector2.zero;
        inputRt.offsetMax = Vector2.zero;

        Image padBase = AddImage(inputPanel.transform, "VirtualPadBase",
            AssetDatabase.LoadAssetAtPath<Sprite>(UI + "VirtualPad4D.png"),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
            new Vector2(230f, 210f), new Vector2(250f, 250f));
        padBase.preserveAspect = true;

        Image pad = AddImage(padBase.transform, "VirtualPad",
            AssetDatabase.LoadAssetAtPath<Sprite>(UI + "VirtualPadTab.png"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(120f, 120f));
        pad.preserveAspect = true;

        // 虚拟面板挂在底盘上，整个 250×250 区域都能接收触摸（教程 9.4.7：允许上下左右移动）
        VirtualPad virtualPad = padBase.gameObject.AddComponent<VirtualPad>();
        virtualPad.maxLength = 120f;
        virtualPad.is4DPad = true;
        virtualPad.tab = pad.rectTransform;

        // 攻击按钮（教程 9.4.8：用 Event Trigger 调用 VirtualPad.Attack）
        GameObject attackGo = new GameObject("AttackButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
        attackGo.transform.SetParent(inputPanel.transform, false);
        RectTransform attackRt = attackGo.GetComponent<RectTransform>();
        attackRt.anchorMin = new Vector2(1f, 0f);
        attackRt.anchorMax = new Vector2(1f, 0f);
        attackRt.anchoredPosition = new Vector2(-220f, 200f);
        attackRt.sizeDelta = new Vector2(180f, 180f);
        Image attackImage = attackGo.GetComponent<Image>();
        attackImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "AttackButton.png");
        attackImage.preserveAspect = true;

        // ---- UIManager ----
        UIManager ui = canvasGo.AddComponent<UIManager>();
        ui.itemImage = itemImage.gameObject;
        ui.mainImage = mainImage.gameObject;
        ui.inputPanel = inputPanel;
        ui.resetButton = retryGo;
        ui.arrowsText = arrowText;
        ui.keysText = keyText;
        ui.hpImage = hpImage;
        ui.hp0Spr = SubSprite(UI + "Life.png", "Life_0");
        ui.hp1Spr = SubSprite(UI + "Life.png", "Life_1");
        ui.hp2Spr = SubSprite(UI + "Life.png", "Life_2");
        ui.hp3Spr = SubSprite(UI + "Life.png", "Life_3");
        ui.gameStartSpr = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "GameStart.png");
        ui.gameOverSpr = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "GameOver.png");
        ui.gameClearSpr = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "GameClear.png");
        ui.attackButton = attackGo;
        ui.virtualPad = virtualPad;

        SaveAsPrefab(canvasGo, "Canvas");
    }

    // ===================================================================
    //  场景：Title（教程 10.1）
    // ===================================================================

    static void BuildTitleScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        GameObject camera = NewCamera(new Vector3(0f, 0f, -10f), 5.4f);

        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image back = AddImage(canvasGo.transform, "Back",
            AssetDatabase.LoadAssetAtPath<Sprite>(TTL + "title_back.png"),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetStretch(back.rectTransform);
        back.preserveAspect = true;

        // 角色立绘放右侧，logo 与按钮放左侧，避免相互遮挡
        Image chara = AddImage(canvasGo.transform, "Chara",
            AssetDatabase.LoadAssetAtPath<Sprite>(TTL + "title_chara.png"),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-480f, -40f), new Vector2(1100f, 868f));
        chara.preserveAspect = true;

        Image logo = AddImage(canvasGo.transform, "Logo",
            AssetDatabase.LoadAssetAtPath<Sprite>(TTL + "title_logo.png"),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(500f, -180f), new Vector2(784f, 228f));
        logo.preserveAspect = true;

        GameObject startGo = NewButton(canvasGo.transform, "StartButton",
            AssetDatabase.LoadAssetAtPath<Sprite>(TTL + "bt_gamestart.png"),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(500f, 60f), new Vector2(402f, 70f));

        GameObject continueGo = NewButton(canvasGo.transform, "ContinueButton",
            AssetDatabase.LoadAssetAtPath<Sprite>(TTL + "bt_continue.png"),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(500f, -90f), new Vector2(317f, 70f));

        TitleManager titleManager = canvasGo.AddComponent<TitleManager>();
        titleManager.firstSceneName = UniTopSceneNames.WorldMap;
        titleManager.startButton = startGo;
        titleManager.continueButton = continueGo;

        Place("SoundManager", Vector3.zero);
        EnsureEventSystem();

        SaveScene(UniTopSceneNames.Title);
        Debug.Log("[UniTop] Title 场景完成");
    }

    // ===================================================================
    //  场景：WorldMap（教程 8.3.5）
    // ===================================================================

    static void BuildWorldMapScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        const int W = 48;
        const int H = 27;

        Tilemap tilemap = NewTilemap("Map");
        int[,] map = new int[W, H];
        Fill(map, T_SEA);

        // 海中岛屿：中心 (24,13)，横向半径 17、纵向半径 10
        float cx = 24f, cy = 13f, rx = 17f, ry = 10f;
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float dy = (y + 0.5f - cy) / ry;
                float d = dx * dx + dy * dy;
                if (d <= 0.62f)
                {
                    map[x, y] = Hash(x, y) < 0.22f ? T_THICK : T_GRASS;
                }
                else if (d <= 0.78f)
                {
                    map[x, y] = T_SAND;
                }
                else if (d <= 1.00f)
                {
                    map[x, y] = T_SHALLOW;
                }
            }
        }

        // 树林与山岳点缀
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (map[x, y] != T_GRASS && map[x, y] != T_THICK)
                {
                    continue;
                }
                float r = Hash(x * 7 + 3, y * 13 + 5);
                if (r < 0.05f)
                {
                    map[x, y] = T_MOUNT;
                }
                else if (r < 0.16f)
                {
                    map[x, y] = T_TREE;
                }
            }
        }

        // 小湖与桥：湖在 (13,9)，桥横穿湖面
        FillEllipse(map, 13, 9, 4, 2, T_SHALLOW);
        for (int x = 9; x <= 17; x++)
        {
            map[x, 9] = T_BRIDGE;
        }

        // 地牢出入口及其两侧的石墙，形成只能从南面进入的一条通道
        map[24, 7] = T_ENTRANCE;
        map[23, 7] = T_ROCK1;
        map[25, 7] = T_ROCK1;
        map[23, 8] = T_ROCK1;
        map[25, 8] = T_ROCK1;
        map[24, 6] = T_ROCK1;
        map[24, 8] = T_GRASS;   // 门前
        map[24, 9] = T_GRASS;
        map[23, 9] = T_GRASS;
        map[25, 9] = T_GRASS;

        // 玩家出生点周围必须可走
        map[24, 12] = T_GRASS;
        map[23, 12] = T_GRASS;
        map[25, 12] = T_GRASS;
        map[24, 13] = T_GRASS;

        // 清理出生点到地牢入口的通路，以及各配置物周围，避免被随机生成的树木/山岳挡住
        ClearArea(map, 24, 11, 1, T_GRASS);
        ClearArea(map, 24, 10, 1, T_GRASS);
        ClearArea(map, 33, 10, 1, T_GRASS);
        ClearArea(map, 20, 15, 1, T_GRASS);
        ClearArea(map, 28, 16, 1, T_GRASS);
        ClearArea(map, 31, 11, 1, T_GRASS);
        ClearArea(map, 16, 11, 1, T_GRASS);
        ClearArea(map, 19, 8, 1, T_GRASS);
        ClearArea(map, 30, 15, 1, T_GRASS);

        // 主干道必须畅通：出生点 ↔ 钥匙宝箱 ↔ 地牢门前，
        // 否则随机生成的树林/山岳可能把关键路线堵死，玩家永远拿不到钥匙。
        ClearCorridor(map, 24, 12, 33, 10, 1);
        ClearCorridor(map, 33, 10, 24, 9, 1);
        ClearCorridor(map, 24, 12, 24, 9, 1);

        ApplyMap(tilemap, map);

        BuildGameplayObjects(UniTopSceneNames.WorldMap, new Vector3(24.5f, 12.5f, 0f), false);

        // 出入口（进入地牢）
        GameObject exit = Place("Exit", new Vector3(24.5f, 7.5f, 0f));
        Exit exitScript = exit.GetComponent<Exit>();
        exitScript.sceneName = UniTopSceneNames.Dungeon1;
        exitScript.doorNumber = 1;
        exitScript.direction = ExitDirection.up;

        // 教程 9.1.6：门配置在世界地图侧的出入口前方
        GameObject door = Place("Door", new Vector3(24.5f, 8.5f, 0f));
        door.GetComponent<Door>().arrangeId = 1;

        // 散落的箭矢
        Place("Item_Arrow", new Vector3(20.5f, 15.5f, 0f)).GetComponent<ItemData>().arrangeId = 1;
        Place("Item_Arrow", new Vector3(28.5f, 16.5f, 0f)).GetComponent<ItemData>().arrangeId = 2;
        Place("Item_Arrow", new Vector3(31.5f, 11.5f, 0f)).GetComponent<ItemData>().arrangeId = 3;
        Place("Item_Life", new Vector3(16.5f, 11.5f, 0f)).GetComponent<ItemData>().arrangeId = 4;

        // 宝箱：里面放着开门用的钥匙
        GameObject box = Place("ItemBox", new Vector3(33.5f, 10.5f, 0f));
        ItemBox itemBox = box.GetComponent<ItemBox>();
        itemBox.itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Item_Key.prefab");
        itemBox.arrangeId = 5;

        // 岛上的史莱姆
        PlaceEnemy(new Vector3(19.5f, 8.5f, 0f), 5.0f, 2.0f, 6);
        PlaceEnemy(new Vector3(30.5f, 15.5f, 0f), 5.0f, 2.0f, 7);

        SaveScene(UniTopSceneNames.WorldMap);
        Debug.Log("[UniTop] WorldMap 场景完成");
    }

    // ===================================================================
    //  场景：dungeon1（教程 8.3.5 / 9.1）
    // ===================================================================

    static void BuildDungeon1Scene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        const int W = 40;
        const int H = 24;

        Tilemap tilemap = NewTilemap("Map");
        int[,] map = new int[W, H];
        Fill(map, T_ROCK1);

        // 石墙的纹理变化
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float r = Hash(x * 5 + 1, y * 3 + 2);
                map[x, y] = r < 0.25f ? T_ROCK2 : (r < 0.5f ? T_ROCK3 : (r < 0.7f ? T_ROCK4 : (r < 0.85f ? T_ROCK5 : T_ROCK1)));
            }
        }

        // 房间与通道
        FillRect(map, 16, 3, 9, 5, T_FLOOR1);    // 入口房间
        FillRect(map, 20, 8, 3, 5, T_FLOOR2);    // 纵向通道
        FillRect(map, 12, 12, 19, 8, T_FLOOR1);  // 中央大厅
        FillRect(map, 4, 8, 9, 6, T_FLOOR2);     // 左侧房间
        FillRect(map, 10, 12, 3, 2, T_FLOOR2);   // 通往左房间
        // 通往 Boss 关：只留 x=21 这一列的 1 格宽通道，
        // 门堵在通道入口，玩家必须先开宝箱拿到钥匙才能进 Boss 关。
        // 若通道是 3 格宽，玩家可以从两侧绕过去，门就形同虚设了。
        FillRect(map, 20, 20, 3, 3, T_ROCK1);
        map[21, 20] = T_FLOOR2;                  // 门就放在这一格
        map[21, 21] = T_FLOOR2;
        map[21, 22] = T_FLOOR2;                  // 最深处是出入口

        // 地面纹理
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (map[x, y] == T_FLOOR1 && Hash(x + 11, y + 7) < 0.18f)
                {
                    map[x, y] = T_FLOOR2;
                }
            }
        }

        // 出入口所在位置（地牢内侧）
        map[20, 4] = T_ENTRANCE;
        map[21, 4] = T_ENTRANCE;
        map[20, 5] = T_FLOOR1;
        map[21, 5] = T_FLOOR1;
        // Boss 出入口（通道最深处）
        map[21, 22] = T_FRAME;

        ApplyMap(tilemap, map);

        BuildGameplayObjects(UniTopSceneNames.Dungeon1, new Vector3(20.5f, 5.5f, 0f), false);

        // 回到世界地图的出入口
        GameObject exit = Place("Exit", new Vector3(20.5f, 4.5f, 0f));
        Exit exitScript = exit.GetComponent<Exit>();
        exitScript.sceneName = UniTopSceneNames.WorldMap;
        exitScript.doorNumber = 1;
        exitScript.direction = ExitDirection.down;

        // 挡住通往 Boss 关的门（需要钥匙才能打开）
        GameObject bossDoor = Place("Door", new Vector3(21.5f, 20.5f, 0f));
        bossDoor.GetComponent<Door>().arrangeId = 6;

        // 前往 Boss 关的出入口（在门的后面）
        GameObject bossExit = Place("Exit", new Vector3(21.5f, 22.5f, 0f));
        Exit bossExitScript = bossExit.GetComponent<Exit>();
        bossExitScript.sceneName = UniTopSceneNames.BossStage;
        bossExitScript.doorNumber = 2;
        bossExitScript.direction = ExitDirection.up;

        // 道具与宝箱
        Place("Item_Arrow", new Vector3(15.5f, 15.5f, 0f)).GetComponent<ItemData>().arrangeId = 1;
        Place("Item_Arrow", new Vector3(26.5f, 17.5f, 0f)).GetComponent<ItemData>().arrangeId = 2;
        Place("Item_Life", new Vector3(7.5f, 10.5f, 0f)).GetComponent<ItemData>().arrangeId = 3;

        GameObject box1 = Place("ItemBox", new Vector3(29.5f, 14.5f, 0f));
        ItemBox itemBox1 = box1.GetComponent<ItemBox>();
        itemBox1.itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Item_Arrow.prefab");
        itemBox1.arrangeId = 4;

        GameObject box2 = Place("ItemBox", new Vector3(6.5f, 12.5f, 0f));
        ItemBox itemBox2 = box2.GetComponent<ItemBox>();
        itemBox2.itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Item_Key.prefab");
        itemBox2.arrangeId = 5;

        // 敌人
        PlaceEnemy(new Vector3(18.5f, 16.5f, 0f), 6.0f, 2.2f, 6);
        PlaceEnemy(new Vector3(24.5f, 15.5f, 0f), 6.0f, 2.2f, 7);
        PlaceEnemy(new Vector3(28.5f, 18.5f, 0f), 6.0f, 2.2f, 8);
        PlaceEnemy(new Vector3(8.5f, 9.5f, 0f), 5.0f, 2.0f, 9);
        PlaceEnemy(new Vector3(13.5f, 17.5f, 0f), 5.0f, 2.0f, 10);

        SaveScene(UniTopSceneNames.Dungeon1);
        Debug.Log("[UniTop] dungeon1 场景完成");
    }

    // ===================================================================
    //  场景：BossStage（教程 10.3.10）
    // ===================================================================

    static void BuildBossStageScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        const int W = 34;
        const int H = 22;

        Tilemap tilemap = NewTilemap("Map");
        int[,] map = new int[W, H];
        Fill(map, T_ROCK1);

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                map[x, y] = RockTile(x, y);
            }
        }

        // 大厅：y=1..15 整片开阔地面。
        // boss 必须站在这种开阔场地里 —— 如果把 boss 塞进窄通道，
        // 它的发射口 gate 会被两侧墙体夹住，子弹一出膛就撞墙，攻击角度只剩下方一小片。
        for (int y = 1; y <= 15; y++)
        {
            for (int x = 1; x < W - 1; x++)
            {
                map[x, y] = Hash(x + 21, y + 31) < 0.15f ? T_FLOOR2 : T_FLOOR1;
            }
        }

        // 分隔墙 y=16：只留 x=16 一格开口。
        // 开口必须只有 1 格 —— boss 的碰撞体半径 1.3，2 格宽的开口堵不严，
        // 玩家能从旁边绕过去直接拿宝箱。
        for (int x = 1; x < W - 1; x++)
        {
            map[x, 16] = RockTile(x, 16);
        }
        map[16, 16] = T_FLOOR2;

        // 深处房间 y=17..19：x=13..20（宝箱与门都在这里）
        for (int y = 17; y <= 19; y++)
        {
            for (int x = 1; x < W - 1; x++)
            {
                map[x, y] = RockTile(x, y);
            }
        }
        for (int y = 17; y <= 19; y++)
        {
            for (int x = 13; x <= 20; x++)
            {
                map[x, y] = T_FLOOR2;
            }
        }

        // 通关层 y=20：只有 x=16 一格可通行，且被门封住
        for (int x = 1; x < W - 1; x++)
        {
            map[x, 20] = RockTile(x, 20);
        }
        map[16, 20] = T_FRAME;

        ApplyMap(tilemap, map);

        BuildGameplayObjects(UniTopSceneNames.BossStage, new Vector3(16.5f, 3.5f, 0f), true);

        // 到达点：进入后不能退出，因此关闭触发器且不指定目标场景
        GameObject arrival = Place("Exit", new Vector3(16.5f, 3.5f, 0f));
        Exit arrivalScript = arrival.GetComponent<Exit>();
        arrivalScript.sceneName = "";
        arrivalScript.doorNumber = 2;
        arrivalScript.direction = ExitDirection.up;
        Collider2D arrivalCollider = arrival.GetComponent<Collider2D>();
        if (arrivalCollider != null)
        {
            arrivalCollider.enabled = false;
        }

        // 教程 10.3.12：深处的门编号为 100，开门即为通关
        GameObject clearExit = Place("Exit", new Vector3(16.5f, 20.5f, 0f));
        Exit clearExitScript = clearExit.GetComponent<Exit>();
        clearExitScript.sceneName = "";
        clearExitScript.doorNumber = 100;
        clearExitScript.direction = ExitDirection.up;

        // 挡住深处的门：门必须在宝箱的后面（更深处）。
        // 若把宝箱放在门后，钥匙就永远拿不到 —— 那是个死锁。
        GameObject door = Place("Door", new Vector3(16.5f, 19.5f, 0f));
        door.GetComponent<Door>().arrangeId = 10;

        // boss 站在大厅里（绝对不能放进窄通道）。
        // 它的发射口 gate 位于自身 y+1.4 处 = (16.5, 15.4)，仍在大厅开阔区，可以 360° 射击；
        // 同时它半径 1.3 的碰撞体（中心 (16.5,15.4)）正好堵住 y=16 的 2 格开口，
        // 挡住后面的宝箱 —— 这正是教程 10.3.10 要求的「用 boss 的碰撞体积挡住宝箱」。
        GameObject boss = Place("Boss", new Vector3(16.5f, 14.0f, 0f));
        boss.GetComponent<BossController>().arrangeId = 0;

        // 宝箱在 boss 后面、门前面（浅处）靠左，玩家击倒 boss 后先来这里拿钥匙。
        // 深处房间有 8 格宽，宝箱在左侧，玩家可以绕过它走到中间去开门。
        GameObject box = Place("ItemBox", new Vector3(14.5f, 18.5f, 0f));
        ItemBox itemBox = box.GetComponent<ItemBox>();
        itemBox.itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "Item_Key.prefab");
        itemBox.arrangeId = 11;

        // 箭矢用尽时自动补充的生成点（全部放在大厅内）
        Vector2[] genPoints =
        {
            new Vector2(4.5f, 4.5f), new Vector2(12.5f, 2.5f), new Vector2(24.5f, 3.5f),
            new Vector2(30.5f, 6.5f), new Vector2(4.5f, 11.5f), new Vector2(30.5f, 12.5f),
            new Vector2(8.5f, 14.5f), new Vector2(26.5f, 14.5f), new Vector2(16.5f, 8.5f),
            new Vector2(20.5f, 11.5f)
        };
        foreach (Vector2 p in genPoints)
        {
            Place("ObjGen", new Vector3(p.x, p.y, 0f));
        }

        // 摄像机在 boss 战时聚焦于玩家与 boss 的中间点
        GameObject roomManager = GameObject.Find("RoomManager");
        if (roomManager != null)
        {
            roomManager.AddComponent<ObjectGenManager>();
        }
        CameraManager cameraManager = Object.FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            cameraManager.otherTarget = boss;
        }

        SaveScene(UniTopSceneNames.BossStage);
        Debug.Log("[UniTop] BossStage 场景完成");
    }

    // ===================================================================
    //  场景共通部分
    // ===================================================================

    /// <summary>每个关卡场景都要配置的：摄像机、玩家、房间管理、Canvas、EventSystem、SoundManager。</summary>
    static void BuildGameplayObjects(string sceneName, Vector3 playerStart, bool isBoss)
    {
        NewCamera(playerStart + new Vector3(0f, 0f, -10f), 5.4f);

        Place("Player", playerStart);
        Place("RoomManager", Vector3.zero);

        // 教程 9.4.6：Canvas 需要指定 Main Camera
        GameObject canvas = Place("Canvas", Vector3.zero);
        Canvas canvasComponent = canvas.GetComponent<Canvas>();
        if (canvasComponent != null)
        {
            canvasComponent.worldCamera = Camera.main;
        }
        UIManager ui = canvas.GetComponent<UIManager>();
        if (ui != null)
        {
            ui.retrySceneName = sceneName;
        }

        Place("SoundManager", Vector3.zero);
        EnsureEventSystem();
    }

    static GameObject PlaceEnemy(Vector3 pos, float range, float speed, int arrangeId)
    {
        GameObject go = Place("Enemy", pos);
        EnemyController enemy = go.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.reactionDistance = range;
            enemy.speed = speed;
            enemy.arrangeId = arrangeId;
        }
        return go;
    }

    // ===================================================================
    //  贴片地图
    // ===================================================================

    static Tilemap NewTilemap(string name)
    {
        GameObject gridGo = new GameObject(name, typeof(Grid));
        Grid grid = gridGo.GetComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        GameObject tilemapGo = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapGo.transform.SetParent(gridGo.transform, false);
        tilemapGo.layer = LAYER_GROUND;

        Tilemap tilemap = tilemapGo.GetComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        tilemap.color = Color.white;

        TilemapRenderer renderer = tilemapGo.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = 0;

        // 教程 8.3.6：为贴片地图添加碰撞体积，只使用 Tilemap Collider 2D。
        // 注意：不要再加 CompositeCollider2D 并开 usedByComposite —— 那样碰撞体会全部交给
        // Composite 生成，而在批处理模式下 Composite 的几何不会自动生成（实测 pathCount=0，
        // 整张地图完全没有碰撞，玩家会直接穿墙）。严格按教程只挂 TilemapCollider2D 即可。
        tilemapGo.AddComponent<TilemapCollider2D>();

        return tilemap;
    }

    static void Fill(int[,] map, int value)
    {
        for (int y = 0; y < map.GetLength(1); y++)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                map[x, y] = value;
            }
        }
    }

    static void FillRect(int[,] map, int x0, int y0, int w, int h, int value)
    {
        for (int y = y0; y < y0 + h; y++)
        {
            for (int x = x0; x < x0 + w; x++)
            {
                if (x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1))
                {
                    map[x, y] = value;
                }
            }
        }
    }

    static void FillEllipse(int[,] map, int cx, int cy, int rx, int ry, int value)
    {
        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1))
                {
                    continue;
                }
                float dx = (x + 0.5f - cx - 0.5f) / (rx + 0.5f);
                float dy = (y + 0.5f - cy - 0.5f) / (ry + 0.5f);
                if (dx * dx + dy * dy <= 1.0f)
                {
                    map[x, y] = value;
                }
            }
        }
    }

    /// <summary>把以 (cx,cy) 为中心、半径 r 的方形区域内的树林与山岳清成地面。</summary>
    static void ClearArea(int[,] map, int cx, int cy, int r, int value)
    {
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1))
                {
                    continue;
                }
                if (map[x, y] == T_TREE || map[x, y] == T_MOUNT)
                {
                    map[x, y] = value;
                }
            }
        }
    }

    /// <summary>石墙的纹理变化（同一坐标恒定，保证每次生成一致）。</summary>
    static int RockTile(int x, int y)
    {
        float r = Hash(x * 5 + 1, y * 3 + 2);
        return r < 0.25f ? T_ROCK2 : (r < 0.5f ? T_ROCK3 : (r < 0.7f ? T_ROCK4 : (r < 0.85f ? T_ROCK5 : T_ROCK1)));
    }

    /// <summary>在两格之间清出一条走廊（只清树林与山岳），保证主干道不会被随机障碍堵死。</summary>
    static void ClearCorridor(int[,] map, int x0, int y0, int x1, int y1, int width)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            ClearArea(map, x, y, width, T_GRASS);
        }
    }

    /// <summary>确定性的伪随机（0~1），保证每次生成的地图一致。</summary>
    static float Hash(int x, int y)
    {
        int n = x * 374761393 + y * 668265263;
        n = (n ^ (n >> 13)) * 1274126177;
        n = n ^ (n >> 16);
        return (float)(n & 0x7FFFFFFF) / 2147483647f;
    }

    static void ApplyMap(Tilemap tilemap, int[,] map)
    {
        for (int y = 0; y < map.GetLength(1); y++)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                Tile tile = Tile(map[x, y]);
                if (tile != null)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
    }

    static Tile Tile(int index)
    {
        return AssetDatabase.LoadAssetAtPath<Tile>(MAP + "Tile/StageMap_" + index + ".asset");
    }

    // ===================================================================
    //  构建设置
    // ===================================================================

    static void ApplyBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(SCN + UniTopSceneNames.Title + ".unity", true),
            new EditorBuildSettingsScene(SCN + UniTopSceneNames.WorldMap + ".unity", true),
            new EditorBuildSettingsScene(SCN + UniTopSceneNames.Dungeon1 + ".unity", true),
            new EditorBuildSettingsScene(SCN + UniTopSceneNames.BossStage + ".unity", true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[UniTop] 构建场景列表已设置");
    }

    // ===================================================================
    //  小工具
    // ===================================================================

    static GameObject NewCamera(Vector3 position, float orthoSize)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = orthoSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = -100f;
        camera.farClipPlane = 100f;
        go.transform.position = position;
        go.AddComponent<CameraManager>();
        return go;
    }

    /// <summary>
    /// EventSystem 是 uGUI 点击/拖拽事件的分发入口（VirtualPad / 按钮都依赖它）。
    /// 用 EmptyScene 建场景时不会自带，必须手动补，否则所有 UI 点了都没反应。
    /// </summary>
    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static GameObject NewButton(Transform parent, string name, Sprite sprite,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return go;
    }

    static Font GetFont()
    {
        // Unity 2022 起内置字体名为 LegacyRuntime.ttf（Arial.ttf 已废弃）
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Text AddText(Transform parent, string name, string content, int fontSize,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Text text = go.AddComponent<Text>();
        text.font = GetFont();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    static Image AddImage(Transform parent, string name, Sprite sprite,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    static void SetStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Sprite SubSprite(string texturePath, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        foreach (Object o in assets)
        {
            Sprite s = o as Sprite;
            if (s != null && s.name == spriteName)
            {
                return s;
            }
        }
        Debug.LogError("[UniTop] 找不到子精灵 " + spriteName + " @ " + texturePath);
        return null;
    }

    static GameObject Place(string prefabName, Vector3 pos)
    {
        string path = PFB + prefabName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("[UniTop] 找不到预制: " + path);
            return null;
        }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        return go;
    }

    static GameObject SaveAsPrefab(GameObject go, string assetName)
    {
        string path = PFB + assetName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void SaveScene(string name)
    {
        System.IO.Directory.CreateDirectory(SCN);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCN + name + ".unity");
    }
}
