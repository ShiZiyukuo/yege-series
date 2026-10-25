using UnityEngine;

/// <summary>
/// 教程 10.3.4「编写用于控制子弹的脚本」。
/// 子弹在经过 deleteTime 后被删除，接触到任意物体就消失。
/// </summary>
public class BulletController : MonoBehaviour
{
    public float deleteTime = 3.0f;

    void Start()
    {
        Destroy(gameObject, deleteTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject);
    }
}
