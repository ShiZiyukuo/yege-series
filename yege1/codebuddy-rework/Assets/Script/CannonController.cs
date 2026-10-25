using UnityEngine;

/// <summary>
/// 固定炮台（教程第7.4节 制作固定炮台）
/// 玩家进入 length 范围后，每隔 delayTime 秒从 gate 位置发射一发炮弹。
/// </summary>
public class CannonController : MonoBehaviour
{
    public GameObject objPrefab;      // 炮弹预制体
    public float delayTime = 3.0f;    // 发射间隔
    public float fireSpeedX = -4.0f;  // 炮弹速度 X
    public float fireSpeedY = 0.0f;   // 炮弹速度 Y
    public float length = 8.0f;       // 感应距离

    GameObject player;
    GameObject gateObj;
    float passedTimes = 0.0f;

    void Start()
    {
        Transform tr = transform.Find("gate");
        if (tr != null)
        {
            gateObj = tr.gameObject;
        }
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (player == null || gateObj == null)
        {
            return;
        }
        if (!CheckLength(player.transform.position))
        {
            return;
        }
        passedTimes += Time.deltaTime;
        if (passedTimes > delayTime)
        {
            passedTimes = 0.0f;
            Vector3 pos = new Vector3(gateObj.transform.position.x, gateObj.transform.position.y, transform.position.z);
            GameObject obj = Instantiate(objPrefab, pos, Quaternion.identity);
            Rigidbody2D rbody = obj.GetComponent<Rigidbody2D>();
            if (rbody != null)
            {
                rbody.AddForce(new Vector2(fireSpeedX, fireSpeedY), ForceMode2D.Impulse);
            }
        }
    }

    bool CheckLength(Vector2 targetPos)
    {
        return length >= Vector2.Distance(transform.position, targetPos);
    }
}
