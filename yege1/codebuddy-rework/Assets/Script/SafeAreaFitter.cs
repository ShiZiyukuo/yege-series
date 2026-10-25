using UnityEngine;

/// <summary>
/// 安全区适配：把 UI 容器收缩到屏幕安全区内，
/// 避免红米 K70 Pro 挖孔屏/手势条遮挡摇杆与按钮。
/// </summary>
[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rectTransform;
    Rect lastSafeArea;
    Vector2Int lastScreenSize;

    void Awake()
    {
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            Apply();
        }
    }

    void Apply()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        if (rectTransform == null)
        {
            return;
        }
        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        Rect safe = Screen.safeArea;
        float w = Screen.width;
        float h = Screen.height;
        if (w <= 0 || h <= 0)
        {
            return;
        }
        Vector2 anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
        Vector2 anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
