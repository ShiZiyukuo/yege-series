using UnityEngine;

/// <summary>
/// 玩家角色控制（教程第3章 / 第4.5节）
/// 负责：左右移动、跳跃、着地判定、动画切换、得分道具、终点与死亡判定。
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    public float speed = 3.0f;      // 移动速度
    public float jump = 9.0f;       // 跳跃力
    public LayerMask groundLayer;   // 地面层（Ground）

    [Header("动画状态名")]
    public string stopAnime = "PlayerStop";
    public string moveAnime = "PlayerMove";
    public string jumpAnime = "PlayerJump";
    public string goalAnime = "PlayerGoal";
    public string deadAnime = "PlayerOver";

    // 游戏状态：playing / gameclear / gameover / gameend
    public static string gameState = "playing";
    // 本帧拾取到的道具分数（由 GameManager 取走）
    public int score = 0;

    Rigidbody2D rbody;
    Animator animator;
    SpriteRenderer spriteRenderer;
    CapsuleCollider2D capsule;

    float axisH = 0.0f;      // 水平输入 -1 / 0 / 1
    bool goJump = false;     // 跳跃请求
    bool onGround = false;   // 是否着地
    bool isMoving = false;   // 是否正在使用虚拟摇杆
    string nowAnime = "";
    string oldAnime = "";

    void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsule = GetComponent<CapsuleCollider2D>();

        nowAnime = stopAnime;
        oldAnime = stopAnime;
        gameState = "playing";
        score = 0;
    }

    void Update()
    {
        if (gameState != "playing")
        {
            return;
        }
        // 虚拟摇杆没有操作时，才读取键盘输入（教程第3章的输入处理）
        if (isMoving == false)
        {
            axisH = Input.GetAxisRaw("Horizontal");
        }
        if (axisH != 0.0f)
        {
            // 用 flipX 翻转朝向，不改变 localScale，避免影响碰撞体与动画
            spriteRenderer.flipX = (axisH < 0.0f);
        }
        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        if (gameState != "playing")
        {
            return;
        }
        // 着地判定：从脚下向下发射一条短射线（教程第3章 Linecast 判定）
        onGround = CheckOnGround();

        if (onGround || axisH != 0.0f)
        {
            rbody.velocity = new Vector2(speed * axisH, rbody.velocity.y);
        }
        if (onGround && goJump)
        {
            Vector2 jumpPw = new Vector2(0, jump);
            rbody.AddForce(jumpPw, ForceMode2D.Impulse);
            goJump = false;
        }

        // 动画切换
        if (onGround)
        {
            nowAnime = (axisH == 0.0f) ? stopAnime : moveAnime;
        }
        else
        {
            nowAnime = jumpAnime;
        }
        if (nowAnime != oldAnime)
        {
            oldAnime = nowAnime;
            animator.Play(nowAnime);
        }
    }

    /// <summary>从胶囊碰撞体底部向下做 Linecast，判断是否踩在 Ground 层上</summary>
    bool CheckOnGround()
    {
        if (capsule == null)
        {
            return false;
        }
        Bounds b = capsule.bounds;
        float x = b.center.x;
        float bottom = b.min.y;
        Vector2 from = new Vector2(x, bottom + 0.15f);
        Vector2 to = new Vector2(x, bottom - 0.20f);
        return Physics2D.Linecast(from, to, groundLayer);
    }

    /// <summary>按下跳跃键（虚拟按钮/键盘都会调用这里）</summary>
    public void Jump()
    {
        goJump = true;
    }

    /// <summary>虚拟摇杆输入（教程第7.7节 支持触摸屏操作）</summary>
    public void SetAxis(float h, float v)
    {
        axisH = h;
        isMoving = (axisH != 0.0f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HitObject(collision.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HitObject(collision.gameObject);
    }

    void HitObject(GameObject obj)
    {
        if (gameState != "playing")
        {
            return;
        }
        if (obj.CompareTag("Goal"))
        {
            Goal();
        }
        else if (obj.CompareTag("Dead"))
        {
            GameOver();
        }
        else if (obj.CompareTag("ScoreItem"))
        {
            ItemData item = obj.GetComponent<ItemData>();
            if (item != null)
            {
                score += item.value;   // 交給 GameManager 累加到本关得分
            }
            Destroy(obj);
        }
    }

    /// <summary>到达终点</summary>
    public void Goal()
    {
        if (gameState != "playing")
        {
            return;
        }
        animator.Play(goalAnime);
        gameState = "gameclear";
        rbody.velocity = Vector2.zero;
    }

    /// <summary>死亡</summary>
    public void GameOver()
    {
        if (gameState != "playing")
        {
            return;
        }
        animator.Play(deadAnime);
        gameState = "gameover";
        rbody.velocity = Vector2.zero;
        if (capsule != null)
        {
            capsule.enabled = false;
        }
        rbody.AddForce(new Vector2(0, 5), ForceMode2D.Impulse);
    }
}
