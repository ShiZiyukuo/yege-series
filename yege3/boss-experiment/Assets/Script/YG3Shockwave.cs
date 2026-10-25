using UnityEngine;

/// <summary>
/// 震波：Boss 砸地后沿地面扩散的金色冲击波。不可格挡（Unblockable），必须跳跃躲避。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class YG3Shockwave : MonoBehaviour
{
    public GameObject poolSource;

    Rigidbody2D rbody;
    float life;
    bool hitPlayer;

    public void Launch(int dir, Vector3 groundPos)
    {
        rbody = rbody != null ? rbody : GetComponent<Rigidbody2D>();
        rbody.gravityScale = 0f;
        rbody.velocity = new Vector2(YG3Tuning.SHOCK_SPEED * dir, 0f);
        transform.position = groundPos;
        life = YG3Tuning.SHOCK_LIFE;
        hitPlayer = false;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = dir < 0;
        }
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
        if (hitPlayer || !other.CompareTag("Player"))
        {
            return;
        }
        YG3PlayerController player = other.GetComponent<YG3PlayerController>();
        if (player == null || player.IsDead)
        {
            return;
        }
        hitPlayer = true;
        // 命中点在玩家脚下才算踩到（跳跃中穿过顶部不判定）
        Collider2D pc = other.GetComponent<Collider2D>();
        bool groundedHit = pc != null && pc.bounds.min.y < transform.position.y + 0.35f;
        if (groundedHit)
        {
            player.GetComponent<YG3Health>().TakeDamage(YG3DamageInfo.Make(
                YG3Tuning.SHOCK_DAMAGE, YG3Tuning.SHOCK_POISE,
                new Vector2(Mathf.Sign(player.transform.position.x - transform.position.x) * 5f, 6f),
                YG3DamageType.Unblockable, gameObject, false, YG3Tuning.HITSTOP_LIGHT * 2f));
        }
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
