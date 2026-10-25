using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换（Button.onClick 绑定，避免 YeGe 旧版 IPointerClickHandler 绕过 interactable 的问题）。
/// </summary>
public class YG3ChangeScene : MonoBehaviour
{
    public string sceneName = YG3SceneNames.Title;

    public void Load()
    {
        YG3SoundManager.SE(YG3Tuning.SE_BUTTON);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void LoadNext()
    {
        YG3SoundManager.SE(YG3Tuning.SE_BUTTON);
        Time.timeScale = 1f;
        SceneManager.LoadScene(YG3GameManager.NextOf(SceneManager.GetActiveScene().name));
    }
}
