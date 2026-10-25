using UnityEngine;

/// <summary>
/// 炮弹（教程第7.4节）
/// 不受重力，撞到任何东西就消失，deleteTime 秒后自动销毁。
/// </summary>
public class ShellController : MonoBehaviour
{
    public float deleteTime = 3.0f;

    void Start()
    {
        Rigidbody2D rbody = GetComponent<Rigidbody2D>();
        if (rbody != null)
        {
            rbody.gravityScale = 0.0f;
        }
        Destroy(gameObject, deleteTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(gameObject);
    }
}
