using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    public float moveX = 0.0f;
    public float moveY = 0.0f;
    public float times = 0.0f;
    public float weight = 0.0f;
    public bool isMoveWhenOn = false;
    public bool isCanMove = true;
    float perDX;
    float perDY;
    Vector3 defPos;
    bool isReverse = false;
    Transform rider = null;      // 站在方块上的玩家（用位移同步代替父子关系）

    // Start is called before the first frame update
    void Start()
    {
        defPos = transform.position;
        float timestep = Time.fixedDeltaTime;
        perDX = moveX / (1.0f / timestep * times);
        perDY = moveY / (1.0f / timestep * times);
        if (isMoveWhenOn)
        {
            isCanMove = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FixedUpdate()
    {
        Vector3 prevPos = transform.position;

        if (isCanMove)
        {
            float x = transform.position.x;
            float y = transform.position.y;
            bool endX = false;
            bool endY = false;
            if (isReverse)
            {
                if ((perDX >= 0.0f && x <= defPos.x) || (perDX < 0.0f && x >= defPos.x))
                {
                    endX = true;
                }
                if ((perDY >= 0.0f && y <= defPos.y) || (perDY < 0.0f && y >= defPos.y))
                {
                    endY = true;
                }
                transform.Translate(new Vector3(-perDX, -perDY, 0));
            }
            else
            {
                if ((perDX >= 0.0f && x >= defPos.x + moveX) || (perDX < 0.0f && x <= defPos.x + moveX))
                {
                    endX = true;
                }
                if ((perDY >= 0.0f && y >= defPos.y + moveY) || (perDY < 0.0f && y <= defPos.y + moveY))
                {
                    endY = true;
                }
                Vector3 v = new Vector3(perDX, perDY, 0);
                transform.Translate(v);
            }
            if (endX && endY) 
            {
                if (isReverse)
                {
                    transform.position = defPos;
                }
                isReverse = !isReverse;
                isCanMove = false;
                if (isMoveWhenOn == false)
                {
                    Invoke("Move",weight);
                }
            }
        }

        // 把方块这一帧的位移同步给站在上面的玩家
        // （不用 SetParent：避开"激活/停用中不能改父级"的报错，也不会被方块缩放带变形）
        Vector3 delta = transform.position - prevPos;
        if (rider != null && delta != Vector3.zero)
        {
            rider.position += delta;
        }
    }

    public void Move()
    {
        CancelInvoke("Move");     // 清掉排队的自动恢复，避免把启动/停止指令覆盖掉
        isCanMove = true;
    }

    public void Stop()
    {
        CancelInvoke("Move");     // 撤销自动恢复，否则过一会儿方块又自己走
        isCanMove = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            rider = collision.transform;      // 只记录，不 SetParent
            if (isMoveWhenOn)
            {
                isCanMove = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (rider == collision.transform)
            {
                rider = null;                 // 只标记离开
            }
        }
    }
}
