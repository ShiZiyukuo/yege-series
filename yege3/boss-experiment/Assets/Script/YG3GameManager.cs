using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 游戏总控：状态机、HUD（HP/耐力/分数/连击）、结算面板、BGM 切换、演出驱动。
/// 沿用 YeGe 的 mainImage/panel/restart/next 结构，但按钮改用 Button.onClick。
/// </summary>
public class YG3GameManager : MonoBehaviour
{
    public enum GameState { Playing, StageClear, GameOver, GameEnd }
    public static GameState State = GameState.Playing;

    public static YG3GameManager Instance;

    [Header("引用")]
    public YG3PlayerController player;

    [Header("HUD")]
    public Image hpFill;            // 玩家血条
    public Image staminaFill;       // 耐力条
    public Text scoreText;
    public Text comboText;

    [Header("中央提示与结算面板")]
    public GameObject mainImage;
    public Sprite startSpr;
    public Sprite clearSpr;
    public Sprite gameOverSpr;
    public GameObject panel;
    public GameObject restartButton;
    public GameObject nextButton;

    [Header("触屏操作 UI")]
    public GameObject inputUI;

    [Header("BGM")]
    public string bgmName = YG3Tuning.BGM_GAME;

    /// <summary>标题/结算场景设为 false，不闪开始图。</summary>
    [HideInInspector] public bool showStartImage = true;

    public static int totalScore = 0;
    int stageScore;
    int combo;
    float comboTimer;
    float comboWindow = 3f;
    Image mainImg;
    YG3BossHealthBar bossBar;
    bool bossBound;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        State = GameState.Playing;
        stageScore = 0;
        mainImg = mainImage != null ? mainImage.GetComponent<Image>() : null;
        if (showStartImage && mainImg != null && startSpr != null)
        {
            mainImg.sprite = startSpr;
            mainImage.SetActive(true);
            Invoke("HideMainImage", 1f);
        }
        if (panel != null)
        {
            panel.SetActive(false);
        }
        if (inputUI != null)
        {
            inputUI.SetActive(Application.isMobilePlatform);
        }
        YG3SoundManager.BGM(bgmName);
        UpdateScore();
    }

    void Update()
    {
        YG3Fx.Tick();   // 顿帧 / 特效生命 / 屏震衰减（唯一驱动点）

        if (State != GameState.Playing)
        {
            return;
        }

        UpdateHud();

        if (combo > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                combo = 0;
                if (comboText != null)
                {
                    comboText.gameObject.SetActive(false);
                }
            }
        }

        if (player == null || player.IsDead)
        {
            return;
        }

        YG3Health hp = player.GetComponent<YG3Health>();
        if (hp != null && hp.IsDead)
        {
            player.Die();
        }
    }

    void UpdateHud()
    {
        if (player == null)
        {
            return;
        }
        YG3Health hp = player.GetComponent<YG3Health>();
        if (hp != null && hpFill != null)
        {
            hpFill.fillAmount = Mathf.Clamp01(hp.hp / hp.maxHp);
        }
        if (staminaFill != null)
        {
            staminaFill.fillAmount = player.StaminaRatio;
        }
        // Boss 血条挂接（BossStage）
        if (!bossBound && bossBar == null)
        {
            bossBar = FindObjectOfType<YG3BossHealthBar>();
        }
        if (bossBar != null)
        {
            bossBar.Refresh();
        }
    }

    // ---------------- 流程 ----------------

    public void AddScore(int amount)
    {
        stageScore += amount;
        combo++;
        comboTimer = comboWindow;
        UpdateScore();
        if (comboText != null && combo > 1)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = combo + " COMBO!";
        }
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = (totalScore + stageScore).ToString();
        }
    }

    /// <summary>触到 Goal / Boss 被击杀。</summary>
    public void StageClear()
    {
        if (State != GameState.Playing)
        {
            return;
        }
        State = GameState.StageClear;
        totalScore += stageScore;
        stageScore = 0;
        ShowResult(clearSpr, false, true);
        YG3SoundManager.StopBGM();
        YG3SoundManager.SE(YG3Tuning.ME_CLEAR, 0.9f);
        HideInputUI();
    }

    public void NotifyPlayerDead()
    {
        if (State != GameState.Playing)
        {
            return;
        }
        State = GameState.GameOver;
        ShowResult(gameOverSpr, true, false);
        YG3SoundManager.StopBGM();
        YG3SoundManager.SE(YG3Tuning.ME_GAME_OVER, 0.9f);
        HideInputUI();
    }

    void ShowResult(Sprite spr, bool canRestart, bool canNext)
    {
        if (mainImage != null)
        {
            mainImage.SetActive(true);
            if (mainImg != null && spr != null)
            {
                mainImg.sprite = spr;
            }
        }
        if (panel != null)
        {
            panel.SetActive(true);
        }
        if (restartButton != null)
        {
            restartButton.GetComponent<Button>().interactable = canRestart;
        }
        if (nextButton != null)
        {
            nextButton.GetComponent<Button>().interactable = canNext;
        }
    }

    void HideMainImage()
    {
        if (mainImage != null)
        {
            mainImage.SetActive(false);
        }
    }

    void HideInputUI()
    {
        if (inputUI != null)
        {
            inputUI.SetActive(false);
        }
    }

    // ---------------- 按钮回调（Builder 绑定 Button.onClick） ----------------

    public void OnRestart()
    {
        YG3SoundManager.SE(YG3Tuning.SE_BUTTON);
        Time.timeScale = 1f;
        totalScore = 0;
        LoadSceneSafe(SceneManager.GetActiveScene().name);
    }

    public void OnNext()
    {
        YG3SoundManager.SE(YG3Tuning.SE_BUTTON);
        Time.timeScale = 1f;
        string next = NextOf(SceneManager.GetActiveScene().name);
        LoadSceneSafe(next);
    }

    void LoadSceneSafe(string scene)
    {
        if (string.IsNullOrEmpty(scene))
        {
            scene = YG3SceneNames.Title;
        }
        SceneManager.LoadScene(scene);
    }

    public static string NextOf(string current)
    {
        switch (current)
        {
            case YG3SceneNames.Title: return YG3SceneNames.Stage1;
            case YG3SceneNames.Stage1: return YG3SceneNames.Stage2;
            case YG3SceneNames.Stage2: return YG3SceneNames.BossStage;
            case YG3SceneNames.BossStage: return YG3SceneNames.Result;
            default: return YG3SceneNames.Title;
        }
    }
}
