using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景切换（教程第5.2节 实现游戏的重启 / 第6.2节 增加标题界面）
/// 挂在按钮图片上，点击后跳转到 sceneName 指定的场景。
/// </summary>
public class ChangeScene : MonoBehaviour, IPointerClickHandler
{
    public string sceneName = "";

    Button button;

    public void OnPointerClick(PointerEventData eventData)
    {
        // Button.interactable = false（置灰）只会拦截 Button 自己的 onClick，
        // 不会拦截本组件的 OnPointerClick，必须手动判断。
        // 缓存 Button 引用：一次 GetComponent，之后每次点击只做一次布尔判断。
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        if (button != null && !button.interactable)
        {
            return;
        }
        Load();
    }

    public void Load()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
