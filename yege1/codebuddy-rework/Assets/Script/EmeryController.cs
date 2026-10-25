using UnityEngine;

/// <summary>
/// 来回活动的敌方角色（教程第7.5节）
/// 沿一个方向巡逻，碰到转向用的墙（Wall）或到达 range 边界就掉头。
/// </summary>
public class EmeryController : MonoBehaviour
{
    public float speed = 3.0f;
    public string direction = "left";   // 初始方向 left / right
    public float range = 0.0f;          // 巡逻范围（0 表示只靠墙转身）

    Vector3 defPos;
    Rigidbody2D rbody;

    void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
        defPos = transform.position;
        ApplyDirection();
    }

    void Update()
    {
        if (range > 0.0f)
        {
            if (transform.position.x < defPos.x - (range / 2.0f))
            {
                direction = "right";
                ApplyDirection();
            }
            else if (transform.position.x > defPos.x + (range / 2.0f))
            {
                direction = "left";
                ApplyDirection();
            }
        }
    }

    void FixedUpdate()
    {
        if (rbody == null)
        {
            return;
        }
        float vx = (direction == "right") ? speed : -speed;
        rbody.velocity = new Vector2(vx, rbody.velocity.y);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Turn();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Turn();
        }
    }

    void Turn()
    {
        direction = (direction == "right") ? "left" : "right";
        ApplyDirection();
    }

    void ApplyDirection()
    {
        // 素材默认朝左，朝右时水平翻转
        Vector3 s = transform.localScale;
        s.x = (direction == "right") ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        transform.localScale = s;
    }
}
