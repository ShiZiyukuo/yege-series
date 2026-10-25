using UnityEngine;

/// <summary>
/// 玩家控制：移动 / 跳跃 / 翻滚（纯位移无无敌帧）/ 格挡与弹反（L）/ 架势与耐力 / 受击硬直 / 动画。
/// 挥剑与射箭由 YG3SwordAttack、YG3BowAttack 分别负责，通过本组件协调状态。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class YG3PlayerController : MonoBehaviour
{
    public enum State { Normal, Attack, Guard, Roll, Stun, Dead }

    [Header("动画状态名")]
    public string stopAnime = "PlayerStop";
    public string moveAnime = "PlayerMove";
    public string jumpAnime = "PlayerJump";
    public string deadAnime = "PlayerDead";

    [HideInInspector] public Rigidbody2D rbody;
    [HideInInspector] public Animator animator;
    SpriteRenderer spriteRenderer;
    CapsuleCollider2D capsule;

    [HideInInspector] public State state = State.Normal;
    [HideInInspector] public float facing = 1f;      // 1 右 / -1 左
    [HideInInspector] public float axisH;

    // 由攻击组件设置 >0 表示动作锁定中（不可移动/格挡/翻滚）
    [HideInInspector] public int attackLock;

    // 格挡
    float guardStartTime;
    float stamina = YG3Tuning.STAMINA_MAX;
    float staminaIdle;

    // 翻滚
    float rollTimer;
    float rollDir;

    // 受击
    float stunTimer;
    float slowTimer;
    Vector2 lastKnock;
    string nowAnime = "";
    string oldAnime = "";

    public bool IsDead { get { return state == State.Dead; } }
    public bool InParryWindow { get { return state == State.Guard && Time.time - guardStartTime <= YG3Tuning.PARRY_WINDOW; } }
    public bool IsGuarding { get { return state == State.Guard; } }
    public float StaminaRatio { get { return stamina / YG3Tuning.STAMINA_MAX; } }

    /// <summary>找玩家（避免各 AI 每处重复写 FindGameObjectWithTag）。</summary>
    public static YG3PlayerController Find()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.GetComponent<YG3PlayerController>() : null;
    }

    void Awake()
    {
        rbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsule = GetComponent<CapsuleCollider2D>();
    }

    void Start()
    {
        nowAnime = oldAnime = stopAnime;
        if (YG3GameManager.Instance != null)
        {
            YG3GameManager.Instance.player = this;
        }
        YG3Health hp = GetComponent<YG3Health>();
        if (hp != null)
        {
            hp.incomingFilter = FilterDamage;
            hp.Damaged += OnDamaged;
            hp.PoiseBroken += OnPoiseBroken;
            hp.Died += Die;
        }
    }

    void Update()
    {
        if (YG3GameManager.State != YG3GameManager.GameState.Playing)
        {
            return;
        }
        if (IsDead)
        {
            return;
        }

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
        }

        // ---- 耐力：持盾持续消耗，其余时间延迟后回复 ----
        if (staminaIdle > 0f)
        {
            staminaIdle -= Time.deltaTime;
        }
        if (state == State.Guard)
        {
            stamina = Mathf.Max(0f, stamina - YG3Tuning.STAMINA_HOLD_PER_SEC * Time.deltaTime);
            if (stamina <= 0f)
            {
                GuardBreak();
            }
            else if (!Input.GetKey(KeyCode.L) && !YG3MobileInput.GuardHeld)
            {
                EndGuard();
            }
        }
        else if (staminaIdle <= 0f && stamina < YG3Tuning.STAMINA_MAX)
        {
            stamina = Mathf.Min(YG3Tuning.STAMINA_MAX, stamina + YG3Tuning.STAMINA_REGEN * Time.deltaTime);
        }

        ReadAxis();

        switch (state)
        {
            case State.Stun:
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                {
                    state = State.Normal;
                }
                break;

            case State.Roll:
                rollTimer -= Time.deltaTime;
                if (rollTimer <= 0f)
                {
                    state = State.Normal;
                }
                break;
        }

        // ---- 输入 ----
        if (state == State.Normal)
        {
            if (Input.GetButtonDown("Jump") || YG3MobileInput.ConsumeJump())
            {
                Jump();
            }
            else if (Input.GetKeyDown(KeyCode.J) || YG3MobileInput.ConsumeAttack())
            {
                GetComponent<YG3SwordAttack>().TryAttack();
            }
            else if (Input.GetKeyDown(KeyCode.K) || YG3MobileInput.ConsumeBow())
            {
                GetComponent<YG3BowAttack>().TryShoot();
            }
            else if (Input.GetKeyDown(KeyCode.LeftShift) || YG3MobileInput.ConsumeRoll())
            {
                TryRoll();
            }
            else if ((Input.GetKeyDown(KeyCode.L) || YG3MobileInput.ConsumeGuard()) && stamina > 0f)
            {
                StartGuard();
            }
        }
        // 攻击后摇期间允许直接翻滚取消（保证连段手感）
        else if (state == State.Attack && (Input.GetKeyDown(KeyCode.LeftShift) || YG3MobileInput.ConsumeRoll()))
        {
            GetComponent<YG3SwordAttack>().Cancel();
            TryRoll();
        }
    }

    void FixedUpdate()
    {
        if (IsDead)
        {
            return;
        }

        bool canMove = (state == State.Normal || state == State.Guard) && attackLock == 0;
        float speed = YG3Tuning.PLAYER_SPEED * (slowTimer > 0f ? 0.55f : 1f);
        if (state == State.Guard)
        {
            speed *= 0.35f;    // 持盾慢走
        }

        if (canMove && Mathf.Abs(axisH) > 0.01f)
        {
            rbody.velocity = new Vector2(speed * axisH, rbody.velocity.y);
        }
        else if (state == State.Roll)
        {
            rbody.velocity = new Vector2(YG3Tuning.ROLL_SPEED * rollDir, rbody.velocity.y);
        }
        else if (state == State.Stun || state == State.Attack)
        {
            // 硬直/攻击中保留击退惯性但不再主动移动
        }
        else if (canMove)
        {
            rbody.velocity = new Vector2(0f, rbody.velocity.y);
        }

        UpdateAnim();
    }

    void ReadAxis()
    {
        float pad = YG3VirtualPad.AxisH;
        if (Mathf.Abs(pad) > 0.01f)
        {
            axisH = pad;
        }
        else
        {
            axisH = Input.GetAxisRaw("Horizontal");
        }
        if (Mathf.Abs(axisH) > 0.01f && state != State.Roll)
        {
            facing = axisH > 0f ? 1f : -1f;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = facing < 0f;
            }
        }
    }

    void UpdateAnim()
    {
        if (state == State.Roll)
        {
            nowAnime = jumpAnime;   // 翻滚借跳跃姿态 + 轻微前倾
        }
        else if (state == State.Guard)
        {
            nowAnime = stopAnime;
        }
        else if (state == State.Attack)
        {
            nowAnime = stopAnime;   // 弧光特效负责打击感
        }
        else if (!IsGrounded())
        {
            nowAnime = jumpAnime;
        }
        else
        {
            nowAnime = Mathf.Abs(axisH) > 0.01f ? moveAnime : stopAnime;
        }
        if (nowAnime != oldAnime)
        {
            oldAnime = nowAnime;
            if (animator != null)
            {
                animator.Play(nowAnime);
            }
        }
        // 翻滚前倾
        if (spriteRenderer != null)
        {
            float targetTilt = state == State.Roll ? -rollDir * 18f : 0f;
            Vector3 e = transform.localEulerAngles;
            e.z = Mathf.LerpAngle(e.z, targetTilt, Time.deltaTime * 14f);
            transform.localEulerAngles = e;
        }
    }

    // ---------------- 动作 ----------------

    /// <summary>跳跃（键盘 Jump 轴 / 触屏跳跃键）。</summary>
    public void Jump()
    {
        if (state != State.Normal || !IsGrounded())
        {
            return;
        }
        rbody.velocity = new Vector2(rbody.velocity.x, YG3Tuning.PLAYER_JUMP);
    }

    public void TryRoll()
    {
        if (state != State.Normal)
        {
            return;
        }
        state = State.Roll;
        rollTimer = YG3Tuning.ROLL_TIME;
        rollDir = Mathf.Abs(axisH) > 0.01f ? Mathf.Sign(axisH) : facing;
        facing = rollDir;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facing < 0f;
        }
    }

    public void StartGuard()
    {
        state = State.Guard;
        guardStartTime = Time.time;
        staminaIdle = YG3Tuning.STAMINA_REGEN_DELAY;
        rbody.velocity = new Vector2(0f, rbody.velocity.y);
    }

    public void EndGuard()
    {
        if (state == State.Guard)
        {
            state = State.Normal;
        }
    }

    void GuardBreak()
    {
        stamina = YG3Tuning.STAMINA_MAX * 0.35f;    // 破防后回一点，避免连死
        EnterStun(YG3Tuning.GUARD_BREAK_STUN);
        YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 0.9f, 0.7f);
        YG3Fx.Shake(YG3Tuning.SHAKE_LIGHT);
    }

    public void EnterStun(float time)
    {
        state = State.Stun;
        stunTimer = time;
        GetComponent<YG3SwordAttack>().Cancel();
    }

    public void SetSlow(float time)
    {
        slowTimer = Mathf.Max(slowTimer, time);
    }

    // ---------------- 伤害接线 ----------------

    /// <summary>挂到 YG3Health.incomingFilter：处理弹反 / 格挡 / 翻滚豁免规则。</summary>
    public YG3DamageInfo? FilterDamage(YG3DamageInfo info)
    {
        if (state == State.Dead)
        {
            return info;
        }

        // 弹反窗口：完美格挡 → 无伤 + 反制攻击者
        if (InParryWindow && info.parryable)
        {
            YG3Fx.ParryRing(transform.position + Vector3.up * 0.9f);
            YG3Fx.Hitstop(YG3Tuning.HITSTOP_PARRY);
            YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 0.8f, 1.6f);
            if (info.source != null)
            {
                info.source.SendMessage("OnParried", SendMessageOptions.DontRequireReceiver);
            }
            return null;
        }

        // 普通格挡：仅正面、可格挡的攻击
        if (state == State.Guard && info.parryable && AttackFromFront(info.source))
        {
            stamina -= YG3Tuning.STAMINA_BLOCK_HIT;
            staminaIdle = YG3Tuning.STAMINA_REGEN_DELAY;
            info.damage *= YG3Tuning.BLOCK_DAMAGE_RATIO;
            info.poise *= 0.2f;
            info.knockback *= YG3Tuning.BLOCK_KNOCK_RATIO;
            info.hitstop = YG3Tuning.HITSTOP_LIGHT * 0.5f;
            YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 0.5f, 1.3f);
            YG3Fx.Spark(transform.position + Vector3.up * 0.9f + Vector3.right * facing * 0.5f, true);
            if (stamina <= 0f)
            {
                GuardBreak();
            }
            return info;
        }

        // 翻滚无无敌帧：照常受击
        return info;
    }

    bool AttackFromFront(GameObject source)
    {
        if (source == null)
        {
            return true;
        }
        Vector2 toSource = source.transform.position - transform.position;
        return Vector2.Dot(toSource, Vector2.right * facing) > -0.1f;
    }

    /// <summary>受伤事件（由 YG3Health.Damaged 调）：击退 + 硬直 + 迟缓。</summary>
    public void OnDamaged(YG3DamageInfo info)
    {
        lastKnock = info.knockback;
        rbody.AddForce(info.knockback, ForceMode2D.Impulse);
        YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 0.8f, 1f);
        YG3Fx.Shake(YG3Tuning.SHAKE_LIGHT);
        if (info.type == YG3DamageType.Heavy)
        {
            EnterStun(YG3Tuning.PLAYER_STUN * 1.5f);
        }
    }

    public void OnPoiseBroken(float stun)
    {
        if (!IsDead)
        {
            EnterStun(stun);
        }
    }

    public void Die()
    {
        if (IsDead)
        {
            return;
        }
        state = State.Dead;
        GetComponent<YG3SwordAttack>().Cancel();
        GetComponent<YG3BowAttack>().Cancel();
        if (animator != null)
        {
            animator.Play(deadAnime);
        }
        if (capsule != null)
        {
            capsule.enabled = false;
        }
        rbody.velocity = Vector2.zero;
        rbody.AddForce(new Vector2(0f, 5f), ForceMode2D.Impulse);
        YG3GameManager.Instance.NotifyPlayerDead();
    }

    public void RespawnReset()
    {
        state = State.Normal;
        stamina = YG3Tuning.STAMINA_MAX;
        stunTimer = 0f;
        attackLock = 0;
        if (capsule != null)
        {
            capsule.enabled = true;
        }
    }

    public bool IsGrounded()
    {
        if (capsule == null)
        {
            return false;
        }
        Bounds b = capsule.bounds;
        Vector2 from = new Vector2(b.center.x, b.min.y + 0.15f);
        Vector2 to = new Vector2(b.center.x, b.min.y - 0.20f);
        return Physics2D.Linecast(from, to, 1 << YG3Tuning.LAYER_GROUND);
    }

    // 碰撞拾取：道具 / 终点 / 坠落
    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead)
        {
            return;
        }
        if (other.CompareTag("ScoreItem"))
        {
            YG3GameManager.Instance.AddScore((int)YG3Tuning.HEDGEHOG_SCORE);
            YG3SoundManager.SE(YG3Tuning.SE_GET, 0.8f);
            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("DeadZone"))
        {
            GetComponent<YG3Health>().TakeDamage(YG3DamageInfo.Make(999f, 0f, Vector2.zero, YG3DamageType.Heavy, null, false, 0f));
        }
        else if (other.CompareTag("Goal"))
        {
            YG3GameManager.Instance.StageClear();
        }
    }

    // 接触伤害：刺猬滚刺 / Boss 身体
    void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead || YG3GameManager.State != YG3GameManager.GameState.Playing)
        {
            return;
        }
        YG3Hedgehog hog = collision.gameObject.GetComponent<YG3Hedgehog>();
        if (hog != null)
        {
            hog.TouchPlayer(this);
        }
    }
}
