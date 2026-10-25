using UnityEngine;

/// <summary>箭矢：直线飞行，命中敌人/Boss 造成伤害与架势伤害，出界或超时回收。</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class YG3Arrow : MonoBehaviour
{
    public GameObject poolSource;    // 用于归还对象池

    Rigidbody2D rbody;
    float life;

    void Awake()
    {
        rbody = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 velocity, GameObject owner)
    {
        rbody.velocity = velocity;
        rbody.gravityScale = 0f;
        life = YG3Tuning.ARROW_LIFE;
        GetComponent<SpriteRenderer>().flipX = velocity.x < 0f;
        gameObject.layer = YG3Tuning.LAYER_PLAYER_ATTACK;
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
        if (other.CompareTag("Player"))
        {
            return;     // 不误伤发射者
        }
        if (other.CompareTag("Ground") || other.CompareTag("Wall") || other.CompareTag("Goal"))
        {
            Despawn();
            return;
        }
        IYG3Damageable target = other.GetComponentInParent<IYG3Damageable>();
        if (target == null)
        {
            return;
        }
        Vector2 dir = rbody.velocity.normalized;
        bool hit = target.TakeDamage(YG3DamageInfo.Make(
            YG3Tuning.ARROW_DAMAGE, YG3Tuning.ARROW_POISE,
            dir * 1.5f, YG3DamageType.PlayerArrow, gameObject, false, YG3Tuning.HITSTOP_LIGHT * 0.6f));
        if (hit)
        {
            YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 0.4f, 1.4f);
        }
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
