using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// 素材导入与切片：全部 PPU=100（与 YeGe 原生一致，120px 砖块 = 1.2 单位）。
/// 多重精灵：Hedgehog(6 帧)、PlayerImage(4x3，弓/箭特判 Pivot)、Boss(5 帧显式矩形)。
/// 单图：needle_gold / shock(Pivot 底部) / slash / parry / spark。
/// </summary>
public static class YG3ImportSettings
{
    const float PPU = 100f;

    [MenuItem("YG3/1. 导入并切片素材")]
    public static void ApplyAll()
    {
        AssetDatabase.Refresh();

        // ---- 刺猬 960x160 → 6 帧 ----
        List<SpriteRect> hog = new List<SpriteRect>();
        string[] hogNames = { "Hedgehog_0", "Hedgehog_1", "Hedgehog_2", "Hedgehog_3", "HedgehogCurl", "HedgehogDead" };
        for (int i = 0; i < 6; i++)
        {
            Vector2 pivot = i == 4 ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.12f);
            hog.Add(Rect(YG3Tuning.DIR_IMG + "Hedgehog.png", hogNames[i],
                new Rect(i * 160f, 0f, 160f, 160f), pivot));
        }
        Slice(YG3Tuning.DIR_IMG + "Hedgehog.png", hog.ToArray());

        // ---- UniTop 玩家表 128x96 → 4x3 格（32px 单元）：9=弓 10=箭 ----
        List<SpriteRect> plr = new List<SpriteRect>();
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int idx = row * 4 + col;
                Vector2 pivot = idx == 9 ? new Vector2(0.4f, 0.5f) :
                                idx == 10 ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.2f);
                plr.Add(Rect(YG3Tuning.DIR_IMG + "PlayerImage.png", "PlayerImage_" + idx,
                    new Rect(col * 32f, row * 32f, 32f, 32f), pivot));
            }
        }
        Slice(YG3Tuning.DIR_IMG + "PlayerImage.png", plr.ToArray());

        // ---- Boss 544x96 → 5 帧显式切片（复用 UniTop 已验证数值，Pivot 底部）----
        Slice(YG3Tuning.DIR_IMG + "Boss.png", new[]
        {
            Rect(YG3Tuning.DIR_IMG + "Boss.png", "Boss_0", new Rect(0f, 0f, 96f, 96f), new Vector2(0.5f, 0f)),
            Rect(YG3Tuning.DIR_IMG + "Boss.png", "Boss_1", new Rect(97f, 0f, 94f, 94f), new Vector2(0.5f, 0f)),
            Rect(YG3Tuning.DIR_IMG + "Boss.png", "Boss_2", new Rect(195f, 0f, 90f, 96f), new Vector2(0.5f, 0f)),
            Rect(YG3Tuning.DIR_IMG + "Boss.png", "Boss_3", new Rect(298f, 0f, 140f, 76f), new Vector2(0.5f, 0f)),
            Rect(YG3Tuning.DIR_IMG + "Boss.png", "Boss_4", new Rect(453f, 0f, 86f, 57f), new Vector2(0.5f, 0f)),
        });

        // ---- 单图 ----
        Single(YG3Tuning.DIR_FX + "needle_gold.png", new Vector2(0.5f, 0.5f));
        Single(YG3Tuning.DIR_FX + "shock.png", new Vector2(0.5f, 0f));
        Single(YG3Tuning.DIR_FX + "slash.png", new Vector2(0.5f, 0.5f));
        Single(YG3Tuning.DIR_FX + "parry.png", new Vector2(0.5f, 0.5f));
        Single(YG3Tuning.DIR_FX + "spark.png", new Vector2(0.5f, 0.5f));

        AssetDatabase.SaveAssets();
        Debug.Log("[YG3] 素材导入与切片完成");
    }

    // ---------------- 工具 ----------------

    static SpriteRect Rect(string path, string name, Rect r, Vector2 pivot)
    {
        SpriteRect sr = new SpriteRect();
        sr.name = name;
        sr.rect = r;
        sr.pivot = pivot;
        sr.alignment = SpriteAlignment.Custom;
        sr.spriteID = GUID.Generate();
        return sr;
    }

    static void Configure(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[YG3] 找不到贴图: " + path);
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PPU;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
    }

    static void Slice(string path, SpriteRect[] rects)
    {
        Configure(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
        Debug.Log("[YG3] 切片 " + path + " x" + rects.Length);
    }

    static void Single(string path, Vector2 pivot)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[YG3] 找不到贴图: " + path);
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PPU;
        importer.spritePivot = pivot;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
    }
}
