using UnityEngine;
public class GimmickBlock : MonoBehaviour
{
    public float length=1.2f,delay=.8f;public bool isDelete;
    Rigidbody2D rb;SpriteRenderer sr;Vector3 origin;Color color;float timer,fallen;bool counting;
    void Awake(){rb=GetComponent<Rigidbody2D>();sr=GetComponent<SpriteRenderer>();origin=transform.position;color=sr.color;ResetBlock();}
    public void ResetBlock(){transform.position=origin;rb.velocity=Vector2.zero;rb.angularVelocity=0;rb.bodyType=RigidbodyType2D.Static;sr.color=color;timer=fallen=0;counting=false;gameObject.SetActive(true);}
    void Update()
    {
        if(!AdventureFlow.WorldPlaying)return;
        var player=GameObject.FindGameObjectWithTag("Player");
        if(rb.bodyType==RigidbodyType2D.Static && player)
        {
            var collider=player.GetComponent<Collider2D>();
            Vector2 foot=collider?new Vector2(collider.bounds.center.x,collider.bounds.min.y):(Vector2)player.transform.position;
            if(Vector2.Distance(foot,(Vector2)origin+Vector2.up*.6f)<=length)counting=true;
        }
        if(counting && rb.bodyType==RigidbodyType2D.Static){timer+=Time.deltaTime;sr.color=Color.Lerp(color,new Color(1,.62f,.42f),timer/Mathf.Max(.1f,delay));transform.position=origin+Vector3.right*Mathf.Sin(timer*45)*.035f;if(timer>=delay){transform.position=origin;rb.bodyType=RigidbodyType2D.Dynamic;}}
        if(rb.bodyType==RigidbodyType2D.Dynamic){fallen+=Time.deltaTime;if(fallen>2.5f){if(AdventureFlow.Current)gameObject.SetActive(false);else if(isDelete)Destroy(gameObject);}}
    }
}
