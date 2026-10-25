using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 教程 10.1.3「编写用来管理标题界面的脚本」+ 10.2.5「编写用于继续游戏处理的脚本」。
/// </summary>
public class TitleManager : MonoBehaviour
{
    public string firstSceneName = "WorldMap";   // 游戏开始时读取的场景名

    public GameObject startButton;
    public GameObject continueButton;

    void Start()
    {
        SoundManager.PlayBgm(BGMType.Title);

        Button start = startButton != null ? startButton.GetComponent<Button>() : null;
        Button cont = continueButton != null ? continueButton.GetComponent<Button>() : null;

        if (start != null)
        {
            start.onClick.RemoveAllListeners();
            start.onClick.AddListener(StartButtonClicked);
        }
        if (cont != null)
        {
            cont.onClick.RemoveAllListeners();
            cont.onClick.AddListener(ContinueButtonClicked);
        }

        // 没有存档时把 CONTINUE 按钮无效化
        string sceneName = PlayerPrefs.GetString("LastScene", "");
        if (cont != null)
        {
            cont.interactable = !string.IsNullOrEmpty(sceneName);
        }
    }

    /// <summary>从头开始游戏：清空所有存档并读取最初的场景。</summary>
    public void StartButtonClicked()
    {
        SoundManager.SEPlay(SEType.Button);

        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("PlayerHP", 3);
        PlayerPrefs.SetString("LastScene", firstSceneName);
        PlayerPrefs.SetInt("LastDoor", 0);
        PlayerPrefs.Save();

        PlayerController.hp = 3;
        PlayerController.gameState = "playing";
        ItemKeeper.hasArrows = 0;
        ItemKeeper.hasKeys = 0;
        RoomManager.doorNumber = 0;

        SceneManager.LoadScene(firstSceneName);
    }

    /// <summary>从中断处继续游戏。</summary>
    public void ContinueButtonClicked()
    {
        SoundManager.SEPlay(SEType.Button);

        string sceneName = PlayerPrefs.GetString("LastScene", "");
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }
        int doorNumber = PlayerPrefs.GetInt("LastDoor", 0);

        PlayerController.hp = PlayerPrefs.GetInt("PlayerHP", 3);
        if (PlayerController.hp <= 0)
        {
            PlayerController.hp = 3;
        }
        PlayerController.gameState = "playing";
        RoomManager.doorNumber = doorNumber;

        SceneManager.LoadScene(sceneName);
    }
}
