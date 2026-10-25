using UnityEngine;

/// <summary>
/// 计时器（教程第6.4节 实现计时功能）
/// 默认倒计时，剩余时间为 0 时游戏失败。
/// </summary>
public class TimeController : MonoBehaviour
{
    public bool isCountDown = true;   // true：倒计时
    public float gameTime = 0.0f;     // 限时（0 表示不显示时间条）
    public bool isTimeOver = false;   // 是否已停止计时
    public float displayTime = 0.0f;  // 当前显示的时间

    float times = 0.0f;

    void Start()
    {
        if (isCountDown)
        {
            displayTime = gameTime;
        }
    }

    void Update()
    {
        if (isTimeOver)
        {
            return;
        }
        times += Time.deltaTime;
        if (isCountDown)
        {
            displayTime = gameTime - times;
            if (displayTime <= 0.0f)
            {
                displayTime = 0.0f;
                isTimeOver = true;
            }
        }
        else
        {
            displayTime = times;
            if (displayTime >= gameTime)
            {
                displayTime = gameTime;
                isTimeOver = true;
            }
        }
    }
}
