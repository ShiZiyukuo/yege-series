using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class AdventureFlow : MonoBehaviour
{
    public enum Mode { Title, Chapter, Journal }
    public enum State { Intro, Playing, Respawning, Paused, Transition, Ending, Complete }
    public static AdventureFlow Current { get; private set; }
    public Mode mode;
    public int chapter;
    public string chapterName;
    public string task;
    public string nextScene;
    public State state = State.Intro;
    public Text progressText, taskText, toastText, heading, body, tinyText;
    public Image[] starImages;
    public CanvasGroup overlay, fade;
    public GameObject hud, controls, titleArt;
    public Button mainButton, secondaryButton, extraButton, skipButton;
    public Text mainLabel, secondaryLabel, extraLabel;
    public Sprite sparkSprite;
    public AudioClip music, clearSound;
    public Transform lantern;
    public SpriteRenderer lanternGlow;
    public SpriteRenderer sky;
    public SpriteRenderer[] routeLights;
    public float endingSeconds = 20f;
    PlayerController player;
    Vector3 checkpoint;
    float toastUntil;
    bool loadPending, skipEnding;
    Coroutine sequence;
    AudioSource bgm, sfx;
    AudioClip gemTone, starTone, switchTone;
    Vector3[] enemyPositions;
    string[] enemyDirections;
    EmeryController[] enemies;
    MovingBlock[] platforms;
    GimmickBlock[] falling;
    Rigidbody2D[] enemyBodies;
    public bool IsPlaying { get { return mode == Mode.Chapter && state == State.Playing; } }
    public static bool WorldPlaying { get { return Current == null || Current.IsPlaying; } }

    void Awake()
    {
        Current = this;
        Time.timeScale = 1;
        bgm = gameObject.AddComponent<AudioSource>(); bgm.loop = true; bgm.clip = music; bgm.volume = .28f;
        sfx = gameObject.AddComponent<AudioSource>(); sfx.volume = .36f;
        gemTone = Tone(880, .14f); starTone = Tone(660, .45f); switchTone = Tone(330, .3f);
        mainButton.onClick.AddListener(MainAction);
        secondaryButton.onClick.AddListener(SecondaryAction);
        extraButton.onClick.AddListener(ExtraAction);
        skipButton.onClick.AddListener(Skip);
    }
    void Start()
    {
        if (music) bgm.Play();
        FadeAlpha(0); skipButton.gameObject.SetActive(false);
        if (mode == Mode.Title) { Title(); return; }
        if (mode == Mode.Journal) { Journal(); return; }
        AdventureSession.Enter(chapter);
        player = FindObjectOfType<PlayerController>(); checkpoint = player.transform.position;
        enemies = FindObjectsOfType<EmeryController>(); enemyPositions = new Vector3[enemies.Length]; enemyDirections = new string[enemies.Length];
        enemyBodies = new Rigidbody2D[enemies.Length];
        for (int i = 0; i < enemies.Length; i++) { enemyPositions[i] = enemies[i].transform.position; enemyDirections[i] = enemies[i].direction; enemyBodies[i] = enemies[i].GetComponent<Rigidbody2D>(); }
        platforms = FindObjectsOfType<MovingBlock>(); falling = FindObjectsOfType<GimmickBlock>();
        if (hud) hud.SetActive(true);
        if (controls) controls.SetActive(Application.isMobilePlatform);
        RefreshHUD(); sequence = StartCoroutine(Intro());
    }
    void Update()
    {
        if (mode != Mode.Chapter) return;
        if (IsPlaying) AdventureSession.Seconds += Time.deltaTime;
        if (toastText && Time.unscaledTime > toastUntil) toastText.text = "";
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (state == State.Playing) Pause(); else if (state == State.Paused) Resume();
        }
        if (state == State.Intro && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) Skip();
    }
    void OnDestroy()
    {
        if (Current == this) Current = null;
        if (gemTone) Destroy(gemTone); if (starTone) Destroy(starTone); if (switchTone) Destroy(switchTone);
    }
    void OnApplicationFocus(bool focused)
    {
        if (!focused && IsPlaying) Pause();
    }
    void Title()
    {
        state = State.Complete;
        if (hud) hud.SetActive(false); if (controls) controls.SetActive(false);
        Panel("点亮归途", "风吹散了归途灯里的三枚星光。\n穿过草坡、旧渡口与遗迹，\n把回家的光，带回来。", "启程", "", "");
        tinyText.text = "THE ADVENTURES OF YEGE    /    一段小小的冒险";
    }
    void Journal()
    {
        state = State.Complete; if (hud) hud.SetActive(false); if (controls) controls.SetActive(false);
        string stamp = AdventureSession.Stamps == 3 ? "三枚印记拼成了你的专属旅行贴纸。" : "还有一些小秘密，留给下一次出发。";
        Panel("归途，已点亮", "草坡启程  ·  旧渡口  ·  遗迹归途\n\n星光  " + AdventureSession.Stars + " / 3      旅行印记  " + AdventureSession.Stamps + " / 3\n\n" + stamp, "再度启程", "返回标题", "");
        tinyText.text = "宝石 " + AdventureSession.Gems + "   /   旅途 " + Mathf.FloorToInt(AdventureSession.Seconds / 60) + " 分 " + Mathf.FloorToInt(AdventureSession.Seconds % 60) + " 秒";
    }
    IEnumerator Intro()
    {
        SetState(State.Intro);
        Panel("0" + (chapter + 1) + "  /  " + chapterName,
            chapter == 0 ? "把三枚星光带回归途灯。\n先沿着宝石，试着跳过草坡。" : task + "\n留意高处：每一章，都藏着一枚旅行印记。", "开始冒险", "", "");
        tinyText.text = chapter == 0 ? "A / D 或方向键移动    SPACE 跳跃    ESC 暂停" : "星光会指引主路，印记属于好奇的人。";
        yield return new WaitForSecondsRealtime(chapter == 0 ? 6f : 3f);
        if (state == State.Intro) BeginPlaying();
    }
    void BeginPlaying()
    {
        overlay.gameObject.SetActive(false); skipButton.gameObject.SetActive(false);
        SetState(State.Playing); Toast(task, 3.5f);
    }
    public void Pause()
    {
        if (!IsPlaying) return;
        SetState(State.Paused);
        Panel("歇一会儿", "星光和发现都已经记下。", "继续旅途", "重玩本章", "返回标题"); tinyText.text = "暂停时，旅途计时也会停下。";
    }
    void Resume() { overlay.gameObject.SetActive(false); SetState(State.Playing); }
    void MainAction()
    {
        if (mode == Mode.Title || mode == Mode.Journal) { AdventureSession.Reset(); Load("AdventureTitle", true); }
        else if (state == State.Intro) { if (sequence != null) StopCoroutine(sequence); BeginPlaying(); }
        else if (state == State.Paused) Resume();
    }
    void SecondaryAction()
    {
        if (mode == Mode.Journal) Load("AdventureTitle");
        else if (state == State.Paused) { AdventureSession.RestartChapter(); Load(SceneManager.GetActiveScene().name); }
    }
    void ExtraAction() { Load("AdventureTitle"); }
    public void Skip()
    {
        if (state == State.Intro) MainAction();
        else if (state == State.Ending) skipEnding = true;
    }
    void Panel(string title, string description, string primary, string secondary, string extra)
    {
        overlay.gameObject.SetActive(true); overlay.alpha = 1; overlay.blocksRaycasts = true;
        heading.text = title; body.text = description;
        mainLabel.text = primary; secondaryLabel.text = secondary; extraLabel.text = extra;
        mainButton.gameObject.SetActive(!string.IsNullOrEmpty(primary)); secondaryButton.gameObject.SetActive(!string.IsNullOrEmpty(secondary)); extraButton.gameObject.SetActive(!string.IsNullOrEmpty(extra));
        var secondaryRect = secondaryButton.GetComponent<RectTransform>();
        secondaryRect.anchoredPosition = new Vector2(string.IsNullOrEmpty(extra) ? 0 : -155, -196);
    }
    public void SetState(State value)
    {
        state = value; PlayerController.gameState = value == State.Playing ? "playing" : "adventurewait";
        if (player) { player.ClearInput(); player.SetFrozen(value != State.Playing); }
        if (controls) controls.SetActive(value == State.Playing && Application.isMobilePlatform);
        if (enemyBodies != null) foreach (var rb in enemyBodies) if (rb) { if (value != State.Playing) rb.velocity = Vector2.zero; rb.simulated = value == State.Playing; }
        if (falling != null) foreach (var block in falling) if (block) block.GetComponent<Rigidbody2D>().simulated = value == State.Playing;
        foreach (var shell in FindObjectsOfType<ShellController>())
        {
            var rb = shell.GetComponent<Rigidbody2D>(); if (rb) rb.simulated = value == State.Playing;
        }
    }
    public void Toast(string message, float duration = 2.5f)
    {
        if (!toastText) return;
        toastText.text = message; toastUntil = Time.unscaledTime + duration;
    }
    public void RefreshHUD()
    {
        if (progressText) progressText.text = "星光  " + AdventureSession.Stars + " / 3    ·    印记  " + AdventureSession.Stamps + " / 3";
        if (taskText) taskText.text = "0" + (chapter + 1) + "  " + chapterName + "    /    " + (AdventureSession.Collected.Contains("star-" + chapter) ? "星光已找到，前往路牌" : task);
        if (starImages != null) for (int i = 0; i < starImages.Length; i++) starImages[i].color = AdventureSession.Collected.Contains("star-" + i) ? new Color(1, .82f, .32f) : new Color(.7f, .77f, .81f, .35f);
    }
    public void Collected(AdventureCollectible item)
    {
        Color c = item.kind == AdventureCollectible.Kind.Stamp ? new Color(.62f, .92f, .83f) : new Color(1, .85f, .4f);
        Burst(item.transform.position, c, item.kind == AdventureCollectible.Kind.Gem ? 6 : 18);
        sfx.PlayOneShot(item.kind == AdventureCollectible.Kind.Gem ? gemTone : starTone);
        if (item.kind == AdventureCollectible.Kind.Star)
        {
            Toast("找到了星光！归途又亮了一点。", 3);
            StartCoroutine(FlyStar(item.transform.position));
            foreach (var light in routeLights) if (light) light.color = new Color(1,.90f,.68f);
        }
        if (item.kind == AdventureCollectible.Kind.Stamp) Toast("发现秘密：" + chapterName + "的旅行印记", 3);
        RefreshHUD();
    }
    IEnumerator FlyStar(Vector3 position)
    {
        if (starImages == null || chapter >= starImages.Length) yield break;
        var canvas = starImages[chapter].GetComponentInParent<Canvas>();
        var rect = canvas.GetComponent<RectTransform>();
        Vector2 start, finish;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Camera.main.WorldToScreenPoint(position), null, out start);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, starImages[chapter].transform.position, null, out finish);
        var go = new GameObject("StarArrival", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(rect, false); rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f); rt.sizeDelta = new Vector2(56,56);
        var image = go.GetComponent<Image>(); image.sprite = sparkSprite; image.raycastTarget = false;
        float t = 0;
        while (t < .8f)
        {
            t += Time.unscaledDeltaTime; float f = Mathf.SmoothStep(0,1,t/.8f);
            rt.anchoredPosition = Vector2.Lerp(start,finish,f) + Vector2.up * Mathf.Sin(f*Mathf.PI)*70;
            rt.localScale = Vector3.one * Mathf.Lerp(1,.55f,f); yield return null;
        }
        Destroy(go);
    }
    public void SwitchActivated(Vector3 pos)
    {
        Burst(pos + Vector3.up, new Color(.5f, 1, .8f), 16); sfx.PlayOneShot(switchTone);
        Toast("渡口醒来了！乘着渡台，去找星光。", 4);
        foreach (var light in routeLights) if (light) light.color = new Color(1, .85f, .42f);
    }
    public void Burst(Vector3 position, Color color, int count)
    {
        if (!sparkSprite) return;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Spark"); go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sparkSprite; sr.color = color; sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * Random.Range(.07f, .15f);
            var p = go.AddComponent<AdventureParticle>(); p.velocity = new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(.7f, 3f), 0);
        }
    }
    public void Checkpoint(Vector3 position)
    {
        // Walking back past an old marker must not move the checkpoint backwards.
        if (position.x > checkpoint.x) checkpoint = position;
    }
    public void Die()
    {
        if (!IsPlaying) return;
        AdventureSession.Deaths++; sequence = StartCoroutine(Respawn());
    }
    IEnumerator Respawn()
    {
        SetState(State.Respawning); player.PlayPose(player.deadAnime); Toast("别着急，归途已经记下。", 1.5f);
        yield return FadeTo(1, .3f);
        foreach (var shell in FindObjectsOfType<ShellController>()) Destroy(shell.gameObject);
        foreach (var block in falling) if (block) block.ResetBlock();
        foreach (var platform in platforms) if (platform) platform.ResetPlatform();
        foreach (var cannon in FindObjectsOfType<CannonController>()) cannon.ResetCannon();
        for (int i = 0; i < enemies.Length; i++) if (enemies[i]) { enemies[i].transform.position = enemyPositions[i]; enemies[i].direction = enemyDirections[i]; if (enemyBodies[i]) enemyBodies[i].velocity = Vector2.zero; }
        player.transform.position = checkpoint; player.ResetMotion(); player.PlayPose(player.stopAnime);
        var cam = Camera.main.GetComponent<CameraManager>(); if (cam) cam.Snap();
        yield return new WaitForSecondsRealtime(.2f); yield return FadeTo(0, .35f);
        SetState(State.Playing);
    }
    public void RequestExit()
    {
        if (!IsPlaying) return;
        // Queue after physics callbacks so the same-step star pickup is always registered first.
        if (loadPending) return;
        loadPending = true; StartCoroutine(ExitAfterPhysics());
    }
    IEnumerator ExitAfterPhysics()
    {
        yield return null;
        loadPending = false;
        if (!IsPlaying) yield break;
        if (!AdventureSession.Collected.Contains("star-" + chapter)) { Toast("还有一枚星光在这片风景里，沿灯笼往回看看。", 3); yield break; }
        if (chapter == 2 && AdventureSession.Stars != 3) { Toast("完整归途需要三枚星光。请从标题启程，走过三章。", 4); yield break; }
        AdventureSession.Completed.Add(chapter);
        if (chapter == 2) { sequence = StartCoroutine(Ending()); yield break; }
        SetState(State.Transition); Toast(chapterName + "，已记入旅途。", 2);
        yield return new WaitForSecondsRealtime(.65f); yield return FadeTo(1, .5f); Load(nextScene);
    }
    IEnumerator Ending()
    {
        SetState(State.Ending); overlay.gameObject.SetActive(false); skipButton.gameObject.SetActive(true);
        Toast("三枚星光，终于找到了归处。", 5);
        var cam = Camera.main.GetComponent<CameraManager>(); if (cam) { cam.cinematic = true; }
        float elapsed = 0; Color initialSky = sky ? sky.color : Color.white;
        Vector3 cameraStart = Camera.main.transform.position;
        var stars = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            stars[i] = new GameObject("ReturningStar"); var sr = stars[i].AddComponent<SpriteRenderer>(); sr.sprite = sparkSprite; sr.color = new Color(1, .86f, .32f); sr.sortingOrder = 20; stars[i].transform.localScale = Vector3.one * .4f;
        }
        while (elapsed < endingSeconds && !skipEnding)
        {
            elapsed += Time.unscaledDeltaTime;
            float light = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6, 12, elapsed));
            if (lanternGlow) { lanternGlow.color = new Color(1, .82f, .32f, light * .72f); lanternGlow.transform.localScale = Vector3.one * Mathf.Lerp(1, 5.2f, light); }
            if (sky) sky.color = Color.Lerp(initialSky, new Color(1, .85f, .68f), light);
            Camera.main.transform.position = Vector3.Lerp(cameraStart, new Vector3(lantern.position.x - 1.5f, .8f, -10), Mathf.SmoothStep(0, 1, elapsed / 8));
            for (int i = 0; i < stars.Length; i++)
            {
                float f = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2 + i * .55f, 6 + i * .55f, elapsed));
                Vector3 start = player.transform.position + Vector3.up * .5f;
                stars[i].transform.position = Vector3.Lerp(start, lantern.position, f) + Vector3.up * Mathf.Sin(f * Mathf.PI) * (1.4f + i * .4f);
                stars[i].SetActive(elapsed >= 2 && f < .99f);
            }
            if (elapsed > 10) foreach (var lightSprite in routeLights) if (lightSprite) lightSprite.color = new Color(1, .83f, .36f);
            if (elapsed > 15) { player.PlayPose(player.goalAnime); Toast("每一点星光，都记得回家的路。", 1); }
            yield return null;
        }
        foreach (var star in stars) Destroy(star);
        if (lanternGlow) { lanternGlow.color = new Color(1, .82f, .32f, .72f); lanternGlow.transform.localScale = Vector3.one * 5.2f; }
        if (clearSound) sfx.PlayOneShot(clearSound, .4f);
        skipButton.gameObject.SetActive(false); yield return FadeTo(1, .6f); Load("AdventureJournal");
    }
    public IEnumerator FadeTo(float alpha, float duration)
    {
        float from = fade.alpha, t = 0; fade.blocksRaycasts = true;
        while (t < duration) { t += Time.unscaledDeltaTime; FadeAlpha(Mathf.Lerp(from, alpha, t / duration)); yield return null; }
        FadeAlpha(alpha); fade.blocksRaycasts = alpha > .99f;
    }
    void FadeAlpha(float value) { fade.alpha = value; fade.blocksRaycasts = value > .99f; }
    public void Load(string scene, bool start = false)
    {
        if (loadPending) return; loadPending = true; Time.timeScale = 1;
        SceneManager.LoadScene(start ? "AdventureStage1" : scene);
    }
    static AudioClip Tone(float frequency, float duration)
    {
        const int rate = 22050; var data = new float[Mathf.CeilToInt(duration * rate)];
        for (int i = 0; i < data.Length; i++) { float t = i / (float)rate; float envelope = Mathf.Sin(Mathf.PI * i / data.Length) * Mathf.Exp(-t * 5); data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * .20f; }
        var clip = AudioClip.Create("AdventureTone", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
    }
}
