using UnityEngine;

/// <summary>
/// 金针弹幕：Boss 齐射的弹体。可被格挡/弹反；
/// 被完美弹反时销毁自身并给 Boss 记架势伤害。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class YG3Needle : MonoBehaviour
{
    public GameObject poolSource;

    Rigidbody2D rbody;
    YG3JinZuWuBoss owner;
    float life;

    public void Launch(Vector2 velocity, YG3JinZuWuBoss fromBoss)
    {
        rbody = rbody != null ? rbody : GetComponent<Rigidbody2D>();
        rbody.velocity = velocity;
        rbody.gravityScale = 0f;
        owner = fromBoss;
        life = 3f;
        GetComponent<SpriteRenderer>().flipX = velocity.x < 0f;
        gameObject.layer = YG3Tuning.LAYER_BOSS_ATTACK;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Despawn();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Wall") || other.CompareTag("Goal"))
        {
            YG3Fx.Spark(transform.position, false);
            Despawn();
            return;
        }
        if (other.CompareTag("Player"))
        {
            YG3PlayerController pc = other.GetComponent<YG3PlayerController>();
            if (pc == null || pc.IsDead)
            {
                return;
            }
            Vector2 dir = GetComponent<Rigidbody2D>().velocity.normalized;
            bool hit = other.GetComponent<YG3Health>().TakeDamage(YG3DamageInfo.Make(
                YG3Tuning.VOLLEY_DAMAGE, YG3Tuning.VOLLEY_POISE,
                -dir * 2.5f, YG3DamageType.Light, gameObject, true, 0f));
            if (hit)
            {
                pc.SetSlow(YG3Tuning.VOLLEY_SLOW);      // 金针迟缓
                YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 0.6f, 1.1f);
            }
            Despawn();
        }
    }

    /// <summary>被完美弹反（SendMessage，source 是本弹体）。</summary>
    public void OnParried()
    {
        if (owner != null)
        {
            owner.AddPosture(YG3Tuning.BOSS_PARRY_POSTURE);
        }
        YG3Fx.Spark(transform.position, true);
        Despawn();
    }

    void Despawn()
    {
        if (poolSource != null)
        {
            YG3ObjectPool.Release(gameObject, poolSource);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
