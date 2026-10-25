using UnityEngine;

/// <summary>
/// 教程 9.3.3「编写敌方角色的脚本」+ 10.2.4「保存已击倒的敌人」。
/// 平时静止待机，玩家靠近反应距离内开始追踪，被箭矢击中 hp 归零后消失。
/// </summary>
public class EnemyController : MonoBehaviour
{
    public int hp = 1;
    public float speed = 2.0f;

    public string idleAnime = "EnemyIdle";
    public string upAnime = "EnemyUp";
    public string downAnime = "EnemyDown";
    public string rightAnime = "EnemyRight";
    public string leftAnime = "EnemyLeft";
    public string deadAnime = "EnemyDead";

    public float reactionDistance = 4.0f;   // 开始追赶玩家的反应距离
    public int arrangeId = 0;

    bool isActive = false;
    bool isDead = false;

    float axisH = 0.0f;
    float axisV = 0.0f;

    string nowAnimation = "";
    string oldAnimation = "";

    Rigidbody2D rbody;
    Animator animator;

    void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        nowAnimation = oldAnimation = idleAnime;
        if (animator != null)
        {
            animator.Play(idleAnime);
        }
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }

        // 玩家游戏失败后会从场景中消失，因此每次都要重新寻找
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            isActive = false;
            axisH = 0.0f;
            axisV = 0.0f;
            ChangeAnimation(idleAnime);
            return;
        }

        if (isActive)
        {
            // 计算朝向玩家的角度，并据此设定动画与移动向量
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            nowAnimation = SelectAnimation(angle);
            axisH = Mathf.Cos(angle * Mathf.Deg2Rad);
            axisV = Mathf.Sin(angle * Mathf.Deg2Rad);
        }
        else
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance <= reactionDistance)
            {
                isActive = true;
            }
            ChangeAnimation(idleAnime);
        }

        ChangeAnimation(nowAnimation);
    }

    void FixedUpdate()
    {
        if (rbody == null)
        {
            return;
        }
        if (isDead)
        {
            rbody.velocity = Vector2.zero;
            return;
        }

        if (isActive && hp > 0)
        {
            rbody.velocity = new Vector2(axisH * speed, axisV * speed);
        }
        else
        {
            rbody.velocity = Vector2.zero;
        }
    }

    void ChangeAnimation(string animation)
    {
        if (string.IsNullOrEmpty(animation) || animation == oldAnimation)
        {
            return;
        }
        if (animator != null)
        {
            animator.Play(animation);
        }
        oldAnimation = animation;
    }

    string SelectAnimation(float angle)
    {
        if (angle > -135.0f && angle <= -45.0f)
        {
            return downAnime;
        }
        if (angle > 45.0f && angle <= 135.0f)
        {
            return upAnime;
        }
        if (angle > -45.0f && angle <= 45.0f)
        {
            return rightAnime;
        }
        return leftAnime;
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
        ChangeAnimation(deadAnime);
        if (animator != null)
        {
            animator.Play(deadAnime);
        }

        // 死亡后即使被玩家碰到也不会造成伤害
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        if (rbody != null)
        {
            rbody.velocity = Vector2.zero;
        }

        SaveDataManager.SetArrangeId(arrangeId, gameObject.tag);
        SoundManager.SEPlay(SEType.Killed);
        Destroy(gameObject, 0.5f);
    }
}
