using UnityEngine;

/// <summary>
/// K 键射箭：无限箭 + 0.35s 冷却。方向取玩家朝向（按住上/W 时向上 45° 斜射）。
/// </summary>
public class YG3BowAttack : MonoBehaviour
{
    public GameObject arrowPrefab;

    float cooldown;
    YG3PlayerController player;

    void Awake()
    {
        player = GetComponent<YG3PlayerController>();
    }

    void Start()
    {
        if (arrowPrefab == null && YG3FxLibrary.Instance != null)
        {
            arrowPrefab = YG3FxLibrary.Instance.arrow;
        }
        if (arrowPrefab != null)
        {
            YG3ObjectPool.Preload(arrowPrefab, 6);
        }
    }

    void Update()
    {
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
        }
    }

    public void TryShoot()
    {
        if (YG3GameManager.State != YG3GameManager.GameState.Playing || player.IsDead)
        {
            return;
        }
        if (cooldown > 0f ||
            player.state == YG3PlayerController.State.Guard ||
            player.state == YG3PlayerController.State.Stun ||
            player.state == YG3PlayerController.State.Roll)
        {
            return;
        }

        float angle = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || YG3VirtualPad.AimUp)
        {
            angle = 45f * player.facing;
        }

        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * player.facing, Mathf.Sin(angle * Mathf.Deg2Rad));
        if (arrowPrefab != null)
        {
            // 发射高度对齐刺猬身体（刺猬碰撞体顶 ~1.3），避免箭从头顶飞过
            GameObject arrow = YG3ObjectPool.Get(arrowPrefab,
                transform.position + Vector3.up * 0.35f,
                Quaternion.Euler(0f, 0f, angle));
            YG3Arrow a = arrow.GetComponent<YG3Arrow>();
            if (a != null)
            {
                a.poolSource = arrowPrefab;
                a.Launch(dir * YG3Tuning.ARROW_SPEED, gameObject);
            }
        }

        cooldown = YG3Tuning.ARROW_COOLDOWN;
        YG3SoundManager.SE(YG3Tuning.SE_SHOOT, 0.7f, 1f);
        player.attackLock++;     // 射击瞬间小硬直
        Invoke("ReleaseLock", 0.08f);
    }

    void ReleaseLock()
    {
        player.attackLock = Mathf.Max(0, player.attackLock - 1);
    }

    public void Cancel()
    {
        CancelInvoke("ReleaseLock");
        player.attackLock = Mathf.Max(0, player.attackLock - 1);
    }
}
