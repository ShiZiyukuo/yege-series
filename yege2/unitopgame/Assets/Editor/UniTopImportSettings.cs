using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 素材导入设置（教程 8.3.2 / 8.3.3 / 8.4.1 / 9.2 / 9.4.3 / 10.3.1）。
/// 全部贴图按教程要求切成多重精灵：TileMap / PlayerImage / EnemyImage / Items /
/// Life / Boss，并逐帧指定 Pivot；音频按 BGM / SE 分别设置压缩方式。
/// </summary>
public static class UniTopImportSettings
{
    public const string MAP = "Assets/Map/";
    public const string PLR = "Assets/Player/";
    public const string ENM = "Assets/Enemy/";
    public const string ITM = "Assets/Item/";
    public const string BOS = "Assets/Boss/";
    public const string UI = "Assets/UIManager/";
    public const string TTL = "Assets/Title/";
    public const string SND = "Assets/SoundManager/";
    public const string TILE = "Assets/Map/Tile/";

    // 教程 8.3.2：每个贴片 32×32 像素，Pixels Per Unit = 32
    const float PPU = 32f;

    [MenuItem("UniTop/1. 导入并切分素材")]
    public static void ApplyAll()
    {
        Directory.CreateDirectory(TILE);
        AssetDatabase.Refresh();

        // ---- 贴片地图 320×96 → 10 列 × 3 行 = 30 个贴片，Pivot 保持 Center ----
        SliceGrid(MAP + "TileMap.png", 10, 3, 32, 32, "TileMap", Pivot(0.5f, 0.5f));

        // ---- 玩家 128×96 → 4 列 × 3 行（教程 8.4.1：Pivot Custom 0.5 / 0.2）----
        SliceGrid(PLR + "PlayerImage.png", 4, 3, 32, 32, "PlayerImage", Pivot(0.5f, 0.2f),
            new Dictionary<int, Vector2>
            {
                { 9, new Vector2(0.4f, 0.5f) },   // 弓：尾端与玩家基准点重合，便于以该点旋转
                { 10, new Vector2(0.5f, 0.5f) }   // 箭矢：旋转基准点放在箭身中央
            });

        // ---- 敌人 128×96 → 4 列 × 3 行（教程 9.3.1：与玩家同样的 32×32 多重精灵）----
        SliceGrid(ENM + "EnemyImage.png", 4, 3, 32, 32, "EnemyImage", Pivot(0.5f, 0.2f));

        // ---- 道具 96×64 → 3 列 × 2 行（钥匙 / 箭矢 / 生命 / 宝箱关 / 宝箱开）----
        SliceGrid(ITM + "Items.png", 3, 2, 32, 32, "Item", Pivot(0.5f, 0.5f));

        // ---- 生命值图像（教程 9.4.3：4 张，自上而下 HP 0→1→2→3）----
        SliceExplicit(UI + "Life.png", new[]
        {
            Mat("Life_0", 0, 356, 320, 122, Pivot(0.5f, 0.5f)),
            Mat("Life_1", 0, 234, 320, 122, Pivot(0.5f, 0.5f)),
            Mat("Life_2", 0, 112, 320, 122, Pivot(0.5f, 0.5f)),
            Mat("Life_3", 0, 0, 320, 112, Pivot(0.5f, 0.5f))
        });

        // ---- Boss（教程 10.3.1：Automatic 切割、Pivot Bottom）----
        SliceExplicit(BOS + "Boss.png", new[]
        {
            Mat("Boss_0", 0, 0, 96, 96, Pivot(0.5f, 0f)),
            Mat("Boss_1", 97, 0, 94, 94, Pivot(0.5f, 0f)),
            Mat("Boss_2", 195, 0, 90, 96, Pivot(0.5f, 0f)),
            Mat("Boss_3", 298, 0, 140, 76, Pivot(0.5f, 0f)),
            Mat("Boss_4", 453, 0, 86, 57, Pivot(0.5f, 0f))
        });

        // ---- 单张像素图（PPU 32 + Point）----
        SinglePixels(ITM + "Door.png");
        SinglePixels(BOS + "bullet.png");

        // ---- UI 用的像素风图像 ----
        SinglePixelUI(UI + "AttackButton.png");
        SinglePixelUI(UI + "VirtualPad4D.png");
        SinglePixelUI(UI + "VirtualPadTab.png");

        // ---- UI 用的平滑图像 ----
        SingleSmoothUI(UI + "ItemImage.png");
        SingleSmoothUI(UI + "button.png");
        SingleSmoothUI(UI + "GameStart.png");
        SingleSmoothUI(UI + "GameOver.png");
        SingleSmoothUI(UI + "GameClear.png");
        SingleSmoothUI(TTL + "title_back.png");
        SingleSmoothUI(TTL + "title_chara.png");
        SingleSmoothUI(TTL + "title_logo.png");
        SingleSmoothUI(TTL + "bt_gamestart.png");
        SingleSmoothUI(TTL + "bt_continue.png");

        SetupAudio();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CreateTiles();
        Debug.Log("[UniTop] 素材导入与切分完成");
    }

