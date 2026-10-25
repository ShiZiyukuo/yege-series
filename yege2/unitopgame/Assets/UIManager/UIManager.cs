using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 教程 9.4.9「编写用于管理游戏UI的脚本」+ 10.2.6「游戏的再挑战处理」+
/// 10.3.12「游戏通关的处理」。
/// </summary>
public class UIManager : MonoBehaviour
{
    int arrowsNum = -1;
    int keysNum = -1;
    int hpNum = -1;

    bool mainImageLocked = false;

    public GameObject itemImage;      // 道具数量显示区
    public GameObject mainImage;      // 游戏状态显示（GAME START / OVER / CLEAR）
    public GameObject inputPanel;     // 虚拟面板与攻击按钮所在的面板
    public GameObject resetButton;    // 再挑战按钮

    public Text arrowsText;
    public Text keysText;
    public Image hpImage;

    public Sprite hp0Spr;
    public Sprite hp1Spr;
    public Sprite hp2Spr;
    public Sprite hp3Spr;

    public Sprite gameStartSpr;
    public Sprite gameOverSpr;
    public Sprite gameClearSpr;

    public GameObject attackButton;   // 攻击按钮（运行时挂接 PointerDown）
    public VirtualPad virtualPad;

    public string retrySceneName = "";

    void Start()
    {
        // 再挑战按钮（用 Button.onClick，天然遵循 interactable 设定）
        if (resetButton != null)
        {
            Button button = resetButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Retry);
            }
            resetButton.SetActive(false);
        }

        SetupAttackButton();

        // 游戏开始时显示 GAME START
        if (PlayerController.gameState == "playing")
        {
            SetMainImage(gameStartSpr);
            Invoke("HideMainImage", 1.5f);
        }

        UpdateItemCount();
        UpdateHP();
    }

    void Update()
    {
        UpdateItemCount();
        UpdateHP();
    }

    /// <summary>教程 9.4.8：攻击按钮通过 Pointer Down 调用虚拟面板的 Attack。</summary>
    void SetupAttackButton()
    {
        if (attackButton == null || virtualPad == null)
        {
            return;
        }

        EventTrigger trigger = attackButton.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = attackButton.gameObject.AddComponent<EventTrigger>();
        }
        if (trigger.triggers == null)
        {
            trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
        }
        if (trigger.triggers.Count > 0)
        {
            return;   // 已经设置过，避免重复挂接
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback = new EventTrigger.TriggerEvent();
        entry.callback.AddListener(delegate { virtualPad.Attack(); });
        trigger.triggers.Add(entry);
    }

    /// <summary>更新道具数量显示。</summary>
    void UpdateItemCount()
    {
        if (arrowsNum != ItemKeeper.hasArrows)
        {
            arrowsNum = ItemKeeper.hasArrows;
            if (arrowsText != null)
            {
                arrowsText.text = arrowsNum.ToString();
            }
        }
        if (keysNum != ItemKeeper.hasKeys)
        {
            keysNum = ItemKeeper.hasKeys;
            if (keysText != null)
            {
                keysText.text = keysNum.ToString();
            }
        }
    }

    /// <summary>更新 HP 显示；HP 归零时显示 GAME OVER 并隐藏操作 UI。</summary>
    void UpdateHP()
    {
        int hp = PlayerController.hp;
        if (hp == hpNum)
        {
            return;
        }
        hpNum = hp;

        if (hpImage != null)
        {
            if (hp <= 0)
            {
                hpImage.sprite = hp0Spr;
            }
            else if (hp == 1)
            {
                hpImage.sprite = hp1Spr;
            }
            else if (hp == 2)
            {
                hpImage.sprite = hp2Spr;
            }
            else
            {
                hpImage.sprite = hp3Spr;
            }
        }

        if (hp <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        if (PlayerController.gameState == "gameclear")
        {
            return;
        }
        mainImageLocked = true;
        SetMainImage(gameOverSpr);
        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }
        if (resetButton != null)
        {
            resetButton.SetActive(true);
        }
    }

    /// <summary>游戏通关（由 Exit 在 doorNumber 为 100 时调用）。</summary>
    public void GameClear()
    {
        if (PlayerController.gameState == "gameclear")
        {
            return;
        }

        mainImageLocked = true;
        PlayerController.gameState = "gameclear";

        SetMainImage(gameClearSpr);
        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }
        if (resetButton != null)
        {
            resetButton.SetActive(false);
        }

        SoundManager.StopBgm();
        SoundManager.SEPlay(SEType.Clear);
        Invoke("GoToTitle", 3.0f);
    }

    /// <summary>通关后删除保存的场景名，使 CONTINUE 按钮失效。</summary>
    void GoToTitle()
    {
        PlayerPrefs.DeleteKey("LastScene");
        PlayerPrefs.Save();
        SceneManager.LoadScene(UniTopSceneNames.Title);
    }

    /// <summary>再挑战：HP 恢复到 3 后重新读取当前关卡。</summary>
    public void Retry()
    {
        SoundManager.SEPlay(SEType.Button);

        PlayerPrefs.SetInt("PlayerHP", 3);
        PlayerController.hp = 3;
        PlayerController.gameState = "playing";
        ItemKeeper.SaveItem();
        PlayerPrefs.Save();

        string sceneName = retrySceneName;
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = SceneManager.GetActiveScene().name;
        }

        RoomManager.doorNumber = PlayerPrefs.GetInt("LastDoor", 0);
        SceneManager.LoadScene(sceneName);
    }

    void SetMainImage(Sprite sprite)
    {
        if (mainImage == null)
        {
            return;
        }
        Image image = mainImage.GetComponent<Image>();
        if (image != null && sprite != null)
        {
            image.sprite = sprite;
        }
        mainImage.SetActive(true);
    }

    void HideMainImage()
    {
        if (mainImageLocked)
        {
            return;
        }
        if (mainImage != null)
        {
            mainImage.SetActive(false);
        }
    }
}
