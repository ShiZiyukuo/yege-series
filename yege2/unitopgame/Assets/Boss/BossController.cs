using UnityEngine;

/// <summary>
/// 教程 10.3.5「编写boss角色的脚本」+ 10.3.6「为动画效果设置事件」。
/// 玩家靠近后切换到攻击动画，动画事件在蓄力 0.75s 后调用 Attack 发射子弹。
/// </summary>
public class BossController : MonoBehaviour
{
    public int hp = 5;
    public float reactionDistance = 7.0f;

    public GameObject bulletPrefab;
    public float bulletSpeed = 4.0f;

    public string idleAnime = "BossIdle";
    public string attackAnime = "BossAttack";
    public string deadAnime = "BossDead";

    public GameObject gate;      // 子弹的发射口（子对象）
    public int arrangeId = 0;

    bool inAttack = false;
    bool isDead = false;

    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (gate == null)
        {
            Transform found = transform.Find("gate");
            if (found != null)
            {
                gate = found.gameObject;
            }
        }

        if (animator != null)
        {
            animator.Play(idleAnime);
        }
    }

    void Update()
    {
        if (isDead || hp <= 0)
        {
            return;
        }
        if (PlayerController.gameState != "playing")
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance <= reactionDistance && !inAttack)
        {
            inAttack = true;
            if (animator != null)
            {
                animator.Play(attackAnime);
            }
        }
        else if (distance > reactionDistance && inAttack)
        {
            inAttack = false;
            if (animator != null)
            {
                animator.Play(idleAnime);
            }
        }
    }

    /// <summary>由 BossAttack 动画的事件调用（教程 10.3.6）。</summary>
    public void Attack()
    {
        if (isDead || bulletPrefab == null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        Vector2 gatePos = gate != null ? (Vector2)gate.transform.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)player.transform.position - gatePos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject bullet = Instantiate(bulletPrefab, gatePos, Quaternion.Euler(0.0f, 0.0f, angle));

        // 教程 10.3.3 用层矩阵关闭 Enemy×Bullet；这里再加一道运行时保险，
        // 否则子弹会从 boss 体内生成而立刻撞到自己消失。
        Collider2D bossCollider = GetComponent<Collider2D>();
        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
        if (bossCollider != null && bulletCollider != null)
        {
            Physics2D.IgnoreCollision(bulletCollider, bossCollider, true);
        }

        Rigidbody2D bulletBody = bullet.GetComponent<Rigidbody2D>();
        if (bulletBody != null)
        {
            bulletBody.AddForce(dir * bulletSpeed, ForceMode2D.Impulse);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || collision.gameObject.tag != "Arrow")
        {
            return;
        }

        hp -= 1;
        if (hp > 0)
        {
            return;
        }

        isDead = true;
        if (animator != null)
        {
            animator.Play(deadAnime);
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 摄像机不再追踪已死亡的 boss
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null && cameraManager.otherTarget == gameObject)
        {
            cameraManager.otherTarget = null;
        }

        SaveDataManager.SetArrangeId(arrangeId, gameObject.tag);
        SoundManager.SEPlay(SEType.BossKilled);
        Destroy(gameObject, 1.0f);
    }
}