    // ===================================================================
    //  贴图
    // ===================================================================

    static Vector2 Pivot(float x, float y)
    {
        return new Vector2(x, y);
    }

    static SpriteMetaData Mat(string name, float x, float y, float w, float h, Vector2 pivot)
    {
        SpriteMetaData m = new SpriteMetaData();
        m.name = name;
        m.rect = new Rect(x, y, w, h);
        m.alignment = (int)SpriteAlignment.Custom;
        m.pivot = pivot;
        m.border = Vector4.zero;
        return m;
    }

    /// <summary>按固定单元格尺寸切割多重精灵。rect 的 Y 以左下角为原点。</summary>
    static void SliceGrid(string path, int cols, int rows, int cw, int ch, string prefix,
        Vector2 defaultPivot, Dictionary<int, Vector2> pivotOverride = null)
    {
        TextureImporter importer = PrepareMultiple(path);
        if (importer == null)
        {
            return;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                Vector2 pivot = defaultPivot;
                if (pivotOverride != null && pivotOverride.ContainsKey(index))
                {
                    pivot = pivotOverride[index];
                }
                // 图像左上角为第 0 行；转成 Unity 的左下角原点
                float x = c * cw;
                float y = tex.height - (r + 1) * ch;
                metas.Add(Mat(prefix + "_" + index, x, y, cw, ch, pivot));
            }
        }
        FinishSlice(importer, metas);
    }

    static void SliceExplicit(string path, SpriteMetaData[] metas)
    {
        TextureImporter importer = PrepareMultiple(path);
        if (importer == null)
        {
            return;
        }
        FinishSlice(importer, new List<SpriteMetaData>(metas));
    }

    static TextureImporter PrepareMultiple(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[UniTop] 找不到贴图: " + path);
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // spriteMeshType / spriteExtrude 只能通过 TextureImporterSettings 设置
        TextureImporterSettings extra = new TextureImporterSettings();
        importer.ReadTextureSettings(extra);
        extra.spriteMeshType = SpriteMeshType.FullRect;
        extra.spriteExtrude = 0;
        importer.SetTextureSettings(extra);

        importer.SaveAndReimport();
        return importer;
    }

    /// <summary>
    /// 写入每个子精灵的矩形与 Pivot。
    /// 2022.3 起 TextureImporter.spritesheet 已不再生效，改用官方的 ISpriteEditorDataProvider。
    /// </summary>
    static void FinishSlice(TextureImporter importer, List<SpriteMetaData> metas)
    {
        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
        {
            Debug.LogError("[UniTop] 取不到精灵数据提供器: " + importer.assetPath);
            return;
        }
        provider.InitSpriteEditorDataProvider();

        List<SpriteRect> rects = new List<SpriteRect>();
        foreach (SpriteMetaData meta in metas)
        {
            SpriteRect rect = new SpriteRect();
            rect.name = meta.name;
            rect.rect = meta.rect;
            rect.alignment = (SpriteAlignment)meta.alignment;
            rect.pivot = meta.pivot;
            rect.border = meta.border;
            rect.spriteID = GUID.Generate();
            rects.Add(rect);
        }

        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();
    }

    static void SinglePixels(string path)
    {
        Single(path, PPU, FilterMode.Point);
    }

    static void SinglePixelUI(string path)
    {
        Single(path, 100f, FilterMode.Point);
    }

    static void SingleSmoothUI(string path)
    {
        Single(path, 100f, FilterMode.Bilinear);
    }

    static void Single(string path, float ppu, FilterMode filter)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[UniTop] 找不到贴图: " + path);
            return;
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spritePixelsPerUnit = ppu;
        settings.filterMode = filter;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        settings.wrapMode = TextureWrapMode.Clamp;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    // ===================================================================
    //  音频（10.4 需要跨场景播放 BGM 与多处 SE）
    // ===================================================================

    static void SetupAudio()
    {
        if (!Directory.Exists(SND))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(SND))
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext != ".wav" && ext != ".mp3")
            {
                continue;
            }

            string path = SND + Path.GetFileName(file);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                continue;
            }

            bool isBgm = Path.GetFileNameWithoutExtension(file).StartsWith("BGM");
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            if (isBgm)
            {
                // BGM 体积大，解压缩后常驻内存会占用过多，改为压缩后驻留
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.quality = 0.7f;
                importer.loadInBackground = true;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.quality = 0.8f;
                importer.loadInBackground = false;
            }
            importer.defaultSampleSettings = s;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }
    }

    // ===================================================================
    //  贴片素材（教程 8.3.3 / 8.3.6）
    // ===================================================================

    /// <summary>
    /// 贴片索引 → 碰撞类型。None 的即教程 8.3.6 点名的「沙地、草地、茂盛的草地、桥、地牢地面」。
    /// 另外把「地牢出入口」与「石框」也设为 None：教程里出入口是用 Custom Physics Shape
    /// 只挡住其余三面、保留可进入的方向，这里简化为整格可通行，由玩家踩到触发器来进出。
    /// </summary>
    static readonly Dictionary<int, Tile.ColliderType> TileColliders = new Dictionary<int, Tile.ColliderType>
    {
        { 2,  Tile.ColliderType.None },   // 沙地
        { 3,  Tile.ColliderType.None },   // 草地
        { 4,  Tile.ColliderType.None },   // 茂盛的草地
        { 6,  Tile.ColliderType.None },   // 桥
        { 9,  Tile.ColliderType.None },   // 地牢出入口
        { 15, Tile.ColliderType.None },   // 地牢地面
        { 16, Tile.ColliderType.None },   // 地牢地面（带方框）
        { 20, Tile.ColliderType.None },   // 石框（ doorway，可通行）
        { 26, Tile.ColliderType.None }    // 地牢地面（另一种）
    };

    public static void CreateTiles()
    {
        Directory.CreateDirectory(TILE);
        List<Sprite> sprites = LoadSprites(MAP + "TileMap.png");
        if (sprites.Count == 0)
        {
            Debug.LogError("[UniTop] TileMap 未切分成功");
            return;
        }

        for (int i = 0; i < 30 && i < sprites.Count; i++)
        {
            string path = TILE + "StageMap_" + i + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprites[i];
            tile.colliderType = TileColliders.ContainsKey(i) ? TileColliders[i] : Tile.ColliderType.Sprite;
            tile.color = Color.white;
            EditorUtility.SetDirty(tile);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[UniTop] 贴片素材生成完成（30 个）");
    }

    // ===================================================================
    //  读取多重精灵的子精灵
    // ===================================================================

    /// <summary>读取某个贴图切分出来的全部子精灵，按名字中的序号排序。</summary>
    public static List<Sprite> LoadSprites(string texturePath)
    {
        List<Sprite> result = new List<Sprite>();
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        foreach (Object o in assets)
        {
            Sprite s = o as Sprite;
            if (s != null)
            {
                result.Add(s);
            }
        }
        result.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
        return result;
    }

    static int ExtractIndex(string name)
    {
        int pos = name.LastIndexOf('_');
        if (pos < 0)
        {
            return 0;
        }
        int value;
        return int.TryParse(name.Substring(pos + 1), out value) ? value : 0;
    }
}
