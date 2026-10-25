using System.Collections;
using UnityEngine;

/// <summary>
/// 金之祖巫（参考《造梦西游4》）：三招式状态机 + 三阶段提速 + 架势破防处决。
/// 招式：金针弹幕齐射（可格挡/弹反）、砸地震波（不可格挡，须跳跃）、召唤刺猬（封顶）。
/// 状态流：Dormant → Idle → Telegraph(读招) → Act → Recover(惩罚窗口) → Idle ...
/// 弹反/读招被打断 → 踉跄；架势满 → Groggy（可处决）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class YG3JinZuWuBoss : MonoBehaviour, IYG3Damageable
{
    public enum State { Dormant, Idle, Telegraph, Volley, Slam, Summon, Recover, Flinch, Groggy, Dead }

    [Header("动画状态名")]
    public string idleAnime = "BossIdle";
    public string chargeAnime = "BossCharge";
    public string volleyAnime = "BossVolley";
    public string slamAnime = "BossSlam";
    public string deadAnime = "BossDead";

    [Header("组件引用")]
    public GameObject needlePrefab;
    public GameObject shockwavePrefab;
    public GameObject hedgehogPrefab;
    public Transform gate;           // 金针发射口

    [HideInInspector] public State state = State.Dormant;
    YG3Health health;
    Rigidbody2D rbody;
    Animator animator;
    SpriteRenderer spriteRenderer;
    float stateTimer;
    int phase;                       // 0/1/2
    float posture;                   // 当前架势伤害累积
    float actTimer;                  // 出招间隔
    float summonCd;
    float touchCd;
    bool volleyDone;                 // Groggy 期间处决已用过
    Coroutine actRoutine;
    Color baseColor = Color.white;

    public bool IsActive { get { return state != State.Dormant && state != State.Dead; } }
    public float HpRatio { get { return health != null ? Mathf.Clamp01(health.hp / health.maxHp) : 0f; } }
    public float PostureRatio { get { return PostureThreshold() > 0f ? Mathf.Clamp01(posture / PostureThreshold()) : 0f; } }
    public int Phase { get { return phase; } }

    void Awake()
    {
        rbody = GetComponent<Rigidbody2D>();
        health = GetComponent<YG3Health>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }
        if (gate == null)
        {
            Transform t = transform.Find("gate");
            if (t != null)
            {
                gate = t;
            }
        }
    }

    void Start()
    {
        if (animator != null)
        {
            animator.Play(idleAnime);
        }
        if (health != null)
        {
            health.Died += OnDied;
            health.PoiseHit += AddPosture;
            // Groggy 期间受伤 ×1.5（挂过滤器）
            health.incomingFilter = FilterBossDamage;
        }
        if (YG3FxLibrary.Instance != null)
        {
            if (needlePrefab == null)
            {
                needlePrefab = YG3FxLibrary.Instance.needle;
            }
            if (shockwavePrefab == null)
            {
                shockwavePrefab = YG3FxLibrary.Instance.shockwave;
            }
            if (hedgehogPrefab == null)
            {
                hedgehogPrefab = YG3FxLibrary.Instance.hedgehog;
            }
        }
        if (needlePrefab != null)
        {
            YG3ObjectPool.Preload(needlePrefab, 12);
        }
        if (shockwavePrefab != null)
        {
            YG3ObjectPool.Preload(shockwavePrefab, 4);
        }
    }

    void Update()
    {
        if (YG3GameManager.State != YG3GameManager.GameState.Playing)
        {
            return;
        }
        touchCd -= Time.deltaTime;
        summonCd -= Time.deltaTime;

        switch (state)
        {
            case State.Dormant:
                TryActivate();
                break;

            case State.Idle:
                actTimer -= Time.deltaTime;
                if (actTimer <= 0f)
                {
                    PickSkill();
                }
                break;

            case State.Telegraph:
                stateTimer -= Time.deltaTime;
                // 读招：金色渐亮预警
                if (spriteRenderer != null)
                {
                    float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
                    spriteRenderer.color = Color.Lerp(baseColor, new Color(1.6f, 1.3f, 0.5f), k);
                }
                if (stateTimer <= 0f)
                {
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = baseColor;
                    }
                    ExecuteSkill();
                }
                break;

            case State.Recover:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    ToIdle();
                }
                break;

            case State.Flinch:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    ToIdle();
                }
                break;

            case State.Groggy:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    EndGroggy();
                }
                break;
        }

        // 接触伤害（Boss 身体）
        if (touchCd <= 0f && state != State.Dormant && state != State.Dead)
        {
            YG3PlayerController player = YG3PlayerController.Find();
            if (player != null && !player.IsDead)
            {
                float dist = Vector2.Distance(transform.position, player.transform.position);
                if (dist < 2.4f)
                {
                    touchCd = 0.9f;
                    player.GetComponent<YG3Health>().TakeDamage(YG3DamageInfo.Make(
                        1f, 12f,
                        new Vector2(Mathf.Sign(player.transform.position.x - transform.position.x) * 4f, 3f),
                        YG3DamageType.Light, gameObject, true, 0f));
                }
            }
        }
    }

    // ---------------- 状态流转 ----------------

    void TryActivate()
    {
        YG3PlayerController player = YG3PlayerController.Find();
        if (player == null || player.IsDead)
        {
            return;
        }
        if (Vector2.Distance(transform.position, player.transform.position) <= YG3Tuning.BOSS_TRIGGER_DISTANCE)
        {
            state = State.Idle;
            actTimer = YG3Tuning.BOSS_ACT_INTERVAL[phase] * 0.6f;
            YG3SoundManager.BGM(YG3Tuning.BGM_BOSS);
            YG3Fx.Shake(YG3Tuning.SHAKE_LIGHT);
        }
    }

    void ToIdle()
    {
        state = State.Idle;
        actTimer = YG3Tuning.BOSS_ACT_INTERVAL[phase];
        if (animator != null)
        {
            animator.Play(idleAnime);
        }
    }

    /// <summary>按距离 + 阶段权重选招（避免连续同一招靠 CD 天然错开）。</summary>
    void PickSkill()
    {
        YG3PlayerController player = YG3PlayerController.Find();
        if (player == null)
        {
            ToIdle();
            return;
        }
        float dist = Mathf.Abs(player.transform.position.x - transform.position.x);
        float roll = Random.value;
        string skill;
        if (dist < 6f)
        {
            skill = roll < 0.55f ? "slam" : (roll < 0.85f ? "volley" : "summon");
        }
        else if (dist < 14f)
        {
            skill = roll < 0.5f ? "volley" : (roll < 0.85f ? "slam" : "summon");
        }
        else
        {
            skill = roll < 0.6f ? "volley" : "summon";   // 太远时不用震波
        }
        if (skill == "summon" && (summonCd > 0f || YG3Hedgehog.AliveCount >= YG3Tuning.SUMMON_MAX_ALIVE))
        {
            skill = roll < 0.5f ? "volley" : "slam";
        }

        state = State.Telegraph;
        stateTimer = YG3Tuning.BOSS_TELEGRAPH[phase];
        pendingSkill = skill;
        YG3SoundManager.SE(skill == "slam" ? YG3Tuning.SE_DAMAGE : YG3Tuning.SE_GET, 0.45f, skill == "slam" ? 0.6f : 0.8f);
        if (animator != null)
        {
            animator.Play(skill == "slam" ? chargeAnime : chargeAnime);
        }
    }

    string pendingSkill;

    void ExecuteSkill()
    {
        if (pendingSkill == "volley")
        {
            state = State.Volley;
            if (actRoutine != null)
            {
                StopCoroutine(actRoutine);
            }
            actRoutine = StartCoroutine(VolleyRoutine());
        }
        else if (pendingSkill == "slam")
        {
            state = State.Slam;
            if (actRoutine != null)
            {
                StopCoroutine(actRoutine);
            }
            actRoutine = StartCoroutine(SlamRoutine());
        }
        else
        {
            state = State.Summon;
            if (actRoutine != null)
            {
                StopCoroutine(actRoutine);
            }
            actRoutine = StartCoroutine(SummonRoutine());
        }
    }

    // ---------------- 招式 ----------------

    IEnumerator VolleyRoutine()
    {
        if (animator != null)
        {
            animator.Play(volleyAnime);
        }
        yield return new WaitForSeconds(0.25f);

        YG3PlayerController player = YG3PlayerController.Find();
        int count = YG3Tuning.VOLLEY_COUNT_BASE + phase * 2;
        float spread = YG3Tuning.VOLLEY_SPREAD * (0.7f + 0.15f * phase);
        Vector2 origin = gate != null ? (Vector2)gate.position : (Vector2)transform.position + Vector2.up * 1.2f;
        Vector2 baseDir = player != null
            ? ((Vector2)player.transform.position + Vector2.up * 0.8f - origin).normalized
            : Vector2.left;

        // 分 3 波齐射
        int waves = 3;
        int perWave = Mathf.CeilToInt(count / (float)waves);
        for (int w = 0; w < waves; w++)
        {
            for (int i = 0; i < perWave; i++)
            {
                float t = count <= 1 ? 0.5f : (float)(w * perWave + i) / (count - 1);
                float ang = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
                ang += Mathf.Lerp(-spread / 2f, spread / 2f, t);
                Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                GameObject needle = YG3ObjectPool.Get(needlePrefab, origin, Quaternion.Euler(0f, 0f, ang));
                YG3Needle n = needle.GetComponent<YG3Needle>();
                if (n != null)
                {
                    n.poolSource = needlePrefab;
                    n.Launch(dir * YG3Tuning.VOLLEY_SPEED * (1f + 0.12f * phase), this);
                }
            }
            YG3SoundManager.SE(YG3Tuning.SE_SHOOT, 0.7f, 0.9f);
            yield return new WaitForSeconds(0.14f);
        }

        actRoutine = null;
        state = State.Recover;
        stateTimer = 0.8f;
    }

    IEnumerator SlamRoutine()
    {
        if (animator != null)
        {
            animator.Play(slamAnime);
        }
        yield return new WaitForSeconds(0.35f);

        // 砸地：屏震 + 音效 + 震波（P1 单向朝玩家，P2+ 双向）
        YG3Fx.Shake(YG3Tuning.SHAKE_HEAVY);
        YG3Fx.Hitstop(YG3Tuning.HITSTOP_LIGHT);
        YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 0.9f, 0.65f);
        YG3PlayerController player = YG3PlayerController.Find();
        int toward = player != null && player.transform.position.x > transform.position.x ? 1 : -1;
        Vector3 ground = transform.position;
        if (shockwavePrefab != null)
        {
            SpawnShock(toward, ground);
            if (phase >= 1)
            {
                SpawnShock(-toward, ground);
            }
        }

        yield return new WaitForSeconds(0.2f);
        actRoutine = null;
        state = State.Recover;
        stateTimer = 1.2f;    // 后摇最长 → 最重要的惩罚窗口
    }

    void SpawnShock(int dir, Vector3 ground)
    {
        GameObject go = YG3ObjectPool.Get(shockwavePrefab, ground, Quaternion.identity);
        YG3Shockwave s = go.GetComponent<YG3Shockwave>();
        if (s != null)
        {
            s.poolSource = shockwavePrefab;
            s.Launch(dir, ground);
        }
    }

    IEnumerator SummonRoutine()
    {
        if (animator != null)
        {
            animator.Play(chargeAnime);
        }
        yield return new WaitForSeconds(0.4f);

        if (hedgehogPrefab != null)
        {
            int alive = YG3Hedgehog.AliveCount;
            int spawn = Mathf.Min(2, YG3Tuning.SUMMON_MAX_ALIVE - alive);
            for (int i = 0; i < spawn; i++)
            {
                int side = i == 0 ? -1 : 1;
                Vector3 pos = transform.position + new Vector3(side * 2.2f, -0.5f, 0f);
                GameObject hog = Instantiate(hedgehogPrefab, pos, Quaternion.identity);
                hog.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                YG3Fx.Spark(pos, false);
            }
            if (spawn > 0)
            {
                summonCd = 25f;
                YG3SoundManager.SE(YG3Tuning.SE_GET, 0.9f, 0.9f);
            }
        }

        actRoutine = null;
        state = State.Recover;
        stateTimer = 0.5f;
    }

    // ---------------- 反制 ----------------

    /// <summary>玩家的伤害过滤器：Groggy 期间受伤 ×1.5，第一次挥剑触发处决。</summary>
    YG3DamageInfo? FilterBossDamage(YG3DamageInfo info)
    {
        if (state == State.Groggy)
        {
            info.damage *= YG3Tuning.BOSS_GROGGY_TAKE_RATIO;
            if (info.type == YG3DamageType.PlayerSword && !volleyDone)
            {
                volleyDone = true;
                ExecuteFlourish();
            }
        }
        return info;
    }

    /// <summary>处决演出：额外大额伤害 + 重演出，并提前结束 Groggy。
    /// 用普通方法而非协程：协程里的 WaitForSeconds 走 scaled time，顿帧期间会延迟数秒。</summary>
    void ExecuteFlourish()
    {
        health.RawDamage(health.maxHp * 0.2f);
        YG3Fx.Hitstop(YG3Tuning.HITSTOP_EXECUTE);
        YG3Fx.Shake(YG3Tuning.SHAKE_HEAVY);
        YG3Fx.Spark(transform.position + Vector3.up * 1f, true);
        YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 1f, 0.5f);
        EndGroggy();
    }

    /// <summary>完美弹反（SendMessage，来自金针或 Boss 本体）。</summary>
    public void OnParried()
    {
        AddPosture(YG3Tuning.BOSS_PARRY_POSTURE);
        // 踉跄：打断当前动作
        if (actRoutine != null)
        {
            StopCoroutine(actRoutine);
            actRoutine = null;
        }
        state = State.Flinch;
        stateTimer = YG3Tuning.BOSS_FLINCH;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
        if (animator != null)
        {
            animator.Play(idleAnime);
        }
        YG3Fx.Shake(YG3Tuning.SHAKE_LIGHT);
    }

    public void AddPosture(float amount)
    {
        if (state == State.Dead || state == State.Dormant)
        {
            return;
        }
        posture += amount;
        if (posture >= PostureThreshold())
        {
            posture = 0f;
            EnterGroggy();
        }
    }

    float PostureThreshold()
    {
        return YG3Tuning.BOSS_BREAK_THRESHOLD[Mathf.Clamp(phase, 0, 2)];
    }

    void EnterGroggy()
    {
        volleyDone = false;
        if (actRoutine != null)
        {
            StopCoroutine(actRoutine);
            actRoutine = null;
        }
        state = State.Groggy;
        stateTimer = YG3Tuning.BOSS_GROGGY;
        rbody.velocity = Vector2.zero;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.75f, 0.75f, 0.85f);
        }
        if (animator != null)
        {
            animator.Play(idleAnime);
        }
        YG3SoundManager.SE(YG3Tuning.SE_DAMAGE, 1f, 0.55f);
        YG3Fx.Shake(YG3Tuning.SHAKE_HEAVY);
    }

    void EndGroggy()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
        ToIdle();
    }

    void OnPostureBreak(float stun)
    {
        if (state != State.Dead)
        {
            EnterGroggy();
        }
    }

    // ---------------- 阶段与死亡 ----------------

    void LateUpdate()
    {
        if (state == State.Dormant || state == State.Dead)
        {
            return;
        }
        int newPhase = HpRatio > 0.66f ? 0 : (HpRatio > 0.33f ? 1 : 2);
        if (newPhase != phase)
        {
            phase = newPhase;
            YG3SoundManager.SE(YG3Tuning.SE_BOSS_KILLED, 0.4f, 1.3f);
            YG3Fx.Shake(YG3Tuning.SHAKE_LIGHT);
        }
    }

    public bool TakeDamage(YG3DamageInfo info)
    {
        if (state == State.Dormant || state == State.Dead)
        {
            return false;
        }
        return health.TakeDamage(info);
    }

    void OnDied()
    {
        state = State.Dead;
        if (actRoutine != null)
        {
            StopCoroutine(actRoutine);
            actRoutine = null;
        }
        if (animator != null)
        {
            animator.Play(deadAnime);
        }
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            c.enabled = false;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
        rbody.velocity = Vector2.zero;
        YG3SoundManager.SE(YG3Tuning.SE_BOSS_KILLED, 1f);
        YG3GameManager.Instance.AddScore(1000);
        YG3Fx.Shake(YG3Tuning.SHAKE_HEAVY);
        Invoke("BossClearStage", 2f);
    }

    void BossClearStage()
    {
        YG3GameManager.Instance.StageClear();
    }
}
