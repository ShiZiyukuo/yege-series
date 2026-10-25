using UnityEngine;

/// <summary>
/// 陷阱方块（教程第7.1节 制作带伤害块体）
/// 玩家靠近到 length 以内时从静态变为动态坠落；isDelete 为真时落地后淡出销毁。
/// </summary>
public class GimmickBlock : MonoBehaviour
{
    public float length = 0.0f;     // 感应距离
    public bool isDelete = false;   // 落地后是否消失
    public float delay = 0.8f;      // 感应到玩家后延迟多久才开始坠落（留给玩家通过的时间）

    bool isFell = false;
    bool isCounting = false;
    float waitTime = 0.0f;
    float fadeTime = 0.5f;
    Rigidbody2D rbody;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rbody.bodyType = RigidbodyType2D.Static;
    }

    void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && rbody.bodyType == RigidbodyType2D.Static)
        {
            float d = Vector2.Distance(transform.position, player.transform.position);
            if (length >= d)
            {
                isCounting = true;
            }
        }
        if (isCounting && rbody.bodyType == RigidbodyType2D.Static)
        {
            waitTime += Time.deltaTime;
            if (waitTime >= delay)
            {
                rbody.bodyType = RigidbodyType2D.Dynamic;
            }
        }
        if (isFell)
        {
            fadeTime -= Time.deltaTime;
            Color col = spriteRenderer.color;
            col.a = fadeTime;
            spriteRenderer.color = col;
            if (fadeTime <= 0.0f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDelete)
        {
            isFell = true;
        }
    }
}
