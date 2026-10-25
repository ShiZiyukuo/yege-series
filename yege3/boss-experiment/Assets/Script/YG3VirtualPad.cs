using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 触屏虚拟摇杆（沿用 YeGe 的 2D 摇杆素材与交互习惯）。
/// 静态属性供 PlayerController / BowAttack 读取。
/// </summary>
public class YG3VirtualPad : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public static float AxisH;
    public static bool AimUp;

    public RectTransform thumb;      // 摇杆头
    public float maxRadius = 60f;

    Vector2 origin;

    void Awake()
    {
        origin = thumb != null ? thumb.anchoredPosition : Vector2.zero;
    }

    public void OnDrag(PointerEventData data)
    {
        Vector2 d = data.delta * (Screen.width < 800 ? 1.4f : 1f);
        Vector2 pos = thumb.anchoredPosition + d;
        if (pos.magnitude > maxRadius)
        {
            pos = pos.normalized * maxRadius;
        }
        thumb.anchoredPosition = pos;
        AxisH = pos.x / maxRadius;
        AimUp = pos.y > maxRadius * 0.45f;
    }

    public void OnEndDrag(PointerEventData data)
    {
        Reset();
    }

    void OnDisable()
    {
        Reset();
    }

    void Reset()
    {
        if (thumb != null)
        {
            thumb.anchoredPosition = origin;
        }
        AxisH = 0f;
        AimUp = false;
    }
}
