using UnityEngine;

/// <summary>
/// 教程 8.4.9「编写射箭的脚本」。
/// 射箭键为 Fire3（默认左 Shift），弓始终朝向行进方向，
/// 面向上方时弓显示在玩家角色后面（position.z = 1）。
/// </summary>
public class ArrowShoot : MonoBehaviour
{
    public float shootSpeed = 8.0f;    // 箭矢速度
    public float shootDelay = 0.25f;   // 射箭后的等待时间

    public GameObject bowPrefab;       // 弓的预制
    public GameObject arrowPrefab;     // 箭矢的预制

    bool inAttack = false;             // 攻击中的旗标（防止连射）
    GameObject bowObj;

    void Start()
    {
        if (bowPrefab == null)
        {
            return;
        }
        // 生成弓并配置为玩家角色的子对象
        bowObj = Instantiate(bowPrefab, transform.position, Quaternion.identity);
        bowObj.transform.SetParent(transform, false);
        bowObj.transform.localPosition = Vector3.zero;
        bowObj.transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        if (PlayerController.gameState != "playing")
        {
            return;
        }

        if (Input.GetButtonDown("Fire3"))
        {
            Attack();
        }

        if (bowObj == null)
        {
            return;
        }

        PlayerController plcnt = GetComponent<PlayerController>();
        float angle = plcnt != null ? plcnt.angleZ : -90.0f;

        // 弓跟随行进方向旋转（以弓的尾端为基准点）
        bowObj.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);

        // 面向上方时把弓放到玩家角色的后面
        Vector3 pos = Vector3.zero;
        if (angle > 45.0f && angle <= 135.0f)
        {
            pos.z = 1.0f;
        }
        bowObj.transform.localPosition = pos;

        // 朝左时避免弓上下颠倒
        SpriteRenderer bowSprite = bowObj.GetComponent<SpriteRenderer>();
        if (bowSprite != null)
        {
            bowSprite.flipY = (angle > 90.0f || angle < -90.0f);
        }
    }

    /// <summary>射箭（教程 8.4.9 的 Attack 方法，虚拟面板也调用它）。</summary>
    public void Attack()
    {
        if (PlayerController.gameState != "playing")
        {
            return;
        }
        if (ItemKeeper.hasArrows <= 0 || inAttack)
        {
            return;
        }
        if (arrowPrefab == null)
        {
            return;
        }

        inAttack = true;
        ItemKeeper.hasArrows -= 1;

        PlayerController plcnt = GetComponent<PlayerController>();
        float angle = plcnt != null ? plcnt.angleZ : -90.0f;

        // 用三角函数生成射箭方向的向量
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.Euler(0.0f, 0.0f, angle));
        Rigidbody2D arrowBody = arrow.GetComponent<Rigidbody2D>();
        if (arrowBody != null)
        {
            arrowBody.AddForce(dir * shootSpeed, ForceMode2D.Impulse);
        }

        SoundManager.SEPlay(SEType.Shoot);
        Invoke("StopAttack", shootDelay);
    }

    void StopAttack()
    {
        inAttack = false;
    }
}
