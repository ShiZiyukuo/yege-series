using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 《The adventures of YeGe 3》一键生成：标签层 → 动画控制器 → 预制体 →
/// 五个场景（Title / Stage1 / Stage2 / BossStage / Result）→ BuildSettings → 自检。
/// 菜单：YG3/1 导入素材 → YG3/2 生成全部内容（YG3/3 批处理构建并退出）。
/// </summary>
public static class YG3Builder
{
    const string PFB = "Assets/Prefab/";
    const string ANIM = "Assets/Prefab/Anim/";
    const string SCN = "Assets/Scenes/";

    const int LAYER_GROUND = 8;
    const int LAYER_ENEMY = 9;
    const int LAYER_BOSS = 10;
    const int LAYER_PATK = 11;
    const int LAYER_BATK = 12;

    // 地面顶面高度 y=0；ground.png 6x2.4；block 1.2
    const float GROUND_TOP = 0f;
    const float GROUND_H = 2.4f;

    // ================================================================ 入口

    public static void ImportAssets()
    {
        YG3ImportSettings.ApplyAll();
    }

    [MenuItem("YG3/2. 生成全部内容")]
    public static void BuildAll()
    {
        YG3ImportSettings.ApplyAll();     // 切片先行（幂等）
        EnsureTagsAndLayers();
        EnsureLayerMatrix();
        PhysicsMaterial2D noFric = EnsureFrictionMaterial();

        AnimatorController playerAnim = CreatePlayerAnimator();
        AnimatorController hogAnim = CreateHedgehogAnimator();
        AnimatorController bossAnim = CreateBossAnimator();

        GameObject player = CreatePlayerPrefab(noFric, playerAnim);
        GameObject hog = CreateHedgehogPrefab(noFric, hogAnim);
        GameObject boss = CreateBossPrefab(noFric, bossAnim);
        GameObject arrow = CreateSimpleProjectilePrefab("Arrow", "PlayerImage_10", new Vector2(1.0f, 0.6f), LAYER_PATK, true);
        GameObject needle = CreateSimpleProjectilePrefab("Needle", "needle_gold", new Vector2(1.7f, 0.5f), LAYER_BATK, true);
        needle.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        GameObject shock = CreateShockwavePrefab();
        GameObject slash = CreateFxPrefab("slash", "slash", 9);
        GameObject parry = CreateFxPrefab("parry", "parry", 9);
        GameObject spark = CreateFxPrefab("spark", "spark", 9, 0.5f);
        GameObject item = CreateItemPrefab();
        GameObject goal = CreateGoalPrefab();

        BuildTitleScene();
        BuildStageScene("Stage1", BuildStage1Content, YG3Tuning.BGM_GAME, false);
        BuildStageScene("Stage2", BuildStage2Content, YG3Tuning.BGM_GAME, false);
        BuildStageScene("BossStage", (root) => BuildBossStageContent(root, boss, hog), YG3Tuning.BGM_BOSS, true);
        BuildResultScene();

        ApplyBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Verify();
        Debug.Log("[YG3] 全部内容生成完成");
    }

