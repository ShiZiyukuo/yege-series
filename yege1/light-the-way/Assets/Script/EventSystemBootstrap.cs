using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// 运行时兜底：场景里缺少 EventSystem 时自动补一个。
/// EventSystem 是 uGUI 所有点击/拖拽事件的分发入口，缺失会导致
/// Button、VirtualPad、MobileJumpButton、ChangeScene 全部失效（键盘不受影响）。
/// 本类只在「场景加载完成时」执行一次判空，不参与 Update，无每帧开销。
/// 一旦 YeGEBuilder 生成的场景自带 EventSystem，这里会自动跳过，不会重复创建。
/// </summary>
public static class EventSystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Ensure();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure();
    }

    static void Ensure()
    {
        // 已销毁的 Unity 对象用 == null 判断同样成立，因此这里可以安全复用静态引用
        if (EventSystem.current != null)
        {
            return;
        }
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
