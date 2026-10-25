using UnityEngine;

/// <summary>
/// 教程 8.4.10「编写控制箭矢的脚本」。
/// 箭矢在 deleteTime 秒后自动删除；命中物体后成为其子对象，
/// 并使物理仿真与碰撞体积无效，呈现「插在物体上」的状态。
/// </summary>
public class ArrowController : MonoBehaviour
{
    public float deleteTime = 2.0f;

    void Start()
    {
        Destroy(gameObject, deleteTime);

        // 箭矢从玩家所在位置发射，出生瞬间一定会和玩家重叠。
        // 教程 8.4.10 通过「层之间的接触设定」关闭 Player×Arrow，
        // 这里再加一道运行时保险，避免该设置未写进工程时箭矢卡在原地。
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Collider2D playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        Collider2D arrowCollider = GetComponent<Collider2D>();
        if (playerCollider != null && arrowCollider != null)
        {
            Physics2D.IgnoreCollision(arrowCollider, playerCollider, true);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            return;
        }

        // 成为被射中物体的子对象
        transform.SetParent(collision.gameObject.transform, true);

        Rigidbody2D rbody = GetComponent<Rigidbody2D>();
        if (rbody != null)
        {
            rbody.simulated = false;
        }
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }
}