    static void ApplyBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        string[] names = { "Title", "Stage1", "Stage2", "BossStage", "Result" };
        foreach (string n in names)
        {
            scenes.Add(new EditorBuildSettingsScene(SCN + n + ".unity", true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[YG3] BuildSettings 场景列表已写入");
    }

    [MenuItem("YG3/3. 批处理构建并退出")]
    public static void BuildAllAndExit()
    {
        BuildAll();
        EditorApplication.Exit(0);
    }

    [MenuItem("YG3/4. 自检")]
    public static void Verify()
    {
        int errors = 0;
        string[] scenes = { "Title", "Stage1", "Stage2", "BossStage", "Result" };
        foreach (string s in scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCN + s + ".unity") == null)
            {
                Debug.LogError("[YG3] 缺少场景: " + s);
                errors++;
            }
        }
        string[] prefabs = { "Player", "Hedgehog", "Boss", "Arrow", "Needle", "Shockwave", "slash", "parry", "spark", "Item", "Goal" };
        foreach (string p in prefabs)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PFB + p + ".prefab") == null)
            {
                Debug.LogError("[YG3] 缺少预制体: " + p);
                errors++;
            }
        }
        if (!Physics2D.GetIgnoreLayerCollision(LAYER_BATK, LAYER_BOSS))
        {
            Debug.LogError("[YG3] 层矩阵未关闭 BossAttack×Boss");
            errors++;
        }
        if (errors == 0)
        {
            Debug.Log("[YG3] 自检 OK：5 场景 / 11 预制体 / 层矩阵 就绪");
        }
    }

    // ================================================================ 标签与层

    static void EnsureTagsAndLayers()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        SerializedObject so = new SerializedObject(assets[0]);

        SerializedProperty tags = so.FindProperty("tags");
        AddTag(tags, "Enemy");
        AddTag(tags, "Boss");
        AddTag(tags, "Ground");
        AddTag(tags, "Wall");
        AddTag(tags, "Goal");
        AddTag(tags, "DeadZone");
        AddTag(tags, "ScoreItem");

        SerializedProperty layers = so.FindProperty("layers");
        SetLayer(layers, LAYER_GROUND, "Ground");
        SetLayer(layers, LAYER_ENEMY, "Enemy");
        SetLayer(layers, LAYER_BOSS, "Boss");
        SetLayer(layers, LAYER_PATK, "PlayerAttack");
        SetLayer(layers, LAYER_BATK, "BossAttack");

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[YG3] 标签与层设置完成");
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

    static void SetLayer(SerializedProperty layers, int index, string name)
    {
        SerializedProperty p = layers.GetArrayElementAtIndex(index);
        if (p.stringValue != name)
        {
            p.stringValue = name;
        }
    }

    static void EnsureLayerMatrix()
    {
        // Boss 弹不撞 Boss/小怪；玩家攻击层之间互不干扰
        Physics2D.IgnoreLayerCollision(LAYER_BATK, LAYER_BOSS, true);
        Physics2D.IgnoreLayerCollision(LAYER_BATK, LAYER_ENEMY, true);
        Physics2D.IgnoreLayerCollision(LAYER_PATK, LAYER_PATK, true);
        Debug.Log("[YG3] 层碰撞矩阵设置完成");
    }

    static PhysicsMaterial2D EnsureFrictionMaterial()
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

    // ================================================================ 动画

    static Sprite S(string name)
    {
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Player/" + name + ".png");
        if (sp != null)
        {
            return sp;
        }
        sp = AssetDatabase.LoadAssetAtPath<Sprite>(YG3Tuning.DIR_FX + name + ".png");
        if (sp != null)
        {
            return sp;
        }
        string[] sheets =
        {
            "Assets/Images/Hedgehog.png",
            "Assets/Images/Boss.png",
            "Assets/Images/PlayerImage.png",
        };
        foreach (string sheet in sheets)
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(sheet);
            foreach (Object o in all)
            {
                if (o is Sprite && o.name == name)
                {
                    return (Sprite)o;
                }
            }
        }
        Debug.LogError("[YG3] 找不到精灵: " + name);
        return null;
    }

    static AnimationClip SpriteClip(string assetName, string[] sprites, float fps, bool loop)
    {
        string path = ANIM + assetName + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
        {
            return clip;
        }
        clip = new AnimationClip();
        clip.frameRate = fps;
        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++)
        {
            frames[i] = new ObjectReferenceKeyframe();
            frames[i].time = i / fps;
            frames[i].value = S(sprites[i]);
        }
        frames[sprites.Length] = new ObjectReferenceKeyframe();
        frames[sprites.Length].time = sprites.Length / fps;
        frames[sprites.Length].value = S(sprites[0]);

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite",
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (!AssetDatabase.IsValidFolder("Assets/Prefab/Anim"))
        {
            AssetDatabase.CreateFolder("Assets/Prefab", "Anim");
        }
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static AnimatorController MakeController(string assetName, string[] stateNames, AnimationClip[] clips)
    {
        string path = PFB + assetName + ".controller";
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        for (int i = 0; i < stateNames.Length; i++)
        {
            AnimatorState st = sm.AddState(stateNames[i]);
            st.motion = clips[i];
        }
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    static AnimatorController CreatePlayerAnimator()
    {
        return MakeController("PlayerAnime",
            new[] { "PlayerStop", "PlayerMove", "PlayerJump", "PlayerDead" },
            new[]
            {
                SpriteClip("PlayerStop", new[] { "player_stop" }, 1f, true),
                SpriteClip("PlayerMove", new[] { "player_run1", "player_run2", "player_run3", "player_run4", "player_run5", "player_run6", "player_run7" }, 12f, true),
                SpriteClip("PlayerJump", new[] { "Player_jump1", "Player_jump2" }, 8f, true),
                SpriteClip("PlayerDead", new[] { "player_over" }, 1f, false),
            });
    }

    static AnimatorController CreateHedgehogAnimator()
    {
        return MakeController("HedgehogAnime",
            new[] { "HedgehogWalk", "HedgehogCurl", "HedgehogDead" },
            new[]
            {
                SpriteClip("HedgehogWalk", new[] { "Hedgehog_0", "Hedgehog_1", "Hedgehog_2", "Hedgehog_3" }, 8f, true),
                SpriteClip("HedgehogCurl", new[] { "HedgehogCurl" }, 1f, true),
                SpriteClip("HedgehogDead", new[] { "HedgehogDead" }, 1f, false),
            });
    }

    static AnimatorController CreateBossAnimator()
    {
        return MakeController("BossAnime",
            new[] { "BossIdle", "BossCharge", "BossVolley", "BossSlam", "BossDead" },
            new[]
            {
                SpriteClip("BossIdle", new[] { "Boss_0" }, 1f, true),
                SpriteClip("BossCharge", new[] { "Boss_1" }, 1f, true),
                SpriteClip("BossVolley", new[] { "Boss_2" }, 1f, true),
                SpriteClip("BossSlam", new[] { "Boss_3" }, 1f, true),
                SpriteClip("BossDead", new[] { "Boss_4" }, 1f, false),
            });
    }

    // ================================================================ 预制体

    static GameObject SavePrefab(GameObject go, string name)
    {
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, PFB + name + ".prefab");
        Object.DestroyImmediate(go);
        return asset;
    }

    static GameObject CreatePlayerPrefab(PhysicsMaterial2D noFric, AnimatorController anim)
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.layer = 0;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S("player_stop");
        sr.sortingOrder = 5;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = YG3Tuning.PLAYER_GRAVITY;   // 加重重力让跳跃干脆

        CapsuleCollider2D col = go.AddComponent<CapsuleCollider2D>();
        // 对齐 YeGe 原版：精灵 180px(PPU100)=1.8 单位、pivot 居中，
        // 碰撞体高 1.7 且中心贴近原点 → 覆盖 y∈[-0.87,0.83]，与精灵吻合
        col.size = new Vector2(1f, 1.7f);
        col.offset = new Vector2(0f, -0.02f);
        col.sharedMaterial = noFric;

        Animator an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = anim;

        go.AddComponent<YG3PlayerController>();
        go.AddComponent<YG3SwordAttack>();
        go.AddComponent<YG3BowAttack>();

        YG3Health hp = go.AddComponent<YG3Health>();
        hp.maxHp = YG3Tuning.PLAYER_MAX_HP;
        hp.poiseMax = YG3Tuning.PLAYER_POISE;
        hp.poiseRegen = YG3Tuning.PLAYER_POISE_REGEN;
        hp.stunTime = YG3Tuning.PLAYER_STUN;
        hp.invulnTime = YG3Tuning.PLAYER_INVULN;

        return SavePrefab(go, "Player");
    }

    static GameObject CreateHedgehogPrefab(PhysicsMaterial2D noFric, AnimatorController anim)
    {
        GameObject go = new GameObject("Hedgehog");
        go.tag = "Enemy";
        go.layer = LAYER_ENEMY;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S("Hedgehog_0");
        sr.sortingOrder = 4;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        // 对齐精灵主体（160px=1.6 单位、pivot 0.12）：覆盖 y∈[-0.055, 1.295]
        col.size = new Vector2(1.2f, 1.35f);
        col.offset = new Vector2(0f, 0.62f);
        col.sharedMaterial = noFric;

        Animator an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = anim;

        YG3Health hp = go.AddComponent<YG3Health>();
        hp.maxHp = YG3Tuning.HEDGEHOG_HP;
        hp.poiseMax = 0f;
        hp.invulnTime = 0.25f;

        go.AddComponent<YG3Hedgehog>();
        return SavePrefab(go, "Hedgehog");
    }

    static GameObject CreateBossPrefab(PhysicsMaterial2D noFric, AnimatorController anim)
    {
        GameObject go = new GameObject("Boss");
        go.tag = "Boss";
        go.layer = LAYER_BOSS;
        go.transform.localScale = new Vector3(3f, 3f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S("Boss_0");
        sr.sortingOrder = 4;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.72f, 1.35f);
        col.offset = new Vector2(0f, 0.62f);

        Animator an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = anim;

        GameObject gate = new GameObject("gate");
        gate.transform.SetParent(go.transform, false);
        gate.transform.localPosition = new Vector3(0f, 1.4f, 0f);

        YG3Health hp = go.AddComponent<YG3Health>();
        hp.maxHp = YG3Tuning.BOSS_MAX_HP;
        hp.poiseMax = 0f;          // Boss 用自己的架势槽（PoiseHit 事件驱动）
        hp.invulnTime = 0.1f;
        hp.stunTime = 0f;

        YG3JinZuWuBoss bs = go.AddComponent<YG3JinZuWuBoss>();
        bs.gate = gate.transform;

        return SavePrefab(go, "Boss");
    }

    /// <summary>弹体通用：贴图 + 触发盒 + 刚体 + 指定脚本。</summary>
    static GameObject CreateSimpleProjectilePrefab(string name, string spriteName, Vector2 boxSize, int layer, bool addRb)
    {
        GameObject go = new GameObject(name);
        go.layer = layer;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S(spriteName);
        sr.sortingOrder = 7;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = boxSize;
        if (addRb)
        {
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }
        switch (name)
        {
            case "Arrow": go.AddComponent<YG3Arrow>(); break;
            case "Needle": go.AddComponent<YG3Needle>(); break;
        }
        return SavePrefab(go, name);
    }

    static GameObject CreateShockwavePrefab()
    {
        GameObject go = new GameObject("Shockwave");
        go.layer = LAYER_BATK;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(YG3Tuning.DIR_FX + "shock.png");
        sr.sortingOrder = 8;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.7f, 1.0f);
        col.offset = new Vector2(0f, 0.5f);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        go.AddComponent<YG3Shockwave>();
        return SavePrefab(go, "Shockwave");
    }

    static GameObject CreateFxPrefab(string name, string spriteName, int order, float scale = 1f)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(YG3Tuning.DIR_FX + spriteName + ".png");
        sr.sortingOrder = order;
        go.transform.localScale = Vector3.one * scale;
        return SavePrefab(go, name);
    }

    static GameObject CreateItemPrefab()
    {
        GameObject go = new GameObject("Item");
        go.tag = "ScoreItem";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(YG3Tuning.DIR_IMG + "item_red.png");
        sr.sortingOrder = 3;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.42f;
        return SavePrefab(go, "Item");
    }

    static GameObject CreateGoalPrefab()
    {
        GameObject go = new GameObject("Goal");
        go.tag = "Goal";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(YG3Tuning.DIR_IMG + "goal.png");
        sr.sortingOrder = 3;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.0f, 1.1f);
        col.offset = new Vector2(0f, 0.55f);
        return SavePrefab(go, "Goal");
    }

    // ================================================================ 场景通用

    static GameObject LoadPrefab(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PFB + name + ".prefab");
    }

    static Sprite Img(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(YG3Tuning.DIR_IMG + name + ".png");
    }

    static Font BuiltinFont()
    {
        Font f = null;
        try
        {
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch { }
        if (f == null)
        {
            try
            {
                f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch { }
        }
        return f;
    }

    /// <summary>通用舞台：先开空场景，再搭相机/背景/事件系统/声音/FX 库/GameManager/UI。</summary>
    static YG3GameManager BuildStageFrame(EditorScene scene, string name, string bgm, bool isBoss)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("Root");

        // ---- 相机 ----
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.position = new Vector3(0f, 2f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.09f, 0.14f);
        GameObject bgGo = new GameObject("CameraBG");
        bgGo.transform.SetParent(camGo.transform, false);
        bgGo.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        bgGo.transform.localPosition = new Vector3(0f, 0f, 10f);
        SpriteRenderer bg = bgGo.AddComponent<SpriteRenderer>();
        bg.sprite = Img("back2");
        bg.sortingOrder = -10;
        YG3CameraManager camMgr = camGo.AddComponent<YG3CameraManager>();

        // ---- 事件系统（历史踩坑：缺 EventSystem 全部 UI 点击失效）----
        GameObject es = new GameObject("EventSystem");
        es.transform.SetParent(root.transform, false);
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // ---- 声音 ----
        GameObject snd = new GameObject("SoundManager");
        snd.transform.SetParent(root.transform, false);
        YG3SoundManager sm = snd.AddComponent<YG3SoundManager>();
        sm.clips = BuildSoundClips();
        sm.bgmClip = FindClip(bgm);
        sm.bgmVolume = 0.4f;

        // ---- FX 库 ----
        GameObject libGo = new GameObject("YG3FxLibrary");
        libGo.transform.SetParent(root.transform, false);
        YG3FxLibrary lib = libGo.AddComponent<YG3FxLibrary>();
        lib.slash = LoadPrefab("slash");
        lib.parry = LoadPrefab("parry");
        lib.spark = LoadPrefab("spark");
        lib.arrow = LoadPrefab("Arrow");
        lib.needle = LoadPrefab("Needle");
        lib.shockwave = LoadPrefab("Shockwave");
        lib.hedgehog = LoadPrefab("Hedgehog");

        // ---- UI ----
        YG3GameManager gm = BuildUI(root, bgm, isBoss);

        scene.root = root;
        scene.cameraManager = camMgr;
        scene.gameManager = gm;
        return gm;
    }

    class EditorScene
    {
        public GameObject root;
        public YG3CameraManager cameraManager;
        public YG3GameManager gameManager;
    }

    static List<YG3SoundManager.NamedClip> BuildSoundClips()
    {
        string[] names =
        {
            YG3Tuning.SE_SHOOT, YG3Tuning.SE_DAMAGE, YG3Tuning.SE_KILLED,
            YG3Tuning.SE_BOSS_KILLED, YG3Tuning.SE_GET, YG3Tuning.SE_BUTTON,
            YG3Tuning.ME_CLEAR, YG3Tuning.ME_GAME_OVER,
            YG3Tuning.BGM_TITLE, YG3Tuning.BGM_GAME, YG3Tuning.BGM_BOSS,
        };
        List<YG3SoundManager.NamedClip> list = new List<YG3SoundManager.NamedClip>();
        foreach (string n in names)
        {
            AudioClip clip = FindClip(n);
            if (clip != null)
            {
                list.Add(new YG3SoundManager.NamedClip { name = n, clip = clip });
            }
        }
        return list;
    }

    static AudioClip FindClip(string name)
    {
        string[] ext = { ".wav", ".mp3" };
        foreach (string e in ext)
        {
            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(YG3Tuning.DIR_SND + name + e);
            if (c != null)
            {
                return c;
            }
        }
        return null;
    }

    // ================================================================ UI

    static YG3GameManager BuildUI(GameObject root, string bgm, bool isBoss)
    {
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(root.transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<YG3SafeAreaFitter>();

        Font font = BuiltinFont();
        YG3GameManager gm = new GameObject("GameManager").AddComponent<YG3GameManager>();
        gm.transform.SetParent(root.transform, false);
        gm.bgmName = bgm;

        // ---- 玩家 HP / 耐力条（左上）----
        Image hpBg = MakeImage(canvasGo.transform, "HPBg", null, new Color(0f, 0f, 0f, 0.55f),
            new Vector2(0.04f, 0.94f), new Vector2(420f, 26f), new Vector2(220f, -6f));
        // Filled 模式必须有真实 sprite，否则 Unity 回退绘制整条矩形并忽略 fillAmount
        Image hpFill = MakeImage(hpBg.transform, "HPFill", Img("UiWhite"), new Color(0.9f, 0.25f, 0.2f),
            Vector2.zero, Vector2.zero, Vector2.zero);
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.rectTransform.anchorMin = new Vector2(0f, 0f);
        hpFill.rectTransform.anchorMax = new Vector2(1f, 1f);
        hpFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        hpFill.rectTransform.offsetMin = new Vector2(4f, 4f);
        hpFill.rectTransform.offsetMax = new Vector2(-4f, -4f);

        Image stBg = MakeImage(canvasGo.transform, "StBg", null, new Color(0f, 0f, 0f, 0.55f),
            new Vector2(0.04f, 0.94f), new Vector2(420f, 14f), new Vector2(220f, -40f));
        Image stFill = MakeImage(stBg.transform, "StFill", Img("UiWhite"), new Color(0.55f, 0.85f, 0.35f),
            Vector2.zero, Vector2.zero, Vector2.zero);
        stFill.type = Image.Type.Filled;
        stFill.fillMethod = Image.FillMethod.Horizontal;
        stFill.rectTransform.anchorMin = new Vector2(0f, 0f);
        stFill.rectTransform.anchorMax = new Vector2(1f, 1f);
        stFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        stFill.rectTransform.offsetMin = new Vector2(3f, 3f);
        stFill.rectTransform.offsetMax = new Vector2(-3f, -3f);

        // ---- 分数 / 连击 ----
        Text score = MakeText(canvasGo.transform, "Score", "0", 44, Color.white, font,
            new Vector2(0.96f, 0.95f), new Vector2(300f, 60f), new Vector2(-140f, -10f));
        Text combo = MakeText(canvasGo.transform, "Combo", "", 52, new Color(1f, 0.85f, 0.3f), font,
            new Vector2(0.5f, 1f), new Vector2(500f, 70f), new Vector2(0f, -150f));
        combo.gameObject.SetActive(false);
        combo.fontStyle = FontStyle.Bold;

        // ---- 中央提示图 ----
        Image mainImage = MakeImage(canvasGo.transform, "MainImage", Img("GameStart"), Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(640f, 123f), Vector2.zero);
        mainImage.raycastTarget = false;

        // ---- 结算面板 ----
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.65f);
        SetRect(panelBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 300f), Vector2.zero);
        panel.SetActive(false);

        Button restart = MakeButton(panel.transform, "Restart", Img("button"), "RETRY", font,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 55f));
        Button next = MakeButton(panel.transform, "Next", Img("button"), "NEXT", font,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -75f));
        restart.onClick.AddListener(gm.OnRestart);
        next.onClick.AddListener(gm.OnNext);

        // ---- 触屏操作（摇杆 + 5 键）----
        GameObject inputUI = new GameObject("InputUI");
        inputUI.transform.SetParent(canvasGo.transform, false);
        inputUI.SetActive(false);

        GameObject pad = new GameObject("VirtualPad");
        pad.transform.SetParent(inputUI.transform, false);
        Image padBg = pad.AddComponent<Image>();
        padBg.sprite = Img("VirtualPad2D");
        SetRect(padBg.rectTransform, new Vector2(0.03f, 0.06f), new Vector2(380f, 160f), Vector2.zero);
        padBg.color = new Color(1f, 1f, 1f, 0.75f);
        GameObject thumb = new GameObject("Thumb");
        thumb.transform.SetParent(pad.transform, false);
        Image thumbImg = thumb.AddComponent<Image>();
        thumbImg.sprite = Img("VirtualPadTab");
        thumbImg.rectTransform.sizeDelta = new Vector2(110f, 110f);
        YG3VirtualPad padScript = pad.AddComponent<YG3VirtualPad>();
        padScript.thumb = thumbImg.rectTransform;

        MakeMobileButton(inputUI.transform, "BtnJump", Img("JumpButton"), null, YG3MobileButton.Kind.Jump,
            new Vector2(0.92f, 0.14f), new Vector2(130f, 130f), new Vector2(0f, 0f));
        MakeMobileButton(inputUI.transform, "BtnAttack", Img("AttackButton"), "J", YG3MobileButton.Kind.Attack,
            new Vector2(0.80f, 0.14f), new Vector2(130f, 130f), new Vector2(0f, 0f));
        MakeMobileButton(inputUI.transform, "BtnBow", Img("AttackButton"), "K", YG3MobileButton.Kind.Bow,
            new Vector2(0.86f, 0.28f), new Vector2(110f, 110f), new Vector2(0f, 0f));
        MakeMobileButton(inputUI.transform, "BtnGuard", Img("button"), "L", YG3MobileButton.Kind.Guard,
            new Vector2(0.72f, 0.28f), new Vector2(110f, 110f), new Vector2(0f, 0f));
        MakeMobileButton(inputUI.transform, "BtnRoll", Img("button"), "Shift", YG3MobileButton.Kind.Roll,
            new Vector2(0.20f, 0.30f), new Vector2(110f, 110f), new Vector2(0f, 0f));

        // ---- Boss 血条（BossStage 专属）----
        if (isBoss)
        {
            GameObject barGo = new GameObject("BossBar");
            barGo.transform.SetParent(canvasGo.transform, false);
            RectTransform barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 1f);
            barRt.anchorMax = new Vector2(0.5f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(900f, 70f);
            CanvasGroup group = barGo.AddComponent<CanvasGroup>();

            Image hpB = MakeImage(barGo.transform, "BossHpBg", null, new Color(0f, 0f, 0f, 0.6f),
                new Vector2(0.5f, 1f), new Vector2(900f, 34f), new Vector2(0f, -26f));
            hpB.raycastTarget = false;
            Image hpF = MakeImage(hpB.transform, "BossHpFill", Img("UiWhite"), new Color(1f, 0.76f, 0.28f),
                Vector2.zero, Vector2.zero, Vector2.zero);
            hpF.raycastTarget = false;
            hpF.type = Image.Type.Filled;
            hpF.fillMethod = Image.FillMethod.Horizontal;
            hpF.rectTransform.anchorMin = new Vector2(0f, 0f);
            hpF.rectTransform.anchorMax = new Vector2(1f, 1f);
            hpF.rectTransform.pivot = new Vector2(0f, 0.5f);
            hpF.rectTransform.offsetMin = new Vector2(4f, 4f);
            hpF.rectTransform.offsetMax = new Vector2(-4f, -4f);

            Image poB = MakeImage(barGo.transform, "PostureBg", null, new Color(0f, 0f, 0f, 0.6f),
                new Vector2(0.5f, 1f), new Vector2(900f, 16f), new Vector2(0f, -64f));
            poB.raycastTarget = false;
            Image poF = MakeImage(poB.transform, "PostureFill", Img("UiWhite"), new Color(1f, 0.35f, 0.2f),
                Vector2.zero, Vector2.zero, Vector2.zero);
            poF.raycastTarget = false;
            poF.type = Image.Type.Filled;
            poF.fillMethod = Image.FillMethod.Horizontal;
            poF.rectTransform.anchorMin = new Vector2(0f, 0f);
            poF.rectTransform.anchorMax = new Vector2(1f, 1f);
            poF.rectTransform.pivot = new Vector2(0f, 0.5f);
            poF.rectTransform.offsetMin = new Vector2(3f, 3f);
            poF.rectTransform.offsetMax = new Vector2(-3f, -3f);

            YG3BossHealthBar bar = barGo.AddComponent<YG3BossHealthBar>();
            bar.hpFill = hpF;
            bar.postureFill = poF;
            bar.group = group;
        }

        // ---- GameManager 引用装配 ----
        gm.hpFill = hpFill;
        gm.staminaFill = stFill;
        gm.scoreText = score;
        gm.comboText = combo;
        gm.mainImage = mainImage.gameObject;
        gm.startSpr = Img("GameStart");
        gm.clearSpr = Img("StageClear");
        gm.gameOverSpr = Img("GameOver");
        gm.panel = panel;
        gm.restartButton = restart.gameObject;
        gm.nextButton = next.gameObject;
        gm.inputUI = inputUI;
        return gm;
    }

    static void SetRect(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    static Image MakeImage(Transform parent, string name, Sprite sprite, Color color,
        Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        SetRect(img.rectTransform, anchor, size, pos);
        return img;
    }

    static Text MakeText(Transform parent, string name, string content, int size, Color color,
        Font font, Vector2 anchor, Vector2 sizeDelta, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = font;
        SetRect(t.rectTransform, anchor, sizeDelta, pos);
        return t;
    }

    static Button MakeButton(Transform parent, string name, Sprite sprite, string label, Font font,
        Vector2 anchor, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        SetRect(img.rectTransform, anchor, new Vector2(360f, 120f), pos);
        if (!string.IsNullOrEmpty(label) && font != null)
        {
            GameObject lt = new GameObject("Label");
            lt.transform.SetParent(go.transform, false);
            Text t = lt.AddComponent<Text>();
            t.text = label;
            t.fontSize = 44;
            t.font = font;
            t.color = new Color(0.2f, 0.14f, 0.06f);
            t.alignment = TextAnchor.MiddleCenter;
            RectTransform rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        return btn;
    }

    static void MakeMobileButton(Transform parent, string name, Sprite sprite, string label,
        YG3MobileButton.Kind kind, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = true;
        if (name == "BtnBow")
        {
            img.color = new Color(0.7f, 0.9f, 1f);
        }
        if (name == "BtnGuard" || name == "BtnRoll")
        {
            img.color = new Color(1f, 1f, 1f, 0.8f);
        }
        SetRect(img.rectTransform, anchor, size, pos);
        go.AddComponent<YG3MobileButton>().kind = kind;
        if (!string.IsNullOrEmpty(label))
        {
            Font font = BuiltinFont();
            if (font != null)
            {
                GameObject lt = new GameObject("Label");
                lt.transform.SetParent(go.transform, false);
                Text t = lt.AddComponent<Text>();
                t.text = label;
                t.fontSize = 40;
                t.font = font;
                t.color = new Color(0.15f, 0.12f, 0.1f);
                t.alignment = TextAnchor.MiddleCenter;
                t.fontStyle = FontStyle.Bold;
                RectTransform rt = t.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }
        }
    }

    // ================================================================ 关卡元素

    static void AddGroundPiece(Transform parent, float x)
    {
        GameObject go = new GameObject("Ground_" + x.ToString("0"));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, GROUND_TOP - GROUND_H / 2f, 0f);
        go.layer = LAYER_GROUND;
        go.tag = "Ground";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Img("ground");
        sr.sortingOrder = 1;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(6f, GROUND_H);
    }

    static void AddGround(Transform parent, float fromX, float toX)
    {
        for (float x = fromX + 3f; x < toX - 0.01f; x += 6f)
        {
            AddGroundPiece(parent, x);
        }
    }

    static void AddBlock(Transform parent, string sprite, float x, float y)
    {
        GameObject go = new GameObject("Block_" + sprite + "_" + x.ToString("0") + "_" + y.ToString("0"));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.layer = LAYER_GROUND;
        go.tag = "Ground";
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Img(sprite);
        sr.sortingOrder = 1;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = sprite == "block1x2" ? new Vector2(1.2f, 2.4f) :
                   sprite == "block2x1" ? new Vector2(2.4f, 1.2f) : new Vector2(1.2f, 1.2f);
    }

    static void AddWallBox(Transform parent, float x, float y, float w, float h)
    {
        GameObject go = new GameObject("Wall");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.layer = LAYER_GROUND;
        go.tag = "Wall";
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);
    }

    static void AddDeadZone(Transform parent, float centerX, float width)
    {
        GameObject go = new GameObject("DeadZone");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, -9f, 0f);
        go.tag = "DeadZone";
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, 2f);
    }

    static GameObject Place(Transform parent, GameObject prefab, float x, float y)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        return go;
    }

    static void AddItems(Transform parent, GameObject prefab, params float[] xs)
    {
        foreach (float x in xs)
        {
            Place(parent, prefab, x, 1.4f);
        }
    }

    static void AddHedgehogs(Transform parent, GameObject prefab, params float[] xs)
    {
        foreach (float x in xs)
        {
            Place(parent, prefab, x, 0.9f);
        }
    }

    static GameObject AddPlayer(Transform parent, GameObject prefab, float x)
    {
        // 碰撞体底部在 transform 下方 0.87；地面顶 y=0 → 放 0.9 让脚刚好着地
        return Place(parent, prefab, x, 0.9f);
    }

    // ================================================================ 各场景

    static void BuildTitleScene()
    {
        EditorScene scene = new EditorScene();
        YG3GameManager gm = BuildStageFrame(scene, "Title", YG3Tuning.BGM_TITLE, false);

        GameObject canvas = GameObject.Find("Root/Canvas");

        Image back = MakeImage(canvas.transform, "TitleBack", Img("title_back"), Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(1920f, 1080f), Vector2.zero);
        back.rectTransform.SetAsFirstSibling();
        back.preserveAspect = true;

        Image logo = MakeImage(canvas.transform, "Logo", Img("title_logo"), Color.white,
            new Vector2(0.5f, 0.72f), new Vector2(760f, 264f), Vector2.zero);
        logo.preserveAspect = true;
        logo.raycastTarget = false;

        Image chara = MakeImage(canvas.transform, "Chara", Img("title_chara"), Color.white,
            new Vector2(0.78f, 0.42f), new Vector2(420f, 566f), Vector2.zero);
        chara.preserveAspect = true;
        chara.raycastTarget = false;

        Button start = MakeButton(canvas.transform, "StartButton", Img("bt_gamestart"), "START", BuiltinFont(),
            new Vector2(0.5f, 0.28f), Vector2.zero);
        YG3ChangeScene change = start.gameObject.AddComponent<YG3ChangeScene>();
        change.sceneName = YG3SceneNames.Stage1;
        start.onClick.AddListener(change.Load);

        // 隐藏玩家 HUD（标题界面用不到）
        gm.showStartImage = false;
        gm.hpFill.transform.parent.gameObject.SetActive(false);
        gm.staminaFill.transform.parent.gameObject.SetActive(false);
        gm.scoreText.gameObject.SetActive(false);
        gm.mainImage.SetActive(false);
        gm.panel.SetActive(false);

        SaveScene(scene.root, "Title");
    }

    delegate void ContentBuilder(Transform parent);

    static void BuildStageScene(string name, ContentBuilder content, string bgm, bool isBoss)
    {
        EditorScene scene = new EditorScene();
        YG3GameManager gm = BuildStageFrame(scene, name, bgm, isBoss);
        content(scene.root.transform);
        gm.inputUI.SetActive(Application.isMobilePlatform);
        SaveScene(scene.root, name);
    }

    static void BuildStage1Content(Transform parent)
    {
        GameObject player = LoadPrefab("Player");
        GameObject hog = LoadPrefab("Hedgehog");
        GameObject item = LoadPrefab("Item");
        GameObject goal = LoadPrefab("Goal");

        AddGround(parent, -18f, 66f);
        AddWallBox(parent, -18.6f, 3f, 1f, 30f);
        AddWallBox(parent, 66.6f, 3f, 1f, 30f);
        AddDeadZone(parent, 24f, 100f);

        AddBlock(parent, "block2x1", 6f, 0.6f);
        AddBlock(parent, "block2x1", 22f, 0.6f);
        AddBlock(parent, "block1x1", 23.2f, 0.6f);
        AddBlock(parent, "block2x1", 38f, 0.6f);
        AddBlock(parent, "block2x1", 40.4f, 0.6f);

        AddHedgehogs(parent, hog, -6f, 8f, 20f, 32f, 44f, 52f);
        AddItems(parent, item, 2f, 14f, 27f, 36f, 48f);
        AddPlayer(parent, player, -15f);
        Place(parent, goal, 64f, 0f);
    }

    static void BuildStage2Content(Transform parent)
    {
        GameObject player = LoadPrefab("Player");
        GameObject hog = LoadPrefab("Hedgehog");
        GameObject item = LoadPrefab("Item");
        GameObject goal = LoadPrefab("Goal");

        // 三段地面 + 两处沟壑（掉下去=DeadZone）
        AddGround(parent, -18f, 0f);
        AddGround(parent, 3.6f, 16f);
        AddGround(parent, 19.6f, 34f);
        AddDeadZone(parent, 8f, 100f);

        AddWallBox(parent, -18.6f, 3f, 1f, 30f);
        AddWallBox(parent, 34.6f, 3f, 1f, 30f);

        // 平台
        AddBlock(parent, "block2x1", 7f, 1.8f);
        AddBlock(parent, "block2x1", 10.5f, 3.0f);
        AddBlock(parent, "block2x1", 14f, 4.2f);
        AddBlock(parent, "block2x1", 22f, 1.8f);
        AddBlock(parent, "block1x1", 24.2f, 1.8f);
        AddBlock(parent, "block2x1", 27f, 3.2f);
        AddBlock(parent, "block1x2", -4f, 1.2f);

        AddHedgehogs(parent, hog, -10f, 5f, 12.5f, 22f, 28f);
        AddItems(parent, item, 7f, 14f, 27f, -4f);
        AddPlayer(parent, player, -15f);
        Place(parent, goal, 30f, 0f);
    }

    static void BuildBossStageContent(Transform parent, GameObject boss, GameObject hog)
    {
        GameObject player = LoadPrefab("Player");

        AddGround(parent, -15f, 25f);
        AddWallBox(parent, -15.6f, 3f, 1f, 30f);
        AddWallBox(parent, 25.6f, 3f, 1f, 30f);
        AddDeadZone(parent, 5f, 50f);

        AddPlayer(parent, player, -11f);
        GameObject b = Place(parent, boss, 14f, GROUND_TOP);
        b.transform.localScale = new Vector3(3f, 3f, 1f);

        // 场边补给
        GameObject item = LoadPrefab("Item");
        AddItems(parent, item, -13f, 23f);
    }

    static void BuildResultScene()
    {
        EditorScene scene = new EditorScene();
        BuildStageFrame(scene, "Result", YG3Tuning.BGM_TITLE, false);

        GameObject canvas = GameObject.Find("Root/Canvas");

        Image clear = MakeImage(canvas.transform, "ClearImage", Img("StageClear"), Color.white,
            new Vector2(0.5f, 0.72f), new Vector2(760f, 146f), Vector2.zero);
        clear.preserveAspect = true;
        clear.raycastTarget = false;

        Font font = BuiltinFont();
        Text score = MakeText(canvas.transform, "TotalScore", "SCORE  0", 64, new Color(1f, 0.9f, 0.5f), font,
            new Vector2(0.5f, 0.5f), new Vector2(900f, 100f), Vector2.zero);
        score.fontStyle = FontStyle.Bold;

        Button again = MakeButton(canvas.transform, "AgainButton", Img("button"), "RETRY", font,
            new Vector2(0.35f, 0.24f), Vector2.zero);
        Button title = MakeButton(canvas.transform, "TitleButton", Img("button"), "TITLE", font,
            new Vector2(0.65f, 0.24f), Vector2.zero);

        YG3ResultController result = new GameObject("ResultController").AddComponent<YG3ResultController>();
        result.transform.SetParent(scene.root.transform, false);
        result.scoreText = score;
        again.onClick.AddListener(result.OnRestart);
        title.onClick.AddListener(result.OnTitle);

        YG3GameManager gm = scene.gameManager;
        gm.showStartImage = false;
        gm.hpFill.transform.parent.gameObject.SetActive(false);
        gm.staminaFill.transform.parent.gameObject.SetActive(false);
        gm.scoreText.gameObject.SetActive(false);
        gm.mainImage.SetActive(false);
        gm.panel.SetActive(false);

        SaveScene(scene.root, "Result");
    }

    // ================================================================ 场景保存

    static void SaveScene(GameObject root, string name)
    {
        bool ok = EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SCN + name + ".unity");
        Debug.Log("[YG3] 场景保存 " + name + " → " + ok);
    }
}
