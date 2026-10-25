using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D robdy;
    float axisH = 0.0f;
    public float speed = 3.0f;
    public float jump = 9.0f;
    public LayerMask groundLayer;
    bool goJump = false;
    bool onGround = false;
    Animator animator;
    SpriteRenderer sr;
    public string stopAnime = "PlayerStop";
    public string moveAnime = "PlayerMove";
    public string jumpAnime = "PlayerJump";
    public string goalAnime = "PlayerGoal";
    public string deadAnime = "PlayerOver";
    string nowAnime = "";
    string oldAnime = "";
    public static string gameState = "playing";
    public int score = 0;
    bool isMoving = false;

    // Start is called before the first frame update
    void Start()
    {
        robdy = this.GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); 
        sr = GetComponent<SpriteRenderer>();
        nowAnime = stopAnime;
        oldAnime = stopAnime;
        gameState = "playing";
    }

    // Update is called once per frame
    void Update()
    {
        if (gameState != "playing")
        {
            return;
        }
        if (isMoving == false)
        {
            axisH = Input.GetAxisRaw("Horizontal");
        }
        if (axisH != 0.0f)
        {
            Debug.Log(axisH > 0.0f ? "�����ƶ�" : "�����ƶ�");
            sr.flipX = (axisH < 0.0f);   // �� flipX ��ת���򣬲����޸� localScale
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
        onGround = Physics2D.Linecast(transform.position, transform.position - (transform.up * 0.1f), groundLayer);
        if (onGround || axisH != 0)
        {
            robdy.velocity = new Vector2(speed * axisH, robdy.velocity.y);
        }
        if (onGround && goJump)
        {
            Debug.Log("��Ծ��");
            Vector2 jumpPw = new Vector2(0, jump);
            robdy.AddForce(jumpPw, ForceMode2D.Impulse);
            goJump = false; 
        }
        if (onGround)
        {
            if (axisH == 0)
            {
                nowAnime = stopAnime;
            }
            else
            {
                nowAnime = moveAnime;
            }
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

    public void Jump()
    {
        goJump = true;
        Debug.Log("��������Ծ����");
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Goal")
        {
            Goal();
        }
        else if (collision.gameObject.tag == "Dead")
        {
            GameOver();
        }
        else if (collision.gameObject.tag == "ScoreItem")
        {
            ItemData item = collision.gameObject.GetComponent<ItemData>();
            score = item.value;
            Destroy(collision.gameObject);
        }
    }
    public void Goal()
    {
        animator.Play(goalAnime);
        gameState = "gameclear";
        GameStop();
    }
    public void GameOver()
    {
        animator.Play(deadAnime);
        gameState = "gameover";
        GameStop();
        GetComponent<CapsuleCollider2D>().enabled = false;
        robdy.AddForce(new Vector2(0, 5), ForceMode2D.Impulse);
    }
    void GameStop()
    {
        Rigidbody2D rbody = GetComponent<Rigidbody2D>();
        robdy.velocity = new Vector2(0, 0);
    }
    public void SetAxis(float h, float v)
    {
        axisH = h;
        if (axisH == 0)
        {
            isMoving = false;
        }
        else
        {
            isMoving = true; 
        }
    }
}
