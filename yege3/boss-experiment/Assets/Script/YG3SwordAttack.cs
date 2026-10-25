using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// J 键挥剑：三段连击。前摇 → 判定窗口（OverlapBoxNonAlloc）→ 后摇（可被下一段/翻滚取消）。
/// 第三段伤害与架势伤害更高，命中演出（顿帧 + 屏震）随段位增强。
/// </summary>
public class YG3SwordAttack : MonoBehaviour
{
    [HideInInspector] public int stage;                 // 0/1/2 → 三段
    float comboTimer;
    Coroutine routine;
    SpriteRenderer spriteRenderer;
    YG3PlayerController player;
    readonly Collider2D[] hits = new Collider2D[16];
    readonly HashSet<Collider2D> hitSet = new HashSet<Collider2D>();

    public bool IsAttacking { get { return routine != null; } }

    void Awake()
    {
        player = GetComponent<YG3PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (stage > 0 && !IsAttacking)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                stage = 0;   // 连段窗口过期
            }
        }
    }

    public void TryAttack()
    {
        if (YG3GameManager.State != YG3GameManager.GameState.Playing || player.IsDead)
        {
            return;
        }
        if (player.state == YG3PlayerController.State.Guard ||
            player.state == YG3PlayerController.State.Stun ||
            player.state == YG3PlayerController.State.Roll)
        {
            return;
        }
        if (IsAttacking)
        {
            return;    // 已在挥剑（连段由协程末段自动衔接）
        }
        if (routine != null)
        {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(Swing(stage));
    }

    IEnumerator Swing(int current)
    {
        player.state = YG3PlayerController.State.Attack;
        player.attackLock++;
        int idx = Mathf.Clamp(current, 0, 2);

        // 前摇
        yield return new WaitForSeconds(YG3Tuning.SWORD_WINDUP[idx]);

        // 判定窗口
        hitSet.Clear();
        float t = 0f;
        bool didHit = false;
        while (t < YG3Tuning.SWORD_ACTIVE[idx])
        {
            t += Time.deltaTime;
            Vector2 center = (Vector2)transform.position + Vector2.right * player.facing * YG3Tuning.SWORD_RANGE * 0.6f;
            Vector2 size = new Vector2(YG3Tuning.SWORD_RANGE, YG3Tuning.SWORD_HEIGHT);
            int n = Physics2D.OverlapBoxNonAlloc(center, size, 0f, hits,
                (1 << YG3Tuning.LAYER_ENEMY) | (1 << YG3Tuning.LAYER_BOSS));
            for (int i = 0; i < n; i++)
            {
                Collider2D c = hits[i];
                if (c == null || hitSet.Contains(c) || !c.isActiveAndEnabled)
                {
                    continue;
                }
                hitSet.Add(c);
                Hit(c, idx, ref didHit);
            }
            yield return null;
        }

        // 后摇（可被翻滚取消 → 玩家 Update 里处理）
        yield return new WaitForSeconds(YG3Tuning.SWORD_RECOVER[idx]);

        player.attackLock = Mathf.Max(0, player.attackLock - 1);
        if (player.state == YG3PlayerController.State.Attack)
        {
            player.state = YG3PlayerController.State.Normal;
        }

        // 连段：窗口内继续按 J → 下一段
        stage = (idx == 2 || !didHit) ? 0 : idx + 1;
        comboTimer = YG3Tuning.COMBO_RESET;
        routine = null;
    }

    void Hit(Collider2D target, int idx, ref bool didHit)
    {
        float dmg = idx == 2 ? YG3Tuning.SWORD3_DAMAGE : (idx == 1 ? YG3Tuning.SWORD2_DAMAGE : YG3Tuning.SWORD1_DAMAGE);
        float poise = idx == 2 ? YG3Tuning.SWORD3_POISE : (idx == 1 ? YG3Tuning.SWORD2_POISE : YG3Tuning.SWORD1_POISE);

        IYG3Damageable damageable = target.GetComponentInParent<IYG3Damageable>();
        if (damageable == null)
        {
            return;
        }

        // 蜷缩刺球：近战被反伤
        YG3Hedgehog hog = target.GetComponentInParent<YG3Hedgehog>();
        if (hog != null && hog.IsSpiky)
        {
            hog.ReflectSword(player);
            didHit = true;
            return;
        }

        YG3DamageInfo info = YG3DamageInfo.Make(
            dmg, poise,
            new Vector2(player.facing * YG3Tuning.SWORD_KNOCKBACK, 2f),
            YG3DamageType.PlayerSword, gameObject, false,
            idx == 2 ? YG3Tuning.HITSTOP_LIGHT * 2f : YG3Tuning.HITSTOP_LIGHT);

        bool applied = damageable.TakeDamage(info);
        if (applied)
        {
            didHit = true;
            SpawnSlash(idx);
            YG3SoundManager.SE(YG3Tuning.SE_SHOOT, 0.45f, 1.5f + 0.15f * idx);
            YG3Fx.Shake(idx == 2 ? YG3Tuning.SHAKE_HEAVY : YG3Tuning.SHAKE_LIGHT);
        }
        else
        {
            SpawnSlash(idx);    // 空挥也出弧光
        }
    }

    void SpawnSlash(int idx)
    {
        if (YG3FxLibrary.Instance == null || YG3FxLibrary.Instance.slash == null)
        {
            return;
        }
        GameObject slash = YG3FxLibrary.Spawn(YG3FxLibrary.Instance.slash,
            transform.position + Vector3.up * 0.85f + Vector3.right * player.facing * 0.75f,
            Quaternion.Euler(0f, player.facing < 0f ? 180f : 0f, -30f + idx * 18f));
        if (slash == null)
        {
            return;
        }
        slash.transform.localScale = Vector3.one * (idx == 2 ? 1.35f : 1f);
        YG3Fx.Life(slash, 0.18f, 1.5f);
    }

    /// <summary>受击/翻滚/死亡时中断当前挥剑。</summary>
    public void Cancel()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        if (player != null && player.attackLock > 0)
        {
            player.attackLock = Mathf.Max(0, player.attackLock - 1);
        }
        if (player != null && player.state == YG3PlayerController.State.Attack)
        {
            player.state = YG3PlayerController.State.Normal;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        YG3PlayerController p = GetComponent<YG3PlayerController>();
        float f = p != null ? p.facing : 1f;
        Vector3 c = transform.position + Vector3.right * f * YG3Tuning.SWORD_RANGE * 0.6f + Vector3.up * 0.2f;
        Gizmos.DrawWireCube(c, new Vector3(YG3Tuning.SWORD_RANGE, YG3Tuning.SWORD_HEIGHT, 0f));
    }
#endif
}
