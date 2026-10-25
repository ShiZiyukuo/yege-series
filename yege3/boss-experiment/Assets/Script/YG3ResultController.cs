using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>结算界面：显示总分 + 回标题 / 再来一次。</summary>
public class YG3ResultController : MonoBehaviour
{
    public Text scoreText;

    void Start()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE  " + YG3GameManager.totalScore;
        }
        Time.timeScale = 1f;
        YG3SoundManager.StopBGM();
    }

    public void OnRestart()
    {
        YG3GameManager.totalScore = 0;
        YG3SoundManager.SE(YG3Tuning.SE_BUTTON);
        SceneManager.LoadScene(YG3SceneNames.Stage1);
    }

    public void OnTitle()
    {
        YG3GameManager.totalScore = 0;
        YG3SoundManager.SE(YG3Tuning.SE_BUTTON);
        SceneManager.LoadScene(YG3SceneNames.Title);
    }
}
