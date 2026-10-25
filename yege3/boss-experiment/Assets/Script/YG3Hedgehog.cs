using UnityEngine;

/// <summary>
/// 刺猬小怪：巡逻 → 追击 → 蜷缩滚刺（接触伤害、近战反伤）→ 死亡。
/// 被箭矢/挥剑击杀；滚刺状态被挥剑命中会反伤攻击者；被弹反直接击杀。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class YG3Hedgehog : MonoBehaviour, IYG3Damageable
{
    public enum State { Patrol, Chase, CurlRoll, Dead }

    [Header("参数")]
    public float speed = YG3Tuning.HEDGEHOG_SPEED;
    public float chaseSpeed = YG3Tuning.HEDGEHOG_CHASE_SPEED;
    public float reaction = YG3Tuning.HEDGEHOG_REACTION;
    public float patrolRange = 3f;

    [Header("动画状态名")]
    public string walkAnime = "HedgehogWalk";
    public string curlAnime = "HedgehogCurl";
    public string deadAnime = "HedgehogDead";

    [HideInInspector] public State state = State.Patrol;

    Rigidbody2D rbody;
    Animator animator;
    SpriteRenderer spriteRenderer;
    YG3Health health;
    Vector3 home;
    int dir = -1;
    float rollTimer;
    float rollCd;
    float touchCd;
    float spin;

    public bool IsSpiky { get { return state == State.CurlRoll; } }
    public bool IsDead { get { return state == State.Dead; } }

    public static int AliveCount;

    void Awake()
    {
        rbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<YG3Health>();
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
        AliveCount++;
    }

    void Start()
    {
        home = transform.position;
        if (animator != null)
        {
            animator.Play(walkAnime);
        }
    }

    void OnDestroy()
    {
        AliveCount--;
    }

    void Update()
    {
        if (IsDead || YG3GameManager.State != YG3GameManager.GameState.Playing)
        {
            return;
        }
        YG3PlayerController player = YG3PlayerController.Find();
        if (player == null || player.IsDead)
        {
            Walk(rbody.velocity.x >= 0f ? 1 : -1, speed * 0.6f);
            return;
        }

        touchCd -= Time.deltaTime;
        rollCd -= Time.deltaTime;
        float dist = Vector2.Distance(transform.position, player.transform.position);

        switch (state)
        {
            case State.Patrol:
                // 巡逻（在家的两侧范围折返）
                if (transform.position.x > home.x + patrolRange)
                {
                    dir = -1;
                }
                else if (transform.position.x < home.x - patrolRange)
                {
                    dir = 1;
                }
                Walk(dir, speed);
                if (dist <= reaction)
                {
                    state = State.Chase;
                }
                break;

            case State.Chase:
                dir = player.transform.position.x > transform.position.x ? 1 : -1;
                Walk(dir, chaseSpeed);
                if (dist <= YG3Tuning.HEDGEHOG_CURL_RANGE && rollCd <= 0f)
                {
                    StartRoll(player);
                }
                else if (dist > reaction * 1.6f)
                {
                    state = State.Patrol;
                }
                break;

            case State.CurlRoll:
                rollTimer -= Time.deltaTime;
                spriteRenderer.transform.Rotate(0f, 0f, -dir * 520f * Time.deltaTime);   // 刺球旋转
                rbody.velocity = new Vector2(dir * YG3Tuning.HEDGEHOG_ROLL_SPEED, rbody.velocity.y);
                if (rollTimer <= 0f)
                {
                    state = State.Chase;
                    rollCd = 1.2f;
                    if (animator != null)
                    {
                        animator.Play(walkAnime);
                    }
                }
                break;
        }
    }

    void Walk(int direction, float v)
    {
        dir = direction;
        rbody.velocity = new Vector2(v * dir, rbody.velocity.y);
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = dir > 0;   // 素材默认朝左
        }
    }

    void StartRoll(YG3PlayerController player)
    {
        state = State.CurlRoll;
        rollTimer = YG3Tuning.HEDGEHOG_ROLL_TIME;
        dir = player.transform.position.x > transform.position.x ? 1 : -1;
        if (animator != null)
        {
            animator.Play(curlAnime);
        }
        YG3SoundManager.SE(YG3Tuning.SE_SHOOT, 0.35f, 0.7f);
    }

    // ---------------- 伤害 ----------------

    public bool TakeDamage(YG3DamageInfo info)
    {
        if (IsDead)
        {
            return false;
        }
        if (state == State.CurlRoll && info.type == YG3DamageType.PlayerSword)
        {
            // 滚刺时近战打不动，会弹开（由 YG3SwordAttack 调 ReflectSword）
            return false;
        }
        bool applied = health.TakeDamage(info);
        if (applied && !health.IsDead && state != State.CurlRoll)
        {
            state = State.Chase;    // 受击后立即进入追击
        }
        return applied;
    }

    /// <summary>挥剑打在滚刺刺球上：反伤攻击者。</summary>
    public void ReflectSword(YG3PlayerController player)
    {
        YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 0.6f, 1.8f);
        YG3Fx.Spark(transform.position + Vector3.up * 0.5f, false);
        player.GetComponent<YG3Health>().TakeDamage(YG3DamageInfo.Make(
            1f, 10f,
            new Vector2(Mathf.Sign(player.transform.position.x - transform.position.x) * 4f, 3f),
            YG3DamageType.Light, gameObject, false, YG3Tuning.HITSTOP_LIGHT));
    }

    /// <summary>被玩家完美弹反（SendMessage）→ 直接击杀。</summary>
    public void OnParried()
    {
        if (!IsDead)
        {
            health.TakeDamage(YG3DamageInfo.Make(999f, 0f, Vector2.zero, YG3DamageType.PlayerSword, null, false, 0f));
        }
    }

    /// <summary>接触玩家（由 YG3PlayerController.OnCollisionStay2D 调）。</summary>
    public void TouchPlayer(YG3PlayerController player)
    {
        if (IsDead || touchCd > 0f)
        {
            return;
        }
        touchCd = 0.8f;
        float poise = IsSpiky ? YG3Tuning.HEDGEHOG_CONTACT_POISE : 10f;
        player.GetComponent<YG3Health>().TakeDamage(YG3DamageInfo.Make(
            YG3Tuning.HEDGEHOG_CONTACT_DMG, poise,
            new Vector2(Mathf.Sign(player.transform.position.x - transform.position.x) * 4f, 3f),
            YG3DamageType.Light, gameObject, true, 0f));
    }

    void OnDamaged(YG3DamageInfo info)
    {
        rbody.AddForce(info.knockback, ForceMode2D.Impulse);
    }

    void OnDied()
    {
        state = State.Dead;
        rbody.velocity = Vector2.zero;
        if (animator != null)
        {
            animator.Play(deadAnime);
        }
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            c.enabled = false;
        }
        YG3GameManager.Instance.AddScore((int)YG3Tuning.HEDGEHOG_SCORE);
        YG3SoundManager.SE(YG3Tuning.SE_KILLED, 0.8f);
        Destroy(gameObject, 0.8f);
    }
}
