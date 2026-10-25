using UnityEngine;

/// <summary>
/// 教程 8.4.5「编写玩家移动的脚本」+ 8.4.13「制作玩家的伤害处理」+
/// 10.2.3「保存和读取玩家HP」。
/// 玩家可以 360° 移动，弓箭朝向行进方向，受到敌人伤害时闪烁并被击退。
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float speed = 4.0f;

    public string upAnime = "PlayerUp";
    public string downAnime = "PlayerDown";
    public string rightAnime = "PlayerRight";
    public string leftAnime = "PlayerLeft";
    public string deadAnime = "PlayerDead";

    string nowAnimation = "";
    string oldAnimation = "";

    float axisH = 0.0f;
    float axisV = 0.0f;

    /// <summary>行进方向的角度（供弓与箭矢使用）。</summary>
    public float angleZ = -90.0f;

    /// <summary>生命值（static：跨场景保持）。</summary>
    public static int hp = 3;

    /// <summary>游戏状态：playing / gameover / gameclear。</summary>
    public static string gameState = "playing";

    bool inDamage = false;

    /// <summary>是否正在使用虚拟面板（为真时不再读取键盘输入）。</summary>
    bool padInput = false;

    Rigidbody2D rbody;
    SpriteRenderer spriteRenderer;
    Animator animator;

    /// <summary>
    /// 场景读取时先复位状态：UIManager 的 Start 会参考 gameState，
    /// 而 Awake 一定早于所有 Start，保证 UI 读到的是本局的状态。
    /// </summary>
    void Awake()
    {
        gameState = "playing";
        inDamage = false;
    }

    void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // 初始朝向为向下（教程 8.4.5）
        nowAnimation = downAnime;
        oldAnimation = downAnime;
        if (animator != null)
        {
            animator.Play(downAnime);
        }

        gameState = "playing";
        inDamage = false;

        // 10.2.3 读取存档中的 HP
        hp = PlayerPrefs.GetInt("PlayerHP", 3);
        if (hp <= 0)
        {
            hp = 3;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    void Update()
    {
        if (gameState != "playing")
        {
            return;
        }
        if (inDamage)
        {
            return;
        }

        // 教程 8.4.5：Top View 用 Vertical 取得纵轴。
        // 关键：SetAxis 是「代替」Input.GetAxisRaw 的，虚拟面板正在操作时不能读键盘，
        // 否则手机上没有键盘输入，GetAxisRaw 恒为 0，会在下一帧把摇杆的值冲刷掉，
        // 表现就是虚拟面板完全推不动角色。
        if (!padInput)
        {
            axisH = Input.GetAxisRaw("Horizontal");
            axisV = Input.GetAxisRaw("Vertical");
        }

        Vector2 toPt = new Vector2(transform.position.x + axisH, transform.position.y + axisV);
        angleZ = GetAngle(toPt);

        nowAnimation = SelectAnimation();
        if (nowAnimation != oldAnimation)
        {
            if (animator != null)
            {
                animator.Play(nowAnimation);
            }
            oldAnimation = nowAnimation;
        }
    }

    void FixedUpdate()
    {
        if (gameState != "playing")
        {
            return;
        }

        if (inDamage)
        {
            // 教程 8.4.13：用 Sin 的取值交替显示/隐藏精灵，呈现闪烁效果
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = Mathf.Sin(Time.time * 30.0f) > 0.0f;
            }
            return;
        }

        if (rbody != null)
        {
            rbody.velocity = new Vector2(axisH * speed, axisV * speed);
        }
    }

    /// <summary>
    /// 由虚拟面板调用，代替 Input.GetAxisRaw 赋值（教程 8.4.5 的 SetAxis）。
    /// 摇杆推动期间置起 padInput，让 Update 不再用键盘覆盖这两个值；
    /// 松手时 VirtualPad 会传 (0,0)，padInput 落下，重新交回键盘控制。
    /// </summary>
    public void SetAxis(float h, float v)
    {
        axisH = h;
        axisV = v;
        padInput = (h != 0.0f || v != 0.0f);
    }

    /// <summary>被放置到出入口时设定朝向（供 RoomManager 调用）。</summary>
    public void FaceDirection(ExitDirection direction)
    {
        switch (direction)
        {
            case ExitDirection.up:
                angleZ = 90.0f;
                nowAnimation = upAnime;
                break;
            case ExitDirection.down:
                angleZ = -90.0f;
                nowAnimation = downAnime;
                break;
            case ExitDirection.left:
                angleZ = 180.0f;
                nowAnimation = leftAnime;
                break;
            default:
                angleZ = 0.0f;
                nowAnimation = rightAnime;
                break;
        }
        oldAnimation = nowAnimation;
        if (animator != null)
        {
            animator.Play(nowAnimation);
        }
    }

    /// <summary>通过两个点计算角度（教程 8.4.5 的 GetAngle）。</summary>
    float GetAngle(Vector2 toPt)
    {
        if (axisH == 0.0f && axisV == 0.0f)
        {
            return angleZ;   // 停止时保持之前的朝向
        }
        float dx = toPt.x - transform.position.x;
        float dy = toPt.y - transform.position.y;
        return Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
    }

    string SelectAnimation()
    {
        if (angleZ > -135.0f && angleZ <= -45.0f)
        {
            return downAnime;
        }
        if (angleZ > 45.0f && angleZ <= 135.0f)
        {
            return upAnime;
        }
        if (angleZ > -45.0f && angleZ <= 45.0f)
        {
            return rightAnime;
        }
        return leftAnime;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            GetDamage(collision);
        }
    }

    /// <summary>教程 8.4.13 的 GetDamage：扣血 + 击退 + 无敌闪烁。</summary>
    void GetDamage(Collision2D collision)
    {
        if (gameState != "playing")
        {
            return;
        }

        hp -= 1;
        PlayerPrefs.SetInt("PlayerHP", hp);   // 10.2.3 立即保存
        SoundManager.SEPlay(SEType.Damage);

        inDamage = true;

        // 从敌人指向自己的方向被击退
        Vector2 dir = ((Vector2)transform.position - (Vector2)collision.gameObject.transform.position).normalized;
        if (rbody != null)
        {
            rbody.velocity = Vector2.zero;
            rbody.AddForce(dir * 4.0f, ForceMode2D.Impulse);
        }

        Invoke("DamageEnd", 0.25f);

        if (hp <= 0)
        {
            GameOver();
        }
    }

    void DamageEnd()
    {
        inDamage = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    void GameOver()
    {
        gameState = "gameover";
        SoundManager.StopBgm();
        SoundManager.SEPlay(SEType.GameOver);
        if (animator != null)
        {
            animator.Play(deadAnime);
        }
        if (rbody != null)
        {
            rbody.velocity = Vector2.zero;
        }
        Destroy(gameObject, 1.0f);
    }
}
