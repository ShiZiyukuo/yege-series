using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏总控（教程第5章 UI 与重启 / 第6章 标题·计时·得分·结果界面 / 第7.6节 声音）
/// 负责：提示图片、重开与下一关按钮、时间显示、分数累计、BGM 与音效。
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("提示图片")]
    public GameObject mainImage;        // 中央提示图片（GameStart / StageClear / GameOver）
    public Sprite startSpr;
    public Sprite gameOverSpr;
    public Sprite gameClearSpr;

    [Header("结算面板")]
    public GameObject panel;
    public GameObject restartButton;
    public GameObject nextButton;

    [Header("时间与分数显示")]
    public GameObject timeBar;
    public GameObject timeText;
    public GameObject scoreText;

    [Header("声音（教程第7.6节）")]
    public AudioClip bgm;
    public AudioClip meGameOver;
    public AudioClip meGameClear;

    [Header("触屏操作 UI")]
    public GameObject inputUI;

    public static int totalScore = 0;   // 累计总分（跨关卡）
    public int stageScore = 0;          // 本关得分

    TimeController timeCnt;
    AudioSource audioSource;
    Image titleImage;

    void Start()
    {
        // 开局显示 GameStart 图片，1 秒后隐藏
        titleImage = mainImage.GetComponent<Image>();
        if (startSpr != null)
        {
            titleImage.sprite = startSpr;
        }
        mainImage.SetActive(true);
        Invoke("InactiveImage", 1.0f);

        if (panel != null)
        {
            panel.SetActive(false);
        }

        timeCnt = GetComponent<TimeController>();
        if (timeCnt != null)
        {
            if (timeCnt.gameTime <= 0.0f && timeBar != null)
            {
                timeBar.SetActive(false);
            }
            timeCnt.isTimeOver = false;
        }

        // BGM
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        if (bgm != null)
        {
            audioSource.clip = bgm;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.Play();
        }

        UpdateScore();
    }

    void Update()
    {
        if (PlayerController.gameState == "gameclear")
        {
            ShowResult(gameClearSpr, false, true);
            if (timeCnt != null)
            {
                timeCnt.isTimeOver = true;
                totalScore += (int)timeCnt.displayTime * 10;   // 剩余时间奖励
            }
            totalScore += stageScore;
            stageScore = 0;
            UpdateScore();
            PlaySound(meGameClear);
            HideInputUI();
        }
        else if (PlayerController.gameState == "gameover")
        {
            ShowResult(gameOverSpr, true, false);
            if (timeCnt != null)
            {
                timeCnt.isTimeOver = true;
            }
            PlaySound(meGameOver);
            HideInputUI();
        }
        else if (PlayerController.gameState == "playing")
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }
            PlayerController playerCnt = player.GetComponent<PlayerController>();

            if (timeCnt != null && timeCnt.gameTime > 0.0f)
            {
                int time = (int)timeCnt.displayTime;
                if (timeText != null)
                {
                    timeText.GetComponent<Text>().text = time.ToString();
                }
                if (time <= 0)
                {
                    playerCnt.GameOver();
                }
            }
            if (playerCnt.score != 0)
            {
                stageScore += playerCnt.score;
                playerCnt.score = 0;
                UpdateScore();
            }
        }
    }

    void ShowResult(Sprite spr, bool canRestart, bool canNext)
    {
        if (mainImage != null)
        {
            mainImage.SetActive(true);
            if (spr != null)
            {
                mainImage.GetComponent<Image>().sprite = spr;
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
        PlayerController.gameState = "gameend";
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    void HideInputUI()
    {
        if (inputUI != null)
        {
            inputUI.SetActive(false);
        }
    }

    void InactiveImage()
    {
        if (mainImage != null)
        {
            mainImage.SetActive(false);
        }
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.GetComponent<Text>().text = (stageScore + totalScore).ToString();
        }
    }

    /// <summary>虚拟跳跃按钮调用（教程第7.7节）</summary>
    public void Jump()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController playerCnt = player.GetComponent<PlayerController>();
            if (playerCnt != null)
            {
                playerCnt.Jump();
            }
        }
    }
}
