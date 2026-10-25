using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 按《轻松上手2D游戏开发：Unity入门》第一部分、第二部分（第1~7章）的步骤
/// 生成全部资源：标签层、玩家动画、预制体、场景（Title / Stage1~3 / Result）与构建设置。
/// </summary>
public static class YeGEBuilder
{
    const string IMG = "Assets/Images/";
    const string SND = "Assets/Sounds/";
    const string PLR = "Assets/Player/";
    const string PFB = "Assets/Prefab/";
    const string SCN = "Assets/Scenes/";

    const int GROUND_LAYER = 8;   // Ground
    const int ENEMY_LAYER = 9;    // Enemy

    const float GROUND_TOP = -2.0f;   // 地面表面高度
    const float BLOCK = 1.2f;         // 1x1 方块边长
    const float GROUND_W = 6.0f;      // ground.png 宽
    const float GROUND_H = 2.4f;      // ground.png 高

    [MenuItem("YeGE/旧教程/生成全部内容")]
    public static void BuildAll()
    {
        if (System.IO.File.Exists("Assets/Adventure/Scenes/AdventureTitle.scene"))
        {
            Debug.LogError("[YeGE] 展示版已初始化。旧教程生成器已停用，防止覆盖共享预制体和动画。请使用 YeGE/展示版 菜单。");
            return;
        }
        EnsureTagsAndLayers();
        PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();
        AnimatorController playerAnim = CreatePlayerAnimator();
        CreatePrefabs(noFriction, playerAnim);

        BuildTitleScene();
        BuildStage1();
        BuildStage2();
        BuildStage3();
        BuildResultScene();

        ApplyBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[YeGE] 全部内容生成完成");
    }

    // ============================== 基础：标签与层 ==============================
    static void EnsureTagsAndLayers()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        SerializedObject so = new SerializedObject(assets[0]);

        SerializedProperty tags = so.FindProperty("tags");
        AddTag(tags, "Player");
        AddTag(tags, "Goal");
        AddTag(tags, "Dead");
        AddTag(tags, "ScoreItem");
        AddTag(tags, "Wall");

        SerializedProperty layers = so.FindProperty("layers");
        SetLayerName(layers, GROUND_LAYER, "Ground");
        SetLayerName(layers, ENEMY_LAYER, "Enemy");

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[YeGE] 标签与层设置完成");
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

    static PhysicsMaterial2D EnsureNoFrictionMaterial()
    {
        string path = PFB + "NoFriction.physicsMaterial2D";
        PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("NoFriction");
            mat.friction = 0f;
            mat.bounciness = 0f;
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    // ============================== 玩家动画（第4.5节） ==============================
    static AnimatorController CreatePlayerAnimator()
    {
        string path = PLR + "PlayerAnime.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        AddClipState(controller, "PlayerStop", new[] { SprP("player_stop") }, 1f, true, true);
        AddClipState(controller, "PlayerMove", new[]
        {
            SprP("player_run1"), SprP("player_run2"), SprP("player_run3"), SprP("player_run4"),
            SprP("player_run5"), SprP("player_run6"), SprP("player_run7")
        }, 12f, true, false);
        AddClipState(controller, "PlayerJump", new[] { SprP("Player_jump1"), SprP("Player_jump2") }, 6f, true, false);
        AddClipState(controller, "PlayerGoal", new[] { SprP("player_clear") }, 1f, false, false);
        AddClipState(controller, "PlayerOver", new[] { SprP("player_over") }, 1f, false, false);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void AddClipState(AnimatorController controller, string name, Sprite[] sprites, float fps, bool loop, bool isDefault)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = fps;

        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";
        binding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe();
            keys[i].time = i / fps;
            keys[i].value = sprites[i];
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.AddObjectToAsset(clip, controller);
        AnimatorState state = controller.AddMotion(clip, 0);
        state.name = name;
        if (isDefault)
        {
            controller.layers[0].stateMachine.defaultState = state;
        }
    }

    // ============================== 预制体（第4.4节） ==============================
    static void CreatePrefabs(PhysicsMaterial2D noFriction, AnimatorController playerAnim)
    {
        CreateBlock("block1x1", "block1x1", new Vector2(BLOCK, BLOCK));
        CreateBlock("block1x2", "block1x2", new Vector2(BLOCK, BLOCK * 2f));
        CreateBlock("block2x1", "block2x1", new Vector2(BLOCK * 2f, BLOCK));
        CreateBlock("ground", "ground", new Vector2(GROUND_W, GROUND_H));

        CreateGoal();
        CreateItem("item_red", "item_red", 100);
        CreateItem("item_blue", "item_blue", 200);
        CreateItem("item_green", "item_green", 400);
        CreateItem("item_white", "item_white", 1000);
        CreateNeedle();
        CreateShell();
        CreateCannon();
        CreateMovingBlock();
        CreateGimmickBlock();
        CreateSwitch();
        CreateEnemies();
        CreateWall();
        CreatePlayer(noFriction, playerAnim);
        Debug.Log("[YeGE] 预制体生成完成");
    }

    static GameObject CreateBlock(string name, string sprite, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.layer = GROUND_LAYER;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr(sprite);
        sr.sortingOrder = 1;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        return SaveAsPrefab(go, name);
    }

    static void CreateGoal()
    {
        GameObject go = new GameObject("goal");
        go.tag = "Goal";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("goal");
        sr.sortingOrder = 2;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.09f, 1.19f);
        SaveAsPrefab(go, "goal");
    }

