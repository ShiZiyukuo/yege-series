using UnityEngine;

/// <summary>刘海屏安全区适配（沿用 YeGe 实现，挂 Canvas 根节点）。</summary>
[RequireComponent(typeof(RectTransform))]
public class YG3SafeAreaFitter : MonoBehaviour
{
    RectTransform rt;
    Vector2 lastMin, lastMax;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea.position != lastMin || Screen.safeArea.size != lastMax)
        {
            Apply();
        }
    }

    void Apply()
    {
        Rect area = Screen.safeArea;
        lastMin = area.position;
        lastMax = area.size;
        Rect pixelRect = rt.rect;

        Vector2 anchorMin = new Vector2(area.x / Screen.width, area.y / Screen.height);
        Vector2 anchorMax = new Vector2((area.x + area.width) / Screen.width, (area.y + area.height) / Screen.height);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
