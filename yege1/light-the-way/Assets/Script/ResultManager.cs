using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结果界面（教程第6.6节 增加结果界面）
/// 显示通关后的累计总分。
/// </summary>
public class ResultManager : MonoBehaviour
{
    public GameObject scoreText;

    void Start()
    {
        if (scoreText != null)
        {
            scoreText.GetComponent<Text>().text = GameManager.totalScore.ToString();
        }
    }
}
