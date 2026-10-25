using UnityEngine;

/// <summary>
/// 摄像机控制（教程第6.3节 制作滚动画面）
/// 跟随玩家横向滚动，并限制在关卡范围内；也支持强制滚动。
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("滚动范围")]
    public float leftLimit = 0.0f;
    public float rightLimit = 0.0f;
    public float topLimit = 0.0f;
    public float bottomLimit = 0.0f;

    [Header("强制滚动")]
    public bool isForceScrollX = false;
    public float forceScrollSpeedX = 0.5f;
    public bool isForceScrollY = false;
    public float forceScrollSpeedY = 0.5f;

    [Header("背景（跟随摄像机，按屏幕比例放大铺满）")]
    public GameObject subScreen;

    GameObject player;
    Vector3 lastCameraPos;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        lastCameraPos = transform.position;
        FitBackground();
    }

    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }
        }

        float x = player.transform.position.x;
        float y = player.transform.position.y;
        float z = transform.position.z;

        if (isForceScrollX)
        {
            x = transform.position.x + (forceScrollSpeedX * Time.deltaTime);
        }
        if (x < leftLimit)
        {
            x = leftLimit;
        }
        else if (x > rightLimit)
        {
            x = rightLimit;
        }
        if (isForceScrollY)
        {
            y = transform.position.y + (forceScrollSpeedY * Time.deltaTime);
        }
        if (y < bottomLimit)
        {
            y = bottomLimit;
        }
        else if (y > topLimit)
        {
            y = topLimit;
        }

        transform.position = new Vector3(x, y, z);

        // 背景始终跟随摄像机（作为摄像机子物体），保证任何屏幕比例都不露边
        if (subScreen != null)
        {
            Vector3 sub = subScreen.transform.localPosition;
            subScreen.transform.localPosition = new Vector3(0, sub.y, sub.z);
        }
    }

    /// <summary>把背景按屏幕宽高比放大，保证任何比例（含红米K70 Pro 的 20:9）都不露边</summary>
    public void FitBackground()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null || subScreen == null)
        {
            return;
        }
        SpriteRenderer sr = subScreen.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            return;
        }
        float viewH = cam.orthographicSize * 2.0f;
        float viewW = viewH * cam.aspect;
        Vector2 size = sr.sprite.bounds.size;
        float scale = Mathf.Max(viewW / size.x, viewH / size.y) * 1.02f;
        subScreen.transform.localScale = new Vector3(scale, scale, 1);
    }
}
