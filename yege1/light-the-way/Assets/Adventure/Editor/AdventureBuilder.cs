using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AdventureBuilder
{
    const string Folder = "Assets/Adventure/Scenes/";
    static readonly string[] Names = { "AdventureTitle", "AdventureStage1", "AdventureStage2", "AdventureStage3", "AdventureJournal" };
    static readonly string[] Chapters = { "草坡启程", "旧渡口", "遗迹归途" };
    static readonly string[] Tasks = { "沿着草坡，找回第一枚星光", "唤醒旧渡口，找回第二枚星光", "穿过遗迹，把最后的星光带回家" };
    static Color ink = new Color(.08f, .20f, .23f), mint = new Color(.64f, .9f, .78f), gold = new Color(1, .83f, .41f);
    static Font font;
    static Sprite panelSprite;
    static List<SpriteRenderer> lamps;
    public static bool NeedsLayoutUpgrade { get { return !File.Exists("AdventureLayoutVersion.txt") || File.ReadAllText("AdventureLayoutVersion.txt").Trim() != "2"; } }

    [MenuItem("YeGE/展示版/初始化展示场景（首次使用）")]
    public static void Initialize()
    {
        if (File.Exists(Folder + Names[0] + ".scene") && !NeedsLayoutUpgrade) { Debug.Log("[Adventure] 展示场景已存在，不覆盖已编辑内容。使用校验或仅构建菜单。"); return; }
        BuildAndValidate();
    }
    public static void BuildAndValidate()
    {
        string backup = "SceneBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        foreach (string name in Names)
        {
            string scenePath = Folder + name + ".scene";
            if (!File.Exists(scenePath)) continue;
            Directory.CreateDirectory(backup);
            File.Copy(scenePath, backup + "/" + name + ".scene");
        }
        PrepareArt();
        BuildMenu(false);
        for (int i = 0; i < 3; i++) BuildChapter(i);
        BuildMenu(true);
        EditorBuildSettings.scenes = Names.Select(n => new EditorBuildSettingsScene(Folder + n + ".scene", true)).ToArray();
        PlayerSettings.productName = "The adventures of YeGe - Light the Way";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.runInBackground = false; PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        AssetDatabase.SaveAssets(); Validate();
        EditorSceneManager.OpenScene(Folder + Names[0] + ".scene");
        File.WriteAllText("AdventureLayoutVersion.txt", "2\n");
        Debug.Log("[Adventure] BUILD_AND_VALIDATE_SUCCESS");
    }
    static void PrepareArt()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles("Assets/Adventure/Art", "*.png"))
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (path.EndsWith("RoundedPanel.png")) importer.spriteBorder = new Vector4(24, 24, 24, 24);
            importer.SaveAndReimport();
        }
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Adventure/Art/AdventureChinese.ttf");
        panelSprite = Art("RoundedPanel");
        if (!font || !panelSprite) throw new Exception("Adventure art/font missing");
    }
    static Sprite Art(string name) { return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Adventure/Art/" + name + ".png"); }
    static Sprite Original(string name) { return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/" + name + ".png"); }
    static GameObject Prefab(string name, float x, float y)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/" + name + ".prefab");
        if (!prefab) throw new Exception("Missing prefab " + name);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.position = new Vector3(x, y, 0); return go;
    }
    static SpriteRenderer Sprite(string name, Sprite sprite, Vector3 pos, Vector3 scale, Color color, int order)
    {
        var go = new GameObject(name); go.transform.position = pos; go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order; return sr;
    }
    static void BuildMenu(bool journal)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10); var cam = camera.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 5;
        cam.backgroundColor = ink;
        var bg = Sprite("Sky", Original("back"), Vector3.zero, Vector3.one, journal?new Color(1,.89f,.73f):new Color(.60f,.73f,.78f), -20);
        bg.transform.SetParent(camera.transform, false); bg.transform.localPosition = new Vector3(0, 0, 10);
        var cm = camera.AddComponent<CameraManager>(); cm.subScreen = bg.gameObject; cm.cinematic = true;
        Ground( -12, 12);
        var hero = Sprite("YeGe", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Player/"+(journal?"player_clear":"player_stop")+".png"), new Vector3(4.4f, -1.4f), Vector3.one * 2f, Color.white, 4);
        hero.transform.position += Vector3.up * (-2f - hero.bounds.min.y);
        Sprite("HeroShadow", Art("Disc"), new Vector3(4.4f, -2.02f), new Vector3(1.3f, .16f, 1), new Color(.12f,.25f,.2f,.25f), 2);
        var light = Lamp(6.1f, -1, 1.55f);
        if(journal)
        {
            light.color=Color.white;
            Sprite("HomeGlow",Art("SoftGlow"),new Vector3(6.1f,-1,0),Vector3.one*2.4f,new Color(1,.80f,.36f,.5f),3);
        }
        for (int i = 0; i < 3; i++) Sprite("Star", Art("Star"), new Vector3(3 + i * 1.2f, 1.5f + .3f * Mathf.Sin(i)), Vector3.one * .32f, gold, 5).gameObject.AddComponent<AdventureParticle>().ambient = true;
        Decorate(-10, 10, 0);
        var flow = new GameObject("AdventureFlow").AddComponent<AdventureFlow>();
        flow.mode = journal ? AdventureFlow.Mode.Journal : AdventureFlow.Mode.Title;
        flow.lantern = light.transform; flow.sky = bg;
        UI(flow, true); flow.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BGM_game_00.wav");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Folder + (journal ? Names[4] : Names[0]) + ".scene");
    }
    static void BuildChapter(int chapter)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Stage" + (chapter + 1) + ".scene");
        // Extract only the character and camera art. All old terrain, walls and hazards go.
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        var camera = Camera.main;
        if (!player || !camera) throw new Exception("Missing original character/camera");
        var cm = camera.GetComponent<CameraManager>();
        var background = cm.subScreen;
        player.transform.SetParent(null, true); camera.transform.SetParent(null, true);
        background.transform.SetParent(null, true);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root != player.gameObject && root != camera.gameObject && root != background)
                UnityEngine.Object.DestroyImmediate(root);
        foreach (var collider in camera.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var collider in background.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var gm in UnityEngine.Object.FindObjectsOfType<GameManager>()) UnityEngine.Object.DestroyImmediate(gm);
        foreach (var timer in UnityEngine.Object.FindObjectsOfType<TimeController>()) UnityEngine.Object.DestroyImmediate(timer);
        var flow = new GameObject("AdventureFlow").AddComponent<AdventureFlow>();
        flow.mode = AdventureFlow.Mode.Chapter; flow.chapter = chapter; flow.chapterName = Chapters[chapter]; flow.task = Tasks[chapter];
        flow.nextScene = chapter < 2 ? Names[chapter + 2] : Names[4];
        flow.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BGM_game_00.wav");
        flow.clearSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/ME_Clear.mp3"); flow.sparkSprite = Art("Star");
        player.transform.position = new Vector3(-10, -.9f, 0);
        player.speed = 3f; player.jump = 9f; player.groundLayer = 1 << 8;
        var rb = player.GetComponent<Rigidbody2D>(); rb.gravityScale = 1.5f; rb.mass = 1;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        float goalX = 99 + chapter * 10;
        cm.leftLimit = -9.5f; cm.rightLimit = goalX - 2; cm.topLimit = 1.7f; cm.bottomLimit = 0; cm.smoothTime = .17f;
        camera.orthographicSize = 5;
        flow.sky = background.GetComponent<SpriteRenderer>();
        flow.sky.color = chapter == 0 ? new Color(1,.95f,.84f) : chapter == 1 ? new Color(.65f,.70f,.92f) : new Color(.78f,.62f,.69f);
        if (chapter == 1) flow.sky.sprite = Original("back2");
        lamps = new List<SpriteRenderer>();
        // Every main-route gap is two units; raised routes always have a ground-level alternative.
        Ground(-12, 20); Ground(22, 49); Ground(51, 77); Ground(79, goalX + 7);
        Platform("SteppingStoneA", new Vector3(21,-2.15f,0), 1.1f);
        Platform("SteppingStoneB", new Vector3(50,-2.15f,0), 1.1f);
        Platform("SteppingStoneC", new Vector3(78,-2.15f,0), 1.1f);
        Platform("MeadowStep", new Vector3(10,-.65f,0), 2.4f);
        Platform("MeadowLookout", new Vector3(13,.8f,0), 3f);
        Gem(chapter,"lookout",13,1.65f);
        Platform("BranchStep", new Vector3(34,-.15f,0), 2.4f);
        Platform("BranchLedge", new Vector3(37,1.45f,0), 3f);
        Platform("BranchReturn", new Vector3(40,.35f,0), 2.6f);
        Collect("stamp-" + chapter, AdventureCollectible.Kind.Stamp, Art("Stamp"),37,2.2f,.70f);
        Hint(31,"高处藏着旅行印记。也可以沿地面继续前进。");
        Gem(chapter,"branch-a",34,.65f); Gem(chapter,"branch-b",40,1.15f);
        if (chapter == 1)
        {
            Platform("FerryLift",new Vector3(30,-.25f,0),2.7f);
            var lift = GameObject.Find("FerryLift").AddComponent<MovingBlock>();
            lift.moveY=1.65f;lift.times=2.2f;lift.weight=.7f;lift.requireSwitch=true;lift.isCanMove=false;
            var lever=Prefab("Switch",27,-1.2f).GetComponent<SwitchAction>();
            lever.targetMoveBlock=lift.gameObject;lever.switchId="ferry-1";lever.on=false;
            Hint(25,"触碰拉杆，让渡台升起。前方地面也能通行。");
            Platform("DockRoof",new Vector3(64,.7f,0),3f);
            Hint(64,"渡口的灯，又有人记得了。",AdventureMarker.Kind.Vista,1.5f);
        }
        if (chapter == 2)
        {
            for(int i=0;i<3;i++)
            {
                Platform("CrumblingBridge"+i,new Vector3(59+i*2.8f,.5f,0),2.2f);
                GameObject.Find("CrumblingBridge"+i).AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
                GameObject.Find("CrumblingBridge"+i).AddComponent<GimmickBlock>().delay=1.1f;
            }
            Hint(55,"古桥会先晃动再坠落。下方还有一条安全小路。");
            var enemy=Prefab("enemy4",88,-1.2f).GetComponent<EmeryController>();enemy.range=4;enemy.speed=1.3f;
            Hint(84,"遗迹的小守卫在巡逻，跳过去就好。");
        }
        Collect("star-"+chapter,AdventureCollectible.Kind.Star,Art("Star"),goalX-7,-.7f,1.1f);
        Hint(goalX-12,"星光就在前面。带着它，走向亮起的路牌。");
        var goal = Sprite("AdventureGoal",Original("goal"),new Vector3(goalX,-1.1f,0),Vector3.one,Color.white,2);
        goal.gameObject.tag="Goal";
        var goalCollider=goal.gameObject.AddComponent<BoxCollider2D>();goalCollider.isTrigger=true;goalCollider.size=new Vector2(1.4f,2.4f);
        if(chapter==2)
        {
            goal.enabled=false;flow.lantern=Lamp(goalX,-.6f,1.8f).transform;
            flow.lanternGlow=Sprite("HomeLight",Art("SoftGlow"),new Vector3(goalX,-.6f,0),Vector3.one,new Color(1,.83f,.4f,0),3);
            TextWorld("归途灯",new Vector3(goalX,1.35f,0),.75f);
        }
        else TextWorld("下一站",new Vector3(goalX,.35f,0),.5f);
        Checkpoint(-7,"试试跳跃，沿着宝石出发。",-7);
        Checkpoint(24,"小灯记下了草坡，可以放心探索。",24);
        Checkpoint(53,"旅途过半，留意高处的小秘密。",53);
        Checkpoint(81,"最后一段路了，星光就在前方。",81);
        for(float x=-4;x<goalX-10;x+=5) Gem(chapter,"route-"+x,x,-.7f);
        var death=new GameObject("AdventureDeathPlane");death.tag="Dead";death.transform.position=new Vector3((goalX-5)/2,-7.5f,0);
        var deathCollider=death.AddComponent<BoxCollider2D>();deathCollider.isTrigger=true;deathCollider.size=new Vector2(goalX+32,2);
        // No invisible solid boundaries: the camera limits frame the route; fall recovery handles escape.
        Decorate(-12,goalX+4,chapter);
        for(float x=-8;x<goalX;x+=12) lamps.Add(Lamp(x,-.7f,.60f));
        flow.routeLights=lamps.ToArray();UI(flow,false);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Folder+Names[chapter+1]+".scene");
    }
    static void Ground(float from, float to)
    {
        for(float x=from;x<to-.01f;x+=6)
        {
            float width=Mathf.Min(6,to-x);
            var go=Prefab("ground",x+width*.5f,-3.2f);
            go.transform.localScale=new Vector3(width/6,1,1);
        }
    }
    static void Platform(string name, Vector3 pos, float width)
    {
        var sr = Sprite(name, Original("block_move"), pos, Vector3.one, new Color(.75f,.9f,.75f), 1);
        sr.transform.localScale = new Vector3(width / sr.sprite.bounds.size.x, .7f, 1);
        sr.gameObject.layer = 8; var col = sr.gameObject.AddComponent<BoxCollider2D>();
    }
    static void Collect(string id, AdventureCollectible.Kind kind, Sprite sprite, float x, float y, float scale)
    {
        var sr = Sprite(id, sprite, new Vector3(x,y,0), Vector3.one*scale, Color.white, 5);
        var col = sr.gameObject.AddComponent<CircleCollider2D>(); col.radius = .40f; col.isTrigger = true;
        var item = sr.gameObject.AddComponent<AdventureCollectible>(); item.id = id; item.kind = kind;
    }
    static void Gem(int chapter, string key, float x, float y) { Collect("gem-"+chapter+"-"+key, AdventureCollectible.Kind.Gem, Original("item_red"), x,y,1); }
    static SpriteRenderer Lamp(float x, float y, float scale)
    {
        Sprite("LampPost", Art("Disc"), new Vector3(x,-1.5f,0), new Vector3(.07f,1.0f,1),new Color(.22f,.36f,.29f),0);
        return Sprite("Lantern",Art("Lantern"),new Vector3(x,y,0),Vector3.one*scale,new Color(.65f,.74f,.61f),4);
    }
    static void Hint(float x, string message, AdventureMarker.Kind kind=AdventureMarker.Kind.Hint, float y=-.7f)
    {
        var go=new GameObject("Hint: "+message);go.transform.position=new Vector3(x,y,0);
        var col=go.AddComponent<BoxCollider2D>();col.isTrigger=true;col.size=new Vector2(1.8f,2);
        var marker=go.AddComponent<AdventureMarker>();marker.kind=kind;marker.message=message;
    }
    static void Checkpoint(float x,string message,float spawnX)
    {
        var sr=Lamp(x,-.9f,.58f);var marker=sr.gameObject.AddComponent<AdventureMarker>();marker.kind=AdventureMarker.Kind.Checkpoint;marker.message=message;marker.lightSprite=sr;
        var col=sr.gameObject.AddComponent<BoxCollider2D>();col.isTrigger=true;col.size=new Vector2(2.5f,3);
        var spawn=new GameObject("SafeSpawn");spawn.transform.position=new Vector3(spawnX,-.9f,0);spawn.transform.SetParent(sr.transform,true);marker.spawn=spawn.transform;
    }
    static void TextWorld(string value,Vector3 position,float size)
    {
        var go=new GameObject(value);go.transform.position=position;
        var mesh=go.AddComponent<TextMesh>();mesh.text=value;mesh.font=font;mesh.fontSize=48;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=new Color(1,.94f,.72f);
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.sortingOrder=6;
    }
    static void Decorate(float from,float to,int chapter)
    {
        Color foliage=chapter==0?new Color(.27f,.57f,.42f):chapter==1?new Color(.29f,.46f,.54f):new Color(.40f,.40f,.48f);
        for(float x=from;x<to;x+=5)
        {
            Sprite("Grass",Art("Reed"),new Vector3(x,-1.67f,0),Vector3.one*.45f,foliage,0);
            Sprite("Hedge",Art("Foliage"),new Vector3(x+1,-2.9f,0),new Vector3(1,.6f,1),foliage,2);
            var mote=Sprite("Firefly",Art("Disc"),new Vector3(x+2,.7f+Mathf.Sin(x)*1.6f,0),Vector3.one*.055f,new Color(1,.93f,.63f,.6f),-1);
            mote.gameObject.AddComponent<AdventureParticle>().ambient=true;
        }
        for(float x=from;x<to;x+=16)
        {
            var cloud=Sprite("DistantCloud",Art("Cloud"),new Vector3(x,3.1f+Mathf.Sin(x)*.6f,0),Vector3.one*1.2f,new Color(1,1,1,.25f),-8);
            cloud.gameObject.AddComponent<AdventureParallax>().factor=.18f;
        }
    }
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=anchor;rt.anchoredPosition=pos;rt.sizeDelta=size;return rt;
    }
    static RectTransform Full(string name,Transform parent)
    {
        var rt=Rect(name,parent,Vector2.zero,Vector2.zero,Vector2.zero);rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;return rt;
    }
    static Image Image(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color,Sprite sprite=null)
    {
        var rt=Rect(name,parent,anchor,pos,size);var image=rt.gameObject.AddComponent<Image>();image.color=color;image.sprite=sprite?sprite:panelSprite;image.type=sprite?UnityEngine.UI.Image.Type.Simple:UnityEngine.UI.Image.Type.Sliced;image.raycastTarget=false;return image;
    }
    static Text Label(string name,Transform parent,string text,int fontSize,Vector2 anchor,Vector2 pos,Vector2 size,Color color,TextAnchor alignment=TextAnchor.MiddleCenter)
    {
        var rt=Rect(name,parent,anchor,pos,size);var label=rt.gameObject.AddComponent<Text>();label.font=font;label.fontSize=fontSize;label.text=text;label.color=color;label.alignment=alignment;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;return label;
    }
    static Button Button(string name,Transform parent,string text,Vector2 anchor,Vector2 pos,Vector2 size,bool primary,out Text label)
    {
        var image=Image(name,parent,anchor,pos,size,primary?mint:new Color(.23f,.35f,.37f));image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(.88f,1,.92f);colors.pressedColor=new Color(.68f,.85f,.73f);button.colors=colors;
        label=Label("Label",image.transform,text,25,new Vector2(.5f,.5f),Vector2.zero,size,primary?ink:new Color(.9f,.96f,.91f));return button;
    }
    static void UI(AdventureFlow flow,bool menu)
    {
        var canvasGo=new GameObject("AdventureUI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
        var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var safe=Full("SafeArea",canvasGo.transform);safe.gameObject.AddComponent<SafeAreaFitter>();
        var hud=Full("HUD",safe);flow.hud=hud.gameObject;
        Image("ProgressCard",hud,new Vector2(0,1),new Vector2(230,-65),new Vector2(390,86),new Color(.06f,.18f,.21f,.9f));
        flow.progressText=Label("Progress",hud,"星光 0 / 3   ·   印记 0 / 3",25,new Vector2(0,1),new Vector2(230,-72),new Vector2(390,50),new Color(.91f,.96f,.89f));
        flow.taskText=Label("Objective",hud,flow.task,22,new Vector2(.5f,1),new Vector2(75,-61),new Vector2(750,72),ink);
        var pause=Button("Pause",hud,"歇一会儿",new Vector2(1,1),new Vector2(-110,-60),new Vector2(180,52),false,out Text unused);UnityEditor.Events.UnityEventTools.AddPersistentListener(pause.onClick,flow.Pause);
        flow.starImages=new Image[3];for(int i=0;i<3;i++)flow.starImages[i]=Image("Star"+i,hud,new Vector2(0,1),new Vector2(149+i*70,-31),new Vector2(28,28),gold,Art("Star"));
        flow.toastText=Label("Toast",hud,"",25,new Vector2(.5f,0),new Vector2(0,132),new Vector2(1040,65),Color.white);
        var shadow=flow.toastText.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,.10f,.16f,.9f);shadow.effectDistance=new Vector2(1,-2);
        var inputs=Full("TouchControls",safe);flow.controls=inputs.gameObject;
        var left=Image("Left",inputs,new Vector2(0,0),new Vector2(108,100),new Vector2(130,120),new Color(.05f,.20f,.23f,.45f));left.raycastTarget=true;left.gameObject.AddComponent<AdventureDirectionButton>().axis=-1;
        Label("Arrow",left.transform,"‹",65,new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,100),Color.white);
        var right=Image("Right",inputs,new Vector2(0,0),new Vector2(258,100),new Vector2(130,120),new Color(.05f,.20f,.23f,.45f));right.raycastTarget=true;right.gameObject.AddComponent<AdventureDirectionButton>().axis=1;
        Label("Arrow",right.transform,"›",65,new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,100),Color.white);
        var jump=Image("Jump",inputs,new Vector2(1,0),new Vector2(-135,100),new Vector2(145,125),new Color(.05f,.20f,.23f,.45f));jump.raycastTarget=true;jump.gameObject.AddComponent<MobileJumpButton>();
        Label("JumpLabel",jump.transform,"跳",38,new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,100),Color.white);
        var overlay=Full("StoryPanel",safe);flow.overlay=overlay.gameObject.AddComponent<CanvasGroup>();
        var shade=Full("Shade",overlay).gameObject.AddComponent<Image>();shade.color=new Color(.03f,.12f,.17f,menu?.15f:.35f);shade.raycastTarget=true;
        var card=Image("StoryCard",overlay,new Vector2(.5f,.5f),new Vector2(menu?-270:0,0),new Vector2(790,565),new Color(.05f,.17f,.20f,.94f));
        Image("TopRule",card.transform,new Vector2(.5f,.5f),new Vector2(0,216),new Vector2(58,5),mint);
        flow.heading=Label("Heading",card.transform,"点亮归途",menu?62:42,new Vector2(.5f,.5f),new Vector2(0,145),new Vector2(710,95),new Color(.96f,.95f,.84f));
        flow.body=Label("Body",card.transform,"",27,new Vector2(.5f,.5f),new Vector2(0,22),new Vector2(700,190),new Color(.75f,.87f,.83f));
        flow.mainButton=Button("MainAction",card.transform,"启程",new Vector2(.5f,.5f),new Vector2(0,-122),new Vector2(260,64),true,out flow.mainLabel);
        flow.secondaryButton=Button("Secondary",card.transform,"",new Vector2(.5f,.5f),new Vector2(-155,-196),new Vector2(260,52),false,out flow.secondaryLabel);
        flow.extraButton=Button("Extra",card.transform,"",new Vector2(.5f,.5f),new Vector2(155,-196),new Vector2(260,52),false,out flow.extraLabel);
        flow.tinyText=Label("Footnote",overlay,"THE ADVENTURES OF YEGE",17,new Vector2(.5f,0),new Vector2(0,38),new Vector2(1400,40),new Color(.91f,.94f,.84f));
        flow.skipButton=Button("SkipEnding",safe,"跳过演出",new Vector2(1,0),new Vector2(-130,46),new Vector2(200,48),false,out Text skipLabel);
        var fade=Full("Fade",canvasGo.transform);var fadeImage=fade.gameObject.AddComponent<Image>();fadeImage.color=ink;fadeImage.raycastTarget=true;flow.fade=fade.gameObject.AddComponent<CanvasGroup>();flow.fade.alpha=0;flow.fade.blocksRaycasts=false;
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
    }
    [MenuItem("YeGE/展示版/校验当前展示场景")]
    public static void Validate()
    {
        var ids=new HashSet<string>(); int count=0;
        foreach(string name in Names)
        {
            var scene=EditorSceneManager.OpenScene(Folder+name+".scene");
            var flows=UnityEngine.Object.FindObjectsOfType<AdventureFlow>();if(flows.Length!=1)throw new Exception(name+": expected one Flow");
            var flow=flows[0];if(!flow.overlay||!flow.fade||!flow.mainButton||!flow.heading||!flow.body)throw new Exception(name+": UI reference missing");
            if(UnityEngine.Object.FindObjectsOfType<EventSystem>().Length!=1)throw new Exception(name+": EventSystem count");
            if(flow.mode==AdventureFlow.Mode.Chapter)
            {
                var items=UnityEngine.Object.FindObjectsOfType<AdventureCollectible>();
                if(items.Count(i=>i.kind==AdventureCollectible.Kind.Star)!=1||items.Count(i=>i.kind==AdventureCollectible.Kind.Stamp)!=1)throw new Exception(name+": collectible count");
                foreach(var item in items)if(string.IsNullOrEmpty(item.id)||!ids.Add(item.id))throw new Exception("Duplicate collectible id "+item.id);
                foreach(var platform in UnityEngine.Object.FindObjectsOfType<MovingBlock>())if(platform.times<=0)throw new Exception("Invalid platform duration");
                if(!UnityEngine.Object.FindObjectOfType<PlayerController>())throw new Exception("Player missing");
                if(UnityEngine.Object.FindObjectsOfType<GameManager>().Length!=0)throw new Exception("Legacy manager still active");
                foreach(var solid in UnityEngine.Object.FindObjectsOfType<Collider2D>())
                {
                    if(!solid.enabled || solid.isTrigger || solid.CompareTag("Player")) continue;
                    var visible = solid.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled && r.color.a>.01f);
                    if(!visible)throw new Exception(name+": invisible solid collider: "+solid.name);
                }
            }
            foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true)) if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)>0)throw new Exception("Missing script: "+tr.name);
            count++;
        }
        File.WriteAllText("AdventureValidation.txt","PASS: "+count+" scenes, "+ids.Count+" unique collectibles, mandatory stars and optional stamps, UI references, event systems, platform settings, no missing scripts.\n");
        Debug.Log("[Adventure] VALIDATION_PASS "+count+" scenes / "+ids.Count+" collectibles");
    }
    [MenuItem("YeGE/展示版/仅构建 Windows 试玩版")]
    public static void BuildWindows()
    {
        BuildPlayerOptions options=new BuildPlayerOptions{scenes=Names.Select(n=>Folder+n+".scene").ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName="Build/Windows/YeGeAdventure.exe",options=BuildOptions.None};
        var report=BuildPipeline.BuildPlayer(options);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("[Adventure] WINDOWS_BUILD_SUCCESS");
    }
    [MenuItem("YeGE/展示版/仅构建 Android APK")]
    public static void BuildAndroid()
    {
        YeGEAndroidBuild.SetupAndroidSettings();PlayerSettings.productName="The adventures of YeGe - Light the Way";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.yege.lighttheway");
        BuildPlayerOptions options=new BuildPlayerOptions{scenes=Names.Select(n=>Folder+n+".scene").ToArray(),target=BuildTarget.Android,locationPathName="Build/YeGeAdventure.apk",options=BuildOptions.None};
        var report=BuildPipeline.BuildPlayer(options);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
        Debug.Log("[Adventure] ANDROID_BUILD_SUCCESS");
    }
}
