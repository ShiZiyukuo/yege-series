using UnityEngine;

/// <summary>
/// 摄像机：跟随玩家（X 轴带边界钳制）+ Boss 战取景（拉远看全局）+ 受击屏震。
/// 屏震偏移从 YG3Fx 取，保证与顿帧共用同一时间源。
/// </summary>
public class YG3CameraManager : MonoBehaviour
{
    public Transform target;
    public float minX = -1000f;
    public float maxX = 1000f;
    public float baseSize = 6f;
    public float bossSize = 9f;

    YG3JinZuWuBoss boss;
    Camera cam;
    float baseY;

    void Awake()
    {
        cam = GetComponent<Camera>();
        baseY = transform.position.y;
    }

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                target = p.transform;
            }
        }
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;

        // Boss 存活时取景两者中点并拉远
        bool bossFrame = false;
        if (boss == null)
        {
            boss = FindObjectOfType<YG3JinZuWuBoss>();
        }
        if (boss != null && boss.IsActive && target != null)
        {
            float mid = (boss.transform.position.x + target.position.x) / 2f;
            pos.x = Mathf.Clamp(mid, minX, maxX);
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, bossSize, Time.deltaTime * 2.5f);
            }
            bossFrame = true;
        }
        else if (target != null)
        {
            pos.x = Mathf.Clamp(target.position.x, minX, maxX);
        }

        if (cam != null && !bossFrame)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, baseSize, Time.deltaTime * 2.5f);
        }

        pos += YG3Fx.ShakeOffset();
        transform.position = pos;
    }
}