    static void CreateItem(string name, string sprite, int value)
    {
        GameObject go = new GameObject(name);
        go.tag = "ScoreItem";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr(sprite);
        sr.sortingOrder = 2;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.34f;
        ItemData data = go.AddComponent<ItemData>();
        data.value = value;
        SaveAsPrefab(go, name);
    }

    static void CreateNeedle()
    {
        GameObject go = new GameObject("needle");
        go.tag = "Dead";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("needle");
        sr.sortingOrder = 2;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.09f, 0.48f);
        SaveAsPrefab(go, "needle");
    }

    static void CreateShell()
    {
        GameObject go = new GameObject("shell");
        go.tag = "Dead";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("shell");
        sr.sortingOrder = 3;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.27f;
        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.gravityScale = 0f;
        rbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        go.AddComponent<ShellController>().deleteTime = 3.0f;
        SaveAsPrefab(go, "shell");
    }

    static void CreateCannon()
    {
        GameObject go = new GameObject("cannon");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("cannon");
        sr.sortingOrder = 2;

        CannonController cannon = go.AddComponent<CannonController>();
        cannon.objPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "shell.prefab");
        cannon.delayTime = 2.5f;
        cannon.fireSpeedX = -5.0f;
        cannon.fireSpeedY = 0.0f;
        cannon.length = 14.0f;

        GameObject gate = new GameObject("gate");
        gate.transform.SetParent(go.transform, false);
        gate.transform.localPosition = new Vector3(-1.25f, 0.15f, 0f);

        SaveAsPrefab(go, "cannon");
    }

    static void CreateMovingBlock()
    {
        GameObject go = new GameObject("MovingBlock");
        go.layer = GROUND_LAYER;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("block_move");
        sr.sortingOrder = 1;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(BLOCK, 0.69f);
        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.bodyType = RigidbodyType2D.Kinematic;
        rbody.gravityScale = 0f;
        go.AddComponent<MovingBlock>();
        SaveAsPrefab(go, "MovingBlock");
    }

    static void CreateGimmickBlock()
    {
        GameObject go = new GameObject("GimmickBlock");
        go.layer = GROUND_LAYER;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("block1x1");
        sr.sortingOrder = 1;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(BLOCK, BLOCK);
        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.gravityScale = 1.5f;
        rbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        GimmickBlock gb = go.AddComponent<GimmickBlock>();
        gb.length = 1.2f;
        gb.isDelete = true;
        gb.delay = 0.8f;
        SaveAsPrefab(go, "GimmickBlock");
    }

    static void CreateSwitch()
    {
        GameObject go = new GameObject("Switch");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Lever_off");
        sr.sortingOrder = 2;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(BLOCK, 1.0f);
        SwitchAction sw = go.AddComponent<SwitchAction>();
        sw.imageOn = Spr("Lever_on");
        sw.imageOff = Spr("Lever_off");
        sw.on = false;
        SaveAsPrefab(go, "Switch");
    }

    static void CreateEnemies()
    {
        for (int i = 1; i <= 4; i++)
        {
            string name = "enemy" + i;
            GameObject go = new GameObject(name);
            go.tag = "Dead";
            go.layer = ENEMY_LAYER;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr(name);
            sr.sortingOrder = 2;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 1.24f);
            col.offset = new Vector2(0f, -0.14f);
            Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
            rbody.gravityScale = 1.5f;
            rbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            rbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            EmeryController emery = go.AddComponent<EmeryController>();
            emery.speed = 2.0f;
            emery.direction = "left";
            emery.range = 0.0f;
            SaveAsPrefab(go, name);
        }
    }

    static void CreateWall()
    {
        GameObject go = new GameObject("WallObject");
        go.tag = "Wall";
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 6f);
        SaveAsPrefab(go, "WallObject");
    }

    static void CreatePlayer(PhysicsMaterial2D noFriction, AnimatorController playerAnim)
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SprP("player_stop");
        sr.sortingOrder = 3;

        Rigidbody2D rbody = go.AddComponent<Rigidbody2D>();
        rbody.gravityScale = 1.5f;
        rbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        rbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D cap = go.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.0f, 1.7f);
        cap.offset = new Vector2(0f, -0.02f);
        cap.sharedMaterial = noFriction;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = playerAnim;

        PlayerController pc = go.AddComponent<PlayerController>();
        pc.speed = 3.0f;
        pc.jump = 9.0f;
        pc.groundLayer = 1 << GROUND_LAYER;

        SaveAsPrefab(go, "Player");
    }

    // ============================== 场景通用构件 ==============================
    static CameraManager BuildCamera(string backSprite, float left, float right, float x)
    {
        GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        Camera cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.19f, 0.30f, 0.47f, 1f);
        camGo.transform.position = new Vector3(x, 0f, -10f);

        GameObject back = new GameObject("back");
        SpriteRenderer sr = back.AddComponent<SpriteRenderer>();
        sr.sprite = Spr(backSprite);
        sr.sortingOrder = -10;
        back.transform.SetParent(camGo.transform, false);
        back.transform.localPosition = new Vector3(0f, 0f, 10f);

        CameraManager cm = camGo.AddComponent<CameraManager>();
        cm.leftLimit = left;
        cm.rightLimit = right;
        cm.topLimit = 0f;
        cm.bottomLimit = 0f;
        cm.subScreen = back;
        return cm;
    }

    /// <summary>铺设地面。区间长度必须是 GROUND_W(6) 的整数倍，保证地面边缘正好落在 x1 / x2 上。</summary>
    static void GroundRun(float x1, float x2)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt((x2 - x1) / GROUND_W));
        for (int i = 0; i < count; i++)
        {
            Place("ground", new Vector3(x1 + GROUND_W * 0.5f + i * GROUND_W, GROUND_TOP - GROUND_H * 0.5f, 0f));
        }
    }

    static void DeathZone(float x1, float x2, float y)
    {
        GameObject go = new GameObject("DeadZone");
        go.tag = "Dead";
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(x2 - x1, 2f);
        go.transform.position = new Vector3((x1 + x2) * 0.5f, y, 0f);
    }

    static GameObject Wall(float x, float y, float height)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + "WallObject.prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = new Vector3(x, y, 0f);
        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        col.size = new Vector2(1f, height);
        return go;
    }

    static GameObject Place(string prefabName, Vector3 pos)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFB + prefabName + ".prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        return go;
    }

    static GameObject PlaceEnemy(int index, Vector3 pos, float range, float speed)
    {
        GameObject go = Place("enemy" + index, pos);
        EmeryController emery = go.GetComponent<EmeryController>();
        emery.range = range;
        emery.speed = speed;
        return go;
    }

    // ============================== UI（第5章 / 第6章 / 第7.7节） ==============================
    static RectTransform NewUI(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>
    /// EventSystem 是 uGUI 点击/拖拽事件的分发入口（Button / VirtualPad / MobileJumpButton / ChangeScene 都依赖它）。
    /// 用 EmptyScene 建场景时不会自带，必须手动补，否则所有 UI 点了都没反应（键盘操作不受影响）。
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

    static Font GetFont()
    {
        // Unity 2022 起内置字体名为 LegacyRuntime.ttf（Arial.ttf 已废弃）
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Text AddText(Transform parent, string content, int fontSize, TextAnchor anchor, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = NewUI("Text", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = GetFont();
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        RectTransform rt = NewUI(name, parent, anchorMin, anchorMax, anchoredPos, size);
        rt.pivot = pivot;
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        return img;
    }

    /// <summary>生成 HUD + 结算面板 + 触屏操作 UI + GameManager</summary>
    static void BuildGameUI(string curScene, string nextScene, float gameTime)
    {
        GameObject canvasGo = new GameObject("GameUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler cs = canvasGo.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
        EnsureEventSystem();

        // 安全区容器：避免挖孔屏/手势条遮挡 UI
        RectTransform safe = NewUI("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        // 分数板
        Image scoreBoard = AddImage(safe, "ScoreBoard", Spr("ScoreBoard"),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(415f, 167f));
        Text scoreText = AddText(scoreBoard.transform, "0", 90, TextAnchor.MiddleCenter, new Vector2(170f, 20f), new Vector2(-30f, -30f));

        // 时间条
        Image timeBar = AddImage(safe, "TimeBar", Spr("TimeBar"),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(412f, 100f));
        Text timeText = AddText(timeBar.transform, "0", 70, TextAnchor.MiddleCenter, new Vector2(150f, 10f), new Vector2(-20f, -15f));

        // 中央提示图片
        Image mainImage = AddImage(safe, "MainImage", Spr("GameStart"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(800f, 154f));

        // 结算面板
        RectTransform panel = NewUI("Panel", safe, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(760f, 420f));
        Image panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);

        Image retryImg = AddImage(panel, "RetryButton", Spr("button"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190f, -60f), new Vector2(340f, 136f));
        retryImg.gameObject.AddComponent<Button>();
        AddText(retryImg.transform, "RETRY", 60, TextAnchor.MiddleCenter, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        ChangeScene retryScene = retryImg.gameObject.AddComponent<ChangeScene>();
        retryScene.sceneName = curScene;

        Image nextImg = AddImage(panel, "NextButton", Spr("button"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(190f, -60f), new Vector2(340f, 136f));
        nextImg.gameObject.AddComponent<Button>();
        AddText(nextImg.transform, "NEXT", 60, TextAnchor.MiddleCenter, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        ChangeScene nextSceneComp = nextImg.gameObject.AddComponent<ChangeScene>();
        nextSceneComp.sceneName = nextScene;

        // 触屏操作 UI（教程第7.7节）
        RectTransform inputUI = NewUI("InputUI", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image padBase = AddImage(inputUI, "VirtualPad", Spr("VirtualPad2D"),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(280f, 220f), new Vector2(570f, 240f));
        Image padTab = AddImage(padBase.transform, "Tab", Spr("VirtualPadTab"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
        VirtualPad pad = padTab.gameObject.AddComponent<VirtualPad>();
        pad.MaxLength = 120f;
        pad.is4DPad = false;

        Image jumpImg = AddImage(inputUI, "JumpButton", Spr("JumpButton"),
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-280f, 240f), new Vector2(230f, 230f));
        jumpImg.gameObject.AddComponent<MobileJumpButton>();

        // 游戏管理对象（时间 + 分数 + 声音）
        GameObject gmGo = new GameObject("GameManager");
        gmGo.transform.SetParent(canvasGo.transform, false);
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.mainImage = mainImage.gameObject;
        gm.startSpr = Spr("GameStart");
        gm.gameOverSpr = Spr("GameOver");
        gm.gameClearSpr = Spr("StageClear");
        gm.panel = panel.gameObject;
        gm.restartButton = retryImg.gameObject;
        gm.nextButton = nextImg.gameObject;
        gm.timeBar = timeBar.gameObject;
        gm.timeText = timeText.gameObject;
        gm.scoreText = scoreText.gameObject;
        gm.bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "BGM_game_00.wav");
        gm.meGameOver = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "ME_GameOver.mp3");
        gm.meGameClear = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "ME_Clear.mp3");
        gm.inputUI = inputUI.gameObject;

        TimeController tc = gmGo.AddComponent<TimeController>();
        tc.isCountDown = true;
        tc.gameTime = gameTime;
        tc.isTimeOver = false;

        AudioSource au = gmGo.AddComponent<AudioSource>();
        au.playOnAwake = false;
        au.loop = false;
    }

    // ============================== 标题界面（第6.2节） ==============================
    static void BuildTitleScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CameraManager cm = BuildCamera("title_back", 0f, 0f, 0f);
        cm.transform.position = new Vector3(0f, 0f, -10f);

        // 标题 LOGO 与角色
        GameObject logo = new GameObject("title_logo");
        SpriteRenderer logoSr = logo.AddComponent<SpriteRenderer>();
        logoSr.sprite = Spr("title_logo");
        logoSr.sortingOrder = 5;
        logo.transform.position = new Vector3(-3.5f, 1.6f, 0f);
        logo.transform.localScale = new Vector3(0.005f, 0.005f, 1f);   // 1500x520 -> 7.5 x 2.6

        GameObject chara = new GameObject("title_chara");
        SpriteRenderer charaSr = chara.AddComponent<SpriteRenderer>();
        charaSr.sprite = Spr("title_chara");
        charaSr.sortingOrder = 5;
        chara.transform.position = new Vector3(5.2f, -1.6f, 0f);
        chara.transform.localScale = new Vector3(0.0035f, 0.0035f, 1f); // 1503x2017 -> 5.3 x 7.1

        // 开始按钮
        GameObject canvasGo = new GameObject("TitleUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler cs = canvasGo.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;

        RectTransform safe = NewUI("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        EnsureEventSystem();

        Image startImg = AddImage(safe, "GameStartButton", Spr("GameStart"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(760f, 147f));
        startImg.gameObject.AddComponent<Button>();
        startImg.gameObject.AddComponent<TitleController>().firstStageName = "Stage1";

        // BGM
        GameObject soundGo = new GameObject("BGM");
        AudioSource au = soundGo.AddComponent<AudioSource>();
        au.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "BGM_game_00.wav");
        au.loop = true;
        au.playOnAwake = true;

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCN + "Title.scene");
        Debug.Log("[YeGE] Title 场景完成");
    }

    // ============================== 第一关（第4~6章） ==============================
    static void BuildStage1()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera("back", -10f, 42f, -10f);

        // 地面：中间留出 3 段宽度为 3 的坑（玩家跳跃水平距离约 3.6）
        GroundRun(-12f, 0f);
        GroundRun(3f, 9f);
        GroundRun(12f, 18f);
        GroundRun(21f, 45f);

        // 坑中的落脚方块与阶梯
        Place("block1x1", new Vector3(10.5f, GROUND_TOP + BLOCK * 0.5f, 0f));
        Place("block1x1", new Vector3(25.0f, GROUND_TOP + BLOCK * 0.5f, 0f));
        Place("block1x2", new Vector3(26.6f, GROUND_TOP + BLOCK, 0f));
        Place("block1x1", new Vector3(28.4f, GROUND_TOP + BLOCK * 0.5f, 0f));

        // 得分道具
        Place("item_red", new Vector3(-9.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_red", new Vector3(-7.6f, GROUND_TOP + 0.8f, 0f));
        Place("item_blue", new Vector3(-3.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_red", new Vector3(5.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_red", new Vector3(6.5f, GROUND_TOP + 0.8f, 0f));
        Place("item_green", new Vector3(15.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_white", new Vector3(26.6f, GROUND_TOP + BLOCK * 2f + 0.8f, 0f));

        // 终点
        Place("goal", new Vector3(40f, GROUND_TOP + 0.6f, 0f));

        // 玩家、死亡区、边界墙
        Place("Player", new Vector3(-10f, GROUND_TOP + 1.1f, 0f));
        DeathZone(-16f, 47f, -7.5f);
        Wall(-12.8f, 0f, 30f);
        Wall(45.8f, 0f, 30f);

        BuildGameUI("Stage1", "Stage2", 60f);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCN + "Stage1.scene");
        Debug.Log("[YeGE] Stage1 场景完成");
    }

    // ============================== 第二关（第7.2 / 7.3 节） ==============================
    static void BuildStage2()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera("back2", -10f, 46f, -10f);

        // 地面：0~6 与 18~24 是两个坑，分别由移动地面搭桥
        GroundRun(-12f, 0f);
        GroundRun(6f, 18f);
        GroundRun(24f, 48f);

        // 自动往返的移动地面（第7.2节）
        GameObject moveBlock1 = Place("MovingBlock", new Vector3(1.0f, GROUND_TOP + 0.35f, 0f));
        MovingBlock mb1 = moveBlock1.GetComponent<MovingBlock>();
        mb1.moveX = 4.0f;
        mb1.moveY = 0.0f;
        mb1.times = 3.0f;
        mb1.weight = 1.0f;
        mb1.isMoveWhenOn = false;
        mb1.isCanMove = true;

        // 尖刺（碰到即失败）
        Place("needle", new Vector3(8.5f, GROUND_TOP + 0.24f, 0f));
        Place("needle", new Vector3(9.7f, GROUND_TOP + 0.24f, 0f));

        // 开关 + 联动的移动地面（第7.3节）
        GameObject moveBlock2 = Place("MovingBlock", new Vector3(19.0f, GROUND_TOP + 0.35f, 0f));
        MovingBlock mb2 = moveBlock2.GetComponent<MovingBlock>();
        mb2.moveX = 4.0f;
        mb2.moveY = 0.0f;
        mb2.times = 3.0f;
        mb2.weight = 1.0f;
        mb2.isMoveWhenOn = true;   // 初始静止：踩上去或拉下拉杆后启动

        GameObject sw = Place("Switch", new Vector3(15.5f, GROUND_TOP + 0.5f, 0f));
        SwitchAction swAct = sw.GetComponent<SwitchAction>();
        swAct.targetMoveBlock = moveBlock2;

        // 道具
        Place("item_red", new Vector3(-9.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_blue", new Vector3(-3.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_red", new Vector3(7.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_green", new Vector3(13.0f, GROUND_TOP + 1.6f, 0f));
        Place("item_blue", new Vector3(27.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_green", new Vector3(33.0f, GROUND_TOP + 0.8f, 0f));

        // 终点
        Place("goal", new Vector3(44f, GROUND_TOP + 0.6f, 0f));

        Place("Player", new Vector3(-10f, GROUND_TOP + 1.1f, 0f));
        DeathZone(-16f, 50f, -7.5f);
        Wall(-12.8f, 0f, 30f);
        Wall(48.8f, 0f, 30f);

        BuildGameUI("Stage2", "Stage3", 90f);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCN + "Stage2.scene");
        Debug.Log("[YeGE] Stage2 场景完成");
    }

    // ============================== 第三关（第7.1 / 7.4 / 7.5 节） ==============================
    static void BuildStage3()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera("back", -10f, 47f, -10f);

        // 地面：0~12 是一段深坑，由会塌陷的方块桥连接
        GroundRun(-12f, 0f);
        GroundRun(12f, 48f);

        // 会塌陷的方块桥（第7.1节）
        for (int i = 0; i < 6; i++)
        {
            Place("GimmickBlock", new Vector3(1.2f + i * 1.8f, GROUND_TOP - BLOCK * 0.5f, 0f));
        }

        // 巡逻的敌方角色（第7.5节）
        PlaceEnemy(1, new Vector3(16f, GROUND_TOP + 0.76f, 0f), 8.0f, 2.0f);
        PlaceEnemy(2, new Vector3(26f, GROUND_TOP + 0.76f, 0f), 8.0f, 2.5f);
        PlaceEnemy(3, new Vector3(36f, GROUND_TOP + 0.76f, 0f), 6.0f, 3.0f);
        Wall(12.6f, -1.0f, 6f);
        Wall(47.4f, -1.0f, 6f);

        // 固定炮台（第7.4节）
        Place("cannon", new Vector3(33f, GROUND_TOP + 0.545f, 0f));

        // 尖刺
        Place("needle", new Vector3(22f, GROUND_TOP + 0.24f, 0f));

        // 道具
        Place("item_red", new Vector3(-9.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_blue", new Vector3(-3.0f, GROUND_TOP + 0.8f, 0f));
        Place("item_green", new Vector3(14.0f, GROUND_TOP + 0.8f, 0f));
        Place("block1x1", new Vector3(30.5f, GROUND_TOP + BLOCK * 0.5f, 0f));
        Place("item_white", new Vector3(30.5f, GROUND_TOP + BLOCK + 0.8f, 0f));
        Place("item_green", new Vector3(40.0f, GROUND_TOP + 0.8f, 0f));

        // 终点
        Place("goal", new Vector3(45f, GROUND_TOP + 0.6f, 0f));

        Place("Player", new Vector3(-10f, GROUND_TOP + 1.1f, 0f));
        DeathZone(-16f, 50f, -7.5f);
        Wall(-12.8f, 0f, 30f);
        Wall(48.8f, 0f, 30f);

        BuildGameUI("Stage3", "Result", 120f);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCN + "Stage3.scene");
        Debug.Log("[YeGE] Stage3 场景完成");
    }

    // ============================== 结果界面（第6.6节） ==============================
    static void BuildResultScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera("title_back", 0f, 0f, 0f);

        GameObject canvasGo = new GameObject("ResultUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler cs = canvasGo.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;

        RectTransform safe = NewUI("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        EnsureEventSystem();

        Image clearImg = AddImage(safe, "StageClear", Spr("StageClear"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(800f, 154f));

        Image board = AddImage(safe, "ScoreBoard", Spr("ScoreBoard"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(600f, 241f));
        Text scoreText = AddText(board.transform, "0", 110, TextAnchor.MiddleCenter, new Vector2(240f, 30f), new Vector2(-40f, -40f));

        Image titleBtn = AddImage(safe, "TitleButton", Spr("button"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -230f), new Vector2(400f, 160f));
        titleBtn.gameObject.AddComponent<Button>();
        AddText(titleBtn.transform, "TITLE", 60, TextAnchor.MiddleCenter, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        titleBtn.gameObject.AddComponent<ChangeScene>().sceneName = "Title";

        GameObject rmGo = new GameObject("ResultManager");
        rmGo.transform.SetParent(canvasGo.transform, false);
        ResultManager rm = rmGo.AddComponent<ResultManager>();
        rm.scoreText = scoreText.gameObject;

        GameObject soundGo = new GameObject("BGM");
        AudioSource au = soundGo.AddComponent<AudioSource>();
        au.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SND + "BGM_game_00.wav");
        au.loop = true;
        au.playOnAwake = true;

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCN + "Result.scene");
        Debug.Log("[YeGE] Result 场景完成");
    }

    // ============================== 构建设置 ==============================
    static void ApplyBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(SCN + "Title.scene", true),
            new EditorBuildSettingsScene(SCN + "Stage1.scene", true),
            new EditorBuildSettingsScene(SCN + "Stage2.scene", true),
            new EditorBuildSettingsScene(SCN + "Stage3.scene", true),
            new EditorBuildSettingsScene(SCN + "Result.scene", true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[YeGE] 构建场景列表已设置");
    }

    // ============================== 小工具 ==============================
    static Sprite Spr(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(IMG + name + ".png");
    }

    static Sprite SprP(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(PLR + name + ".png");
    }

    static GameObject SaveAsPrefab(GameObject go, string assetName)
    {
        string path = PFB + assetName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
