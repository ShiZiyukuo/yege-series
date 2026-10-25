using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 教程 9.4.7「设定360°的虚拟面板」（沿用第 7.7 节的 VirtualPad）。
/// 本次允许上下左右移动，因此 is4DPad 为 true。
///
/// 脚本挂在「底盘（VirtualPadBase）」而不是手柄上：整个 250×250 的底盘都能接收触摸，
/// 手指按在底盘任意位置就能推动，不必精确命中 120×120 的手柄 —— 手机上手感差别很大。
/// 手柄（tab）只负责把当前方向显示出来。
/// </summary>
public class VirtualPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform tab;              // 手柄（子对象），仅用于显示
    public float maxLength = 120.0f;       // 手柄可移动的最大半径（Canvas 单位）
    public bool is4DPad = true;            // 是否允许上下方向

    GameObject player;
    RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        player = GameObject.FindGameObjectWithTag("Player");
        if (tab != null)
        {
            tab.anchoredPosition = Vector2.zero;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveTab(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveTab(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (tab != null)
        {
            tab.anchoredPosition = Vector2.zero;
        }
        SetPlayerAxis(0.0f, 0.0f);
    }

    void MoveTab(PointerEventData eventData)
    {
        if (rect == null)
        {
            return;
        }

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, eventData.position, eventData.pressEventCamera, out local))
        {
            return;
        }

        if (is4DPad == false)
        {
            local.y = 0.0f;
        }
        if (local.magnitude > maxLength)
        {
            local = local.normalized * maxLength;
        }
        if (tab != null)
        {
            tab.anchoredPosition = local;
        }

        Vector2 axis = local.magnitude > 0.001f ? local.normalized : Vector2.zero;
        SetPlayerAxis(axis.x, axis.y);
    }

    /// <summary>把方向传给玩家角色（场景切换后会换成新的玩家角色，因此需要重新寻找）。</summary>
    void SetPlayerAxis(float h, float v)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        if (player == null)
        {
            return;
        }
        PlayerController plcnt = player.GetComponent<PlayerController>();
        if (plcnt != null)
        {
            plcnt.SetAxis(h, v);
        }
    }

    /// <summary>攻击按钮的转接（教程 9.4.8），转发给玩家角色上的射箭脚本。</summary>
    public void Attack()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        if (player == null)
        {
            return;
        }
        ArrowShoot shoot = player.GetComponent<ArrowShoot>();
        if (shoot != null)
        {
            shoot.Attack();
        }
    }
}
