using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 标题界面控制（教程第6.2节 增加标题界面）
/// 点击 GameStart 图片后清零总分并进入第一关。
/// </summary>
public class TitleController : MonoBehaviour, IPointerClickHandler
{
    public string firstStageName = "Stage1";

    Button button;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        if (button != null && !button.interactable)
        {
            return;
        }
        StartGame();
    }

    public void StartGame()
    {
        GameManager.totalScore = 0;
        PlayerController.gameState = "playing";
        SceneManager.LoadScene(firstStageName);
    }
}
