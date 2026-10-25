using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 演出服务：顿帧（timeScale 短时归零）、屏震、火花粒子、弹反光环。
/// 全部走 unscaledDeltaTime，不影响固定步长物理的正确性。
/// </summary>
public static class YG3Fx
{
    static float hitstopUntil;      // Time.unscaledTime
    static float shakeAmp;
    static GameObject sparkPrefab;
    static GameObject parryPrefab;
    static readonly List<YG3PooledFx> live = new List<YG3PooledFx>();

    /// <summary>顿帧：命中时 timeScale 压到 0.05，真实时间到达后恢复。</summary>
    public static void Hitstop(float realSeconds)
    {
        if (realSeconds <= 0f)
        {
            return;
        }
        float until = Time.unscaledTime + realSeconds;
        if (until > hitstopUntil)
        {
            hitstopUntil = until;
        }
    }

    public static bool InHitstop()
    {
        return Time.unscaledTime < hitstopUntil;
    }

    /// <summary>每帧由 GameManager 驱动（唯一入口，避免多对象抢 timeScale）。</summary>
    public static void Tick()
    {
        if (InHitstop())
        {
            Time.timeScale = 0.05f;
        }
        else if (Time.timeScale < 1f)
        {
            Time.timeScale = 1f;
        }

        // 回收过期特效
        for (int i = live.Count - 1; i >= 0; i--)
        {
            if (live[i] == null)
            {
                live.RemoveAt(i);
            }
            else if (live[i].TickLife(Time.unscaledDeltaTime))
            {
                Object.Destroy(live[i].gameObject);
                live.RemoveAt(i);
            }
        }
    }

    public static void Shake(float amplitude)
    {
        shakeAmp = Mathf.Max(shakeAmp, amplitude);
    }

    /// <summary>由 CameraManager.LateUpdate 调：取本轮抖动偏移并衰减。</summary>
    public static Vector3 ShakeOffset()
    {
        if (shakeAmp <= 0f)
        {
            return Vector3.zero;
        }
        shakeAmp = Mathf.Max(0f, shakeAmp - Time.unscaledDeltaTime * 1.4f);
        float a = shakeAmp * shakeAmp * 10f;
        return new Vector3(
            (Mathf.PerlinNoise(Time.unscaledTime * 40f, 0f) - 0.5f) * a,
            (Mathf.PerlinNoise(0f, Time.unscaledTime * 40f) - 0.5f) * a,
            0f);
    }

    public static void BindPrefabs(GameObject spark, GameObject parry)
    {
        sparkPrefab = spark;
        parryPrefab = parry;
    }

    /// <summary>命中火花。playerHit=true 用暖白，敌伤用暗红。</summary>
    public static void Spark(Vector3 pos, bool playerHit)
    {
        if (sparkPrefab == null)
        {
            return;
        }
        GameObject go = Object.Instantiate(sparkPrefab, pos, Quaternion.identity);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = playerHit ? new Color(1f, 0.95f, 0.7f, 1f) : new Color(0.9f, 0.3f, 0.2f, 1f);
        }
        go.transform.localScale = Vector3.one * (playerHit ? 0.9f : 1.2f);
        Track(go, 0.35f, 1.6f);
    }

    /// <summary>弹反成功光环。</summary>
    public static void ParryRing(Vector3 pos)
    {
        if (parryPrefab == null)
        {
            return;
        }
        GameObject go = Object.Instantiate(parryPrefab, pos, Quaternion.identity);
        Track(go, 0.4f, 1.4f);
    }

    static void Track(GameObject go, float life, float scaleTo)
    {
        YG3PooledFx fx = go.AddComponent<YG3PooledFx>();
        fx.life = life;
        fx.scaleTo = scaleTo;
        live.Add(fx);
    }

    /// <summary>给任意临时特效挂生命周期（缩放渐隐后销毁）。</summary>
    public static void Life(GameObject go, float life, float scaleTo)
    {
        if (go != null)
        {
            Track(go, life, scaleTo);
        }
    }
}

/// <summary>短命特效：计时缩放渐隐后由 YG3Fx 统一销毁。</summary>
public class YG3PooledFx : MonoBehaviour
{
    public float life = 0.35f;
    public float scaleTo = 1.4f;
    float age;
    SpriteRenderer sr;
    Vector3 baseScale;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    /// <summary>返回 true 表示生命周期结束。</summary>
    public bool TickLife(float dt)
    {
        age += dt;
        float t = Mathf.Clamp01(age / life);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;
        }
        transform.localScale = Vector3.Lerp(baseScale, baseScale * scaleTo, t);
        return age >= life;
    }
}
