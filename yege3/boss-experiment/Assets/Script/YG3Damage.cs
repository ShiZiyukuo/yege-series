using UnityEngine;

/// <summary>伤害类型：决定格挡/霸体/演出差异。</summary>
public enum YG3DamageType
{
    Light,        // 普通攻击：金针、刺猬接触
    Heavy,        // 重攻击：震波（必破架势）
    Unblockable,  // 不可格挡
    PlayerSword,  // 玩家挥剑
    PlayerArrow,  // 玩家箭矢
}

/// <summary>统一伤害包：所有敌我攻击都通过它传递。</summary>
public struct YG3DamageInfo
{
    public float damage;
    public float poise;          // 架势/霸体伤害
    public Vector2 knockback;
    public YG3DamageType type;
    public bool parryable;       // 能否被格挡或弹反
    public GameObject source;    // 攻击来源（弹反时回调 OnParried）
    public float hitstop;

    public static YG3DamageInfo Make(float dmg, float poise, Vector2 knock,
        YG3DamageType type, GameObject source, bool parryable, float hitstop)
    {
        YG3DamageInfo info = new YG3DamageInfo();
        info.damage = dmg;
        info.poise = poise;
        info.knockback = knock;
        info.type = type;
        info.source = source;
        info.parryable = parryable;
        info.hitstop = hitstop;
        return info;
    }
}

/// <summary>可受伤契约：玩家 / 刺猬 / Boss 共用。</summary>
public interface IYG3Damageable
{
    /// <summary>造成伤害。返回是否真正命中（无敌/格挡豁免时为 false）。</summary>
    bool TakeDamage(YG3DamageInfo info);
}
