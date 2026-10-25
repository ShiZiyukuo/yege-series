using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 触屏跳跃按钮（教程第7.7节 支持触摸屏操作）
/// </summary>
public class MobileJumpButton : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }
        PlayerController plcnt = player.GetComponent<PlayerController>();
        if (plcnt != null)
        {
            plcnt.Jump();
        }
    }
}
