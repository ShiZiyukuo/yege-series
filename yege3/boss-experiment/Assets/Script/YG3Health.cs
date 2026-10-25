using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 统一生命组件：HP + 架势(Poise) + 受击无敌帧 + 白闪 + 架势破硬直。
/// 玩家通过 incomingFilter 挂入格挡/弹反/翻滚判定（YG3PlayerController）。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class YG3Health : MonoBehaviour, IYG3Damageable
{
    [Header("生命")]
    public float maxHp = 3f;
    public float hp;

    [Header("架势（破则硬直）")]
    public float poiseMax = 60f;
    public float poiseRegen = 30f;
    public float stunTime = 0.4f;

    [Header("受击无敌")]
    public float invulnTime = 0.8f;

    /// <summary>玩家用它改写伤害（格挡/弹反）。返回 null 表示完全抵消。</summary>
    public Func<YG3DamageInfo, YG3DamageInfo?> incomingFilter;

    /// <summary>真正受伤（可用于击退、硬直、音效）。</summary>
    public event Action<YG3DamageInfo> Damaged;
    /// <summary>架势破（被打出硬直）。</summary>
    public event Action<float> PoiseBroken;
    /// <summary>每次带架势伤害的攻击命中（Boss 用来累积破防槽）。</summary>
    public event Action<float> PoiseHit;
    /// <summary>死亡。</summary>
    public event Action Died;

    public bool IsDead { get { return hp <= 0f; } }
    public bool IsInvuln { get { return invulnTimer > 0f; } }

    float invulnTimer;
    float poise;
    float poiseDelay;
    SpriteRenderer spriteRenderer;
    Coroutine flashCo;
    Color baseColor = Color.white;

    void Awake()
    {
        hp = maxHp;
        poise = poiseMax;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        if (invulnTimer > 0f)
        {
            invulnTimer -= Time.deltaTime;
            // 无敌帧闪烁
            if (spriteRenderer != null && !IsDead)
            {
                bool on = (invulnTimer * 14f) % 2f > 1f;
                spriteRenderer.enabled = !on;
            }
            if (invulnTimer <= 0f && spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
        }

        // 架势回复（受击后延迟）
        if (poiseDelay > 0f)
        {
            poiseDelay -= Time.deltaTime;
        }
        else if (poise < poiseMax)
        {
            poise = Mathf.Min(poiseMax, poise + poiseRegen * Time.deltaTime);
        }
    }

    public bool TakeDamage(YG3DamageInfo info)
    {
        if (IsDead)
        {
            return false;
        }
        // 让玩家组件先判定格挡/弹反/翻滚，可能抵消或改写
        if (incomingFilter != null)
        {
            YG3DamageInfo? filtered = incomingFilter(info);
            if (filtered == null)
            {
                return false;   // 被弹反/格挡完全化解
            }
            info = filtered.Value;
        }

        if (invulnTimer > 0f)
        {
            return false;
        }

        float taken = info.damage;
        // 破防（Groggy）期间受伤加成由调用方（Boss 自己）处理；这里只做通用流程
        hp = Mathf.Max(0f, hp - taken);
        invulnTimer = invulnTime;
        poiseDelay = 1f;

        // 白闪
        if (spriteRenderer != null)
        {
            if (flashCo != null)
            {
                StopCoroutine(flashCo);
            }
            flashCo = StartCoroutine(FlashWhite());
        }

        if (info.hitstop > 0f)
        {
            YG3Fx.Hitstop(info.hitstop);
        }
        YG3Fx.Spark(transform.position, info.type == YG3DamageType.PlayerSword || info.type == YG3DamageType.PlayerArrow);

        Damaged.SafeInvoke(info);

        // 架势：PoiseHit 对所有单位触发（Boss 累积破防槽）；PoiseBroken 仅 poiseMax>0 的单位（玩家）
        if (info.poise > 0f)
        {
            PoiseHit.SafeInvoke(info.poise);
        }
        if (info.poise > 0f && poiseMax > 0f)
        {
            poise -= info.poise;
            if (poise <= 0f)
            {
                poise = poiseMax;   // 破后重置，等待下次累积
                PoiseBroken.SafeInvoke(stunTime);
            }
        }

        if (hp <= 0f)
        {
            invulnTimer = 0f;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
            Died.SafeInvoke();
        }
        return true;
    }

    /// <summary>回满（重开关卡用）。</summary>
    public void ResetAll()
    {
        hp = maxHp;
        poise = poiseMax;
        invulnTimer = 0f;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = baseColor;
        }
    }

    /// <summary>Groggy 期间由 Boss 自行放大伤害：直接扣血入口。</summary>
    public void RawDamage(float amount)
    {
        hp = Mathf.Max(0f, hp - amount);
        if (hp <= 0f && !IsDead)
        {
            Died.SafeInvoke();
        }
    }

    IEnumerator FlashWhite()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }
        spriteRenderer.color = new Color(2.5f, 2.5f, 2.5f, 1f);
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            spriteRenderer.color = Color.Lerp(new Color(2.5f, 2.5f, 2.5f, 1f), baseColor, t / 0.12f);
            yield return null;
        }
        spriteRenderer.color = baseColor;
    }
}

/// <summary>空事件安全调用的小工具。</summary>
public static class YG3EventExt
{
    public static void SafeInvoke(this Action a)
    {
        if (a != null)
        {
            a();
        }
    }
    public static void SafeInvoke<A>(this Action<A> a, A arg)
    {
        if (a != null)
        {
            a(arg);
        }
    }
}
